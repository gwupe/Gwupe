using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Documents;
using Gwupe.Agent.Components.Functions.API;
using Gwupe.Agent.Components.Functions.Chat.ChatElement;
using Gwupe.Agent.Components.Functions.FileSend.ChatElement;
using Gwupe.Agent.Components.Functions.RemoteDesktop.ChatElement;
using Gwupe.Cloud.Messaging.API;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;
using Gwupe.Common.Security;
using Gwupe.Communication.P2P.P2P.Tunnel;
using log4net;
using Timer = System.Timers.Timer;

namespace Gwupe.Agent.Components.Functions.Chat
{
    //internal delegate void ChatEvent(object sender, ChatActivity args);

    internal class Function : FunctionImpl
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Function));

        // Event to be fired if a message is sent or received.
        //internal event ChatEvent NewMessage;
        public override String Name { get { return "Chat"; } }

        // Our app context
        private readonly GwupeClientAppContext _appContext;
        // Our engagement
        private readonly Engagement _engagement;
        // The actual conversation
        public Conversation Conversation;
        // Who we are talking to
        private readonly String _to;
        // The thread id
        private String _threadId;

        // Thread which handles the sending of messages (sending is async)
        private readonly Thread _chatSender;
        private readonly ConcurrentQueue<SelfChatElement> _chatQueue;
        private Dictionary<BlitsMeCommand, Func<List<String>, bool>> BlitsMeCommands;

        internal Function(GwupeClientAppContext appContext, Engagement engagement)
        {
            this._appContext = appContext;
            this._engagement = engagement;
            this._to = engagement.SecondParty.Person.Username;
            SetupCommands();
            Conversation = new Conversation(appContext);
            _chatQueue = new ConcurrentQueue<SelfChatElement>();
            _chatSender = new Thread(ProcessChats) { Name = "ChatSender-" + _to, IsBackground = true };
            _chatSender.Start();

        }

        public override void Close()
        {
            if (_chatSender != null && _chatSender.IsAlive)
            {
                _chatSender.Abort();
            }
        }

        private void ProcessChats()
        {
            while (true)
            {
                while (_chatQueue.Count > 0)
                {
                    SelfChatElement chatElement;
                    if (_chatQueue.TryPeek(out chatElement))
                    {
                        chatElement.DeliveryState = ChatDeliveryState.Trying;
                        var chatMessageRq = new ChatMessageRq()
                            {
                                message = chatElement.Message,
                                username = _to,
                                interactionId = _engagement.Interactions.CurrentOrNewInteraction.Id,
                                chatId = _threadId ?? (_threadId = Util.getSingleton().generateString(6))
                            };
                        try
                        {
                            Response response = _appContext.ConnectionManager.Connection.Request<ChatMessageRq, ChatMessageRs>(chatMessageRq);
                            chatElement.DeliveryState = ChatDeliveryState.Delivered;
                            _chatQueue.TryDequeue(out chatElement);
                        }
                        catch (Exception e)
                        {
                            Logger.Error("Failed to send chat message to " + _to + " : " + e.Message, e);
                            // Set all pending to still trying
                            foreach (SelfChatElement element in _chatQueue.ToArray())
                            {
                                element.DeliveryState = ChatDeliveryState.FailedTrying;
                            }
                            Thread.Sleep(1000);
                        }
                    }
                    else
                    {
                        Logger.Error("Failed to peek into the chat queue, cannot process message");
                        // Failed to dequeue, wait a second
                        Thread.Sleep(1000);
                    }
                }
                lock (_chatQueue)
                {
                    while (_chatQueue.Count == 0)
                    {
                        Monitor.Wait(_chatQueue);
                    }
                }
            }
        }

        internal void ReceiveChatMessage(String message, String chatId, String interactionId, String shortCode, string userName)
        {
            BaseChatElement newMessage = new TargetChatElement()
            {
                Message = message,
                Speaker = _to,
                SpeakTime = DateTime.Now,
                UserName = userName,
                Avatar = _engagement.SecondParty.Person.Avatar,
            };
            Conversation.AddMessage(newMessage);
            if (chatId != null)
            {
                _threadId = chatId;
            }
            // Fire the event
            OnActivate(EventArgs.Empty);
            OnNewActivity(new ChatActivity(_engagement, ChatActivity.CHAT_RECEIVE)
                             {
                                 From = _to,
                                 To = _appContext.CurrentUserManager.CurrentUser.Username,
                                 Message = message
                             });
            OnDeactivate(EventArgs.Empty);
        }

        internal ChatElement.BaseChatElement LogSystemMessage(String message)
        {
            ChatElement.BaseChatElement chatElement = new SystemChatElement()
            {
                Message = message,
                SpeakTime = DateTime.Now
            };
            Conversation.AddMessage(chatElement);
            // Fire the event
            OnNewActivity(new ChatActivity(_engagement, ChatActivity.LOG_SYSTEM)
                {
                    From = "_SYSTEM",
                    To = _appContext.CurrentUserManager.CurrentUser.Username,
                    Message = message
                });
            return chatElement;
        }

        internal ChatElement.BaseChatElement LogSecondPartySystemMessage(String message)
        {
            ChatElement.BaseChatElement chatElement = new TargetSystemChatElement()
            {
                Message = message,
                SpeakTime = DateTime.Now
            };
            Conversation.AddMessage(chatElement);
            // Fire the event
            OnNewActivity(new ChatActivity(_engagement, ChatActivity.LOG_SYSTEM)
            {
                From = "_SECONDPARTYSYSTEM",
                To = _appContext.CurrentUserManager.CurrentUser.Username,
                Message = message
            });
            return chatElement;
        }


        internal void LogServiceCompleteMessage(string message, bool requestFeedback = true)
        {
            if (requestFeedback)
            {
                Conversation.AddMessage(new ServiceCompleteChatElement(_engagement)
                {
                    Message = message,
                    SpeakTime = DateTime.Now
                });
            }
            else
            {
                Conversation.AddMessage(new SystemChatElement
                {
                    Message = message,
                    SpeakTime = DateTime.Now
                });
            }
            // Fire the event
            OnNewActivity(new ChatActivity(_engagement, ChatActivity.LOG_SERVICE)
            {
                From = "_SYSTEM",
                To = _appContext.CurrentUserManager.CurrentUser.Username,
                Message = message
            });
        }

        internal void SendChatMessage(String message)
        {
            if (ParseSystemCommand(message)) return;
            OnActivate(EventArgs.Empty);
            try
            {
                var chatElement = new SelfChatElement()
                {
                    Message = message,
                    SpeakTime = DateTime.Now,
                    Avatar = GwupeClientAppContext.CurrentAppContext.CurrentUserManager.CurrentUser.Avatar,
                };
                lock (_chatQueue)
                {
                    _chatQueue.Enqueue(chatElement);
                    Monitor.PulseAll(_chatQueue);
                }
                Conversation.AddMessage(chatElement);
                // Fire the event
                OnNewActivity(new ChatActivity(_engagement, ChatActivity.CHAT_SEND)
                {
                    From = _appContext.CurrentUserManager.CurrentUser.Username,
                    To = _to,
                    Message = message
                });
            }
            catch (Exception e)
            {
                Logger.Error("Failed to send chat message to " + _to + " : " + e.Message, e);
                this.LogSystemMessage("Message Send Failure");
            }
            finally
            {
                OnDeactivate(EventArgs.Empty);
            }
        }



        public ChatElement.BaseChatElement LogErrorMessage(string message)
        {
            var chatElement = new SystemErrorElement()
            {
                Message = message,
                SpeakTime = DateTime.Now
            };
            Conversation.AddMessage(chatElement);
            // Fire the event
            OnNewActivity(new ChatActivity(_engagement, ChatActivity.LOG_ERROR)
            {
                From = "_SYSTEM_ERROR",
                To = _appContext.CurrentUserManager.CurrentUser.Username,
                Message = message
            });
            return chatElement;
        }

        private bool ParseSystemCommand(String message)
        {
            if (message.StartsWith("/"))
            {
                BlitsMeCommand command;
                String[] commandElements = message.Split(new char[] { ' ' });
                if (commandElements.Length > 0 && BlitsMeCommand.TryParse(commandElements[0].Split(new char[] { '/' })[1], out command))
                {
                    return BlitsMeCommands[command](commandElements.Skip(1).ToList());
                }
                Logger.Warn("Failed to parse " + message + " into a command, probably not one.");
            }
            return false;
        }

        private bool DebugCommand(List<string> list)
        {
            if (list.Count > 0)
            {
                if ("true".Equals(list[0].ToLower()))
                {
                    _appContext.Debug = true;
                    LogSystemMessage("Debugging is now enabled.");
                    return true;
                }
                if ("false".Equals(list[0].ToLower()))
                {
                    _appContext.Debug = false;
                    LogSystemMessage("Debugging is now disabled.");
                    return true;
                }
            }
            LogSystemMessage("Usage: /Debug <true|false>");
            return true;
        }

        private void SetupCommands()
        {
            BlitsMeCommands = new Dictionary<BlitsMeCommand, Func<List<string>, bool>>
            {
                {BlitsMeCommand.Debug, DebugCommand},
                {BlitsMeCommand.P2P, P2PCommand},
                {BlitsMeCommand.ReportFault, ReportFault},
                {BlitsMeCommand.RestartService, RestartService},
            };
        }

        private bool ReportFault(List<string> arg)
        {
            ThreadPool.QueueUserWorkItem(state => GwupeClientAppContext.CurrentAppContext.GenerateFaultReport());
            return true;
        }

        private bool RestartService(List<string> arg)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                if (GwupeClientAppContext.CurrentAppContext.RestartBlitsMeService())
                {
                    LogSystemMessage("BlitsMe Service restarted");
                }
                else
                {
                    LogErrorMessage("BlitsMe Service failed to restart");
                }
            });
            return true;
        }

        private bool P2PCommand(List<string> list)
        {
            if (list.Count > 0)
            {
                List<SyncType> syncTypes = new List<SyncType>();
                foreach (var option in list)
                {
                    SyncType syncType;
                    if (SyncType.TryParse(option, out syncType))
                    {
                        syncTypes.Add(syncType);
                    }
                }
                if (syncTypes.Count > 0)
                {
                    GwupeClientAppContext.CurrentAppContext.SettingsManager.SyncTypes = syncTypes;
                    LogSystemMessage("P2P is now set to " + String.Join(", ", syncTypes));
                    return true;
                }
            }
            LogSystemMessage("Usage: /P2P [<Internal|External|Facilitator|All>]");
            return true;
        }
    }

    internal enum BlitsMeCommand
    {
        Debug,
        P2P,
        ReportFault,
        RestartService
    };

    internal class ChatActivity : EngagementActivity
    {
        internal const String CHAT_SEND = "CHAT_REQUEST";
        internal const String LOG_ERROR = "LOG_ERROR";
        internal const String LOG_SYSTEM = "LOG_SYSTEM";
        internal const String CHAT_RECEIVE = "CHAT_RECEIVE";
        internal const String LOG_SERVICE = "LOG_SERVICE";
        internal const String LOG_RDP_REQUEST = "LOG_RDP_REQUEST";
        internal const String LOG_FILE_SEND_REQUEST = "LOG_FILE_SEND_REQUEST";
        internal String Message;

        internal ChatActivity(Engagement engagement, String activity)
            : base(engagement, "CHAT", activity)
        {
        }
    }

}
