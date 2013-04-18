using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Agent.Components.Person.Presence
{
    interface IPresence : IComparable<IPresence>
    {
        String Mode { get; }
        String Type { get; }
        int Priority { get; }
        String Resource { get; }

        Boolean IsOnline { get; }
        Boolean IsPresent { get; }
        String Status { get; }
    }
}
