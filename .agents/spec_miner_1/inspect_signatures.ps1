$gameBin = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client"
Add-Type -Path "$gameBin\TaleWorlds.Library.dll"
Add-Type -Path "$gameBin\TaleWorlds.Core.dll"
Add-Type -Path "$gameBin\TaleWorlds.CampaignSystem.dll"

$eventsType = [TaleWorlds.CampaignSystem.CampaignEvents]

# Let's inspect methods on CampaignEventReceiver (which matches the events and has argument names)
$receiverType = [TaleWorlds.CampaignSystem.CampaignEventReceiver]
$receiverMethods = $receiverType.GetMethods([System.Reflection.BindingFlags]'Public,NonPublic,Instance')

$dict = @{}
foreach ($m in $receiverMethods) {
    $paramStrs = foreach ($p in $m.GetParameters()) {
        "$($p.ParameterType.Name) $($p.Name)"
    }
    $dict[$m.Name] = ($paramStrs -join ", ")
}

$props = $eventsType.GetProperties([System.Reflection.BindingFlags]'Public,Static')

$outputList = foreach ($p in $props) {
    $t = $p.PropertyType
    $genArgs = $t.GetGenericArguments()
    $argsTypes = if ($genArgs) { ($genArgs | ForEach-Object { $_.Name }) -join ", " } else { "" }
    
    # Check if there is a matching receiver method
    # Usually receiver method matches event name or event name without "Event"
    $baseName = $p.Name
    $methName = $baseName
    if (-not $dict.ContainsKey($methName) -and $methName.EndsWith("Event")) {
        $methName = $methName.Substring(0, $methName.Length - 5)
    }
    
    $paramsDetailed = if ($dict.ContainsKey($methName)) { $dict[$methName] } elseif ($dict.ContainsKey($baseName)) { $dict[$baseName] } else { $argsTypes }
    
    [PSCustomObject]@{
        EventName = $p.Name
        EventType = $t.Name
        TypeArgs = $argsTypes
        Params = $paramsDetailed
    }
}

$outputList | Export-Csv -Path ".agents\spec_miner_1\campaign_events_detailed.csv" -NoTypeInformation
Write-Host "Detailed events exported. Total: $($outputList.Count)"
