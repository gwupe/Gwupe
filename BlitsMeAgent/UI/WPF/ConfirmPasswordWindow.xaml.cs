using System.Windows;
using System.Windows.Input;

namespace BlitsMe.Agent.UI.WPF
{
	/// <summary>
	/// Interaction logic for ConfirmPasswordWindow.xaml
	/// </summary>
	public partial class ConfirmPasswordWindow : Window
	{
	    public bool Cancelled = false;
		public ConfirmPasswordWindow()
		{
			this.InitializeComponent();
            Activated += (sender, args) => ConfirmPassword.Focus();
		}

        private void password_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Cancelled = false;
                Close();
            }
        }

		private void ConfirmButtonClick(object sender, System.Windows.RoutedEventArgs e)
		{
		    Cancelled = false;
		    Close();
		}

		private void CancelButtonClick(object sender, System.Windows.RoutedEventArgs e)
		{
		    Cancelled = true;
            Close();
        }
	}
}