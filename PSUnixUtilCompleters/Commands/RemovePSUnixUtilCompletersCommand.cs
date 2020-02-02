using System;
using System.Management.Automation;

namespace PSUnixUtilCompleters.Commands
{
    [Cmdlet(VerbsCommon.Remove, Utils.ModulePrefix + "Completers")]
    public class RemovePSUnixUtilCompletersCommand : PSCmdlet
    {
        protected override void EndProcessing()
        {
            InvokeCommand.InvokeScript("Remove-Module -Name PSUnixUtilCompleters");
        }
    }
}