#requires -Version 6.0

param(
    [ValidateSet('Debug', 'Release')]
    [string]
    $Configuration = 'Debug',

    [switch]
    $Clean
)

$ErrorActionPreference = 'Stop'

$script:ModuleName = 'PSUnixUtilCompleters'
$script:OutDir = "$PSScriptRoot/out"
$script:OutModuleDir = "$script:OutDir/$script:ModuleName"
$script:SrcDir = "$PSScriptRoot/PSUnixUtilCompleters"
$script:Framework = 'netstandard2.0'
$script:ZshCompleterScriptLocation = "$script:OutModuleDir/zcomplete.sh"

$script:Artifacts = @{
    "${script:ModuleName}.psd1" = "${script:ModuleName}.psd1"
    "PSUnixUtilCompleters/bin/$Configuration/${script:Framework}/PSUnixUtilCompleters.dll" = 'PSUnixUtilCompleters.dll'
    "PSUnixUtilCompleters/bin/$Configuration/${script:Framework}/PSUnixUtilCompleters.pdb" = 'PSUnixUtilCompleters.pdb'
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
    Exec { dotnet build }
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
