using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace PSUnixUtilCompleters
{
    internal static class UnixHelpers
    {
        private readonly static IReadOnlyList<string> s_nativeUtilDirs = new []
        {
            "/usr/local/sbin",
            "/usr/local/bin",
            "/usr/sbin",
            "/usr/bin",
            "/sbin",
            "/bin"
        };

        private readonly static Lazy<IReadOnlyList<string>> s_nativeUtilNamesLazy = 
            new Lazy<IReadOnlyList<string>>(GetNativeUtilNames);

        internal static IReadOnlyList<string> NativeUtilDirs => s_nativeUtilDirs;

        internal static IReadOnlyList<string> NativeUtilNames => s_nativeUtilNamesLazy.Value;

        private static IReadOnlyList<string> GetNativeUtilNames()
        {
            var commandSet = new HashSet<string>(StringComparer.Ordinal);
            foreach (string utilDir in s_nativeUtilDirs)
            {
                foreach (string utilPath in Directory.GetFiles(utilDir))
                {
                    if (IsExecutable(utilPath))
                    {
                        commandSet.Add(Path.GetFileName(utilPath));
                    }
                }
            }
            var commandList = new List<string>(commandSet);
            return commandList;
        }

        private static bool IsExecutable(string path)
        {
            return access(path, X_OK) != -1;
        }

        private const int X_OK = 0x01;

        [DllImport("libc")]
        private static extern int access(string pathname, int mode);
    }
}