using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.UnixTabCompletion.Commands
{
    [Cmdlet(VerbsCommon.Remove, Utils.ModuleName)]
    public class RemoveUnixTabCompletionCommand : PSCmdlet
    {
        protected override void EndProcessing()
        {
            InvokeCommand.InvokeScript("Remove-Module -Name Microsoft.PowerShell.UnixTabCompletion -Scope All -Force");
        }
    }
}