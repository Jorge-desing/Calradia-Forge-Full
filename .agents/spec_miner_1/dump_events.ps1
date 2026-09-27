$gameBin = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client"
Add-Type -Path "$gameBin\TaleWorlds.Library.dll"
Add-Type -Path "$gameBin\TaleWorlds.Core.dll"
Add-Type -Path "$gameBin\TaleWorlds.CampaignSystem.dll"

$props = [TaleWorlds.CampaignSystem.CampaignEvents].GetProperties([System.Reflection.BindingFlags]'Public,Static')

$list = foreach ($p in $props) {
    $t = $p.PropertyType
    $genArgs = $t.GetGenericArguments()
    $argsFull = if ($genArgs) { ($genArgs | ForEach-Object { $_.FullName }) -join ", " } else { "(void)" }
    [PSCustomObject]@{
        EventName = $p.Name
        Signature = "$($t.Name)<$argsFull>"
    }
}

$list | Export-Csv -Path ".agents\spec_miner_1\all_campaign_events.csv" -NoTypeInformation
Write-Host "Total CampaignEvents properties: $($list.Count)"
