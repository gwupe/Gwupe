using System;
using System.IO;
using Gwupe.Agent.Managers;
using Gwupe.Cloud.Communication;

namespace Gwupe.Agent.Components.RepeatedConnection
{
    internal class WaitingRepeatedConnection
    {
        public string RepeatId;
        public Action<String, CoupledConnection> ConnectionEstablishedCallback;
        public Func<MemoryStream, bool> ReadDataCallback;
    }
}