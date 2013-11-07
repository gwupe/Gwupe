using System;
using System.ComponentModel;

namespace BlitsMe.Agent.Components.Functions.Chat.ChatElement
{



    public class SelfChatElement : DeliverableChatElement
    {
        public override string Speaker
        {
            get { return "_SELF"; }
            set {  }
        }


    }
}
