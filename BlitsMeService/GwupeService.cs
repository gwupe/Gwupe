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
using Gwupe.Common.Security;
using Gwupe.Service.ServiceHost;
using log4net;
using log4net.Config;
using Microsoft.Win32;

namespace Gwupe.Service
{
    public partial class GwupeService : ServiceBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GwupeService));
        private WebClient _webClient;
        private Timer _updateCheck;
#if DEBUG
        private const String UpdateServer = "dev.gwupe.com";
        private const int UpdateCheckInterval = 300;
        public const String BuildMarker = "_Dev";
#else
        private const String UpdateServer = "gwupe.com";
        private const int UpdateCheckInterval = 3600;
        public const String BuildMarker = "";
#endif
        // FIXME: Move this to a global config file at some point
        private const string VncServiceName = "GwupeSupportService" + BuildMarker;
        private const int VncServiceTimeoutMs = 30000;
        private String _version;
        private X509Certificate2 _cacert;
        private bool _checkingUpdate;

        public List<String> Servers;
        private System.ServiceModel.ServiceHost _serviceHost;
        public GwupeService()
        {
            InitializeComponent();
            if (!EventLog.SourceExists("GwupeService"))
                EventLog.CreateEventSource("GwupeService", "Application");
            EventLog.WriteEntry("GwupeService", "Initialised the GwupeService" + BuildMarker);
        }

        protected override void OnStart(string[] args)
        {
            EventLog.WriteEntry("GwupeService", "Starting GwupeService" + BuildMarker);
            XmlConfigurator.Configure(Assembly.GetExecutingAssembly().GetManifestResourceStream("Gwupe.Service.log4net.xml"));
            _version = Regex.Replace(FileVersionInfo.GetVersionInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                                                   "/Gwupe.Agent.exe").FileVersion, "\\.[0-9]+$", "");
            Logger.Info("Gwupe Service Starting Up [" + Environment.UserName + ", " + _version + "]");
            SaveVersion();
#if DEBUG
            foreach (var manifestResourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                Logger.Debug("Embedded Resource : " + manifestResourceName);
            }
