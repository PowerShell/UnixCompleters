using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace PSUnixUtilCompleters
{
    public class UtilCompleterInitializer : IModuleAssemblyInitializer
    {
        private readonly static PropertyInfo s_executionContext = typeof(Runspace).GetProperty("ExecutionContext", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly static PropertyInfo s_nativeArgumentCompleters = s_executionContext.PropertyType.GetProperty("NativeArgumentCompleters", BindingFlags.NonPublic | BindingFlags.Instance);

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
            string[] utilNames = GetNativeUtilNames();
            RegisterCompletersForCommands(utilNames);
        }

        public void RegisterCompletersForCommands(IEnumerable<string> commands)
        {
                object executionContext = s_executionContext.GetValue(Runspace.DefaultRunspace);

                var nativeArgumentCompleters = (Dictionary<string, ScriptBlock>)s_nativeArgumentCompleters.GetValue(executionContext);
                if (nativeArgumentCompleters == null)
                {
                    s_nativeArgumentCompleters.SetValue(executionContext, new Dictionary<string, ScriptBlock>());
                    nativeArgumentCompleters = (Dictionary<string, ScriptBlock>)s_nativeArgumentCompleters.GetValue(executionContext);
                }

                foreach (string command in commands)
                {
                    nativeArgumentCompleters[command] = CreateCompleterScriptBlockForCommand(command);
                }
        }

        public ScriptBlock CreateCompleterScriptBlockForCommand(string command)
        {
            string script = new StringBuilder(256)
                .Append("param($wordToComplete,$commandAst,$cursorPosition)[PSUnixUtilCompleters.BashUtilCompleterCache]::CompleteCommand('")
                .Append(command)
                .Append("',$wordToComplete,$commandAst,$cursorPosition)")
                .ToString();

            return ScriptBlock.Create(script);
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
