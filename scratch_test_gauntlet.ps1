$bin = 'C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client'
Add-Type -Path (Join-Path $bin 'TaleWorlds.Library.dll')
Add-Type -Path (Join-Path $bin 'TaleWorlds.GauntletUI.dll')

$layoutType = [TaleWorlds.GauntletUI.Layout.StackLayout]
$m = $layoutType.GetMethod('MeasureLinear', [System.Reflection.BindingFlags]'Instance,NonPublic')
$body = $m.GetMethodBody()
$il = $body.GetILAsByteArray()

Write-Host "Method: MeasureLinear, IL Size: $($il.Length)"
$module = $m.Module
for ($i = 0; $i -lt $il.Length; $i++) {
    $b = $il[$i]
    if ($b -eq 0x28 -or $b -eq 0x6f) {
        $token = [BitConverter]::ToInt32($il, $i + 1)
        try {
            $member = $module.ResolveMember($token)
            if ($member.Name -match 'Size|Policy|Height|Stretch|Cover|Fixed|Bound|Measure') {
                Write-Host "IL_$($i.ToString('X4')): $($member.DeclaringType.Name).$($member.Name)"
            }
        } catch {}
        $i += 4
    }
}
