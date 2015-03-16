using System.ComponentModel;
using Gwupe.Agent.Components.Functions.Chat.ChatElement;

namespace Gwupe.Agent.Components.Functions.RemoteDesktop.ChatElement
{
    public class RdpRequestChatElement : TrueFalseChatElement
    {
        public override string Speaker { get { return "_RDP_REQUEST"; } set { } }
    }
}
