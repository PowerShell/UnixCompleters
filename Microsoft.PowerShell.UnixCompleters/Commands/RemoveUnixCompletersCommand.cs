using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.UnixCompleters.Commands
{
    [Cmdlet(VerbsCommon.Remove, Utils.ModulePrefix + "s")]
    public class RemoveUnixCompleters : PSCmdlet
    {
        protected override void EndProcessing()
        {
            InvokeCommand.InvokeScript("Remove-Module -Name Microsoft.PowerShell.UnixCompleters");
        }
    }
}