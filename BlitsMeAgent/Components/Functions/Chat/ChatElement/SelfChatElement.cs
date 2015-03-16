using System;
using System.ComponentModel;

namespace Gwupe.Agent.Components.Functions.Chat.ChatElement
{



    public class SelfChatElement : DeliverableChatElement
    {
        public override string Speaker
        {
            get { return "_SELF"; }
            set {  }
        }
        public byte[] Avatar { get; set; }

    }
}
