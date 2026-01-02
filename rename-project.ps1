# Rename MechCommander to MechanizedArmourCommander
# Run this script after closing Visual Studio and any file locks

Write-Host "Mechanized Armour Commander - Project Rename Script" -ForegroundColor Green
Write-Host "====================================================" -ForegroundColor Green
Write-Host ""

# Check if old directories exist
if (-not (Test-Path "src\MechCommander.UI")) {
    Write-Host "Project already renamed or directories not found!" -ForegroundColor Yellow
    exit
}

Write-Host "Step 1: Renaming project directories..." -ForegroundColor Cyan
Rename-Item -Path "src\MechCommander.UI" -NewName "MechanizedArmourCommander.UI"
Rename-Item -Path "src\MechCommander.Core" -NewName "MechanizedArmourCommander.Core"
Rename-Item -Path "src\MechCommander.Data" -NewName "MechanizedArmourCommander.Data"

Write-Host "Step 2: Renaming .csproj files..." -ForegroundColor Cyan
Rename-Item -Path "src\MechanizedArmourCommander.UI\MechCommander.UI.csproj" -NewName "MechanizedArmourCommander.UI.csproj"
Rename-Item -Path "src\MechanizedArmourCommander.Core\MechCommander.Core.csproj" -NewName "MechanizedArmourCommander.Core.csproj"
Rename-Item -Path "src\MechanizedArmourCommander.Data\MechCommander.Data.csproj" -NewName "MechanizedArmourCommander.Data.csproj"

Write-Host "Step 3: Updating .csproj content..." -ForegroundColor Cyan
# Update UI project references
$uiProj = Get-Content "src\MechanizedArmourCommander.UI\MechanizedArmourCommander.UI.csproj"
$uiProj = $uiProj -replace 'MechCommander', 'MechanizedArmourCommander'
Set-Content "src\MechanizedArmourCommander.UI\MechanizedArmourCommander.UI.csproj" $uiProj

# Update Core project
$coreProj = Get-Content "src\MechanizedArmourCommander.Core\MechanizedArmourCommander.Core.csproj"
$coreProj = $coreProj -replace 'MechCommander', 'MechanizedArmourCommander'
Set-Content "src\MechanizedArmourCommander.Core\MechanizedArmourCommander.Core.csproj" $coreProj

# Update Data project
$dataProj = Get-Content "src\MechanizedArmourCommander.Data\MechanizedArmourCommander.Data.csproj"
$dataProj = $dataProj -replace 'MechCommander', 'MechanizedArmourCommander'
Set-Content "src\MechanizedArmourCommander.Data\MechanizedArmourCommander.Data.csproj" $dataProj

Write-Host "Step 4: Updating namespaces in C# files..." -ForegroundColor Cyan
Get-ChildItem -Path "src" -Filter "*.cs" -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName
    $newContent = $content -replace 'MechCommander', 'MechanizedArmourCommander'
    Set-Content $_.FullName $newContent
}

Write-Host "Step 5: Updating XAML files..." -ForegroundColor Cyan
Get-ChildItem -Path "src" -Filter "*.xaml" -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName
    $newContent = $content -replace 'MechCommander', 'MechanizedArmourCommander'
    Set-Content $_.FullName $newContent
}

Write-Host "Step 6: Cleaning build artifacts..." -ForegroundColor Cyan
Get-ChildItem -Path "src" -Directory -Recurse | Where-Object { $_.Name -eq "bin" -or $_.Name -eq "obj" } | Remove-Item -Recurse -Force

Write-Host ""
Write-Host "Rename complete!" -ForegroundColor Green
Write-Host "You can now open MechanizedArmourCommander.sln in Visual Studio" -ForegroundColor Green
