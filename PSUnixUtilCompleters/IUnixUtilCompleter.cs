using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSUnixUtilCompleters
{
    public interface IUnixUtilCompleter
    {
        IEnumerable<string> FindCompletableCommands();

        IEnumerable<CompletionResult> CompleteCommand(
            string command,
            string wordToComplete,
            CommandAst commandAst,
            int cursorPosition);
    }
}