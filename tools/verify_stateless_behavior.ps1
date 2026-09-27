param(
    [string]$Workspace = (Get-Item (Join-Path $PSScriptRoot "..")).FullName
)

$ErrorActionPreference = 'Stop'
Write-Host "=== Stateless CampaignBehavior Acceptance Verification ===" -ForegroundColor Cyan
Write-Host "Target Workspace: $Workspace" -ForegroundColor Gray

# 1. Compilation Verification
Write-Host "`n[1/4] Checking Release build of CalradiaForge.sln..." -ForegroundColor Yellow
Push-Location $Workspace
try {
    dotnet build CalradiaForge.sln -c Release -v:minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Release build failed with exit code $LASTEXITCODE."
        exit 1
    }
    Write-Host "  [OK] CalradiaForge.sln compiled successfully with 0 errors." -ForegroundColor Green
}
finally {
    Pop-Location
}

# 2. Statelessness Check
Write-Host "`n[2/4] Checking for zero SaveableTypeDefiner and stateless SyncData..." -ForegroundColor Yellow
$modDir = Join-Path $Workspace "src/CalradiaForge.Mod"
if (-not (Test-Path $modDir)) {
    Write-Error "CalradiaForge.Mod directory not found at $modDir"
    exit 1
}

$saveableTypes = Get-ChildItem -Path $modDir -Recurse -Filter *.cs | Select-String -Pattern ':\s*SaveableTypeDefiner\b'
if ($saveableTypes) {
    Write-Error "Found SaveableTypeDefiner inheritance:`n$($saveableTypes | Out-String)"
    exit 1
}

$cbDir = Join-Path $modDir "CampaignBehaviors"
if (-not (Test-Path $cbDir)) {
    Write-Error "CampaignBehaviors directory not found at $cbDir"
    exit 1
}

$behaviorFiles = Get-ChildItem -Path $cbDir -Filter *.cs
if (-not $behaviorFiles -or $behaviorFiles.Count -eq 0) {
    Write-Error "No behavior files found in $cbDir"
    exit 1
}

foreach ($file in $behaviorFiles) {
    $content = Get-Content $file.FullName -Raw
    if ($content -match 'public\s+override\s+void\s+SyncData\s*\(\s*IDataStore\s+(\w+)\s*\)\s*\{([\s\S]*?)\}') {
        $paramName = $Matches[1]
        $body = $Matches[2]
        if ($body -match "$paramName\s*\.\s*SyncData\b") {
            Write-Error "SyncData in $($file.Name) contains dataStore.SyncData call: $body"
            exit 1
        }
    } else {
        Write-Error "SyncData(IDataStore) override missing or malformed in $($file.Name)"
        exit 1
    }

    if ($content -match '\[\s*Saveable(Field|Property)') {
        Write-Error "Found [SaveableField] or [SaveableProperty] in $($file.Name)"
        exit 1
    }
}
Write-Host "  [OK] Zero SaveableTypeDefiner and zero SyncData serialization verified across $($behaviorFiles.Count) behavior file(s)." -ForegroundColor Green

# 3. Anti-Shadowing Check
Write-Host "`n[3/4] Checking GEMINI.md Anti-Shadowing constraints..." -ForegroundColor Yellow
$badFolders = Get-ChildItem -Path $modDir -Recurse -Directory | Where-Object { $_.Name -eq "Campaign" }
if ($badFolders) {
    Write-Error "Forbidden folder 'Campaign' found: $($badFolders.FullName)"
    exit 1
}

$badNamespaces = Get-ChildItem -Path $modDir -Recurse -Filter *.cs | Select-String -Pattern '\bnamespace\s+([A-Za-z0-9_\.]*\.)?Campaign\b(\s*;|\s*\{|$)'
if ($badNamespaces) {
    Write-Error "Forbidden namespace ending in Campaign found:`n$($badNamespaces | Out-String)"
    exit 1
}

$badClasses = Get-ChildItem -Path $modDir -Recurse -Filter *.cs | Select-String -Pattern '\bclass\s+Campaign\b'
if ($badClasses) {
    Write-Error "Forbidden class named 'Campaign' found:`n$($badClasses | Out-String)"
    exit 1
}
Write-Host "  [OK] Anti-shadowing verified: zero folders, namespaces, or classes named 'Campaign'." -ForegroundColor Green

# 4. SubModule Registration Check
Write-Host "`n[4/4] Checking SubModule.OnGameStart registration..." -ForegroundColor Yellow
$subModuleFile = Join-Path $modDir "SubModule.cs"
if (-not (Test-Path $subModuleFile)) {
    Write-Error "SubModule.cs not found at $subModuleFile"
    exit 1
}

$subContent = Get-Content $subModuleFile -Raw
if ($subContent -match 'protected\s+override\s+void\s+OnGameStart\s*\([^)]*\)\s*\{([\s\S]*?)\n\s*\}') {
    $methodBody = $Matches[1]
    $hasAddBehavior = ($methodBody -match 'campaignStarter\.AddBehavior\s*\(\s*new\s+(?:[A-Za-z0-9_\.]+\.)?ClanCharacterProgressionBehavior\s*\(\s*\)\s*\);') -or
                      ($methodBody -match 'campaignStarter\.AddBehavior\s*\(\s*new\s+[\w\.]*Behavior\s*\(\s*\)\s*\);')
    if (-not $hasAddBehavior) {
        Write-Error "campaignStarter.AddBehavior(...) call not found in SubModule.OnGameStart:`n$methodBody"
        exit 1
    }
} else {
    Write-Error "OnGameStart method not found in SubModule.cs"
    exit 1
}
Write-Host "  [OK] SubModule properly registers CampaignBehavior in OnGameStart via AddBehavior()." -ForegroundColor Green

Write-Host "`n=======================================================" -ForegroundColor Cyan
Write-Host "SUCCESS: All architectural and acceptance criteria passed!" -ForegroundColor Green
Write-Host "=======================================================" -ForegroundColor Cyan
exit 0
