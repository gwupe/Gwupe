using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Service
{
    class BLMRegistry
    {
        public static string PreReleaseKey = "PreRelease";
        public const String Root = @"SOFTWARE\BlitsMe" + BMService.BuildMarker;
        public const String ServerIPsKey = "serverIPs";
        public const String VersionKey = "Version";
    }
}
