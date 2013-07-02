using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using BlitsMe.Agent.Components.Chat;
using BlitsMe.Agent.Components.Person;
using log4net;
using log4net.Repository.Hierarchy;

namespace BlitsMe.Agent.UI.WPF.Engage
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : UserControl
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ChatWindow));
        private readonly BlitsMeClientAppContext _appContext;
        private readonly EngagementWindow _engagementWindow;
        internal readonly Chat _chat;
        private DateTime _lastMessage;

        public ChatWindow(BlitsMeClientAppContext appContext, EngagementWindow engagementWindow)
        {
            this.InitializeComponent();
            _appContext = appContext;
            _engagementWindow = engagementWindow;
            _chat = _engagementWindow.Engagement.Chat;
            _chat.NewMessage += ChatOnNewMessage;
            ChatPanelViewer.ScrollToBottom();
            DataContext = new ChatWindowDataContext(_appContext, this);
            Logger.Debug(ChatPanel.Items.Count + " vs " + _chat.Conversation.Exchange.Count);
        }

        #region EventHandlers

        private void ChatOnNewMessage(object sender, ChatEventArgs args)
        {
            if (!Dispatcher.CheckAccess()) // Only run with dispatcher
            {
                Dispatcher.Invoke(new Action(() => ChatOnNewMessage(sender, args)));
                return;
            }
            // Only run event handler if the dispatcher is not shutting down.
            if (!Dispatcher.HasShutdownStarted)
            {
                ChatPanelViewer.ScrollToBottom();
                if (_lastMessage.Day != DateTime.Now.Day)
                {
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
        private readonly BlitsMeClientAppContext _appContext;
        private readonly ChatWindow _chatWindow;
        public Person Self { get; private set; }
        public DispatchingCollection<ObservableCollection<ChatElement>, ChatElement> Exchange { get; private set; }

        public ChatWindowDataContext(BlitsMeClientAppContext appContext, ChatWindow chatWindow)
        {
            this._appContext = appContext;
            this._chatWindow = chatWindow;
            Self = _appContext.CurrentUserManager.CurrentUser;
            this.Exchange =
                new DispatchingCollection<ObservableCollection<ChatElement>, ChatElement>(
                    _chatWindow._chat.Conversation.Exchange, chatWindow.Dispatcher);
        }

        // Command Handler
        public ICommand SendMessage
        {
            get
            {
                return _sendMessage ?? (_sendMessage = new SendMessageCommand(_appContext, _chatWindow._chat, _chatWindow.messageBox));
            }
        }
    }

    // Command which send messages
    public class SendMessageCommand : ICommand
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SendMessageCommand));
        private readonly BlitsMeClientAppContext _appContext;
        private readonly Chat _chat;
        private readonly TextBox _textBox;

        internal SendMessageCommand(BlitsMeClientAppContext appContext, Chat chat, TextBox textBox)
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