$ErrorActionPreference = 'Stop'
$durationSeconds = 12
$counterFrequency = [System.Diagnostics.Stopwatch]::Frequency

try {
    $all = @(Get-CimInstance Win32_Process)
    $targetNames = @('ChatGPT.exe', 'codex.exe', 'codex-computer-use.exe', 'explorer.exe', 'dwm.exe')
    $selected = @($all | Where-Object { $targetNames -contains $_.Name })
    $watchers = @($all | Where-Object {
        $_.Name -ieq 'powershell.exe' -and
        $_.CommandLine -match 'CodexCaptureCompat\\persistence\.ps1' -and
        $_.CommandLine -match '\-Action\s+Watch'
    })
    $selected += $watchers
    $selected = @($selected | Sort-Object ProcessId -Unique)

    if ($selected.Count -eq 0) {
        Write-Error 'No target processes were found; no measurements were collected.'
        exit 3
    }

    $metadata = @{}
    $processTotals = @{}
    $threadTotals = @{}
    foreach ($item in $selected) {
        $metadata[[int]$item.ProcessId] = [pscustomobject]@{
            Name = [string]$item.Name
            ParentProcessId = [int]$item.ParentProcessId
            ImagePath = [string]$item.ExecutablePath
            CommandLine = [string]$item.CommandLine
        }
        $processTotals[[int]$item.ProcessId] = [pscustomobject]@{ CpuSeconds = 0.0; Samples = 0 }
    }

    function Get-ProcessSnapshot {
        param([int[]]$ProcessIds, [int[]]$ThreadProcessIds)

        $snapshot = @{}
        $wanted = [System.Collections.Generic.HashSet[int]]::new()
        $threadWanted = [System.Collections.Generic.HashSet[int]]::new()
        foreach ($processId in $ProcessIds) { [void]$wanted.Add([int]$processId) }
        foreach ($processId in $ThreadProcessIds) { [void]$threadWanted.Add([int]$processId) }

        foreach ($process in (Get-Process)) {
            $processId = [int]$process.Id
            if (-not $wanted.Contains($processId)) { continue }
            $threads = @{}
            if ($threadWanted.Contains($processId)) {
                foreach ($thread in $process.Threads) {
                    try { $threads[[int]$thread.Id] = [double]$thread.TotalProcessorTime.TotalSeconds } catch { }
                }
            }
            try {
                $snapshot[$processId] = [pscustomobject]@{
                    StartTimeTicks = $process.StartTime.ToUniversalTime().Ticks
                    CpuSeconds = [double]$process.TotalProcessorTime.TotalSeconds
                    Threads = $threads
                }
            }
            catch { }
        }
        return $snapshot
    }

    $processIds = @($metadata.Keys | ForEach-Object { [int]$_ })
    $helperEntry = $selected | Where-Object { $_.Name -ieq 'codex-computer-use.exe' } | Select-Object -First 1
    $threadAnalysisIds = @($processIds | Where-Object {
        $meta = $metadata[[int]$_]
        $meta.Name -ieq 'codex-computer-use.exe' -or
        $meta.Name -ieq 'explorer.exe' -or
        $meta.Name -ieq 'dwm.exe' -or
        ($helperEntry -and [int]$_ -eq [int]$helperEntry.ParentProcessId)
    })
    $previous = Get-ProcessSnapshot -ProcessIds $processIds -ThreadProcessIds $threadAnalysisIds
    $started = [System.Diagnostics.Stopwatch]::GetTimestamp()
    $last = $started
    while (([System.Diagnostics.Stopwatch]::GetTimestamp() - $started) -lt ($durationSeconds * $counterFrequency)) {
        Start-Sleep -Milliseconds 1000
        $now = [System.Diagnostics.Stopwatch]::GetTimestamp()
        $elapsed = [double]($now - $last) / $counterFrequency
        $last = $now
        $current = Get-ProcessSnapshot -ProcessIds $processIds -ThreadProcessIds $threadAnalysisIds

        foreach ($processId in $processIds) {
            if (-not $previous.ContainsKey($processId) -or -not $current.ContainsKey($processId)) { continue }
            $before = $previous[$processId]
            $after = $current[$processId]
            if ($before.StartTimeTicks -ne $after.StartTimeTicks) { continue }

            $cpuDelta = [math]::Max(0.0, $after.CpuSeconds - $before.CpuSeconds)
            $processTotals[$processId].CpuSeconds += $cpuDelta
            $processTotals[$processId].Samples++

            foreach ($threadId in $after.Threads.Keys) {
                if (-not $before.Threads.ContainsKey($threadId)) { continue }
                $threadKey = "$processId/$threadId"
                $threadDelta = [math]::Max(0.0, $after.Threads[$threadId] - $before.Threads[$threadId])
                if (-not $threadTotals.ContainsKey($threadKey)) {
                    $threadTotals[$threadKey] = [pscustomobject]@{ ProcessId = $processId; ThreadId = [int]$threadId; CpuSeconds = 0.0 }
                }
                $threadTotals[$threadKey].CpuSeconds += $threadDelta
            }
        }

        $previous = $current
    }

    $totalElapsed = [double]([System.Diagnostics.Stopwatch]::GetTimestamp() - $started) / $counterFrequency
    Write-Output ("Sample duration: {0:N1}s. CPU percentages below are percent of one logical processor; this is process CPU, not pointer latency." -f $totalElapsed)
    Write-Output ''
    Write-Output 'Process CPU:'
    $processRows = foreach ($processId in $processIds) {
        $total = $processTotals[$processId]
        if ($total.Samples -eq 0) { continue }
        $meta = $metadata[$processId]
        [pscustomobject]@{
            Process = $meta.Name
            PID = $processId
            ParentPID = $meta.ParentProcessId
            AvgOneCorePercent = [math]::Round(($total.CpuSeconds / $totalElapsed) * 100, 1)
            CpuSeconds = [math]::Round($total.CpuSeconds, 3)
            Samples = $total.Samples
            Image = $meta.ImagePath
        }
    }
    $processRows | Sort-Object AvgOneCorePercent -Descending | Format-Table -AutoSize

    Write-Output ''
    Write-Output 'Top CPU threads:'
    $threadRows = foreach ($entry in $threadTotals.Values) {
        $meta = $metadata[[int]$entry.ProcessId]
        [pscustomobject]@{
            Process = $meta.Name
            PID = $entry.ProcessId
            TID = $entry.ThreadId
            AvgOneCorePercent = [math]::Round(($entry.CpuSeconds / $totalElapsed) * 100, 1)
            CpuSeconds = [math]::Round($entry.CpuSeconds, 3)
        }
    }
    $threadRows | Sort-Object AvgOneCorePercent -Descending | Select-Object -First 12 | Format-Table -AutoSize

    $watcherRows = @($selected | Where-Object { $_.Name -ieq 'powershell.exe' })
    if ($watcherRows.Count -gt 0) {
        Write-Output ''
        Write-Output 'Managed watcher PID(s) were included by matching the staged persistence.ps1 -Action Watch command line.'
    }
}
catch {
    Write-Error $_
    exit 1
}
