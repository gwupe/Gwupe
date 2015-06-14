using System;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using log4net;
using log4net.Config;

namespace Gwupe.Sandbox
{
    class Class1 : ApplicationContext
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Class1));
        private String path = @"e:\infile";
        private String path2 = @"e:\tmp\outfile";

        public Class1()
        {
            XmlConfigurator.Configure(Assembly.GetExecutingAssembly().GetManifestResourceStream("Gwupe.Sandbox.log4net.xml"));
            UdpClient client = new UdpClient();
            client.Connect("10.168.1.28",51040);
            for (int i = 0; i < 10; i++)
            {
                client.Send(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}, 10);
                Console.WriteLine("Sent " + i);
                Thread.Sleep(1000);
            }
        }

    }
}
