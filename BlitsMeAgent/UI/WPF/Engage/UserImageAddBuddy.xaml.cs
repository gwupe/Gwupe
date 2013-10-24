using BlitsMe.Agent.Components.Person;
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

namespace BlitsMe.Agent.UI.WPF.Engage
{
    /// <summary>
    /// Interaction logic for UserImageAddBuddy.xaml
    /// </summary>
    public partial class UserImageAddBuddy : UserControl
    {
        public UserImageAddBuddy()
        {
            InitializeComponent();
            BlitsMeClientAppContext appContext = BlitsMeClientAppContext.CurrentAppContext.UIManager.GetAppcontext();
            //DataContext = SearchResult;
            //new AddPerson(appContext, SearchResult.Person);
        }
    }

    // Command which send messages
    public class AddPerson : ICommand
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AddPerson));
        private readonly BlitsMeClientAppContext _appContext;
        private readonly Person _person;

        internal AddPerson(BlitsMeClientAppContext appContext, Person person)
        {
            _appContext = appContext;
            _person = person;
        }

        public void Execute(object parameter)
        {
            _appContext.RosterManager.AddPerson(_person);
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
