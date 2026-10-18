# Builds Countdown App with the C# compiler shipped in Windows.
# No .NET SDK, NuGet packages or browser runtime are required.

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcDir = Join-Path $root 'src'
$outDir = Join-Path $root 'build'

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $csc)) {
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $csc)) {
    throw 'C# compiler (csc.exe) was not found in the .NET Framework directory.'
}

$frameworkDir = Split-Path -Parent $csc
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

function Stop-RunningInstance {
    param([string]$TargetPath)

    $running = @(Get-Process -Name 'CountdownApp' -ErrorAction SilentlyContinue)
    if ($running.Count -eq 0) { return }

    Write-Host 'CountdownApp is running; closing it so the executable can be replaced ...'
    foreach ($item in $running) {
        try {
            Stop-Process -Id $item.Id -Force -ErrorAction Stop
        }
        catch {
            Write-Warning ('Could not close process {0}: {1}' -f $item.Id, $_.Exception.Message)
        }
    }

    for ($attempt = 0; $attempt -lt 20; $attempt++) {
        if (-not (Get-Process -Name 'CountdownApp' -ErrorAction SilentlyContinue)) { break }
        Start-Sleep -Milliseconds 250
    }

    if (Test-Path -LiteralPath $TargetPath) {
        for ($attempt = 0; $attempt -lt 10; $attempt++) {
            try {
                $stream = [System.IO.File]::Open($TargetPath, 'Open', 'ReadWrite', 'None')
                $stream.Close()
                break
            }
            catch {
                Start-Sleep -Milliseconds 250
            }
        }
    }
}

$referenceNames = @(
    'System.dll',
    'System.Core.dll',
    'System.Drawing.dll',
    'System.Windows.Forms.dll',
    'System.Web.Extensions.dll'
)

$references = @()
foreach ($name in $referenceNames) {
    $references += ('/reference:{0}' -f (Join-Path $frameworkDir $name))
}

$webViewCore = Join-Path $root 'lib\WebView2\Microsoft.Web.WebView2.Core.dll'
$webViewForms = Join-Path $root 'lib\WebView2\Microsoft.Web.WebView2.WinForms.dll'
if (-not (Test-Path -LiteralPath $webViewCore) -or -not (Test-Path -LiteralPath $webViewForms)) {
    throw 'WebView2 SDK assemblies are missing from lib\WebView2.'
}
$references += ('/reference:{0}' -f $webViewCore)
$references += ('/reference:{0}' -f $webViewForms)

$sources = Get-ChildItem -Path (Join-Path $srcDir '*.cs') | Sort-Object Name | ForEach-Object {
    $_.FullName
}
if (-not $sources) {
    throw "No source files were found in $srcDir"
}

$output = Join-Path $outDir 'CountdownApp.exe'
Stop-RunningInstance -TargetPath $output

$arguments = @(
    '/nologo',
    '/target:winexe',
    '/platform:x64',
    '/optimize+',
    '/codepage:65001',
    '/main:CountdownApp.Program',
    ('/out:{0}' -f $output)
) + $references + $sources

$icon = Join-Path $srcDir 'app.ico'
if (Test-Path -LiteralPath $icon) {
    $arguments += ('/win32icon:{0}' -f $icon)
    Write-Host 'Embedding app.ico'
}
else {
    Write-Host 'app.ico not found; run tools\make-icon.ps1 to generate it.'
}

$manifest = Join-Path $srcDir 'app.manifest'
if (Test-Path -LiteralPath $manifest) {
    $arguments += ('/win32manifest:{0}' -f $manifest)
    Write-Host 'Applying app.manifest (high DPI aware)'
}

Write-Host 'Compiling CountdownApp.exe ...'
& $csc @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE"
}

$webViewSource = Join-Path $root 'lib\WebView2'
foreach ($name in @(
    'Microsoft.Web.WebView2.Core.dll',
    'Microsoft.Web.WebView2.WinForms.dll',
    'WebView2Loader.dll',
    'LICENSE-WebView2.txt',
    'NOTICE-WebView2.txt'
)) {
    Copy-Item -LiteralPath (Join-Path $webViewSource $name) -Destination $outDir -Force
}

$webSource = Join-Path $root 'web'
$webOutput = Join-Path $outDir 'web'
New-Item -ItemType Directory -Force -Path $webOutput | Out-Null
Get-ChildItem -LiteralPath $webSource -Recurse -File | ForEach-Object {
    $relative = $_.FullName.Substring($webSource.Length).TrimStart('\')
    $destination = Join-Path $webOutput $relative
    $destinationDirectory = Split-Path -Parent $destination
    New-Item -ItemType Directory -Force -Path $destinationDirectory | Out-Null
    Copy-Item -LiteralPath $_.FullName -Destination $destination -Force
}

Write-Host 'Copied WebView2 runtime files and web assets'
Write-Host ('Build succeeded: {0}' -f $output)
