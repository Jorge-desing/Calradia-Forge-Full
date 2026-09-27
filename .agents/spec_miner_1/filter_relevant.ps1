$events = Import-Csv -Path ".agents\spec_miner_1\all_campaign_events.csv"

$keywords = @("Hero", "Clan", "Character", "Companion", "Birth", "Age", "Death", "Killed", "Execute", "Wound", "Marri", "Pregnant", "Pregnancy", "Child", "Skill", "Perk", "Level", "Renown", "Influence", "Tick", "Succession", "Heir", "Governor")

$relevant = $events | Where-Object {
    $name = $_.EventName
    $sig = $_.Signature
    $match = $false
    foreach ($k in $keywords) {
        if ($name -match $k -or $sig -match $k) {
            $match = $true
            break
        }
    }
    $match
}

$relevant | Sort-Object EventName | Format-Table -AutoSize
Write-Host "Matched: $($relevant.Count)"
