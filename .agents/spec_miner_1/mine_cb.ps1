$gameBin = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client"
Add-Type -Path "$gameBin\TaleWorlds.Library.dll"
Add-Type -Path "$gameBin\TaleWorlds.Core.dll"
Add-Type -Path "$gameBin\TaleWorlds.CampaignSystem.dll"

$cb = [TaleWorlds.CampaignSystem.CampaignBehaviors.CompanionsCampaignBehavior]
$methods = $cb.GetMethods([System.Reflection.BindingFlags]'Public,NonPublic,Instance')
foreach ($m in $methods) {
    $pParams = ($m.GetParameters() | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ", "
    Write-Host "$($m.ReturnType.Name) $($m.Name)($pParams)"
}
