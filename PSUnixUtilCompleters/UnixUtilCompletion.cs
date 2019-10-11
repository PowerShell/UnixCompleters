using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;

namespace PSUnixUtilCompleters
{
    public static class UnixUtilCompletion
    {
        private static string s_fullTypeName = typeof(UnixUtilCompletion).FullName;

        [ThreadStatic]
        internal static IUnixUtilCompleter s_unixCompleter;

        public static IEnumerable<CompletionResult> CompleteCommand(
            string command,
            string wordToComplete,
            CommandAst commandAst,
            int cursorPosition)
        {
            if (s_unixCompleter == null)
            {
                return Enumerable.Empty<CompletionResult>();
            }

            return s_unixCompleter.CompleteCommand(command, wordToComplete, commandAst, cursorPosition);
        }

        internal static void SetCompleter(IUnixUtilCompleter completer)
        {
            s_unixCompleter = completer;
        }

        internal static ScriptBlock CreateInvocationScriptBlock(string command)
        {
            string script = new StringBuilder(256)
                .Append("param($wordToComplete,$commandAst,$cursorPosition)[")
                .Append(s_fullTypeName)
                .Append("]::")
                .Append(nameof(CompleteCommand))
                .Append("('")
                .Append(command)
                .Append("',$wordToComplete,$commandAst,$cursorPosition)")
                .ToString();

            return ScriptBlock.Create(script);
        }
    }
}