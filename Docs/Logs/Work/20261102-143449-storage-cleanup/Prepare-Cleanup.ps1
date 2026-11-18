$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath('D:\UnityProjects\Demo')
$work=Join-Path $root 'Docs\Logs\Work\20261102-143449-storage-cleanup'
$backup=Join-Path $root 'Docs\Backups\20261102-143449-storage-cleanup'
$directories=@(
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
 'Play64',
 'build/SeasideReview'
)
$protected=@(
 'Project6.4','build/FirstScene_Seaside','build/FirstScene_Seaside.zip',
 '.git','maps','statgame.cmd','Docs/Logs/Work','Docs/Logs/Build','Docs/Logs/History',
 'Docs/Backups/20261031-111631-recover-unsaved-scene',
 'Docs/Backups/20261101-121507-exclude-build-push'
) | ForEach-Object {[IO.Path]::GetFullPath((Join-Path $root $_))}

function Check-Target([string]$candidate) {
    $resolved=(Resolve-Path -LiteralPath $candidate).ProviderPath.TrimEnd('\')
    if(!$resolved.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase)){throw "Outside Demo: $resolved"}
    foreach($keep in $protected){
        if($resolved.Equals($keep,[StringComparison]::OrdinalIgnoreCase) -or
           $resolved.StartsWith($keep+'\',[StringComparison]::OrdinalIgnoreCase) -or
           $keep.StartsWith($resolved+'\',[StringComparison]::OrdinalIgnoreCase)) {throw "Protected target overlap: $resolved"}
    }
    $ancestor=$resolved
    while($ancestor.Length -ge $root.Length){
        if((Get-Item -LiteralPath $ancestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw "Reparse point: $ancestor"}
        if($ancestor -eq $root){break}
        $ancestor=Split-Path $ancestor -Parent
    }
    return $resolved
}

$targets=[Collections.Generic.List[object]]::new()
foreach($relative in $directories){
    $path=Join-Path $root $relative
    if(Test-Path -LiteralPath $path){$targets.Add([pscustomobject]@{path=(Check-Target $path);kind='directory'})}
}
$zipPaths=@(Get-ChildItem -LiteralPath (Join-Path $root 'Docs/Logs/Packaging') -File -Recurse -Filter '*.zip')
foreach($name in @('Seaside-Unity64-Startup-Project.zip','Seaside-Unity64-Startup-Windows.zip')){
    $path=Join-Path $root $name
    if(Test-Path -LiteralPath $path){$zipPaths+=Get-Item -LiteralPath $path}
}
foreach($file in $zipPaths){$targets.Add([pscustomobject]@{path=(Check-Target $file.FullName);kind='file'})}

$running=Get-CimInstance Win32_Process
foreach($target in $targets){
    foreach($process in $running){
        if(($process.ExecutablePath -and $process.ExecutablePath.StartsWith($target.path+'\',[StringComparison]::OrdinalIgnoreCase)) -or
           ($process.Name -eq 'Unity.exe' -and $process.CommandLine -and $process.CommandLine.Replace('/','\').IndexOf($target.path,[StringComparison]::OrdinalIgnoreCase) -ge 0)){
            throw "Target is in use by process $($process.ProcessId): $($target.path)"
        }
    }
}

$removedFiles=[Collections.Generic.List[object]]::new()
foreach($target in $targets){
    $items=if($target.kind -eq 'directory'){@(Get-ChildItem -LiteralPath $target.path -Recurse -Force)}else{@(Get-Item -LiteralPath $target.path -Force)}
    if(@($items | Where-Object {$_.Attributes -band [IO.FileAttributes]::ReparsePoint}).Count){throw "Reparse point inside $($target.path)"}
    $files=@($items | Where-Object {!$_.PSIsContainer})
    $bytes=($files | Measure-Object Length -Sum).Sum
    $target | Add-Member bytes ([long]$bytes)
    $target | Add-Member fileCount $files.Count
    foreach($file in $files){$removedFiles.Add([pscustomobject]@{path=$file.FullName;bytes=$file.Length;target=$target.path})}
}
$allFiles=@(Get-ChildItem -LiteralPath $root -Recurse -Force -File)
$totalBefore=($allFiles | Measure-Object Length -Sum).Sum
$targets | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $work 'deletion-plan.json') -Encoding utf8
$removedFiles | Export-Csv -LiteralPath (Join-Path $work 'deletion-file-inventory.csv') -NoTypeInformation -Encoding utf8

# Protect every remaining file except known mutable caches/logs and this task's own records.
$skipPrefixes=@('.git','Project6.4/Library','Project6.4/Temp','Project6.4/Logs','Project6.4/UserSettings','Docs/Logs/Runtime','build/Docs/Logs/Runtime') |
    ForEach-Object {[IO.Path]::GetFullPath((Join-Path $root $_))}
$skipPrefixes+=@($work,$backup)
$manifest=[Collections.Generic.List[object]]::new()
foreach($file in $allFiles){
    $path=$file.FullName
    $skip=$path -eq (Join-Path $root 'Docs/CHANGELOG.md')
    foreach($target in $targets){if($path -eq $target.path -or $path.StartsWith($target.path+'\',[StringComparison]::OrdinalIgnoreCase)){$skip=$true;break}}
    if($skip){continue}
    foreach($prefix in $skipPrefixes){if($path -eq $prefix -or $path.StartsWith($prefix+'\',[StringComparison]::OrdinalIgnoreCase)){$skip=$true;break}}
    if($skip){continue}
    $manifest.Add([pscustomobject]@{path=$path;bytes=$file.Length;sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash})
}
$manifest | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $work 'protected-files-before.json') -Encoding utf8
$tracked=@(git -C $root ls-files)
if($LASTEXITCODE -ne 0){throw 'Cannot read tracked files'}
foreach($relative in $tracked){
    $path=[IO.Path]::GetFullPath((Join-Path $root $relative))
    foreach($target in $targets){if($path -eq $target.path -or $path.StartsWith($target.path+'\',[StringComparison]::OrdinalIgnoreCase)){throw "Tracked deletion target: $relative"}}
}
$summary=[ordered]@{totalBytesBefore=[long]$totalBefore;plannedBytes=[long](($targets | Measure-Object bytes -Sum).Sum);targets=$targets.Count;plannedFiles=$removedFiles.Count;protectedFiles=$manifest.Count;gitHead=(git -C $root rev-parse HEAD).Trim()}
$summary | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $work 'plan-summary.json') -Encoding utf8
$summary | ConvertTo-Json
