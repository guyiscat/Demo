param([string[]]$GameArgs,[switch]$ArtPreview)
$ErrorActionPreference='Stop'
$release=Split-Path $PSScriptRoot -Parent
$folder=Join-Path $release 'Docs\Logs\Runtime'
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$log=Join-Path $folder ((Get-Date -Format 'yyyyMMdd-HHmmss-fff')+'-engine.log')
if($ArtPreview){$GameArgs=@('-artPreview')+@($GameArgs)}
& (Join-Path $PSScriptRoot 'SeasideWhitebox.exe') -logFile $log @GameArgs | Out-Null
exit $LASTEXITCODE
