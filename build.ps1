$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$outputDirectory = Join-Path $projectRoot 'dist'
$outputFile = Join-Path $outputDirectory 'screen-color-picker-helper.exe'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw '.NET Framework 4.x C# compiler not found.'
}

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
& $compiler /nologo /target:winexe /optimize+ "/out:$outputFile" `
    /reference:System.Windows.Forms.dll /reference:System.Drawing.dll `
    (Join-Path $projectRoot 'src\Program.cs')
if ($LASTEXITCODE -ne 0) { throw "Compiler exited with $LASTEXITCODE." }

Write-Output $outputFile
