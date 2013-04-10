using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Service
{
    class BLMRegistry
    {
        public const String root = @"SOFTWARE\BlitsMe" + BMService.BuildMarker;
        public const String serverIPsKey = "serverIPs";
        public const String version = "Version";
    }
}
