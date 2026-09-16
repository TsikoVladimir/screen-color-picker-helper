$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$outputDirectory = Join-Path $projectRoot 'dist'
$intermediateDirectory = Join-Path $projectRoot 'obj'
$outputFile = Join-Path $outputDirectory 'screen-color-picker-helper.exe'
$versionFile = Join-Path $projectRoot 'VERSION'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw '.NET Framework 4.x C# compiler not found.'
}

$version = (Get-Content -Raw -LiteralPath $versionFile).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') {
    throw "VERSION must use MAJOR.MINOR.PATCH format, got '$version'."
}
$assemblyVersion = "$version.0"

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $intermediateDirectory -Force | Out-Null

$assemblyInfo = Join-Path $intermediateDirectory 'AssemblyInfo.g.cs'
$assemblySource = @"
using System.Reflection;

[assembly: AssemblyTitle("Screen Color Picker Helper")]
[assembly: AssemblyDescription("Open-source Windows screen color picker helper")]
[assembly: AssemblyCompany("Vladimir Tsiko")]
[assembly: AssemblyProduct("Screen Color Picker Helper")]
[assembly: AssemblyCopyright("Copyright (c) 2026 Vladimir Tsiko")]
[assembly: AssemblyVersion("$assemblyVersion")]
[assembly: AssemblyFileVersion("$assemblyVersion")]
"@
[System.IO.File]::WriteAllText($assemblyInfo, $assemblySource, [System.Text.UTF8Encoding]::new($false))

& $compiler /nologo /target:winexe /optimize+ "/out:$outputFile" `
    /reference:System.Windows.Forms.dll /reference:System.Drawing.dll `
    $assemblyInfo (Join-Path $projectRoot 'src\Program.cs')
if ($LASTEXITCODE -ne 0) { throw "Compiler exited with $LASTEXITCODE." }

Write-Output $outputFile
