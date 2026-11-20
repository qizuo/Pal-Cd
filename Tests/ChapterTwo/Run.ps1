param([string]$Unity = 'D:/unity-1122-2/2022.3.62-1/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$validation = Join-Path $repo 'Temp/ChapterTwoValidation'
$project = Join-Path $validation 'Project'
New-Item -ItemType Directory -Force "$project/Assets/Editor", "$project/Assets/HouyiPrologue", "$project/Assets/ArcheryDemo", "$project/Assets/Standard Assets/Character Controllers", "$project/Assets/Resources", "$project/Packages", "$project/ProjectSettings" | Out-Null
foreach ($folder in @('Scripts', 'Art')) { Copy-Item "$repo/Assets/HouyiPrologue/$folder" "$project/Assets/HouyiPrologue" -Recurse -Force }
Get-ChildItem "$repo/Assets/ArcheryDemo" -File | Copy-Item -Destination "$project/Assets/ArcheryDemo" -Force
Copy-Item "$repo/Assets/HouyiAppearance" "$project/Assets" -Recurse -Force
Copy-Item "$repo/Assets/Standard Assets/Character Controllers/*" "$project/Assets/Standard Assets/Character Controllers" -Recurse -Force
Copy-Item "$repo/Assets/Resources/Houyi" "$project/Assets/Resources" -Recurse -Force
foreach ($entry in @(
    @('DynamicElements_Effects/materials', @('Ring.png', 'tribal_runes_circle_blue.png', 'AnimatedLineBlue.png')),
    @('FX Explosion/Texture', @('FXExplo1.tga', 'FXFlash.tga', 'FXSpark.tga', 'FXSmoke1.tga'))
)) {
    $folder = $entry[0]
    New-Item -ItemType Directory -Force "$project/Assets/$folder" | Out-Null
    foreach ($texture in $entry[1]) { Copy-Item "$repo/Assets/$folder/$texture", "$repo/Assets/$folder/$texture.meta" "$project/Assets/$folder" -Force }
}
Copy-Item "$repo/ProjectSettings/ProjectVersion.txt", "$repo/ProjectSettings/InputManager.asset" "$project/ProjectSettings" -Force
$manifest = Get-Content "$repo/Packages/manifest.json" -Raw | ConvertFrom-Json
$deps = [ordered]@{}
$manifest.dependencies.PSObject.Properties | Where-Object { $_.Name -like 'com.unity.modules.*' } | ForEach-Object { $deps[$_.Name] = $_.Value }
@{ dependencies = $deps } | ConvertTo-Json -Depth 4 | Set-Content "$project/Packages/manifest.json" -Encoding UTF8
Copy-Item "$PSScriptRoot/ChapterTwoChecks.cs" "$project/Assets/Editor" -Force
Copy-Item "$PSScriptRoot/ChapterThreeChecks.cs" "$project/Assets/Editor" -Force
$process = Start-Process -FilePath $Unity -ArgumentList @('-batchmode', '-projectPath', ('"' + $project + '"'), '-executeMethod', 'ChapterTwoChecks.Run', '-logFile', ('"' + $validation + '/validation.log"')) -WindowStyle Hidden -PassThru
Write-Output "Validation started (PID $($process.Id)). Result: $validation/chapter-two-result.txt"
