using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Timers;
using Microsoft.Win32;
using BlitsMe.Service.ServiceHost;
using log4net;
using log4net.Config;

namespace BlitsMe.Service
{
    public partial class BMService : ServiceBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BMService));
        private WebClient _webClient;
        private readonly Timer _updateCheck;
#if DEBUG
        private const String UpdateServer = "s1.i.dev.blits.me";
        private const int UpdateCheckInterval = 120;
        public const String BuildMarker = "_Dev";
#else
        private const String UpdateServer = "s1.i.blits.me";
        private const int UpdateCheckInterval = 3600;
        public const String BuildMarker = "";
#endif
        // FIXME: Move this to a global config file at some point
        private const string VncServiceName = "BlitsMeSupportService" + BuildMarker;
        private const int VncServiceTimeoutMs = 30000;
        private readonly String _version;
        private X509Certificate2 _cacert;

        public List<String> Servers;
        private System.ServiceModel.ServiceHost _serviceHost;
        public BMService()
        {
            InitializeComponent();
            XmlConfigurator.Configure(Assembly.GetExecutingAssembly().GetManifestResourceStream("BlitsMe.Service.log4net.xml"));
            _version = Regex.Replace(FileVersionInfo.GetVersionInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                                                   "/BlitsMe.Agent.exe").FileVersion, "\\.[0-9]+$", "");
            Logger.Info("BlitsMeService Starting Up [" + System.Environment.UserName + ", " + _version + "]");
            SaveVersion();
#if DEBUG
            foreach (var manifestResourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                Logger.Debug("Embedded Resource : " + manifestResourceName);
            }
#endif
            // Check for update on startup

            // Don't do this hear, it could hold up starting of the service and thats tre bad
            //CheckForNewVersion();
            // check for updates every interval
            _updateCheck = new Timer(UpdateCheckInterval * 1000);
            _updateCheck.Elapsed += delegate { CheckForNewVersion(); };
            _updateCheck.Start();
        }

        private void CheckForNewVersion()
        {
            if (_webClient == null)
            {
                _webClient = new WebClient();
                try
                {
                    var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BlitsMe.Service.cacert.pem");
                    Byte[] certificateData = new Byte[stream.Length];
                    stream.Read(certificateData, 0, certificateData.Length);
                    _cacert = new X509Certificate2(certificateData);
                    Logger.Info("Will use certificate from CA " + _cacert.GetNameInfo(X509NameType.SimpleName, true) +
                                ", verified? " + _cacert.Verify());
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to get the certificate : " + e.Message, e);
                }
            }

            ServicePointManager.ServerCertificateValidationCallback += ValidateServerWithCA;
            try
            {
                Version assemblyVersion = new Version(_version);
                String versionInfomation = _webClient.DownloadString("https://" + UpdateServer + "/updates/update.php?ver=" + assemblyVersion);
                if (Regex.Match(versionInfomation, "^[0-9]+\\.[0-9]+\\.[0-9]+:BlitsMeSetup.*").Success)
                {
                    String[] versionParts = versionInfomation.Split('\n')[0].Split(':');
                    Version updateVersion = new Version(versionParts[0]);
                    if (assemblyVersion.CompareTo(updateVersion) < 0)
                    {
                        Logger.Debug("Upgrade Available : " + assemblyVersion + " => " + updateVersion);
                        try
                        {

                            Logger.Info("Downloading update " + versionParts[1]);
                            String fileLocation = Path.GetTempPath() + versionParts[1];
                            _webClient.DownloadFile("https://" + UpdateServer + "/updates/" + versionParts[1],
                                                    fileLocation);
                            Logger.Info("Downloaded update " + versionParts[1]);
                            String logfile = Path.GetTempPath() + "BlitsMeInstall.log";
                            Logger.Info("Executing " + fileLocation + ", log file is " + logfile);
                            Process.Start(fileLocation, "/qn /lvx " + logfile);
                        }
                        catch (Exception e)
                        {
                            Logger.Error("Failed to download update : " + e.Message, e);
                        }
                    }
                    else
                    {
                        Logger.Debug("No update available, current version " + assemblyVersion + ", available version " +
                                     updateVersion + ", checking again in " + (UpdateCheckInterval / 60) + " minutes.");
                    }
                }
                else
                {
                    Logger.Error("Failed to check for updates, the update request return invalid information : " + versionInfomation);
                }
            }
            catch (Exception e)
            {
                Logger.Warn("Failed to check for update : " + e.Message, e);
            }
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback -= ValidateServerWithCA;
            }
        }

        private bool ValidateServerWithCA(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            bool isValid = false;
            if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors)
            {
                X509Chain chain0 = new X509Chain();
                chain0.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                // add all your extra certificate chain
                chain0.ChainPolicy.ExtraStore.Add(new X509Certificate2(_cacert));
                chain0.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                isValid = chain0.Build((X509Certificate2)certificate);
            }
            return isValid;
        }

        protected override void OnStart(string[] args)
        {
            initServers();
            // do we need this connection?
            //connection = new CloudConnection(servers);
            _serviceHost = new System.ServiceModel.ServiceHost(new BlitsMeService(this), new Uri("net.pipe://localhost/BlitsMeService" + BuildMarker));
            _serviceHost.Open();
        }

        private void initServers()
        {
            if (Servers == null || Servers.Count == 0)
            {
                try
                {
                    Servers = getServerIPs();
                    saveServerIPs(Servers);
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to get the server IP's : " + e.Message, e);
                }
            }
        }

        public List<String> getServerIPs()
        {
            RegistryKey bmKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(BLMRegistry.root);
            String ipKey = (String)bmKey.GetValue(BLMRegistry.serverIPsKey);
            return new List<String>(ipKey.Split(','));
        }

        private void SaveVersion()
        {
            try
            {
                RegistryKey root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).CreateSubKey(BLMRegistry.root, RegistryKeyPermissionCheck.ReadWriteSubTree);
                root.SetValue(BLMRegistry.version, _version);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to save version to registry : " + e.Message, e);
            }
        }

        public void Ping()
        {
            
        }

        public void saveServerIPs(List<String> newIPs)
        {
            // Lets add some
            try
            {
                RegistryKey ips = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).CreateSubKey(BLMRegistry.root, RegistryKeyPermissionCheck.ReadWriteSubTree);
                ips.SetValue(BLMRegistry.serverIPsKey, String.Join(",", newIPs.ToArray()));
            }
            catch (Exception e2)
            {
                // TODO log something to event log
                Logger.Error("Failed to determine server IP's from the registry [" + e2.GetType() + "] : " + e2.Message, e2);
            }
        }

        public bool VNCStartService()
        {
            ServiceController service = new ServiceController(VncServiceName);

            try
            {
                if (service.Status != ServiceControllerStatus.Running)
                {
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(VncServiceTimeoutMs));
                }
            }
            catch (System.ServiceProcess.TimeoutException e)
            {
                Logger.Error("VNCServer service failed to start in a reasonable time : " + e.Message, e);
                return false;
            }
            catch (Exception e)
            {
                Logger.Error("VNCServer service failed to start : " + e.Message, e);
                return false;
            }

            return true;
        }


        protected override void OnStop()
        {
            _serviceHost.Close();
            Logger.Info("BlitsMe Service Shutting Down");
        }

    }
}
