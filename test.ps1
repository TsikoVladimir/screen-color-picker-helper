param(
    [switch]$SkipInteractiveCheck
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$expectedVersion = ((Get-Content -Raw -LiteralPath (Join-Path $projectRoot 'VERSION')).Trim() + '.0')
& (Join-Path $projectRoot 'build.ps1') | Write-Output
$executable = Join-Path $projectRoot 'dist\screen-color-picker-helper.exe'
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $executable)) {
    throw 'Build did not produce the helper executable.'
}

$versionInfo = (Get-Item -LiteralPath $executable).VersionInfo
$expectedMetadata = @{
    ProductName = 'Screen Color Picker Helper'
    ProductVersion = $expectedVersion
    FileVersion = $expectedVersion
    CompanyName = 'Vladimir Tsiko'
    OriginalFilename = 'screen-color-picker-helper.exe'
}
foreach ($entry in $expectedMetadata.GetEnumerator()) {
    if ($versionInfo.($entry.Key) -ne $entry.Value) {
        throw "$($entry.Key) metadata mismatch: expected '$($entry.Value)', got '$($versionInfo.($entry.Key))'."
    }
}

$invalid = Start-Process -FilePath $executable -ArgumentList '--invalid-argument' -PassThru -Wait -WindowStyle Hidden
if ($invalid.ExitCode -ne 16) {
    throw "Invalid-argument check returned $($invalid.ExitCode), expected 16."
}

if (-not $SkipInteractiveCheck) {
    $hook = Start-Process -FilePath $executable -ArgumentList '--picker-hook-check' -PassThru -Wait -WindowStyle Hidden
    if ($hook.ExitCode -ne 0) {
        throw "Picker hook check returned $($hook.ExitCode), expected 0."
    }
}

Write-Output "All checks passed for $executable ($expectedVersion)."
