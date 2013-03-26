using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Diagnostics;

namespace BlitsMe.Service
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        public override void Install(System.Collections.IDictionary stateSaver)
        {
            base.Install(stateSaver);
            ServiceController controller = new ServiceController("BlitsMe");
            try {
            controller.Start();
            }
         catch (Exception ex)
         {
             String source = "BlitsMeService Installer";
             String log = "Application";
             if (!EventLog.SourceExists(source))
             {
                 EventLog.CreateEventSource(source, log);
             }

             EventLog eLog = new EventLog();
             eLog.Source = source;

             eLog.WriteEntry(@"The service could not be started. Please start the service manually. Error: " + ex.Message, EventLogEntryType.Error);

         }
        }
    }
}
