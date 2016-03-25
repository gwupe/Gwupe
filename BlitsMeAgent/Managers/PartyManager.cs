using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Gwupe.Agent.Components.Person;
using Gwupe.Cloud.Messaging.Elements;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;
using log4net;
using log4net.Repository.Hierarchy;

namespace Gwupe.Agent.Managers
{
    public class PartyManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (PartyManager));
        public ObservableCollection<Party> Parties { get; private set; }
        private readonly Dictionary<String, Party> _partyLookup;
        private readonly object _partiesLock = new object();

        public PartyManager()
        {
            Parties = new ObservableCollection<Party>();
            _partyLookup = new Dictionary<string, Party>();
        }

        public Party AddUpdatePartyFromElement(PartyElement partyElement)
        {
            //Logger.Debug("Updating party " + partyElement.user + " from partyElement.");
            Party party = null;
            lock (_partiesLock)
            {
                if (!_partyLookup.TryGetValue(partyElement.user, out party))
                {
                    //Logger.Debug("Party " + partyElement.user + " doesn't exist locally, creating.");
                    // create the party
                    if (partyElement is UserElement)
                    {
                        party = new Person(partyElement as UserElement);
                    }
                    else
                    {
                        party = new Team(partyElement as TeamElement);
                    }
                    _partyLookup[party.Username] = party;
                    Parties.Add(party);
                }
                else
                {
                    //Logger.Debug("Party " + partyElement.user + " does exist locally, updating.");
                    // update the party
                    if (partyElement is UserElement)
                    {
                        (party as Person)?.InitPerson(partyElement as UserElement);
                    }
                    else
                    {
                        (party as Team)?.InitTeam(partyElement as TeamElement);
                    }
                }
            }
            return party;
        }

        public Party GetParty(String username, bool live = false)
        {
            // only retrieve locally if its not a live lookup
            if (!live)
            {
                lock (_partiesLock)
                {
                    if (_partyLookup.ContainsKey(username))
                    {
                        return _partyLookup[username];
                    }
                }
            }
            var response = GwupeClientAppContext.CurrentAppContext.ConnectionManager.Connection.Request<VCardRq, VCardRs>(new VCardRq(username));
            // Make sure we update the relationship
            GwupeClientAppContext.CurrentAppContext.RelationshipManager.AddUpdateRelationship(username, new Relationship(response.relationshipElement));
            lock (_partiesLock)
            {
                AddUpdatePartyFromElement(response.PartyElement);
                return _partyLookup[username];
            }
        }

    }
}