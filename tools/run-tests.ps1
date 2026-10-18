$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$buildDir = Join-Path $root 'build'
New-Item -ItemType Directory -Force -Path $buildDir | Out-Null

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $csc)) {
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $csc)) {
    throw 'C# compiler (csc.exe) was not found in the .NET Framework directory.'
}

$frameworkDir = Split-Path -Parent $csc
$output = Join-Path $buildDir 'CountdownLogicTests.exe'
$sources = @(
    (Join-Path $root 'src\Models.cs'),
    (Join-Path $root 'src\EventStore.cs'),
    (Join-Path $root 'src\CountdownEngine.cs'),
    (Join-Path $root 'tools\CountdownLogicTests.cs')
)

$arguments = @(
    '/nologo',
    '/target:exe',
    '/platform:anycpu',
    '/codepage:65001',
    ('/out:{0}' -f $output),
    ('/reference:{0}' -f (Join-Path $frameworkDir 'System.dll')),
    ('/reference:{0}' -f (Join-Path $frameworkDir 'System.Core.dll')),
    ('/reference:{0}' -f (Join-Path $frameworkDir 'System.Web.Extensions.dll'))
) + $sources

Write-Host 'Compiling CountdownLogicTests.exe ...'
& $csc @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Test compilation failed with exit code $LASTEXITCODE"
}

& $output
if ($LASTEXITCODE -ne 0) {
    throw "Tests failed with exit code $LASTEXITCODE"
}
