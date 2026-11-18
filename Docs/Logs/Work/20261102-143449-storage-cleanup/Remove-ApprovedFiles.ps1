$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath('D:\UnityProjects\Demo')
$work=Join-Path $root 'Docs\Logs\Work\20261102-143449-storage-cleanup'
$plan=@(Get-Content -LiteralPath (Join-Path $work 'deletion-plan.json') -Raw | ConvertFrom-Json)
$summary=Get-Content -LiteralPath (Join-Path $work 'plan-summary.json') -Raw | ConvertFrom-Json
$manifest=@(Get-Content -LiteralPath (Join-Path $work 'protected-files-before.json') -Raw | ConvertFrom-Json)
$journal=Join-Path $work 'deletion-journal.jsonl'
$allowedDirectories=@(
 'Docs/Backups/20261030-170137-github-handoff/validation',
 'Docs/Backups/20261030-222238-bg3-camera-bounds/validation',
 'Docs/Backups/20261031-110419-camera-follow-size/validation',
 'Docs/Backups/20261031-161909-startup-page/validation',
 'Docs/Backups/20261031-163523-api-deprecation-cleanup/validation',
 'Docs/Backups/20261027-demo-migration-seaside-art/Play',
 'Docs/Backups/20261030-170137-github-handoff/archived/Play',
 'Docs/Backups/20261030-170137-github-handoff/before/Play64',
 'Docs/Backups/20261030-222238-bg3-camera-bounds/replaced-Play64',
 'Docs/Backups/20261030-222238-bg3-camera-bounds/before/Play64',
 'Docs/Backups/20261030-part1-backgrounds/Play64',
 'Docs/Backups/20261031-110419-camera-follow-size/before/Play64',
 'Docs/Backups/20261031-161909-startup-page/before/Play64',
 'Docs/Backups/20261031-163523-api-deprecation-cleanup/before/Play64',
 'Docs/Backups/20261029-hierarchy-refactor/Project6.4/Library',
 'Docs/Backups/20261030-170137-github-handoff/archived/Project/Library',
 'Play64','build/SeasideReview'
) | ForEach-Object {[IO.Path]::GetFullPath((Join-Path $root $_))}
$allowedRootZips=@('Seaside-Unity64-Startup-Project.zip','Seaside-Unity64-Startup-Windows.zip') |
    ForEach-Object {[IO.Path]::GetFullPath((Join-Path $root $_))}
$archiveRoot=[IO.Path]::GetFullPath((Join-Path $root 'Docs/Logs/Packaging'))
if((git -C $root rev-parse HEAD).Trim() -ne $summary.gitHead){throw 'Git HEAD changed since planning'}

