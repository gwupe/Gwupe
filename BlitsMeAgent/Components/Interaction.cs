using System;
using System.Collections.Generic;
using BlitsMe.Common.Security;
using log4net;

namespace BlitsMe.Agent.Components
{
    internal class Interaction
    {
        private readonly Engagement _engagement;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Interaction));
        private bool _expired;

        internal String Id
        {
            get
            {
                return _id;
            }
            set { _id = value; }
        }

        internal DateTime LastActivity { get; private set; }
        internal Dictionary<String, Rating> Ratings;
        internal List<EngagementActivity> Activities = new List<EngagementActivity>();
        private string _id;
#if DEBUG
        private const double InteractionTimeout = 30; // 1/2 hour interaction timeout
#else
        private const double InteractionTimeout = 240; // 4 hour interaction timeout
#endif

        internal Interaction(Engagement engagement, String id = null)
        {
            _engagement = engagement;
            _id = String.IsNullOrEmpty(id) ? Util.getSingleton().generateString(6) : id;
            Logger.Debug("Starting Interaction " + _id);
            LastActivity = DateTime.Now;
            _expired = false;
        }

        internal bool Expired
        {
            get
            {
                if (!_expired)
                {
                    // Only expires if there is nothing unread, otherwise interaction stays open
                    if (!_engagement.IsUnread)
                    {
                        var diff = DateTime.Now - LastActivity;
                        if (diff.TotalMinutes > InteractionTimeout)
                        {
                            Expire();
                            return true;
                        }
                    }
                    return false;
                }
                return true;
            }
        }

        internal void Expire()
        {
            Logger.Debug("Interaction " + _id + " with " + _engagement.SecondParty.Person.Username + " expired");
            _expired = true;
        }

        internal void AddRating(String ratingName, int rating)
        {
            if (Ratings.ContainsKey(ratingName))
            {
                Ratings[ratingName].Value = rating;
            }
            else
            {
                Ratings.Add(ratingName, new Rating(ratingName) { Value = rating });
            }
        }

        internal void RecordActivity(EngagementActivity activity)
        {
            Activities.Add(activity);
            LastActivity = DateTime.Now;
            Logger.Debug("[" + Id + "] Recorded activity " + activity);
        }
    }
}
