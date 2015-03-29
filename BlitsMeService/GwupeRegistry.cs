using System;

namespace Gwupe.Service
{
    class GwupeRegistry
    {
        public const string PreReleaseKey = "PreRelease";
        public const String Root = @"SOFTWARE\BlitsMe" + GwupeService.BuildMarker;
        public const String ServerIPsKey = "serverIPs";
        public const String VersionKey = "Version";
        public const String AutoUpgradeKey = "AutoUpgrade";
    }
}
