$gameBin = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client"
Add-Type -Path "$gameBin\TaleWorlds.Library.dll"
Add-Type -Path "$gameBin\TaleWorlds.Core.dll"
Add-Type -Path "$gameBin\TaleWorlds.CampaignSystem.dll"

function Inspect-TypeMethods($type, $label) {
    Write-Host "=== $label ==="
    $methods = $type.GetMethods([System.Reflection.BindingFlags]'Public,Instance,DeclaredOnly') | Sort-Object Name
    foreach ($m in $methods) {
        $pParams = ($m.GetParameters() | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ", "
        Write-Host "$($m.ReturnType.Name) $($m.Name)($pParams)"
    }
}

Inspect-TypeMethods ([TaleWorlds.CampaignSystem.ComponentInterfaces.PregnancyModel]) "PREGNANCY MODEL"
Inspect-TypeMethods ([TaleWorlds.CampaignSystem.ComponentInterfaces.MarriageModel]) "MARRIAGE MODEL"
Inspect-TypeMethods ([TaleWorlds.CampaignSystem.ComponentInterfaces.HeirSelectionCalculationModel]) "HEIR SELECTION CALCULATION MODEL"
