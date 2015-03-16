using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using Gwupe.Agent.Components;
using Gwupe.Agent.Components.Functions.Chat;
using Gwupe.Agent.Components.Functions.Chat.ChatElement;
using Gwupe.Agent.Components.Person;
using Gwupe.Agent.UI.WPF.Search;
using log4net;
using log4net.Repository.Hierarchy;

namespace Gwupe.Agent.UI.WPF.Engage
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : UserControl
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ChatWindow));
        private readonly GwupeClientAppContext _appContext;
        private readonly EngagementWindow _engagementWindow;
        internal readonly Function Chat;
        private DateTime _lastMessage;

        public ChatWindow(GwupeClientAppContext appContext, EngagementWindow engagementWindow)
        {
            this.InitializeComponent();
            _appContext = appContext;
            _engagementWindow = engagementWindow;
            Chat = _engagementWindow.Engagement.Functions["Chat"] as Function;
            Chat.NewActivity += ChatOnNewMessage;
            ChatPanelViewer.ScrollToBottom();
            DataContext = new ChatWindowDataContext(_appContext, this);

            // need to do this here, because we get weird errors if its part of the data context.
            ChatPanel.ItemsSource = new DispatchingCollection<ObservableCollection<IChatMessage>, IChatMessage>(Chat.Conversation.Exchange, Dispatcher);
        }

        public ChatWindow()
        {

        }

        #region EventHandlers

        private void ChatOnNewMessage(object sender, EngagementActivity e)
        {
            if (!Dispatcher.CheckAccess()) // Only run with dispatcher
            {
                Dispatcher.Invoke(new Action(() => ChatOnNewMessage(sender, e)));
                return;
            }
            // Only run event handler if the dispatcher is not shutting down.
            if (!Dispatcher.HasShutdownStarted)
            {
                ChatPanelViewer.ScrollToBottom();
                if (_lastMessage.Day != DateTime.Now.Day)
                {
                    Logger.Debug("Rolling over into new day, adjusting times in " + _engagementWindow.Engagement.SecondParty.Person.Username);
                    // This is to make sure that all the items 'friendly dates' remain correct on midnight rollover
                    ChatPanel.Items.Refresh();
                }
                _lastMessage = DateTime.Now;

            }
        }

        #endregion

    }

    class ChatWindowDataContext
    {
        private SendMessageCommand _sendMessage;
        private readonly GwupeClientAppContext _appContext;
        private readonly ChatWindow _chatWindow;
        public Person Self { get; private set; }

        public ChatWindowDataContext(GwupeClientAppContext appContext, ChatWindow chatWindow)
        {
            this._appContext = appContext;
            this._chatWindow = chatWindow;
            Self = _appContext.CurrentUserManager.CurrentUser;
        }

        // Command Handler
        public ICommand SendMessage
        {
            get
            {
                return _sendMessage ?? (_sendMessage = new SendMessageCommand(_appContext, _chatWindow.Chat, _chatWindow.messageBox));
            }
        }
    }

    // Command which send messages
    public class SendMessageCommand : ICommand
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SendMessageCommand));
        private readonly GwupeClientAppContext _appContext;
        private readonly Function _chat;
        private readonly TextBox _textBox;

        internal SendMessageCommand(GwupeClientAppContext appContext, Function chat, TextBox textBox)
        {
            this._appContext = appContext;
            this._chat = chat;
            this._textBox = textBox;
        }

        public void Execute(object parameter)
        {
            String message = _textBox.Text.Trim();
            if (message.Length > 0)
            {
                _chat.SendChatMessage(message);
            }
            _textBox.Clear();

        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}