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

function Inspect-TypeMethods($type, $label) {
    Write-Host "=== $label ==="
    $methods = $type.GetMethods([System.Reflection.BindingFlags]'Public,Instance,DeclaredOnly') | Sort-Object Name
    foreach ($m in $methods) {
        $pParams = ($m.GetParameters() | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ", "
        Write-Host "$($m.ReturnType.Name) $($m.Name)($pParams)"
    }
}

# Inspect Hero Developer
Inspect-TypeProperties ([TaleWorlds.CampaignSystem.CharacterDevelopment.HeroDeveloper]) "HERO DEVELOPER PROPERTIES"
Inspect-TypeMethods ([TaleWorlds.CampaignSystem.CharacterDevelopment.HeroDeveloper]) "HERO DEVELOPER METHODS"

# Inspect CharacterDevelopmentModel
Inspect-TypeMethods ([TaleWorlds.CampaignSystem.ComponentInterfaces.CharacterDevelopmentModel]) "CHARACTER DEVELOPMENT MODEL METHODS"

# Inspect ClanTierModel
Inspect-TypeMethods ([TaleWorlds.CampaignSystem.ComponentInterfaces.ClanTierModel]) "CLAN TIER MODEL METHODS"
