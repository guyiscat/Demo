$ErrorActionPreference='Stop'
$demoRoot=Split-Path (Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent) -Parent
$game=Join-Path $demoRoot 'Play64\SeasideWhitebox.exe'
$cases=@(
    @{name='preview-regression';flags=@('-artPreview','-regression')},
    @{name='preview-calibration';flags=@('-artPreview','-calibrate')},
    @{name='original-camera-smoke';flags=@('-cameraPreview','-smokeTest')},
    @{name='preview-camera-smoke';flags=@('-artPreview','-cameraPreview','-smokeTest')},
    @{name='original-trail-smoke';flags=@('-trailPreview','-smokeTest')},
    @{name='preview-trail-smoke';flags=@('-artPreview','-trailPreview','-smokeTest')}
)
$results=@()
foreach($case in $cases){
    $log=Join-Path $PSScriptRoot ($case.name+'.log')
    $arguments=@('-batchmode','-nographics')+$case.flags+@('-logFile',('"'+$log+'"'))
    $process=Start-Process -FilePath $game -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.Handle | Out-Null
    if(!$process.WaitForExit(90000)){$process.Kill();throw ('Case timed out: '+$case.name)}
    $code=$process.ExitCode
    $content=Get-Content -LiteralPath $log -Raw
    $errors=$content -match '(?m)^(NullReferenceException|InvalidOperationException|MissingReferenceException|FAIL )'
    if($code -ne 0 -or $errors){throw ('Case failed: '+$case.name+' exit='+$code)}
    if($case.name -match 'camera|trail'){
        $expectedEnd=if($case.name -match 'trail'){'SEASIDE_TRAIL_PREVIEW_FINISHED'}else{'SEASIDE_SMOKE_PASS'}
        if($content -notmatch 'SEASIDE_PREVIEW_MAP_HIDDEN' -or $content -notmatch $expectedEnd){throw ('Preview evidence missing: '+$case.name)}
    }
    $results+=[pscustomobject]@{case=$case.name;exit=$code;result='PASS';log=$log}
    if($case.name -eq 'preview-regression'){
        Copy-Item -LiteralPath (Join-Path $demoRoot 'Docs\standalone-playtest-regression.txt') -Destination (Join-Path $PSScriptRoot 'preview-runtime-regression.txt')
    }
    if($case.name -eq 'preview-calibration'){
        Copy-Item -LiteralPath (Join-Path $demoRoot 'Docs\standalone-calibration.txt') -Destination (Join-Path $PSScriptRoot 'preview-runtime-calibration.txt')
    }
}
$results | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'preview-runtime-status.json') -Encoding utf8
$results | ConvertTo-Json -Depth 4
