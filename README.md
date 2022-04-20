# Microsoft.PowerShell.UnixTabCompletion

PowerShell parameter completers for native commands on Linux and macOS.

This module uses completers supplied in traditional Unix shells
to complete native utility parameters in PowerShell.

![Completions with apt example](completions.gif)

Currently, this module supports completions from zsh and bash.
By default it will look for zsh and then bash to run completions
(since zsh's completions seem to be generally better).

## Basic usage

To enable unix utility completions,
install this module and add the following to your profile:

```powershell
Import-Module Microsoft.PowerShell.UnixTabCompletion
```

There is also an alternate command, `Import-PSUnixTabCompletion`,
that has the same functionality but is discoverable by command completion.

This will register argument completers for all native commands
found in the usual Unix util directories.

Given the nature of native completion results,
you may find this works best with PSReadLine's MenuComplete mode:

```powershell
Import-Module PSUnixTabCompletion

Set-PSReadLineKeyHandler -Key Tab -Function MenuComplete
```

## Further configuration

If you wish to set a preferred shell, you can do so by setting an environment variable:

```powershell
$env:COMPLETION_SHELL_PREFERENCE = 'bash'

# OR

$env:COMPLETION_SHELL_PREFERENCE = '/bin/bash'

Import-Module PSUnixTabCompletion
```

Note that you must do this before you load the module,
and that setting it after loading will have no effect.

If you want to change the completer after loading,
you can do so from PowerShell like so:

```powershell
Set-PSUnixTabCompletion -ShellType Zsh

# Or if you have a shell installed to a particular path
Set-PSUnixTabCompletion -Shell "/bin/zsh"

# You can even write your own utility completer by implementing `IUnixUtilCompleter`
$myCompleter = [MyCompleter]::new()
Set-PSUnixTabCompletion -Completer $myCompleter
```

You can retrieve the current configuration with the `Get-PSUnixTabCompletion` cmdlet:

```powershell
PS> Get-PSUnixTabCompletion

Name
----
ZshUtilCompleter
```

## Supporting different versions of Bash

`bash` may have different tab completion depending on the version and system.
To provide greater flexibility, setting `bash` tab completion via `Set-PSUnixTabCompletion` supports an additional parameter.
If your completion script is in an alternative location,
you may provide the location of the completion script as a parameter to `Set-PSUnixTabCompletion`.
By default, this value is set to `/usr/share/bash-completion/bash_completion` which should work for most Linux systems.
This is roughly equivalent to the `source /usr/share/bash-completion/bash_completion` which may be needed by MacOS.

```powershell
Set-PSUnixTabCompletion -ShellType Bash -CompletionScript /usr/local/etc/bash_completion
```

When the shell is set to bash, `Get-PSUnixTabCompletion` will return the completion script.

```powershell
[UnixCompleters-1|main↑0↓0•0+1?2] 🐚> Set-PSUnixTabCompletion -ShellType Bash -CompletionScript /usr/local/etc/bash_completion
[UnixCompleters-1|main↑0↓0•0+1?2] 🐚> Get-PSUnixTabCompletion

CompletionScript               Name
----------------               ----
/usr/local/etc/bash_completion BashUtilCompleter
```

## Unregistering UNIX util completions

The Microsoft.PowerShell.UnixTabCompletion module will unregister completers
for all the commands it registered completers for
when removed:

```powershell
Remove-Module Microsoft.PowerShell.PSUnixTabCompletion
```

As with loading, there is also a convenience command provided for this:

```powershell
Remove-PSUnixTabCompletion
```

## Building the module yourself

Microsoft.PowerShell.UnixTabCompletion comes with a PowerShell build script,
which you can invoke to build the module yourself with:

```powershell
./build.ps1 -Clean
```

This will output the built module to `out/Microsoft.PowerShell.UnixTabCompletion`.

## Credits

All the zsh completions provided by this module are made possible
by the work of [@Valodim](https://github.com/Valodim)'s zsh completion project,
[zsh-capture-completion](https://github.com/Valodim/zsh-capture-completion),
which this module invokes to get completion results.

The bash completions provided by this module are adapted from the work
done by [@mikebattista](https://github.com/mikebattista) for his
[PowerShell-WSL-interop](https://github.com/mikebattista/PowerShell-WSL-Interop) PowerShell module.
