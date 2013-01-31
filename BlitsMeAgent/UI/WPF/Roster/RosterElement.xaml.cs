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
	public partial class RosterElement : UserControl
	{
        public Person Person { get; set; }

        public BitmapImage AvatarSource
        {
            get
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = new MemoryStream(Person.Avatar);
                image.EndInit();
                return image;

            }
        }

        public int MissingStarCalc
        {
            get
            {
                return 75 - (Person.Rating * 75 / 100);
            }
        }

		public RosterElement(Person person)
		{
			this.InitializeComponent();
		    this.Person = person;
		}

	}
}