using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Gwupe.Agent.Components.Person.Presence
{
    public enum PresenceMode { chat, available, away, xa, dnd };
    public enum PresenceType { available, unavailable };

    interface IPresence : IComparable<IPresence>, INotifyPropertyChanged
    {
        PresenceMode Mode { get; }
        PresenceType Type { get; }
        int Priority { get; }
        String Resource { get; }
        String ShortCode { get; }
        ClientInfo ClientInfo { get; }

        Boolean IsOnline { get; }
        Boolean IsPresent { get; }
        String Status { get; }

    }
}
