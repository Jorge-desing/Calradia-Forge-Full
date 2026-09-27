$gameBin = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client"
Add-Type -Path "$gameBin\TaleWorlds.Library.dll"
Add-Type -Path "$gameBin\TaleWorlds.Core.dll"
Add-Type -Path "$gameBin\TaleWorlds.CampaignSystem.dll"

# Search for classes related to companion / wanderer
$types = [System.AppDomain]::CurrentDomain.GetAssemblies() | 
    ForEach-Object { $_.GetTypes() } | 
    Where-Object { $_.Namespace -eq 'TaleWorlds.CampaignSystem.CampaignBehaviors' -or $_.Namespace -eq 'TaleWorlds.CampaignSystem' -or $_.Namespace -eq 'TaleWorlds.CampaignSystem.GameComponents' }

$wandererTypes = $types | Where-Object { $_.Name -match "Companion|Wanderer|HeroCreator" }
foreach ($wt in $wandererTypes) {
    Write-Host "Type: $($wt.FullName)"
    $methods = $wt.GetMethods([System.Reflection.BindingFlags]'Public,Static,DeclaredOnly')
    foreach ($m in $methods) {
        $pParams = ($m.GetParameters() | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ", "
        Write-Host "  static $($m.ReturnType.Name) $($m.Name)($pParams)"
    }
}
