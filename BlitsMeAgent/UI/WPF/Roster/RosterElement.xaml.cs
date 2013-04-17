using System.ComponentModel;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using BlitsMe.Agent.Components.Person;
using BlitsMe.Agent.UI.WPF.Engage;

namespace BlitsMe.Agent.UI.WPF.Roster
{
	/// <summary>
	/// Interaction logic for RosterElement.xaml
	/// </summary>
	public partial class RosterElement : UserControl, INotifyPropertyChanged
	{
	    private bool _isActive;
	    private Person _person;
	    public Person Person
	    {
	        get { return _person; }
            set { _person = value; OnPropertyChanged(new PropertyChangedEventArgs("Person")); }
	    }

        private string ToolTip { get; set; }

	    public bool IsActive
	    {
	        get { return _isActive; }
	        set { _isActive = value; OnPropertyChanged(new PropertyChangedEventArgs("IsActive")); }
	    }

	    public RosterElement(Person person)
		{
			InitializeComponent();
		    Person = person;
	        ToolTip = "Chat with " + person.Firstname;
            IsActive = false;
        }

	    public event PropertyChangedEventHandler PropertyChanged;

	    public void OnPropertyChanged(PropertyChangedEventArgs e)
	    {
	        PropertyChangedEventHandler handler = PropertyChanged;
	        if (handler != null) handler(this, e);
	    }
	}
}