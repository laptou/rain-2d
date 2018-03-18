using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Win32;

namespace Rain.Native
{
    public class VersionHelper
    {
        private const string CurrentVersionRegistryKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion";

        public static int GetBuildNumber()
        {
            // registry key is REG_SZ
            var build = (string)Registry.GetValue(CurrentVersionRegistryKey, "CurrentBuildNumber", "0");
            return int.Parse(build);
        }

        public static int GetMinorNumber()
        {
            // registry key is DWORD
            return (int)Registry.GetValue(CurrentVersionRegistryKey, "CurrentMinorVersionNumber", "0");
        }

        public static int GetMajorNumber()
        {
            // registry key is DWORD
            return (int)Registry.GetValue(CurrentVersionRegistryKey, "CurrentMajorVersionNumber", "0");
        }

        public static bool RequireVersion(int major, int minor, int build, int servicePack)
        {
            var current = Environment.OSVersion.Version;

            if (current.Major > major) return true;
            if (current.Minor > minor) return true;
            if (current.Build > build) return true;
            if (current.Revision >= servicePack) return true;

            return false;
        }
    }
}