# Recheck protected data before any irreversible removal.
foreach($record in $manifest){
    if(!(Test-Path -LiteralPath $record.path -PathType Leaf)){throw "Protected file missing: $($record.path)"}
    if((Get-Item -LiteralPath $record.path).Length -ne $record.bytes -or
       (Get-FileHash -LiteralPath $record.path -Algorithm SHA256).Hash -ne $record.sha256){throw "Protected file changed: $($record.path)"}
}
$running=Get-CimInstance Win32_Process
foreach($target in $plan){
    $resolved=(Resolve-Path -LiteralPath $target.path).ProviderPath.TrimEnd('\')
    if(!$resolved.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase)){throw "Outside workspace: $resolved"}
    $item=Get-Item -LiteralPath $resolved -Force
    if($item.PSIsContainer){
        if($resolved -notin $allowedDirectories){throw "Directory not whitelisted: $resolved"}
    } elseif($resolved -notin $allowedRootZips -and !($resolved.StartsWith($archiveRoot+'\',[StringComparison]::OrdinalIgnoreCase) -and $resolved.EndsWith('.zip',[StringComparison]::OrdinalIgnoreCase))){
        throw "File not whitelisted: $resolved"
    }
    $ancestor=$resolved
    while($ancestor.Length -ge $root.Length){
        if((Get-Item -LiteralPath $ancestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw "Reparse ancestor: $ancestor"}
        if($ancestor -eq $root){break}
        $ancestor=Split-Path $ancestor -Parent
    }
    $items=if($item.PSIsContainer){@(Get-ChildItem -LiteralPath $resolved -Recurse -Force)}else{@($item)}
    if(@($items | Where-Object {$_.Attributes -band [IO.FileAttributes]::ReparsePoint}).Count){throw "Reparse child: $resolved"}
    $files=@($items | Where-Object {!$_.PSIsContainer})
    if($files.Count -ne $target.fileCount -or [long](($files | Measure-Object Length -Sum).Sum) -ne $target.bytes){throw "Target content changed since plan: $resolved"}
    foreach($process in $running){
        if(($process.ExecutablePath -and $process.ExecutablePath.StartsWith($resolved+'\',[StringComparison]::OrdinalIgnoreCase)) -or
           ($process.Name -eq 'Unity.exe' -and $process.CommandLine -and $process.CommandLine.Replace('/','\').IndexOf($resolved,[StringComparison]::OrdinalIgnoreCase) -ge 0)){
            throw "Target currently in use: $resolved"
        }
    }
}

$deletedBytes=0L
$deletedTargets=0
foreach($target in $plan){
    # Validate the absolute literal path again immediately before deleting it.
    $resolved=(Resolve-Path -LiteralPath $target.path).ProviderPath.TrimEnd('\')
    if(!$resolved.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase)){throw "Unsafe final path: $resolved"}
    if($target.kind -eq 'directory' -and $resolved -notin $allowedDirectories){throw "Unsafe directory: $resolved"}
    @{at=(Get-Date -Format o);state='START';path=$resolved;bytes=$target.bytes} | ConvertTo-Json -Compress | Add-Content -LiteralPath $journal -Encoding utf8
    if($target.kind -eq 'directory'){Remove-Item -LiteralPath $resolved -Recurse -Force}
    else{Remove-Item -LiteralPath $resolved -Force}
    if(Test-Path -LiteralPath $resolved){throw "Deletion incomplete: $resolved"}
    $deletedBytes+=[long]$target.bytes;$deletedTargets++
    @{at=(Get-Date -Format o);state='DELETED';path=$resolved;bytes=$target.bytes} | ConvertTo-Json -Compress | Add-Content -LiteralPath $journal -Encoding utf8
}

$failures=[Collections.Generic.List[object]]::new()
foreach($record in $manifest){
    if(!(Test-Path -LiteralPath $record.path -PathType Leaf)){$failures.Add(@{path=$record.path;problem='MISSING'});continue}
    if((Get-Item -LiteralPath $record.path).Length -ne $record.bytes -or
       (Get-FileHash -LiteralPath $record.path -Algorithm SHA256).Hash -ne $record.sha256){$failures.Add(@{path=$record.path;problem='HASH_MISMATCH'})}
}
$afterFiles=@(Get-ChildItem -LiteralPath $root -Recurse -Force -File)
$result=[ordered]@{
 result=if($failures.Count -eq 0){'PASS'}else{'FAIL'}
 completed=(Get-Date -Format o)
 removedTargets=$deletedTargets
 removedFiles=[long]$summary.plannedFiles
 removedBytes=$deletedBytes
 totalBytesBefore=[long]$summary.totalBytesBefore
 totalBytesAfter=[long](($afterFiles | Measure-Object Length -Sum).Sum)
 protectedFilesVerified=$manifest.Count
 protectedFileFailures=@($failures.ToArray())
 mainProjectAndLatestGameUnchanged=($failures.Count -eq 0)
 mainProjectLibraryRetained=(Test-Path -LiteralPath (Join-Path $root 'Project6.4/Library'))
 latestBuildZipRetained=(Test-Path -LiteralPath (Join-Path $root 'build/FirstScene_Seaside.zip'))
 gitHead=(git -C $root rev-parse HEAD).Trim()
}
$result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $work 'cleanup-result.json') -Encoding utf8
$result | ConvertTo-Json -Depth 5
if($failures.Count){throw 'Protected data verification failed; inspect cleanup-result.json'}
