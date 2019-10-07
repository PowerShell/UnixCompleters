using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using System.Text.RegularExpressions;

namespace PSUnixUtilCompleters
{
    public class BashUtilCompleterCache
    {

        private static readonly string s_resolveCompleterCommandTemplate = string.Join("; ", new []
        {
            "-lic \". /usr/share/bash-completion 2>/dev/null",
            "__load_completion {0} 2>/dev/null",
            "complete -p {0} 2>/dev/null | sed -E 's/^complete.*-F ([^ ]+).*$/\\1/'\""
        });

        private static readonly ConcurrentDictionary<string, string> s_commandCompletionFunctions = new ConcurrentDictionary<string, string>();

        public static IEnumerable<CompletionResult> CompleteCommand(
            string command,
            string wordToComplete,
            CommandAst commandAst,
            int cursorPosition)
        {
            string completerFunction = ResolveCommandCompleterFunction(command);

            int cursorWordIndex = 0;
            string previousWord = commandAst.CommandElements[0].Extent.Text;
            for (int i = 1; i < commandAst.CommandElements.Count; i++)
            {
                IScriptExtent elementExtent = commandAst.CommandElements[i].Extent;

                if (cursorPosition < elementExtent.EndColumnNumber)
                {
                    previousWord = commandAst.CommandElements[i - 1].Extent.Text;
                    cursorWordIndex = i;
                    break;
                }

                if (cursorPosition == elementExtent.EndColumnNumber)
                {
                    previousWord = elementExtent.Text;
                    cursorWordIndex = i + 1;
                    break;
                }

                if (cursorPosition < elementExtent.StartColumnNumber)
                {
                    previousWord = commandAst.CommandElements[i - 1].Extent.Text;
                    cursorWordIndex = i;
                    break;
                }

                if (i == commandAst.CommandElements.Count - 1 && cursorPosition > elementExtent.EndColumnNumber)
                {
                    previousWord = elementExtent.Text;
                    cursorWordIndex = i + 1;
                    break;
                }
            }

            string commandLine = "'" + commandAst.Extent.Text + "'";
            string bashWordArray;

            // Handle a case like '/mnt/c/Program Files'/<TAB> where the slash is outside the string
            IScriptExtent currentExtent = commandAst.CommandElements[cursorWordIndex].Extent;      // The presumed slash-prefixed string
            IScriptExtent previousExtent = commandAst.CommandElements[cursorWordIndex - 1].Extent; // The string argument
            if (currentExtent.Text.StartsWith("/") && currentExtent.StartColumnNumber == previousExtent.EndColumnNumber)
            {
                commandLine = commandLine.Replace(previousExtent.Text + currentExtent.Text, wordToComplete);
                bashWordArray = BuildCompWordsBashArrayString(commandAst.Extent.Text, replaceAt: cursorPosition, replacementWord: wordToComplete);
            }
            else
            {
                bashWordArray = BuildCompWordsBashArrayString(commandAst.Extent.Text);
            }

            string completionCommand = BuildCompletionCommand(
                command,
                COMP_LINE: commandLine,
                COMP_WORDS: bashWordArray,
                COMP_CWORD: cursorWordIndex,
                COMP_POINT: cursorPosition,
                completerFunction,
                wordToComplete,
                previousWord);

            List<string> completionResults = InvokeBashWithArguments(completionCommand)
                .Split('\n')
                .Distinct(StringComparer.Ordinal)
                .ToList();

            completionResults.Sort(StringComparer.Ordinal);

            string previousCompletion = null;
            foreach (string completionResult in completionResults)
            {
                if (string.IsNullOrEmpty(completionResult))
                {
                    continue;
                }

                int equalsIndex = wordToComplete.IndexOf('=');

                string completionText;
                string listItemText;
                if (equalsIndex >= 0)
                {
                    completionText = wordToComplete.Substring(0, equalsIndex) + completionResult;
                    listItemText = completionResult;
                }
                else
                {
                    completionText = completionResult;
                    listItemText = completionText;
                }

                if (completionText.Equals(previousCompletion))
                {
                    listItemText += " ";
                }

                previousCompletion = completionText;

                yield return new CompletionResult(
                    completionText,
                    listItemText,
                    CompletionResultType.ParameterName,
                    completionText);
            }
        }

