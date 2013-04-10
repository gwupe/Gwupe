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

        Boolean IsOnline { get; }
        Boolean IsAvailable { get; }
        String Status { get; }
    }
}
