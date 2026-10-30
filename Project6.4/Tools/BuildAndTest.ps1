param([string]$Unity='D:\Development\Unity\6000.4.12f1\Editor\Unity.exe')
$ErrorActionPreference='Stop'
$release=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$project=Split-Path $PSScriptRoot -Parent
$run=Join-Path $release ('Docs\Logs\Build\'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
$status=[ordered]@{started=(Get-Date -Format o);unity=$Unity;project=$project;phase='build';result='RUNNING'}
function RunProcess([string]$executable,[string[]]$arguments) {
    # Start-Process -Wait waits for descendants too (e.g. persistent compiler servers).
    # Wait on the editor/player itself and read that process's exit code instead.
    $process=Start-Process -FilePath $executable -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.Handle | Out-Null
    $process.WaitForExit()
    return $process.ExitCode
}
function Snapshot([string]$name) {
    $target=Join-Path $run $name
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $release 'Docs') -File |
        Where-Object {$_.Extension -in @('.txt','.csv')} | Copy-Item -Destination $target
}
try {
    if(!(Test-Path -LiteralPath $Unity)){throw "Unity editor not found; supply -Unity with the installed Unity.exe path: $Unity"}
    $editorExit=RunProcess $Unity @('-batchmode','-nographics','-quit','-projectPath',('"'+$project+'"'),'-executeMethod','PlaytestFixBuild.Build','-logFile',('"'+(Join-Path $run 'editor-build.log')+'"'))
    $status.editorExit=$editorExit
    Snapshot 'editor-reports'
    if($editorExit -ne 0){throw 'Editor build/tests failed'}
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'StartWithLogs.ps1') -Destination (Join-Path $release 'Play64\StartWithLogs.ps1') -Force
    $status.phase='runtime-regression'
    $runtimeExit=RunProcess (Join-Path $release 'Play64\SeasideWhitebox.exe') @('-batchmode','-nographics','-regression','-logFile',('"'+(Join-Path $run 'runtime-regression.log')+'"'))
    $status.regressionExit=$runtimeExit
    Snapshot 'runtime-reports'
    if($runtimeExit -ne 0){throw 'Runtime regressions failed'}
    $status.phase='runtime-calibration'
    $calibrationExit=RunProcess (Join-Path $release 'Play64\SeasideWhitebox.exe') @('-batchmode','-nographics','-calibrate','-logFile',('"'+(Join-Path $run 'runtime-calibration.log')+'"'))
    $status.calibrationExit=$calibrationExit
    Snapshot 'runtime-reports'
    if($calibrationExit -ne 0){throw 'Runtime calibration failed'}
    $status.result='PASS'
} catch {$status.result='FAIL';$status.error=$_.Exception.Message;throw}
finally {
    $status.finished=Get-Date -Format o
    $status | ConvertTo-Json | Out-File -LiteralPath (Join-Path $run 'status.json') -Encoding utf8
    Write-Output ('LOG_FOLDER='+$run)
}
