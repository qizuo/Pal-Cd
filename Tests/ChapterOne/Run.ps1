param([string]$Unity = 'D:/unity-1122-2/2022.3.62-1/Editor/Unity.exe', [switch]$SaveGame, [switch]$Skill)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$validationRoot = Join-Path $repo 'Temp/ChapterOneValidation'
if (Get-Process Unity -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $Unity }) {
    Write-Output 'Ensure this isolated validation project is not already open in Unity.'
}
New-Item -ItemType Directory -Force "$validationRoot/Assets/Editor", "$validationRoot/Packages", "$validationRoot/ProjectSettings" | Out-Null
Copy-Item "$repo/Assets/HouyiPrologue" "$validationRoot/Assets" -Recurse -Force
New-Item -ItemType Directory -Force "$validationRoot/Assets/ArcheryDemo", "$validationRoot/Assets/Standard Assets/Character Controllers", "$validationRoot/Assets/Modles/Npc", "$validationRoot/Assets/Modles/Enemy" | Out-Null
Get-ChildItem "$repo/Assets/ArcheryDemo" -File | Copy-Item -Destination "$validationRoot/Assets/ArcheryDemo" -Force
Copy-Item "$repo/Assets/Standard Assets/Character Controllers/*" "$validationRoot/Assets/Standard Assets/Character Controllers" -Recurse -Force
Copy-Item "$repo/Assets/Modles/Enemy/fox" "$validationRoot/Assets/Modles/Enemy" -Recurse -Force
foreach ($name in @('LaoTou', 'ShuSheng')) { Copy-Item "$repo/Assets/Modles/Npc/$name*" "$validationRoot/Assets/Modles/Npc" -Recurse -Force }
Copy-Item "$repo/Assets/Modles/Npc/Materials" "$validationRoot/Assets/Modles/Npc" -Recurse -Force
Copy-Item "$repo/Assets/MSYH.TTF", "$repo/Assets/MSYH.TTF.meta" "$validationRoot/Assets" -Force
New-Item -ItemType Directory -Force "$validationRoot/Assets/Resources" | Out-Null
Copy-Item "$repo/Assets/Resources/Houyi" "$validationRoot/Assets/Resources" -Recurse -Force
foreach ($entry in @(
    @('DynamicElements_Effects/materials', @('Ring.png', 'tribal_runes_circle_blue.png', 'AnimatedLineBlue.png')),
    @('FX Explosion/Texture', @('FXExplo1.tga', 'FXFlash.tga', 'FXSpark.tga', 'FXSmoke1.tga'))
)) {
    $folder = $entry[0]
    New-Item -ItemType Directory -Force "$validationRoot/Assets/$folder" | Out-Null
    foreach ($texture in $entry[1]) { Copy-Item "$repo/Assets/$folder/$texture", "$repo/Assets/$folder/$texture.meta" "$validationRoot/Assets/$folder" -Force }
}
Copy-Item "$repo/ProjectSettings/ProjectVersion.txt", "$repo/ProjectSettings/InputManager.asset" "$validationRoot/ProjectSettings" -Force
$manifest = Get-Content "$repo/Packages/manifest.json" -Raw | ConvertFrom-Json
$deps = [ordered]@{}
$manifest.dependencies.PSObject.Properties | Where-Object { $_.Name -like 'com.unity.modules.*' } | ForEach-Object { $deps[$_.Name] = $_.Value }
@{ dependencies = $deps } | ConvertTo-Json -Depth 4 | Set-Content "$validationRoot/Packages/manifest.json" -Encoding UTF8
Copy-Item "$PSScriptRoot/ChapterOneValidation.cs" "$validationRoot/Assets/Editor" -Force
foreach ($name in @('ChapterOneProbe.cs', 'HouyiSaveProbe.cs', 'SunburstProbe.cs')) {
    $oldProbe = Join-Path $validationRoot "Assets/$name"
    if (Test-Path $oldProbe) { Remove-Item -LiteralPath $oldProbe }
}
if ($Skill) { Copy-Item "$PSScriptRoot/../PlayerControls/SunburstProbe.cs" "$validationRoot/Assets" -Force }
elseif ($SaveGame) { Copy-Item "$PSScriptRoot/../SaveGame/HouyiSaveProbe.cs" "$validationRoot/Assets" -Force }
else { Copy-Item "$PSScriptRoot/ChapterOneProbe.cs" "$validationRoot/Assets" -Force }
$resultName = if ($Skill) { 'Sunburst-Validation.txt' } elseif ($SaveGame) { 'SaveGame-Validation.txt' } else { 'ChapterOne-Validation.txt' }
$result = Join-Path $validationRoot $resultName
if (Test-Path $result) { Remove-Item -LiteralPath $result }
$process = Start-Process -FilePath $Unity -ArgumentList @('-batchmode', '-projectPath', ('"' + $validationRoot + '"'), '-executeMethod', 'ChapterOneValidation.Run', '-logFile', ('"' + $validationRoot + '/validation.log"')) -WindowStyle Hidden -PassThru
Write-Output "Validation started (PID $($process.Id)). Results: $result; details: validation.log"
