using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Agent.Components.Activity
{
    class InteractionActivity : EngagementActivity
    {
        internal const String READ = "READ";
        internal String Message;

        internal InteractionActivity(Engagement engagement, String activity)
            : base(engagement, "INTERACTION", activity)
        {
        }
    }
}
