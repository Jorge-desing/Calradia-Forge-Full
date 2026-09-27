import csv

with open(r".agents\spec_miner_1\all_events_full.csv", mode="r", encoding="utf-8") as f:
    reader = csv.DictReader(f)
    events = list(reader)

search_names = [
    "HeroCreated", "OnGivenBirthEvent", "HeroGrowsOutOfInfancyEvent", "HeroReachesTeenAgeEvent",
    "HeroComesOfAgeEvent", "BeforeHeroKilledEvent", "HeroKilledEvent", "HeroWounded",
    "OnHeroCombatHitEvent", "HeroOccupationChangedEvent", "HeroRelationChanged", "OnHeroChangedClanEvent",
    "HeroPrisonerTaken", "HeroPrisonerReleased", "OnHeroActivatedEvent", "OnHeroGetsBusyEvent",
    "OnHeroUnregisteredEvent", "OnHeroTeleportationRequestedEvent",
    "OnClanCreatedEvent", "OnClanDestroyedEvent", "ClanTierIncrease", "OnClanLeaderChangedEvent",
    "OnHeirSelectionRequestedEvent", "OnHeirSelectionOverEvent", "OnPlayerCharacterChangedEvent",
    "OnClanChangedKingdomEvent", "OnClanDefectedEvent", "RulingClanChanged", "OnClanInfluenceChangedEvent",
    "OnClanEarnedGoldFromTributeEvent", "RebelliousClanDisbandedAtSettlement", "RebellionFinished",
    "NewCompanionAdded", "CompanionRemoved", "OnHeroJoinedPartyEvent", "OnPartyLeaderChangedEvent",
    "OnGovernorChangedEvent", "OnMarriageOfferedToPlayerEvent", "OnMarriageOfferCanceledEvent",
    "BeforeHeroesMarried", "RomanticStateChanged", "HeroGainedSkill", "HeroLevelledUp",
    "PerkOpenedEvent", "PerkResetEvent", "PlayerTraitChangedEvent", "RenownGained",
    "PlayerUpgradedTroopsEvent", "OnUnitRecruitedEvent",
    "HourlyTickEvent", "QuarterHourlyTickEvent", "HourlyTickPartyEvent", "HourlyTickSettlementEvent",
    "HourlyTickClanEvent", "DailyTickEvent", "DailyTickPartyEvent", "DailyTickSettlementEvent",
    "DailyTickTownEvent", "DailyTickHeroEvent", "DailyTickClanEvent", "WeeklyTickEvent"
]

print(f"{'Event Name':<35} | {'Generic Args'}")
print("-" * 80)
for name in search_names:
    matches = [e for e in events if e['Name'] == name]
    if matches:
        m = matches[0]
        args = m['GenericArgs']
        if not args:
            args = "(none)"
        print(f"{m['Name']:<35} | {args}")
    else:
        print(f"{name:<35} | NOT FOUND")
