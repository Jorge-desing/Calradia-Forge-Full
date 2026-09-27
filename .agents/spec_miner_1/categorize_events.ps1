$events = Import-Csv -Path ".agents\spec_miner_1\campaign_events_detailed.csv"

function Show-Category($title, $filter) {
    Write-Host "=================================================="
    Write-Host "=== $title ==="
    Write-Host "=================================================="
    $matches = $events | Where-Object {
        $name = $_.EventName
        $sig = $_.TypeArgs
        $filter.Invoke($name, $sig)
    }
    foreach ($m in $matches) {
        Write-Host "$($m.EventName) -> ($($m.Params))"
    }
}

Show-Category "HERO LIFECYCLE & COMBAT" { param($n, $s) $n -match "Hero" -or $n -match "Birth" -or $n -match "Age" -or $n -match "Death" -or $n -match "Killed" -or $n -match "Wound" -or $n -match "Prisoner" }
Show-Category "CLAN & SUCCESSION" { param($n, $s) $n -match "Clan" -or $n -match "Heir" -or $n -match "Succession" -or $n -match "Companion" }
Show-Category "CHARACTER PROGRESSION & PERKS" { param($n, $s) $n -match "Skill" -or $n -match "Level" -or $n -match "Perk" -or $n -match "Trait" -or $n -match "Renown" -or $n -match "Influence" }
Show-Category "MARRIAGE & ROMANCE" { param($n, $s) $n -match "Marri" -or $n -match "Romance" -or $n -match "Pregnant" -or $n -match "Birth" }
Show-Category "PERIODIC TICKS" { param($n, $s) $n -match "Tick" }
