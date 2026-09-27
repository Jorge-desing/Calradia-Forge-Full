$gameBin = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client"
Add-Type -Path "$gameBin\TaleWorlds.Library.dll"
Add-Type -Path "$gameBin\TaleWorlds.Core.dll"
Add-Type -Path "$gameBin\TaleWorlds.CampaignSystem.dll"

$hc = [TaleWorlds.CampaignSystem.HeroCreator]
$methods = $hc.GetMethods([System.Reflection.BindingFlags]'Public,NonPublic,Static')
foreach ($m in $methods) {
    $pParams = ($m.GetParameters() | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ", "
    Write-Host "$($m.ReturnType.Name) $($m.Name)($pParams)"
}
