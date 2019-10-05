using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSUnixUtilCompleters
{
    public class BashUtilCompleterCache
    {
        private static readonly string ResolveCompleterCommandTemplate = 
            "-lic \". /usr/share/bash-completion 2>/dev/null; __load_completion {0} 2>/dev/null; complete -p {0} 2>/dev/null | sed -E 's/^complete.*-F ([^ ]+).*$/\\1/'\"";

        private static readonly ConcurrentDictionary<string, IReadOnlyList<string>> s_commandCompletionFunctions = new ConcurrentDictionary<string, IReadOnlyList<string>>();

        public IEnumerable<CompletionResult> CompleteCommand(
            string command,
            string wordToComplete,
            CommandAst commandAst,
            int cursorPosition)
        {
            return Enumerable.Empty<CompletionResult>();
        }

        public static IReadOnlyList<string> ResolveCommandCompleterFunctions(string commandName)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                throw new ArgumentException(nameof(commandName));
            }

            return s_commandCompletionFunctions.GetOrAdd(commandName, new Lazy<IReadOnlyList<string>>(() => {
                string resolveCompleterInvocation = string.Format(ResolveCompleterCommandTemplate, commandName);

                using (var bashSubproc = new Process())
                {
                    bashSubproc.StartInfo.FileName = "/bin/bash";
                    bashSubproc.StartInfo.Arguments = resolveCompleterInvocation;
                    bashSubproc.StartInfo.UseShellExecute = false;
                    bashSubproc.StartInfo.RedirectStandardOutput = true;
                    bashSubproc.Start();

                    return bashSubproc.StandardOutput.ReadToEnd().Split(' ');
                }
            }).Value);
        }
    }
}
