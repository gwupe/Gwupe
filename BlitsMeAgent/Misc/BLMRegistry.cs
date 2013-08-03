using System;
using System.Collections.Generic;
using BlitsMe.Common.Security;
using Microsoft.Win32;
using log4net;

namespace BlitsMe.Agent.Misc
{
    public class BLMRegistry
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(BLMRegistry));
        public const String root = @"SOFTWARE\BlitsMe" + Program.BuildMarker;
        public const String serverIPsKey = "serverIPs";
        public const String usernameKey = "username";
        public const String profileKey = "profile";
        public const String passwordHashKey = "password";
        public const String lastVersionKey = "lastVersion";

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
                logger.Error("Failed to get registry value for " + regKey + " from registry [" + root + "] : " + e.Message);
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
                logger.Error("Failed to set registry value [" + regValue + "] for " + regKey + " from registry [" + root + "] : " + e.Message);
            }
        }
        /*
                private String generateProfile() {
                    byte[] data = new byte[128];
                    System.Security.Principal.WindowsIdentity.GetCurrent().User.AccountDomainSid.GetBinaryForm(data,0);
                    MD5 md5 = new MD5CryptoServiceProvider();
                    byte[] hashArray = md5.ComputeHash(data);
                    StringBuilder hash = new StringBuilder();
                    for (int i = 0; i < hashArray.Length; i++)
                    {
                        hash.Append(hashArray[i].ToString("X2"));
                    }
                    String profile = hash.ToString();
                    setRegValue(profileKey, profile);
                    return profile;
                }
        
                private String generateWorkstation()
                {
                    return FingerPrint.Value();
                }
                */
    }
}
