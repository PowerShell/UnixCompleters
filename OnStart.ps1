# Informative messages to let users know that completers have been registered

Write-Verbose "Registering UNIX native tab completion"

# We don't have access to the module at load time, since loading occurs last
# Instead we set up a one-time event to set the OnRemove scriptblock once the module has been loaded
$null = Register-EngineEvent -SourceIdentifier PowerShell.OnIdle -MaxTriggerCount 1 -Action {
    $m = Get-Module Microsoft.PowerShell.UnixTabCompletion
    $m.OnRemove = {
        Write-Verbose "Deregistering UNIX native tab completion"
    }
}