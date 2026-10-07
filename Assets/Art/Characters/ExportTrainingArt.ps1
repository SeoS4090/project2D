param(
    [Parameter(Mandatory = $true)]
    [string] $AsepriteExe
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$generator = Join-Path $PSScriptRoot 'GenerateTrainingHero.lua'
$heroSource = Join-Path $PSScriptRoot 'TrainingHero.aseprite'
$dummySource = Join-Path $PSScriptRoot 'TrainingDummy.aseprite'
$frameRoot = Join-Path $PSScriptRoot 'Frames'
$temporaryRoot = Join-Path $env:TEMP ('training-art-' + [guid]::NewGuid().ToString('N'))

if (!(Test-Path -LiteralPath $AsepriteExe -PathType Leaf)) {
    throw "Aseprite executable not found: $AsepriteExe"
}

function Invoke-Aseprite([string[]] $Arguments) {
    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $AsepriteExe
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    foreach ($argument in $Arguments) { $startInfo.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::Start($startInfo)
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) {
        throw "Aseprite exited with code $($process.ExitCode): $($Arguments -join ' ')"
    }
}

New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $frameRoot 'Body'), (Join-Path $frameRoot 'Sword') | Out-Null

Push-Location $projectRoot
try {
    Invoke-Aseprite @('--batch', '--script', $generator)
    if (!(Test-Path -LiteralPath $heroSource -PathType Leaf) -or !(Test-Path -LiteralPath $dummySource -PathType Leaf)) {
        throw 'Aseprite did not generate both editable source files.'
    }

    for ($frame = 1; $frame -le 32; $frame++) {
        foreach ($layer in @('Body', 'Greatsword')) {
            $name = if ($layer -eq 'Body') { 'body' } else { 'sword' }
            $folder = if ($layer -eq 'Body') { 'Body' } else { 'Sword' }
            $temporaryPng = Join-Path $temporaryRoot ("{0}_{1:D2}.png" -f $name, $frame)
            $destination = Join-Path $frameRoot ("{0}\{1}_{2:D2}.png" -f $folder, $name, $frame)
            Invoke-Aseprite @('--batch', $heroSource, '--frame-range', "$frame,$frame", '--layer', $layer, '--save-as', $temporaryPng)
            if (!(Test-Path -LiteralPath $temporaryPng -PathType Leaf)) {
                throw "Aseprite failed to export $layer frame $frame."
            }
            Copy-Item -LiteralPath $temporaryPng -Destination $destination -Force
        }
    }

    $temporaryDummy = Join-Path $temporaryRoot 'TrainingDummy.png'
    Invoke-Aseprite @('--batch', $dummySource, '--save-as', $temporaryDummy)
    if (!(Test-Path -LiteralPath $temporaryDummy -PathType Leaf)) {
        throw 'Aseprite failed to export the training dummy.'
    }
    Copy-Item -LiteralPath $temporaryDummy -Destination (Join-Path $PSScriptRoot 'TrainingDummy.png') -Force
}
finally {
    Pop-Location
    $resolvedTemporaryRoot = (Resolve-Path -LiteralPath $temporaryRoot).Path
    $resolvedTempBase = (Resolve-Path -LiteralPath $env:TEMP).Path
    if (!$resolvedTemporaryRoot.StartsWith($resolvedTempBase, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove temporary export data outside the temp folder: $resolvedTemporaryRoot"
    }
    [IO.Directory]::Delete($resolvedTemporaryRoot, $true)
}
