param([string[]]$AdditionalProjectRoots=@())
$ErrorActionPreference='Stop'
$release=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$target=Join-Path $release ('Docs\Logs\History\'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $target -Force | Out-Null
$records=New-Object 'System.Collections.Generic.List[object]'
$skipped=New-Object 'System.Collections.Generic.List[string]'
function Visit([string]$source,[string]$label,[string]$base) {
    foreach($entry in Get-ChildItem -LiteralPath $source -Force) {
        if($entry.PSIsContainer) {
            if($entry.Name -in @('Library','Temp','obj','.git','node_modules','Packages','Play','Windows','Builds','Logs','Backups')){continue}
            Visit $entry.FullName $label $base
        } elseif($entry.Extension -in @('.log','.md','.txt','.csv')) {
            if($entry.FullName -match '\\ProjectSettings\\'){continue}
            CopyRecord $entry $label $base
        }
    }
}
function CopyRecord($file,[string]$label,[string]$base) {
    $relative=$file.FullName.Substring($base.Length).TrimStart('\')
    $dest=Join-Path $target (Join-Path $label $relative)
    New-Item -ItemType Directory -Path (Split-Path $dest -Parent) -Force | Out-Null
    try {
        Copy-Item -LiteralPath $file.FullName -Destination $dest
        $stream=[IO.File]::OpenRead($dest);$sha=[Security.Cryptography.SHA256]::Create()
        try{$hash=([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-','')}
        finally{$stream.Dispose();$sha.Dispose()}
        $records.Add([pscustomobject]@{source=$file.FullName;archived=$dest;bytes=$file.Length;modified=$file.LastWriteTime.ToString('o');sha256=$hash})
    } catch {$skipped.Add($file.FullName+' :: '+$_.Exception.Message)}
}
foreach($folder in (@($release)+$AdditionalProjectRoots)) {
    if(!(Test-Path -LiteralPath $folder)){$skipped.Add('MISSING: '+$folder);continue}
    Visit $folder (Split-Path $folder -Leaf) $folder
    $unityLogs=Join-Path $folder 'Project\Logs'
    if(Test-Path -LiteralPath $unityLogs){Get-ChildItem -LiteralPath $unityLogs -Recurse -File -Filter '*.log' | ForEach-Object {CopyRecord $_ (Split-Path $folder -Leaf) $folder}}
}
$records | Export-Csv -LiteralPath (Join-Path $target 'manifest.csv') -Encoding UTF8 -NoTypeInformation
$skipped | Out-File -LiteralPath (Join-Path $target 'unavailable.txt') -Encoding UTF8
Write-Output ('ARCHIVE='+$target+' FILES='+$records.Count+' UNAVAILABLE='+$skipped.Count)
