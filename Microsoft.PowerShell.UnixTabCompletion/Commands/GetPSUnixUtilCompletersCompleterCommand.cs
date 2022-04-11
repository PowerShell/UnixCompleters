using System.Management.Automation;

namespace Microsoft.PowerShell.UnixTabCompletion.Commands
{
    ///<summary>
    ///Retrieve the current unix completer.
    ///</summary>
    [Cmdlet(VerbsCommon.Get, Utils.ModuleName)]
    public class GetUnixTabCompletionCommand : PSCmdlet
    {
        protected override void EndProcessing()
        {
            WriteObject(CompleterGlobals.UnixUtilCompleter);
        }
    }
}