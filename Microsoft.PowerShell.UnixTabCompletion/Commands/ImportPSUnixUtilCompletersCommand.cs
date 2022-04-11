using System;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace Microsoft.PowerShell.UnixTabCompletion.Commands
{
    [Cmdlet(VerbsData.Import, Utils.ModuleName)]
    public class ImportUnixTabCompletionCommand : PSCmdlet
    {
        protected override void EndProcessing()
        {
            // Do nothing here; this command does its job by autoloading
        }
    }
}