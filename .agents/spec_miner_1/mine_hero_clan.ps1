$gameBin = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client"
Add-Type -Path "$gameBin\TaleWorlds.Library.dll"
Add-Type -Path "$gameBin\TaleWorlds.Core.dll"
Add-Type -Path "$gameBin\TaleWorlds.CampaignSystem.dll"

function Inspect-TypeProperties($type, $label) {
    Write-Host "=== $label ==="
    $props = $type.GetProperties([System.Reflection.BindingFlags]'Public,Instance') | Sort-Object Name
    foreach ($p in $props) {
        Write-Host "$($p.PropertyType.Name) $($p.Name) { $(if ($p.CanRead){'get; '} else {''})$(if ($p.CanWrite){'set; '} else {''})}"
    }
}

Inspect-TypeProperties ([TaleWorlds.CampaignSystem.Hero]) "HERO PROPERTIES"
Inspect-TypeProperties ([TaleWorlds.CampaignSystem.Clan]) "CLAN PROPERTIES"
