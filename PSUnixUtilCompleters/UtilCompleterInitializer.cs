using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Runtime.InteropServices;

namespace PSUnixUtilCompleters
{
    public class UtilCompleterInitializer : IModuleAssemblyInitializer
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

        public void OnImport()
        {
        }

        public string[] GetNativeUtilNames()
        {
            var commands = new List<string>();
            foreach (string utilDir in s_nativeUtilDirs)
            {
                foreach (string utilPath in Directory.GetFiles(utilDir))
                {
                    if (IsExecutable(utilPath))
                    {
                        commands.Add(Path.GetFileName(utilPath));
                    }
                }
            }
            return commands.ToArray();
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