        private static string EscapeCompletionResult(string completionResult)
        {
            completionResult = completionResult.Trim();

            if (!completionResult.Contains(' '))
            {
                return completionResult;
            }

            return "'" + completionResult.Replace("'", "''") + "'";
        }

        public static string ResolveCommandCompleterFunction(string commandName)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                throw new ArgumentException(nameof(commandName));
            }

            return s_commandCompletionFunctions.GetOrAdd(commandName, new Lazy<string>(() => {
                string resolveCompleterInvocation = string.Format(s_resolveCompleterCommandTemplate, commandName);
                string completerFunction = InvokeBashWithArguments(resolveCompleterInvocation).Trim();

                if (string.IsNullOrEmpty(completerFunction) || completerFunction.StartsWith("complete"))
                {
                    completerFunction = "_minimal";
                }

                return completerFunction;
            }).Value);
        }

        private static string InvokeBashWithArguments(string argumentString)
        {
            using (var bashProc = new Process())
            {
                bashProc.StartInfo.FileName = "/bin/bash";
                //bashProc.StartInfo.FileName = "/mnt/c/Users/Robert Holt/Documents/Dev/sandbox/testexe/bin/Debug/netcoreapp3.0/publish/testexe";
                bashProc.StartInfo.Arguments = argumentString;
                bashProc.StartInfo.UseShellExecute = false;
                bashProc.StartInfo.RedirectStandardOutput = true;
                bashProc.Start();

                return bashProc.StandardOutput.ReadToEnd();
            }
        }

        private static string BuildCompWordsBashArrayString(
            string line,
            int replaceAt = -1,
            string replacementWord = null)
        {
            // Build a bash array of line components, like "('ls' '-a')"

            string[] lineElements = line.Split();

            int approximateLength = 0;
            foreach (string element in lineElements)
            {
                approximateLength += lineElements.Length + 2;
            }

            var sb = new StringBuilder(approximateLength);

            sb.Append('(')
                .Append('\'')
                .Append(lineElements[0].Replace("'", "\\'"))
                .Append('\'');

            if (replaceAt < 1)
            {
                for (int i = 1; i < lineElements.Length; i++)
                {
                    sb.Append(' ')
                        .Append('\'')
                        .Append(lineElements[i].Replace("'", "\\'"))
                        .Append('\'');
                }
            }
            else
            {
                for (int i = 1; i < lineElements.Length; i++)
                {
                    if (i == replaceAt - 1)
                    {
                        continue;
                    }

                    if (i == replaceAt)
                    {
                        sb.Append(' ').Append(replacementWord);
                        continue;
                    }

                    sb.Append(' ')
                        .Append('\'')
                        .Append(lineElements[i].Replace("'", "\\'"))
                        .Append('\'');
                }
            }

            sb.Append(')');

            return sb.ToString();
        }

        private static string BuildCompletionCommand(
            string command,
            string COMP_LINE,
            string COMP_WORDS,
            int COMP_CWORD,
            int COMP_POINT,
            string completionFunction,
            string wordToComplete,
            string previousWord)
        {
            return new StringBuilder(512)
                .Append("-lic \". /usr/share/bash-completion/bash_completion 2>/dev/null; ")
                .Append("__load_completion ").Append(command).Append(" 2>/dev/null; ")
                .Append("COMP_LINE=").Append(COMP_LINE).Append("; ")
                .Append("COMP_WORDS=").Append(COMP_WORDS).Append("; ")
                .Append("COMP_CWORD=").Append(COMP_CWORD).Append("; ")
                .Append("COMP_POINT=").Append(COMP_POINT).Append("; ")
                .Append("bind 'set completion-ignore-case on' 2>/dev/null; ")
                .Append(completionFunction)
                    .Append(" '").Append(command).Append("'")
                    .Append(" '").Append(wordToComplete).Append("'")
                    .Append(" '").Append(previousWord).Append("' 2>/dev/null; ")
                .Append("IFS=$'\\n'; ")
                .Append("echo \"\"\"${COMPREPLY[*]}\"\"\"\"")
                .ToString();
        }
    }
}
