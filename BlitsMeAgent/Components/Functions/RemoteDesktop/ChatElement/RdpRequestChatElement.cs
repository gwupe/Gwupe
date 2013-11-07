using BlitsMe.Agent.Components.Functions.Chat.ChatElement;

namespace BlitsMe.Agent.Components.Functions.RemoteDesktop.ChatElement
{
    public class RdpRequestChatElement : TrueFalseChatElement
    {
        public override string Speaker { get { return "_RDP_REQUEST"; } set { } }
    }
}
