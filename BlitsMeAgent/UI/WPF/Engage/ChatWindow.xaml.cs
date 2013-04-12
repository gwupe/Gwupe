using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using BlitsMe.Agent.Components.Chat;
using BlitsMe.Agent.Components.Person;
using log4net;

namespace BlitsMe.Agent.UI.WPF.Engage
{
	/// <summary>
	/// Interaction logic for ChatWindow.xaml
	/// </summary>
	public partial class ChatWindow : UserControl
	{
	    private readonly BlitsMeClientAppContext _appContext;
	    private readonly EngagementWindow _engagementWindow;
	    internal readonly Chat _chat;

	    public ChatWindow(BlitsMeClientAppContext appContext, EngagementWindow engagementWindow)
		{
            this.InitializeComponent();
            _appContext = appContext;
            _engagementWindow = engagementWindow;
	        _chat = _engagementWindow.Engagement.Chat;
	        _chat.NewMessage += ChatOnNewMessage;
            ChatPanelViewer.ScrollToBottom();
	        DataContext = new ChatWindowDataContext(_appContext, this);

		}

        #region EventHandlers

        private void ChatOnNewMessage(object sender, ChatEventArgs args)
        {
            // Only run event handler if the dispatcher is not shutting down.
            if (!Dispatcher.HasShutdownStarted)
            {
                if (Dispatcher.CheckAccess())
                {
                    ChatPanelViewer.ScrollToBottom();
                }
                else
                {
                    Dispatcher.Invoke(new Action(() => ChatPanelViewer.ScrollToBottom()));
                }
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
                return  _sendMessage ?? (_sendMessage = new SendMessageCommand(_appContext, _chatWindow._chat, _chatWindow.messageBox));
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
            _chat.SendChatMessage(_textBox.Text);
            _textBox.Clear();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}