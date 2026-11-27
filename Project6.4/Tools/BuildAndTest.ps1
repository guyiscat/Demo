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
        $status.phase='campaign-runtime'
        $campaignReport=Join-Path $run 'campaign-runtime-checks.txt'
        $testSave=Join-Path $run 'test-save.json'
        $campaignExit=RunProcess (Join-Path $release 'Play64\SeasideWhitebox.exe') @('-batchmode','-nographics','-campaignChecks','-campaignSavePath',('"'+$testSave+'"'),'-campaignReport',('"'+$campaignReport+'"'),'-logFile',('"'+(Join-Path $run 'campaign-runtime.log')+'"'))
        $status.campaignExit=$campaignExit
        if($campaignExit -ne 0 -or !(Test-Path -LiteralPath $campaignReport)){throw 'Campaign progression or checkpoint persistence checks failed'}
        foreach($phase in @('write','read')){
            $probeReport=Join-Path $run ('save-probe-'+$phase+'.txt')
            $probeExit=RunProcess (Join-Path $release 'Play64\SeasideWhitebox.exe') @('-batchmode','-nographics','-saveProbe',$phase,'-campaignSavePath',('"'+$testSave+'"'),'-campaignReport',('"'+$probeReport+'"'),'-logFile',('"'+(Join-Path $run ('save-probe-'+$phase+'.log'))+'"'))
            $status[('saveProbe'+$phase+'Exit')]=$probeExit
            if($probeExit -ne 0 -or !(Test-Path -LiteralPath $probeReport)){throw 'Cross-process Continue checks failed'}
        }
        $mediaReport=Join-Path $run 'media-runtime-checks.txt'
        $mediaExit=RunProcess (Join-Path $release 'Play64\SeasideWhitebox.exe') @('-batchmode','-nographics','-mediaChecks','-mediaReport',('"'+$mediaReport+'"'),'-logFile',('"'+(Join-Path $run 'media-runtime.log')+'"'))
        $status.mediaExit=$mediaExit
        if($mediaExit -ne 0 -or !(Test-Path -LiteralPath $mediaReport)){throw 'Imported artwork or region music checks failed'}
        $checkpointReport=Join-Path $run 'checkpoint-runtime-checks.txt'
        $checkpointExit=RunProcess (Join-Path $release 'Play64\SeasideWhitebox.exe') @('-batchmode','-nographics','-checkpointChecks','-checkpointReport',('"'+$checkpointReport+'"'),'-logFile',('"'+(Join-Path $run 'checkpoint-runtime.log')+'"'))
        $status.checkpointExit=$checkpointExit
        if($checkpointExit -ne 0 -or !(Test-Path -LiteralPath $checkpointReport)){throw 'Automatic checkpoint trigger or safe respawn checks failed'}
        $respawnFolder=Join-Path $run 'respawn'
        $respawnExit=RunProcess (Join-Path $release 'Play64\SeasideWhitebox.exe') @('-batchmode','-nographics','-respawnChecks','-respawnReport',('"'+$respawnFolder+'"'),'-logFile',('"'+(Join-Path $run 'respawn-runtime.log')+'"'))
        $status.respawnExit=$respawnExit
        if($respawnExit -ne 0 -or !(Test-Path -LiteralPath (Join-Path $respawnFolder 'respawn-runtime-checks.txt'))){throw 'Thorn death / timed checkpoint revival checks failed'}
        $settingsFolder=Join-Path $run 'settings'
        $settingsExit=RunProcess (Join-Path $release 'Play64\SeasideWhitebox.exe') @('-batchmode','-nographics','-settingsChecks','-settingsReport',('"'+$settingsFolder+'"'),'-logFile',('"'+(Join-Path $run 'settings-runtime.log')+'"'))
        $status.settingsExit=$settingsExit
        if($settingsExit -ne 0 -or !(Test-Path -LiteralPath (Join-Path $settingsFolder 'settings-runtime-checks.txt'))){throw 'Settings artwork, bindings, brightness or sound checks failed'}
        $menuFolder=Join-Path $run 'menus'
        $menuExit=RunProcess (Join-Path $release 'Play64\SeasideWhitebox.exe') @('-batchmode','-nographics','-menuChecks','-campaignSavePath',('"'+(Join-Path $run 'menus-save.json')+'"'),'-menuReport',('"'+$menuFolder+'"'),'-logFile',('"'+(Join-Path $run 'menus-runtime.log')+'"'))
        $status.menuExit=$menuExit
        if($menuExit -ne 0 -or !(Test-Path -LiteralPath (Join-Path $menuFolder 'menu-runtime-checks.txt'))){throw 'Pause/help UI input isolation checks failed'}
        $newArtFolder=Join-Path $run 'new-art'
        $newArtExit=RunProcess (Join-Path $release 'Play64\SeasideWhitebox.exe') @('-batchmode','-nographics','-newArtChecks','-campaignSavePath',('"'+(Join-Path $run 'new-art-save.json')+'"'),'-newArtReport',('"'+$newArtFolder+'"'),'-logFile',('"'+(Join-Path $run 'new-art-runtime.log')+'"'))
        $status.newArtExit=$newArtExit
        if($newArtExit -ne 0 -or !(Test-Path -LiteralPath (Join-Path $newArtFolder 'new-art-runtime-checks.txt'))){throw 'New pause / artwork / portal / seaside skill checks failed'}
        foreach($phase in @('write','read')){
            $slotFolder=Join-Path $run ('slots-'+$phase)
            $slotArgs=@('-batchmode','-nographics','-campaignSavePath',('"'+(Join-Path $run 'slots-save.json')+'"'),'-slotReport',('"'+$slotFolder+'"'),'-logFile',('"'+(Join-Path $run ('slots-'+$phase+'.log'))+'"'))
            if($phase -eq 'write'){$slotArgs+='-slotChecks'}else{$slotArgs+=@('-slotProbe','read')}
            $slotExit=RunProcess (Join-Path $release 'Play64\SeasideWhitebox.exe') $slotArgs
            $status[('slots'+$phase+'Exit')]=$slotExit
            if($slotExit -ne 0 -or !(Test-Path -LiteralPath (Join-Path $slotFolder 'slot-runtime-checks.txt'))){throw ('Multi-slot save/load '+$phase+' checks failed')}
        }
        $status.result='PASS'
        return
    }
} catch {$status.result='FAIL';$status.error=$_.Exception.Message;throw}
finally {
    $status.finished=Get-Date -Format o
    $status | ConvertTo-Json | Out-File -LiteralPath (Join-Path $run 'status.json') -Encoding utf8
    Write-Output ('LOG_FOLDER='+$run)
}
