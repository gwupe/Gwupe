using System;
using System.IO;
using BlitsMe.Agent.Managers;
using BlitsMe.Cloud.Communication;

namespace BlitsMe.Agent.Components.RepeatedConnection
{
    internal class WaitingRepeatedConnection
    {
        public string RepeatId;
        public Action<String, CoupledConnection> ConnectionEstablishedCallback;
        public Func<MemoryStream, bool> ReadDataCallback;
    }
}