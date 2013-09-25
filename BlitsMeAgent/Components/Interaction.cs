using System;
using System.Collections.Generic;
using BlitsMe.Common.Security;
using log4net;

namespace BlitsMe.Agent.Components
{
    internal class Interaction
    {
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
        private const double InteractionTimeout = 1; // 4 hour interaction timeout

        internal Interaction(String id = null)
        {
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
                    var diff = DateTime.Now - LastActivity;
                    if (diff.TotalMinutes > InteractionTimeout)
                    {
                        Expire();
                        return true;
                    }
                    return false;
                }
                return true;
            }
        }

        internal void Expire()
        {
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
