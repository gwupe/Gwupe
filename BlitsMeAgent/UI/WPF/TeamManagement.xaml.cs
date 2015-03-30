using Gwupe.Agent.UI.WPF.API;

namespace Gwupe.Agent.UI.WPF
{
	/// <summary>
	/// Interaction logic for TeamManagement.xaml
	/// </summary>
	public partial class TeamManagement : IDashboardContentControl, IGwupeUserControl
	{
		public TeamManagement()
		{
			this.InitializeComponent();
		}

	    public void SetAsMain(Dashboard dashboard)
	    {
	        
	    }
	}
}