#endif
            // Check for update on startup

            // check for updates every interval
            _updateCheck = new Timer(UpdateCheckInterval * 1000);
            _updateCheck.Elapsed += delegate { CheckForNewVersion(); };
            _updateCheck.Start();
            initServers();
            _serviceHost = new System.ServiceModel.ServiceHost(new ServiceHost.GwupeService(this), new Uri("net.pipe://localhost/GwupeService" + BuildMarker));
            _serviceHost.Open();
        }

        private void CheckForNewVersion()
        {
            if (_checkingUpdate) return;
            _checkingUpdate = true;
            if (_webClient == null)
            {
                _webClient = new WebClient();
                try
                {
                    var stream =
                        Assembly.GetExecutingAssembly().GetManifestResourceStream("Gwupe.Service.cacert.pem");
                    Byte[] certificateData = new Byte[stream.Length];
                    stream.Read(certificateData, 0, certificateData.Length);
                    _cacert = new X509Certificate2(certificateData);
                    Logger.Info("Will use certificate from CA " + _cacert.GetNameInfo(X509NameType.SimpleName, true));
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
                var downloadUrl = GetUpdateUrl(assemblyVersion);
                Logger.Debug("Checking for new version from " + downloadUrl);
                String versionInfomation =
                    _webClient.DownloadString(downloadUrl);
                if (Regex.Match(versionInfomation, "^[0-9]+\\.[0-9]+\\.[0-9]+:(BlitsMe|Gwupe)SetupFull" + BuildMarker + ".*").Success)
                {
                    String[] versionParts = versionInfomation.Split('\n')[0].Split(':');
                    Version updateVersion = new Version(versionParts[0]);
                    if (assemblyVersion.CompareTo(updateVersion) < 0)
                    {
                        Logger.Debug((IsPreRelease() ? "PreRelease " : "") + "Upgrade Available : " + assemblyVersion + " => " + updateVersion);
                        if (IsAutoUpgrade())
                        {
                            try
                            {
                                Logger.Info("Downloading update " + versionParts[1]);
                                String fileLocation = Path.GetTempPath() + versionParts[1];
                                _webClient.DownloadFile(GetDownloadUrl(versionParts[1]),
                                    fileLocation);
                                Logger.Info("Downloaded update " + versionParts[1]);
                                String logfile = Path.GetTempPath() + "GwupeInstall" + BuildMarker + ".log";
                                Logger.Info("Executing " + fileLocation + ", log file is " + logfile);
                                if (Regex.Match(fileLocation, ".*.msi$").Success)
                                {
                                    Process.Start(fileLocation, "/qn /lvx " + logfile);
                                }
                                else
                                {
                                    Process.Start(fileLocation,
                                        " /silent /passive /install /quiet /norestart /log " + logfile);
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.Error("Failed to download update : " + e.Message, e);
                            }
                        }
                        else
                        {
                            Logger.Warn("AutoUpgrade is disabled, will not attempt to upgrade");
                        }
                    }
                    else
                    {
                        Logger.Debug("No update available, current version " + assemblyVersion +
                                     ", available version " +
                                     updateVersion + ", checking again in " + (UpdateCheckInterval / 60) + " minutes.");
                    }
                }
                else
                {
                    Logger.Error("Failed to check for updates, the update request return invalid information : " +
                                 versionInfomation);
                }
            }
            catch (Exception e)
            {
                Logger.Warn("Failed to check for update : " + e.Message, e);
            }
            finally
            {
                Logger.Debug("Upgrade check complete.");
                ServicePointManager.ServerCertificateValidationCallback -= ValidateServerWithCA;
                _checkingUpdate = false;
            }
        }

        private string GetDownloadUrl(string filename)
        {
            return "https://" + UpdateServer + "/updates/" + (IsPreRelease() ? "prerelease/" : "") + filename;
        }

        private string GetUpdateUrl(Version assemblyVersion)
        {

            return "https://" + UpdateServer + "/updates/update.php" +
                "?ver=" + assemblyVersion +
                "&hwid=" + HardwareFingerprint() +
                "&upver=2.0" +
                (IsPreRelease() ? "&prerelease=true" : "");
        }

        private bool IsPreRelease()
        {
            try
            {
                RegistryKey bmKey =
                    RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(
                        GwupeRegistry.Root);
                String preRelease = (String)bmKey.GetValue(GwupeRegistry.PreReleaseKey);
                if (preRelease != null)
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.Debug("Threw exception trying to get preReleaseKey from Registry : " + e.Message);
                return false;
            }
            return false;
        }

        private bool IsAutoUpgrade()
        {
            // This defaults to yes, only if the key is found and its value is no does an auto upgrade not run
            try
            {
                RegistryKey bmKey =
                    RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(
                        GwupeRegistry.Root);
                String autoUpgrade = (String)bmKey.GetValue(GwupeRegistry.AutoUpgradeKey);
                Logger.Debug("AutoUpgrade is " + autoUpgrade);
                if (autoUpgrade != null && autoUpgrade.Equals("no"))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Logger.Debug("Threw exception trying to get preReleaseKey from Registry : " + e.Message);
            }
            return true;
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
            RegistryKey bmKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(GwupeRegistry.Root);
            String ipKey = (String)bmKey.GetValue(GwupeRegistry.ServerIPsKey);
            return new List<String>(ipKey.Split(','));
        }

        private void SaveVersion()
        {
            try
            {
                RegistryKey root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).CreateSubKey(GwupeRegistry.Root, RegistryKeyPermissionCheck.ReadWriteSubTree);
                root.SetValue(GwupeRegistry.VersionKey, _version);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to save version to registry : " + e.Message, e);
            }
        }

        public String HardwareFingerprint()
        {
            Logger.Debug("Got HardwareDesc : " + FingerPrint.HardwareDescription());
            Logger.Debug("Got fingerprint : " + FingerPrint.Value());
            return FingerPrint.Value();
        }

        public void Ping()
        {

        }

        public void saveServerIPs(List<String> newIPs)
        {
            // Lets add some
            try
            {
                RegistryKey ips = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).CreateSubKey(GwupeRegistry.Root, RegistryKeyPermissionCheck.ReadWriteSubTree);
                ips.SetValue(GwupeRegistry.ServerIPsKey, String.Join(",", newIPs.ToArray()));
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
            Logger.Info("Gwupe Service Shutting Down");
        }

    }
}
