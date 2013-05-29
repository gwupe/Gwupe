using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Common
{
    public class OsUtils
    {
        public static bool IsWinVistaOrHigher()
        {
            OperatingSystem OS = Environment.OSVersion;
            return (OS.Platform == PlatformID.Win32NT) && (OS.Version.Major >= 6);
        }
    }
}
