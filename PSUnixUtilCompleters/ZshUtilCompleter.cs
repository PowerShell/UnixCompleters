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
                // The completer script sometimes has a bug
                // where it returns odd strings with VT100 escapes in it,
                // so just filter those out.
                if (string.IsNullOrEmpty(result) || result.Contains("\u001B["))
                {
                    continue;
                }

                int spaceIndex = result.IndexOf(' ');

                string completionText = spaceIndex < 0
                    ? result.Trim()
                    : result.Substring(0, spaceIndex);

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
                zshProc.StartInfo.RedirectStandardError = true;
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
            string completionText;
            if (cursorPosition == commandAst.Extent.Text.Length)
            {
                completionText = commandAst.Extent.Text;
            }
            else if (cursorPosition > commandAst.Extent.Text.Length)
            {
                completionText = commandAst.Extent.Text + " ";
            }
            else
            {
                completionText = commandAst.Extent.Text.Substring(0, cursorPosition);
            }

            return new StringBuilder(s_completionScriptPath.Length + commandAst.Extent.Text.Length)
                .Append('"').Append(s_completionScriptPath).Append("\" ")
                .Append('"').Append(completionText.Replace("\"", "\"\"\"")).Append("\"")
                .ToString();
        }
    }
}