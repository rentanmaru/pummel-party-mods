<#
  配布用zipを作るスクリプト（Thunderstore形式。GitHub Releaseにもそのまま使う）
  使い方:
    powershell -ExecutionPolicy Bypass -File tools\package.ps1 -Project PummelHello
  出力:
    dist\<Project>-<version>.zip
#>
param(
    [string]$Project = "PummelHello",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$projDir = Join-Path $root "src\$Project"
$tsDir = Join-Path $root "thunderstore\$Project"
$distDir = Join-Path $root "dist"

# 1. ビルド（ゲームの plugins フォルダへのコピーも同時に行われる）
dotnet build (Join-Path $projDir "$Project.csproj") -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "ビルドに失敗しました" }

# 2. 必須ファイルがそろっているか確認
foreach ($name in @("manifest.json", "icon.png", "README.md")) {
    if (-not (Test-Path (Join-Path $tsDir $name))) { throw "$tsDir に $name がありません" }
}

# 3. manifest.json のバージョンと DLL のバージョンが一致しているか確認
$manifest = Get-Content (Join-Path $tsDir "manifest.json") -Raw -Encoding UTF8 | ConvertFrom-Json
$version = $manifest.version_number
$dll = Join-Path $projDir "bin\$Configuration\$Project.dll"
$dllVersion = ([version](Get-Item $dll).VersionInfo.FileVersion).ToString(3)
if ($dllVersion -ne $version) {
    throw "バージョン不一致: manifest.json=$version / DLL=$dllVersion（csproj の <Version> を合わせてください）"
}

# 4. zip を作る（zip内のパスは「/」区切り。Compress-Archive は「\」になるので使わない）
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
New-Item -ItemType Directory -Force $distDir | Out-Null
$zipPath = Join-Path $distDir "$Project-$version.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath }

$entries = [ordered]@{
    "manifest.json"                        = Join-Path $tsDir "manifest.json"
    "icon.png"                             = Join-Path $tsDir "icon.png"
    "README.md"                            = Join-Path $tsDir "README.md"
    "CHANGELOG.md"                         = Join-Path $tsDir "CHANGELOG.md"
    "BepInEx/plugins/$Project/$Project.dll" = $dll
}

$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($entry in $entries.GetEnumerator()) {
        if (Test-Path $entry.Value) {
            [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $entry.Value, $entry.Key)
        }
    }
}
finally {
    $zip.Dispose()
}

Write-Host "作成しました: $zipPath"
