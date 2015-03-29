using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Gwupe.Common.Security;
using log4net.Repository.Hierarchy;
using Microsoft.Win32;
using log4net;

namespace Gwupe.Agent.Misc
{
    public class GwupeUserRegistry
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GwupeUserRegistry));
        public const String root = @"SOFTWARE\BlitsMe" + Program.BuildMarker;
        public const String serverIPsKey = "serverIPs";
        public const String usernameKey = "username";
        public const String profileKey = "profile";
        public const String passwordHashKey = "password";
        public const String lastVersionKey = "lastVersion";
        public const String loginAsGuestKey = "loginAsGuest";
        public const String experimentalKey = "experimental";
        public const String preReleaseKey = "PreRelease";
        public const String autoUpgradeKey = "AutoUpgrade";

        public string Username {
            get { return getRegValue(usernameKey); }
            set { setRegValue(usernameKey, value); }
        }
        public string PasswordHash {
            get { return getRegValue(passwordHashKey); }
            set { setRegValue(passwordHashKey, value); }
        }
        public string Profile {
            get { return getRegValue(profileKey); }
            set { setRegValue(profileKey, value); }
        }

        public string LastVersion
        {
            get { return getRegValue(lastVersionKey); }
            set { setRegValue(lastVersionKey, value);}
        }

        public bool LoginAsGuest
        {
            get
            { return "yes".Equals(getRegValue(loginAsGuestKey)); }
            set { setRegValue(loginAsGuestKey, value ? "yes" : "no" );}
        }

        public List<String> getServerIPs()
        {
            String serverString = getRegValue(serverIPsKey);
            return (serverString == null || serverString.Equals("")) ? null : new List<String>(serverString.Split(','));
        }

        public void saveServerIPs(List<String> newIPs)
        {
            setRegValue(serverIPsKey, String.Join(",", newIPs.ToArray()));
        }

        public string getRegValue(String regKey, bool hklm = false) {
            try
            {
                RegistryKey bmKey = hklm ? RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(root) : 
                    RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32).OpenSubKey(root);
                String regValue = (String)bmKey.GetValue(regKey);
                return regValue;
            }
            catch (Exception e)
            {
                Logger.Error("Failed to get registry value for " + regKey + " from registry [" + root + "] : " + e.Message);
            }
            return null;
        }

        private void setRegValue(string regKey, string regValue) {
            try {
                RegistryKey bmKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32).CreateSubKey(root, RegistryKeyPermissionCheck.ReadWriteSubTree);
                bmKey.SetValue(regKey, regValue);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to set registry value [" + regValue + "] for " + regKey + " from registry [" + root + "] : " + e.Message);
            }
        }

        public bool PreRelease
        {
            get
            {
                try
                {
                    String value = getRegValue(preReleaseKey,true);
                    if (value != null && value.ToLower().Equals("yes"))
                    {
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to get prerelease key value from registry", e);
                }
                return false;
            }
        }

        public bool AutoUpgrade
        {
            get
            {
                try
                {
                    String value = getRegValue(autoUpgradeKey, true);
                    if (value != null && value.ToLower().Equals("no"))
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to get prerelease key value from registry", e);
                }
                return true;
            }
        }

        public bool Experimental
        {
            get {
                try
                {
                    String value = getRegValue(experimentalKey);
                    if (value != null && value.ToLower().Equals("yes"))
                    {
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to get experimental key value from registry",e);
                }
                return false;
            }
            set
            {
                setRegValue(experimentalKey,value ? "yes" : "no");
            }
        }
    }
}
