param([string]$Unity='D:\Development\Unity\6000.4.12f1\Editor\Unity.exe',[switch]$StartupPage,[switch]$GenerateStartupScene,[switch]$ForestWhitebox,[switch]$MineWhitebox,[switch]$SnowWhitebox,[switch]$LighthouseWhitebox)
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
    # The retired whitebox is a test fixture now. Default builds use the real menu/first level.
    if(!$StartupPage -and !$ForestWhitebox -and !$MineWhitebox -and !$SnowWhitebox -and !$LighthouseWhitebox){$StartupPage=$true}
    if(!(Test-Path -LiteralPath $Unity)){throw "Unity editor not found; supply -Unity with the installed Unity.exe path: $Unity"}
    if((@($StartupPage,$ForestWhitebox,$MineWhitebox,$SnowWhitebox,$LighthouseWhitebox) | Where-Object {$_}).Count -gt 1){throw 'Choose one independent build target.'}
    if($ForestWhitebox -and ($StartupPage -or $GenerateStartupScene)){throw 'ForestWhitebox is an independent build; do not combine it with StartupPage.'}
    if($MineWhitebox -and ($ForestWhitebox -or $StartupPage -or $GenerateStartupScene)){throw 'MineWhitebox is an independent build; choose one target.'}
    if($GenerateStartupScene -and !$StartupPage){throw 'GenerateStartupScene requires StartupPage and an isolated project.'}
    $method=if($SnowWhitebox){'SnowBuilder.Build'}elseif($LighthouseWhitebox){'LighthouseBuilder.Build'}elseif($MineWhitebox){'MineBuilder.Build'}elseif($ForestWhitebox){'ForestBuilder.Build'}elseif($GenerateStartupScene){'StartGameSetup.GenerateAndBuild'}else{'StartGameSetup.ValidateAndBuild'}
    $editorArguments=@('-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod',$method,'-logFile',('"'+(Join-Path $run 'editor-build.log')+'"'))
    if(!$StartupPage){$editorArguments+='-nographics'}
    $editorExit=RunProcess $Unity $editorArguments
    $status.editorExit=$editorExit
    Snapshot 'editor-reports'
    if($editorExit -ne 0){throw 'Editor build/tests failed'}
    if($SnowWhitebox -or $LighthouseWhitebox){
        $levelName=if($SnowWhitebox){'Snow'}else{'Lighthouse'}
        $status.phase=$levelName.ToLowerInvariant()+'-runtime'
        $thermalReport=Join-Path $run ($levelName.ToLowerInvariant()+'-runtime-checks.txt')
        $thermalExit=RunProcess (Join-Path $release ('Play64\'+$levelName+'\'+$levelName+'Whitebox.exe')) @('-batchmode','-nographics','-thermalChecks','-thermalReport',('"'+$thermalReport+'"'),'-logFile',('"'+(Join-Path $run 'thermal-runtime.log')+'"'))
        $status.thermalExit=$thermalExit
        if($thermalExit -ne 0){throw ($levelName+' runtime checks failed')}
        if(!(Test-Path -LiteralPath $thermalReport)){throw 'Player exited without a thermal runtime report'}
        $status.result='PASS'
        return
    }
    if($MineWhitebox){
        $status.phase='mine-runtime'
        $mineReport=Join-Path $run 'mine-runtime-checks.txt'
        $mineExit=RunProcess (Join-Path $release 'Play64\Mine\MineWhitebox.exe') @('-batchmode','-nographics','-mineChecks','-mineReport',('"'+$mineReport+'"'),'-logFile',('"'+(Join-Path $run 'mine-runtime.log')+'"'))
        $status.mineExit=$mineExit
        if($mineExit -ne 0){throw 'Mine runtime checks failed'}
        if(!(Test-Path -LiteralPath $mineReport)){throw 'Mine player exited without a runtime report'}
        $status.result='PASS'
        return
    }
    if($ForestWhitebox){
        $status.phase='forest-runtime'
        $forestReport=Join-Path $run 'forest-runtime-checks.txt'
        $forestExit=RunProcess (Join-Path $release 'Play64\Forest\ForestWhitebox.exe') @('-batchmode','-nographics','-forestChecks','-forestReport',('"'+$forestReport+'"'),'-logFile',('"'+(Join-Path $run 'forest-runtime.log')+'"'))
        $status.forestExit=$forestExit
        if($forestExit -ne 0){throw 'Forest runtime checks failed'}
        if(!(Test-Path -LiteralPath $forestReport)){throw 'Forest player exited without a runtime report'}
        $status.result='PASS'
        return
    }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'StartWithLogs.ps1') -Destination (Join-Path $release 'Play64\StartWithLogs.ps1') -Force
    if($StartupPage){
        $status.phase='startup-runtime'
        $startupExit=RunProcess (Join-Path $release 'Play64\SeasideWhitebox.exe') @('-batchmode','-force-d3d11','-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-startupSmoke','-logFile',('"'+(Join-Path $run 'startup-runtime.log')+'"'))
        $status.startupExit=$startupExit
        if($startupExit -ne 0){throw 'Startup runtime checks failed'}
        $status.result='PASS'
        return
    }
} catch {$status.result='FAIL';$status.error=$_.Exception.Message;throw}
finally {
    $status.finished=Get-Date -Format o
    $status | ConvertTo-Json | Out-File -LiteralPath (Join-Path $run 'status.json') -Encoding utf8
    Write-Output ('LOG_FOLDER='+$run)
}
