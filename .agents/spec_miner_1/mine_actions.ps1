$gameBin = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client"
Add-Type -Path "$gameBin\TaleWorlds.Library.dll"
Add-Type -Path "$gameBin\TaleWorlds.Core.dll"
Add-Type -Path "$gameBin\TaleWorlds.CampaignSystem.dll"

# 1. Inspect Actions in TaleWorlds.CampaignSystem.Actions
Write-Host "=== ACTIONS IN TaleWorlds.CampaignSystem.Actions ==="
$actionTypes = [System.AppDomain]::CurrentDomain.GetAssemblies() | 
    ForEach-Object { $_.GetTypes() } | 
    Where-Object { $_.Namespace -eq 'TaleWorlds.CampaignSystem.Actions' -and $_.IsPublic }

foreach ($at in $actionTypes) {
    $methods = $at.GetMethods([System.Reflection.BindingFlags]'Public,Static') | Where-Object { $_.Name -like "*Apply*" }
    foreach ($m in $methods) {
        $pParams = ($m.GetParameters() | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ", "
        Write-Host "$($at.Name).$($m.Name)($pParams)"
    }
}
