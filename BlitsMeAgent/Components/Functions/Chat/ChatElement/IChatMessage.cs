using System;

namespace BlitsMe.Agent.Components.Functions.Chat.ChatElement
{
    public interface IChatMessage
    {
        String Speaker { get; }
        String Message { get; }
        DateTime SpeakTime { get; }
        bool LastWord { get; set; }
        //Engagement Engagement { get; }
    }
}