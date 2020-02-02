using System;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace PSUnixUtilCompleters.Commands
{
    [Cmdlet(VerbsData.Import, Utils.ModulePrefix + "Completers")]
    public class ImportPSUnixUtilCompletersCommand : PSCmdlet
    {
        protected override void EndProcessing()
        {
            // Do nothing here; this command does its job by autoloading
        }
    }
}