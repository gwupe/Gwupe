using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Gwupe.Agent.Components.Person;
using Gwupe.Cloud.Messaging.Elements;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;
using log4net;

namespace Gwupe.Agent.Managers
{
    public class RelationshipManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (RelationshipManager));
        public ObservableCollection<Relationship> Relationships;
        private readonly Dictionary<String, Relationship> _userRelationshipLookup;
        private readonly object _relationshipLock = new object();

        public RelationshipManager()
        {
            Relationships = new ObservableCollection<Relationship>();
            _userRelationshipLookup = new Dictionary<string, Relationship>();
        }

        public void AddUpdateRelationship(String username, Relationship relationship)
        {
            lock (_relationshipLock)
            {
                if (_userRelationshipLookup.ContainsKey(username))
                {
                    Relationships.Remove(_userRelationshipLookup[username]);
                    _userRelationshipLookup[username] = relationship;
                    Relationships.Add(relationship);
                }
                else
                {
                    _userRelationshipLookup.Add(username,relationship);
                    Relationships.Add(relationship);
                }
            }
        }

        public Relationship GetRelationship(String username)
        {
            Relationship relationship = null;
            lock (_relationshipLock)
            {
                if (!_userRelationshipLookup.TryGetValue(username, out relationship))
                {
                    relationship = Relationship.NoRelationship;
                }
            }
            return relationship;
        }

        internal void UpdateRelationship(String contactUsername, Relationship relationship, String tokenId, String securityKey)
        {
            var response = GwupeClientAppContext.CurrentAppContext.ConnectionManager.Connection
                .Request<UpdateRelationshipRq, UpdateRelationshipRs>(
                    new UpdateRelationshipRq()
                    {
                        relationshipElement = new RelationshipElement()
                        {
                            theyHaveUnattendedAccess = relationship.TheyHaveUnattendedAccess,
                            ihaveUnattendedAccess = relationship.IHaveUnattendedAccess
                        },
                        contactUsername = contactUsername,
                        tokenId = tokenId,
                        securityKey = securityKey
                    });
            AddUpdateRelationship(contactUsername,new Relationship(response.relationshipElement));
        }
    }
}