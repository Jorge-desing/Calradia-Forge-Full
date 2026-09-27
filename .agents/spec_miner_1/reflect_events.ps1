$gameBin = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client"
Add-Type -Path "$gameBin\TaleWorlds.Library.dll"
Add-Type -Path "$gameBin\TaleWorlds.Core.dll"
Add-Type -Path "$gameBin\TaleWorlds.CampaignSystem.dll"

$props = [TaleWorlds.CampaignSystem.CampaignEvents].GetProperties([System.Reflection.BindingFlags]'Public,Static')

$results = foreach ($p in $props) {
    $t = $p.PropertyType
    $genArgs = $t.GetGenericArguments()
    $argsStr = if ($genArgs) { ($genArgs | ForEach-Object { $_.Name }) -join ", " } else { "" }
    [PSCustomObject]@{
        Name = $p.Name
        TypeName = $t.Name
        Args = $argsStr
    }
}

$results | Sort-Object Name | Format-Table -AutoSize
