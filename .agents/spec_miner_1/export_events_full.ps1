$gameBin = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client"
Add-Type -Path "$gameBin\TaleWorlds.Library.dll"
Add-Type -Path "$gameBin\TaleWorlds.Core.dll"
Add-Type -Path "$gameBin\TaleWorlds.CampaignSystem.dll"

$eventsType = [TaleWorlds.CampaignSystem.CampaignEvents]
$props = $eventsType.GetProperties([System.Reflection.BindingFlags]'Public,Static')

$list = foreach ($p in $props) {
    $t = $p.PropertyType
    $genArgs = $t.GetGenericArguments()
    $argsTypes = if ($genArgs) { ($genArgs | ForEach-Object { $_.FullName }) -join ", " } else { "" }
    
    [PSCustomObject]@{
        Name = $p.Name
        Type = $t.Name
        GenericCount = $genArgs.Length
        GenericArgs = $argsTypes
    }
}

$list | Export-Csv -Path ".agents\spec_miner_1\all_events_full.csv" -NoTypeInformation
Write-Host "Exported $($list.Count) events."
