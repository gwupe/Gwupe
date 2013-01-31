using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MutexManager;

namespace BlitsMe.Agent
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // don't want more than 1 started
            if (!SingleInstance.Start()) {
                MessageBox.Show("BlitsMe Already Running", "BlitsMe",
                    MessageBoxButtons.OK);
                return;
            }
            Thread.CurrentThread.Name = "MAIN";
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                var applicationContext = new BlitsMeClientAppContext();
                Application.Run(applicationContext);
            }

            catch (Exception ex)
            {

                MessageBox.Show(ex.Message + ( ex.InnerException != null ? " : " + ex.InnerException.Message : ""), "Program Terminated Unexpectedly",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
