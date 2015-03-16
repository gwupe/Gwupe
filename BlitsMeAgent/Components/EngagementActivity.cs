using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gwupe.Agent.Components
{
    internal abstract class EngagementActivity : EventArgs
    {
        internal String Function { get; set; }
        internal String Activity { get; private set; }
        internal Engagement Engagement { get; private set; }
        internal String From { get; set; }
        internal String To { get; set; }

        internal EngagementActivity(Engagement engagement, String function, String activity)
        {
            Engagement = engagement;
            Activity = activity;
            Function = function;
        }

        public override String ToString()
        {
            return "EngagementActivity { " + Function + "-" + Activity + "}";
        }
    }

}
