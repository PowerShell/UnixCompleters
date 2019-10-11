using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;
using System.Text;

namespace PSUnixUtilCompleters
{
    public class ZshUtilCompleter : IUnixUtilCompleter
    {
        private static readonly string s_completionScriptPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "zcomplete.sh");

        private readonly string _zshPath;

        private readonly HashSet<string> _seenCompletions;

        public ZshUtilCompleter(string zshPath)
        {
            _zshPath = zshPath;
            _seenCompletions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<string> FindCompletableCommands()
        {
            return UnixHelpers.NativeUtilNames;
        }

        public IEnumerable<CompletionResult> CompleteCommand(
            string command,
            string wordToComplete,
            CommandAst commandAst,
            int cursorPosition)
        {
            string zshArgs = CreateZshCompletionArgs(command, wordToComplete, commandAst, cursorPosition - commandAst.Extent.StartOffset);
            _seenCompletions.Clear();
            foreach (string result in InvokeWithZsh(zshArgs).Split('\n'))
            {
                int spaceIndex = result.IndexOf(' ');

                if (spaceIndex < 0 || spaceIndex >= result.Length)
                {
                    continue;
                }

                string completionText = result.Substring(0, spaceIndex);
                string listItemText = completionText;

                // Deal with case sensitivity
                while (!_seenCompletions.Add(listItemText))
                {
                    listItemText += " ";
                }

                yield return new CompletionResult(
                    completionText,
                    listItemText,
                    CompletionResultType.ParameterName,
                    completionText);
            }
        }

        private string InvokeWithZsh(string arguments)
        {
            using (var zshProc = new Process())
            {
                zshProc.StartInfo.RedirectStandardOutput = true;
                zshProc.StartInfo.FileName = this._zshPath;
                zshProc.StartInfo.Arguments = arguments;

                zshProc.Start();

                return zshProc.StandardOutput.ReadToEnd();
            }
        }

        private static string CreateZshCompletionArgs(
            string command,
            string wordToComplete,
            CommandAst commandAst,
            int cursorPosition)
        {
            return new StringBuilder(s_completionScriptPath.Length + commandAst.Extent.Text.Length)
                .Append('"').Append(s_completionScriptPath).Append("\" ")
                .Append('"').Append(commandAst.Extent.Text.Substring(0, cursorPosition).Replace("\"", "\"\"\"")).Append('"')
                .ToString();
        }
    }
}