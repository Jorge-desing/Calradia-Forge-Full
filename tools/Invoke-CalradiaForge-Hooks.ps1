[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Operation,
    [Parameter(Mandatory = $true)][int]$ProcessId,
    [string[]]$HookIds,
    [string]$HookIdsJson,
    [string]$Token,
    [string]$Session,
    [switch]$Confirm,
    [switch]$ValidateOnly,
    [int]$ReadTimeoutMilliseconds = 15000
)

$ErrorActionPreference = 'Stop'
$pipe = $null
$responseBytes = $null
try {
    if ($ProcessId -lt 1) { throw 'ProcessId must be between 1 and Int32.MaxValue.' }
    if ($ReadTimeoutMilliseconds -lt 1 -or $ReadTimeoutMilliseconds -gt 15000) {
        throw 'ReadTimeoutMilliseconds must be between 1 and 15000.'
    }
    $actions = @{
        Status = 'hook-snapshots'; Verify = 'hook-verify'; ApplyPlan = 'hook-apply-plan'
        RevertPlan = 'hook-revert-plan'; ConfirmApply = 'hook-apply-confirm'
        ConfirmRevert = 'hook-revert-confirm'; Cancel = 'hook-plan-cancel'
    }
    if (-not $actions.ContainsKey($Operation)) { throw 'Unsupported typed hook operation.' }
    function Assert-BoundedToken([string]$Value, [string]$Label) {
        if ([string]::IsNullOrWhiteSpace($Value) -or $Value.Length -gt 128 -or
            $Value -cne $Value.Trim() -or @($Value.ToCharArray() | Where-Object { [char]::IsControl($_) }).Count -ne 0) {
            throw "$Label must contain 1-128 printable characters without surrounding whitespace."
        }
    }
    $argument = ''
    if ($Operation -in @('Verify', 'ApplyPlan', 'RevertPlan')) {
        if ($PSBoundParameters.ContainsKey('HookIdsJson')) {
            if ($PSBoundParameters.ContainsKey('HookIds')) { throw 'Supply HookIds or HookIdsJson, not both.' }
            if ([string]::IsNullOrWhiteSpace($HookIdsJson) -or $HookIdsJson.Length -gt 16384 -or
                -not $HookIdsJson.Trim().StartsWith('[') -or -not $HookIdsJson.Trim().EndsWith(']')) {
                throw 'HookIdsJson must be a bounded JSON string array.'
            }
            $parsedIds = ConvertFrom-Json -InputObject $HookIdsJson
            if ($parsedIds -isnot [Array]) { throw 'HookIdsJson must decode to an array.' }
            foreach ($parsedId in $parsedIds) {
                if ($parsedId -isnot [string]) { throw 'HookIdsJson accepts string hook IDs only.' }
            }
            $HookIds = [string[]]$parsedIds
        }
        if ($null -eq $HookIds -or $HookIds.Count -lt 1 -or $HookIds.Count -gt 32) {
            throw 'Select between 1 and 32 unique hook IDs.'
        }
        $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
        foreach ($hookId in $HookIds) {
            Assert-BoundedToken $hookId 'Hook ID'
            if (-not $seen.Add($hookId)) { throw 'Hook IDs must be unique using exact ordinal identity.' }
        }
        if ($PSBoundParameters.ContainsKey('Token') -or $PSBoundParameters.ContainsKey('Session') -or $Confirm.IsPresent) {
            throw 'Selection operations do not accept confirmation or session parameters.'
        }
        $argument = @{ HookIds = @($HookIds) } | ConvertTo-Json -Compress
    } elseif ($Operation -in @('ConfirmApply', 'ConfirmRevert')) {
        Assert-BoundedToken $Token 'Token'
        if (-not $Confirm.IsPresent) { throw 'Confirmation requires the explicit -Confirm switch and an existing plan token.' }
        if ($PSBoundParameters.ContainsKey('HookIds') -or $PSBoundParameters.ContainsKey('HookIdsJson') -or $PSBoundParameters.ContainsKey('Session')) {
            throw 'Confirmation accepts a token, not hook selection or session parameters.'
        }
        $argument = @{ Token = $Token } | ConvertTo-Json -Compress
    } elseif ($Operation -eq 'Cancel') {
        Assert-BoundedToken $Token 'Token'
        Assert-BoundedToken $Session 'Session'
        if ($PSBoundParameters.ContainsKey('HookIds') -or $PSBoundParameters.ContainsKey('HookIdsJson') -or $Confirm.IsPresent) {
            throw 'Cancel accepts session and token only.'
        }
        $argument = @{ Session = $Session; Token = $Token } | ConvertTo-Json -Compress
    } elseif ($PSBoundParameters.ContainsKey('HookIds') -or $PSBoundParameters.ContainsKey('HookIdsJson') -or $PSBoundParameters.ContainsKey('Token') -or
              $PSBoundParameters.ContainsKey('Session') -or $Confirm.IsPresent) {
        throw 'Status does not accept selection or confirmation parameters.'
    }
    if ($ValidateOnly.IsPresent) {
        @{ Valid = $true; Operation = $Operation; Action = $actions[$Operation]; ProcessId = $ProcessId } |
            ConvertTo-Json -Compress
        exit 0
    }

    $requestId = [Guid]::NewGuid().ToString('N')
    $request = @{ Version = 1; Id = $requestId; Action = $actions[$Operation]; Argument = $argument; Seed = 148 } |
        ConvertTo-Json -Compress
    $utf8 = New-Object System.Text.UTF8Encoding($false, $true)
    $requestBytes = $utf8.GetBytes($request + "`n")
    if ($requestBytes.Length -gt 65536) { throw 'Request exceeds the 65536-byte transport bound.' }
    $pipe = New-Object System.IO.Pipes.NamedPipeClientStream('.', "CalradiaForge-$ProcessId",
        [System.IO.Pipes.PipeDirection]::InOut, [System.IO.Pipes.PipeOptions]::Asynchronous)
    $pipe.Connect(3000)
    $writeTask = $pipe.WriteAsync($requestBytes, 0, $requestBytes.Length)
    if (-not $writeTask.Wait(3000)) { throw 'Request write timed out; outcome is uncertain. Refresh status before another plan; do not retry confirmation.' }
    [void]$writeTask.GetAwaiter().GetResult()

    $deadline = [Diagnostics.Stopwatch]::StartNew()
    $responseBytes = New-Object System.IO.MemoryStream
    $buffer = New-Object byte[] 1024
    $complete = $false
    while (-not $complete) {
        $remaining = $ReadTimeoutMilliseconds - [int]$deadline.ElapsedMilliseconds
        if ($remaining -le 0) { throw 'Response timed out; outcome is uncertain. Refresh status before another plan; do not retry confirmation.' }
        $readTask = $pipe.ReadAsync($buffer, 0, $buffer.Length)
        if (-not $readTask.Wait($remaining)) { throw 'Response timed out; outcome is uncertain. Refresh status before another plan; do not retry confirmation.' }
        $count = $readTask.GetAwaiter().GetResult()
        if ($count -eq 0) { throw 'Pipe closed before a complete response; outcome is uncertain. Do not retry confirmation.' }
        for ($index = 0; $index -lt $count; $index++) {
            if ($buffer[$index] -eq 10) { $complete = $true; break }
            if ($responseBytes.Length -ge 65536) { throw 'Response exceeds the 65536-byte utility bound; outcome is uncertain.' }
            $responseBytes.WriteByte($buffer[$index])
        }
    }
    $responseText = $utf8.GetString($responseBytes.ToArray()).TrimEnd([char]13)
    $response = $responseText | ConvertFrom-Json
    if ($null -eq $response -or ($response.Version -isnot [int] -and $response.Version -isnot [long]) -or $response.Version -ne 1 -or
        -not [StringComparer]::Ordinal.Equals([string]$response.Id, $requestId) -or
        $response.Success -isnot [bool]) {
        throw 'Invalid response version, request identity, or success flag; outcome is uncertain.'
    }
    $response | ConvertTo-Json -Compress -Depth 16
    if (-not $response.Success) { exit 1 }
    exit 0
} catch {
    @{ Success = $false; Error = $_.Exception.Message; Retried = $false } | ConvertTo-Json -Compress
    exit 1
} finally {
    if ($null -ne $pipe) { $pipe.Dispose() }
    if ($null -ne $responseBytes) { $responseBytes.Dispose() }
}
