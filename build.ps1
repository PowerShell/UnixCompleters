#requires -Version 6.0

[CmdletBinding(DefaultParameterSetName = 'Build')]
param(
    [Parameter(ParameterSetName = 'Build')]
    [ValidateSet('Debug', 'Release')]
    [string]
    $Configuration = 'Debug',

    [Parameter(ParameterSetName = 'Build')]
    [switch]
    $Clean,

    [Parameter(ParameterSetName = 'Package')]
    [switch]
    $Package,

    [Parameter(ParameterSetName = 'Package')]
    [switch]
    $Signed,

    [Parameter(ParameterSetName = 'Test')]
    [switch]
    $Build,

    [Parameter(ParameterSetName = 'Test')]
    [switch]
    $Test
)

$ErrorActionPreference = 'Stop'

$script:ModuleName = 'PSUnixUtilCompleters'
$script:ModuleVersion = (Import-PowerShellDataFile -Path "${PSScriptRoot}/${ModuleName}.psd1").ModuleVersion
$script:OutDir = "${PSScriptRoot}/out"
$script:ModuleBase = "${PSScriptRoot}/out/${script:ModuleName}"
$script:OutModuleDir = "${ModuleBase}/${script:ModuleVersion}"
$script:SrcDir = "$PSScriptRoot/PSUnixUtilCompleters"
$script:Framework = 'netstandard2.1'
$script:ZshCompleterScriptLocation = "${script:OutModuleDir}/zcomplete.sh"

$script:Artifacts = @{
    "OnStart.ps1" = "OnStart.ps1"
    "${script:ModuleName}.psd1" = "${script:ModuleName}.psd1"
    "PSUnixUtilCompleters/bin/$Configuration/${script:Framework}/PSUnixUtilCompleters.dll" = 'PSUnixUtilCompleters.dll'
    "LICENSE" = "LICENSE.txt"
}
if ( $Configuration -eq 'Debug' ) {
    ${script:Artifacts}["PSUnixUtilCompleters/bin/$Configuration/${script:Framework}/PSUnixUtilCompleters.pdb"] = 'PSUnixUtilCompleters.pdb'
}

function Exec([scriptblock]$sb, [switch]$IgnoreExitcode)
{
    $backupEAP = $script:ErrorActionPreference
    $script:ErrorActionPreference = "Continue"
    try
    {
        & $sb
        # note, if $sb doesn't have a native invocation, $LASTEXITCODE will
        # point to the obsolete value
        if ($LASTEXITCODE -ne 0 -and -not $IgnoreExitcode)
        {
            throw "Execution of {$sb} failed with exit code $LASTEXITCODE"
        }
    }
    finally
    {
        $script:ErrorActionPreference = $backupEAP
    }
}

if ($PSCmdlet.ParameterSetName -eq 'Build' -or $Build)
{
    try
    {
        $null = Get-Command dotnet -ErrorAction Stop
    }
    catch
    {
        throw 'Unable to find dotnet executable'
    }

    if ($Clean)
    {
        foreach ($path in $script:OutDir,"${script:SrcDir}/bin","${script:SrcDir}/obj")
        {
            if (Test-Path -Path $path)
            {
                Remove-Item -Force -Recurse -Path $path -ErrorAction Stop
            }
        }
    }

    Push-Location $script:SrcDir
    try
    {
        Exec { dotnet build --configuration $Configuration }
    }
    finally
    {
        Pop-Location
    }

    New-Item -ItemType Directory -Path $script:OutModuleDir -ErrorAction SilentlyContinue

    foreach ($artifactEntry in $script:Artifacts.GetEnumerator())
    {
        Copy-Item -Path $artifactEntry.Key -Destination (Join-Path $script:OutModuleDir $artifactEntry.Value) -ErrorAction Stop
    }

    # We need the zsh completer script to drive zsh completions
    Invoke-WebRequest -Uri 'https://raw.githubusercontent.com/Valodim/zsh-capture-completion/master/capture.zsh' -OutFile $script:ZshCompleterScriptLocation
}

if ($Test) {
    $pwsh = (Get-Process -Id $PID).Path
    $testPath = "$PSScriptRoot/tests"
    & $pwsh -noprofile -c "Import-Module Pester -Max 4.99.99; Invoke-Pester '$testPath'"
}

# if we're signed, the files must be in 'signed' rather than 'out' directory
if ($Package) {
    $packagePath = "$PSScriptRoot/packages"
    $repoName = [guid]::NewGuid().ToString("N")
    if ( $Signed ) {
        $moduleLocation = "$psScriptRoot/signed/${script:ModuleName}/${script:ModuleVersion}"
    }
    else {
        $moduleLocation = ${script:ModuleBase}
    }
    if (-not (Test-Path $packagePath)) {
        $null = New-Item -ItemType Directory -Path $packagePath
    }
    try {
        Register-PSRepository -Name $repoName -SourceLocation $packagePath -PublishLocation $packagePath -InstallationPolicy Trusted
        Publish-Module -Path "${moduleLocation}" -Repository $repoName
    }
    finally {
        Unregister-PSRepository -Name $repoName
    }
}
