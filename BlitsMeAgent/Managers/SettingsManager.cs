using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Communication.P2P.P2P.Tunnel;

namespace BlitsMe.Agent.Managers
{
    internal class SettingsManager
    {
        internal List<SyncType> SyncTypes = new List<SyncType> { SyncType.All };
    }
}
