using Gwupe.Agent.Components;
using Gwupe.Agent.Components.Functions.Chat;
using Gwupe.Agent.Components.Person;
using Gwupe.Agent.Components.Search;
using Gwupe.Agent.UI.WPF.Search;
using Gwupe.Cloud.Messaging.Elements;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Gwupe.Agent.UI.WPF.Engage
{
    /// <summary>
    /// Interaction logic for UserImageOthers.xaml
    /// </summary>
    public partial class UserImageOthers : UserControl
    {
        public UserImageOthers()
        {
            InitializeComponent();
            GwupeClientAppContext appContext = GwupeClientAppContext.CurrentAppContext.UIManager.GetAppcontext();
            Engagement engagement = null;//GwupeClientAppContext.CurrentAppContext.UIManager.GetSourceObject();
            DataContext = new ChatWindowDataContext(appContext,engagement);
        }


        class ChatWindowDataContext
        {
            private SendMessageCommand _sendMessage;
            private readonly GwupeClientAppContext _appContext;
            //private readonly ChatWindow _chatWindow;
            public Party Self { get; private set; }
            //public DispatchingCollection<ObservableCollection<ChatElement>, ChatElement> Exchange { get; private set; }

            public ChatWindowDataContext(GwupeClientAppContext appContext, Engagement engagement)
            {
                this._appContext = appContext;
                //this._chatWindow = chatWindow;
                Self = engagement.SecondParty.Party;
                //  this.Exchange =
                //    new DispatchingCollection<ObservableCollection<ChatElement>, ChatElement>(
                //      _chatWindow._chat.Conversation.Exchange, chatWindow.Dispatcher);
            }

            // Command Handler
            //public ICommand SendMessage
            //{
            //    get
            //    {
            //        return _sendMessage ?? (_sendMessage = new SendMessageCommand(_appContext, _chatWindow.Chat, _chatWindow.messageBox));
            //    }
            //}
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
}
