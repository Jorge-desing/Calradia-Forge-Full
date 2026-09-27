$gameBin = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client"
Add-Type -Path "$gameBin\TaleWorlds.Library.dll"
Add-Type -Path "$gameBin\TaleWorlds.Core.dll"
Add-Type -Path "$gameBin\TaleWorlds.CampaignSystem.dll"

$enumTypes = @(
    [TaleWorlds.CampaignSystem.Actions.KillCharacterAction+KillCharacterActionDetail],
    [TaleWorlds.CampaignSystem.Actions.RemoveCompanionAction+RemoveCompanionDetail],
    [TaleWorlds.CampaignSystem.Actions.ChangeRelationAction+ChangeRelationDetail],
    [TaleWorlds.CampaignSystem.Actions.ChangeKingdomAction+ChangeKingdomActionDetail],
    [TaleWorlds.CampaignSystem.Hero+CharacterStates],
    [TaleWorlds.CampaignSystem.Romance+RomanceLevelEnum],
    [TaleWorlds.CampaignSystem.HeroGetsBusyReasons],
    [TaleWorlds.CampaignSystem.Actions.EndCaptivityDetail],
    [TaleWorlds.CampaignSystem.Actions.TeleportHeroAction+TeleportationDetail]
)

foreach ($et in $enumTypes) {
    Write-Host "=== $($et.FullName) ==="
    [System.Enum]::GetNames($et) | ForEach-Object { Write-Host "  $_ = $([System.Convert]::ToInt32([System.Enum]::Parse($et, $_)))" }
}
