using System;
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
        private enum ShellType
        {
            None = 0,
            Zsh,
            Bash,
        }

        private readonly static IReadOnlyDictionary<string, ShellType> s_shells = new Dictionary<string, ShellType>()
        {
            { "zsh", ShellType.Zsh },
            { "bash", ShellType.Bash },
        };

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
            if (!TryFindShell(out string shellExePath, out ShellType shellType))
            {
                return;
            }
            string[] utilNames = GetNativeUtilNames();
            RegisterCompletersForCommands(utilNames, shellExePath, shellType);
        }

        private void RegisterCompletersForCommands(IEnumerable<string> commands, string shellExePath, ShellType shellType)
        {
            if (shellType == ShellType.None)
            {
                return;
            }

            object executionContext = s_executionContext.GetValue(Runspace.DefaultRunspace);

            var nativeArgumentCompleters = (Dictionary<string, ScriptBlock>)s_nativeArgumentCompleters.GetValue(executionContext);
            if (nativeArgumentCompleters == null)
            {
                s_nativeArgumentCompleters.SetValue(executionContext, new Dictionary<string, ScriptBlock>());
                nativeArgumentCompleters = (Dictionary<string, ScriptBlock>)s_nativeArgumentCompleters.GetValue(executionContext);
            }

            switch (shellType)
            {
                case ShellType.Zsh:
                    foreach (string command in commands)
                    {
                        nativeArgumentCompleters[command] = CreateZshCompleterScriptBlockForCommand(shellExePath, command);
                    }
                    return;

                case ShellType.Bash:
                    foreach (string command in commands)
                    {
                        nativeArgumentCompleters[command] = CreateBashCompleterScriptBlockForCommand(shellExePath, command);
                    }
                return;
            }

        }

        public ScriptBlock CreateBashCompleterScriptBlockForCommand(string shellExePath, string command)
        {
            string script = new StringBuilder(256)
                .Append("param($wordToComplete,$commandAst,$cursorPosition)[PSUnixUtilCompleters.BashUtilCompleterCache]::CompleteCommand('")
                .Append(shellExePath)
                .Append("','")
                .Append(command)
                .Append("',$wordToComplete,$commandAst,$cursorPosition)")
                .ToString();

            return ScriptBlock.Create(script);
        }

        public ScriptBlock CreateZshCompleterScriptBlockForCommand(string shellExePath, string command)
        {
            string script = new StringBuilder(256)
                .Append("param($wordToComplete,$commandAst,$cursorPosition)[PSUnixUtilCompleters.ZshUtilCompletion]::CompleteCommand('")
                .Append(shellExePath)
                .Append("','")
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

        private bool TryFindShell(out string foundShell, out ShellType shellType)
        {
            bool useBash = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("UseBashInDemo"));
            foreach (KeyValuePair<string, ShellType> shell in s_shells)
            {
                if (useBash && !shell.Key.Equals("bash"))
                {
                    continue;
                }

                for (int i = s_nativeUtilDirs.Count - 1; i >= 0; i--)
                {
                    string shellPath = Path.Combine(s_nativeUtilDirs[i], shell.Key);
                    if (File.Exists(shellPath))
                    {
                        foundShell = shellPath;
                        shellType = shell.Value;
                        return true;
                    }
                }
            }

            foundShell = null;
            shellType = ShellType.None;
            return false;
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
