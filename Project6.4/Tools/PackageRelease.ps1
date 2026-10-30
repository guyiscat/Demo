$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$release=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$run=Join-Path $release ('Docs\Logs\Packaging\'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
function Pack([string]$name,[string[]]$parts) {
    $path=Join-Path $run $name
    $stream=[IO.File]::Open($path,[IO.FileMode]::CreateNew)
    $zip=New-Object IO.Compression.ZipArchive($stream,[IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach($part in $parts) {
            $source=Join-Path $release $part;$item=Get-Item -LiteralPath $source
            $files=if($item.PSIsContainer){Get-ChildItem -LiteralPath $source -Recurse -File}else{@($item)}
            foreach($f in $files) {
                if($f.Name -like '~$*'){continue} # Excel lock files are not map source files.
                $relative=$f.FullName.Substring($release.Length+1).Replace('\','/')
                if($relative -match '^Docs/(Backups|Logs/(Packaging|Runtime))/'){continue}
                if($relative -match '(^|/)Assets/_Recovery(/|\.meta$)'){continue}
                if($f.Extension -in @('.zip','.7z','.unitypackage')){continue}
                # Snapshot logs even while the reopened editor is appending to them.
                # The original files are only read, and bytes appended after opening are excluded.
                $sourceStream=[IO.File]::Open($f.FullName,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
                try {
                    $remaining=$sourceStream.Length
                    $entry=$zip.CreateEntry($relative,[IO.Compression.CompressionLevel]::Optimal)
                    $entryStream=$entry.Open()
                    try {
                        $buffer=New-Object byte[] 65536
                        while($remaining -gt 0){
                            $read=$sourceStream.Read($buffer,0,[int][Math]::Min($buffer.Length,$remaining))
                            if($read -eq 0){break}
                            $entryStream.Write($buffer,0,$read);$remaining-=$read
                        }
                        if($remaining -ne 0){throw ('Source changed while packaging: '+$f.FullName)}
                    }finally{$entryStream.Dispose()}
                }finally{$sourceStream.Dispose()}
            }
        }
    }finally{$zip.Dispose();$stream.Dispose()}
    $check=[IO.Compression.ZipFile]::OpenRead($path)
    try {
        if(@($check.Entries | Where-Object {$_.FullName -match '(^|/)(Backups|Library|Temp|UserSettings|Packaging)/'}).Count){throw 'Unexpected cache, backup, or recursive archive entry'}
        $records=@($check.Entries | ForEach-Object {[pscustomobject]@{entry=$_.FullName;bytes=$_.Length}})
        $records | Export-Csv -LiteralPath (Join-Path $run ($name+'.entries.csv')) -NoTypeInformation -Encoding utf8
    }finally{$check.Dispose()}
    $old=Join-Path $release $name
    if(Test-Path -LiteralPath $old){Copy-Item -LiteralPath $old -Destination (Join-Path $run ('previous-'+$name))}
    Copy-Item -LiteralPath $path -Destination $old -Force
    Write-Output $path
}
$projectName=Split-Path (Split-Path $PSScriptRoot -Parent) -Leaf
Pack 'Seaside-Unity64-Hierarchy-Windows.zip' @('Play64','StartGame64.cmd','ArtPreview64.cmd','CameraPreview64.cmd','README.md','README-Unity64.md','CONTRIBUTING.md','Docs')
Pack 'Seaside-Unity64-Hierarchy-Project.zip' @("$projectName\Assets","$projectName\Packages","$projectName\ProjectSettings","$projectName\Tools","$projectName\AGENTS.md","$projectName\.gitignore",'AGENTS.md','.gitignore','.gitattributes','Figures','maps','StartGame64.cmd','ArtPreview64.cmd','CameraPreview64.cmd','README.md','README-Unity64.md','CONTRIBUTING.md','Docs')
