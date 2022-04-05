using System.Management.Automation;

namespace PSUnixUtilCompleters.Commands
{
    ///<summary>
    ///Retrieve the current unix completer.
    ///</summary>
    [Cmdlet(VerbsCommon.Get, Utils.ModulePrefix + "Completer")]
    public class GetPSUnixUtilCompletersCompleterCommand : PSCmdlet
    {
        protected override void EndProcessing()
        {
            WriteObject(CompleterGlobals.UnixUtilCompleter);
        }
    }
}