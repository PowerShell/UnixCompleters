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
    public static class ZshUtilCompletion
    {
        private static string s_completionScriptPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "zcomplete.sh");

        public static IEnumerable<CompletionResult> CompleteCommand(
            string zshPath,
            string command,
            string wordToComplete,
            CommandAst commandAst,
            int cursorPosition)
        {
            string zshArgs = CreateZshCompletionArgs(command, wordToComplete, commandAst, cursorPosition - commandAst.Extent.StartOffset);
            var seenCompletions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string result in InvokeWithZsh(zshPath, zshArgs).Split('\n'))
            {
                int spaceIndex = result.IndexOf(' ');

                if (spaceIndex < 0 || spaceIndex >= result.Length)
                {
                    continue;
                }

                string completionText = result.Substring(0, spaceIndex);
                string listItemText = completionText;

                // Deal with case sensitivity
                while (!seenCompletions.Add(listItemText))
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

        private static string InvokeWithZsh(string zshPath, string arguments)
        {
            using (var zshProc = new Process())
            {
                zshProc.StartInfo.RedirectStandardOutput = true;
                zshProc.StartInfo.FileName = zshPath;
                zshProc.StartInfo.Arguments = arguments;

                zshProc.Start();

                return zshProc.StandardOutput.ReadToEnd();
            }
        }
    }
}