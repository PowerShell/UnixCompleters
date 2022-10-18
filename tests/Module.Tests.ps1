# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Remove-PSUnixTabCompletion tests" {
    It 'Remove-PSUnixTabCompletion should not have an error' {
        $output = pwsh -c "Import-Module $PSScriptRoot/../out/Microsoft.PowerShell.UnixTabCompletion; Remove-PSUnixTabCompletion" 2>&1
        $output | Should -BeNullOrEmpty
    }
}
