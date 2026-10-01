$ErrorActionPreference = 'Stop'
$utility = Join-Path $PSScriptRoot 'Invoke-CalradiaForge-Hooks.ps1'
$validCases = @(
    @{ Operation = 'Status' },
    @{ Operation = 'Verify'; HookIdsJson = '["first","First"]' },
    @{ Operation = 'Verify'; HookIds = @('first', 'First') },
    @{ Operation = 'ApplyPlan'; HookIds = @('cf.fixture.callback', 'cf.fixture.il') },
    @{ Operation = 'RevertPlan'; HookIds = @('cf.fixture.callback') },
    @{ Operation = 'ConfirmApply'; Token = 'reviewed-token'; Confirm = $true },
    @{ Operation = 'ConfirmRevert'; Token = 'reviewed-token'; Confirm = $true },
    @{ Operation = 'Cancel'; Token = 'reviewed-token'; Session = 'current-session' }
)
$invalidCases = @(
    @{ Operation = 'ArbitraryAction' },
    @{ Operation = 'Verify'; HookIdsJson = '[1]' },
    @{ Operation = 'Verify'; HookIdsJson = '["same","same"]' },
    @{ Operation = 'Verify'; HookIdsJson = '[]' },
    @{ Operation = 'Verify'; HookIdsJson = '["one"]'; HookIds = @('two') },
    @{ Operation = 'Status'; ProcessId = 0 },
    @{ Operation = 'Status'; ReadTimeoutMilliseconds = 15001 },
    @{ Operation = 'Status'; HookIds = @('unexpected') },
    @{ Operation = 'Verify' },
    @{ Operation = 'ApplyPlan'; HookIds = @('duplicate', 'duplicate') },
    @{ Operation = 'ApplyPlan'; HookIds = @(' leading') },
    @{ Operation = 'ApplyPlan'; HookIds = @("control`n") },
    @{ Operation = 'ApplyPlan'; HookIds = @('x' * 129) },
    @{ Operation = 'ApplyPlan'; HookIds = @(1..33 | ForEach-Object { "hook-$_" }) },
    @{ Operation = 'RevertPlan'; HookIds = @('hook'); Token = 'unexpected' },
    @{ Operation = 'ConfirmApply'; Token = 'token' },
    @{ Operation = 'ConfirmRevert'; Confirm = $true },
    @{ Operation = 'ConfirmApply'; Token = ('x' * 129); Confirm = $true },
    @{ Operation = 'ConfirmApply'; Token = 'token'; Confirm = $true; HookIds = @('unexpected') },
    @{ Operation = 'Cancel'; Token = 'token' },
    @{ Operation = 'Cancel'; Token = "bad`n"; Session = 'session' }
)
$actionByOperation = @{
    Status = 'hook-snapshots'; Verify = 'hook-verify'; ApplyPlan = 'hook-apply-plan'
    RevertPlan = 'hook-revert-plan'; ConfirmApply = 'hook-apply-confirm'
    ConfirmRevert = 'hook-revert-confirm'; Cancel = 'hook-plan-cancel'
}
try {
    $passed = 0
    foreach ($case in $validCases + $invalidCases) {
        $parameters = @{ ProcessId = 1; ValidateOnly = $true }
        foreach ($key in $case.Keys) { $parameters[$key] = $case[$key] }
        $expected = if ($passed -lt $validCases.Count) { 0 } else { 1 }
        $output = & $utility @parameters
        if ($LASTEXITCODE -ne $expected) {
            throw "Validation case $passed ($($case.Operation)) exited $LASTEXITCODE; expected $expected."
        }
        $result = ($output -join "`n") | ConvertFrom-Json
        if ($expected -eq 0 -and ($result.Valid -ne $true -or $result.Operation -ne $case.Operation -or
            $result.Action -ne $actionByOperation[$case.Operation] -or $result.ProcessId -ne 1)) {
            throw "Valid arguments did not produce matching ValidateOnly metadata: $($case.Operation)."
        }
        if ($expected -eq 1 -and $result.Success -ne $false) { throw 'Invalid arguments did not produce a structured error.' }
        $passed++
    }
    $transportPassed = 0
    $fixture = Join-Path $PSScriptRoot 'HookUtility-PipeFixture.ps1'
    $outputRoot = Join-Path $PSScriptRoot '../artifacts/hook-utility-transport'
    [IO.Directory]::CreateDirectory($outputRoot) | Out-Null
    $transportCases = @(
        @{ Mode = 'Success'; Operation = 'Status'; ExpectedAction = 'hook-snapshots'; ExpectedExit = 0 },
        @{ Mode = 'Failure'; Operation = 'Status'; ExpectedAction = 'hook-snapshots'; ExpectedExit = 1 },
        @{ Mode = 'Success'; Operation = 'ApplyPlan'; HookIds = @('cf.fixture.callback', 'cf.fixture.il'); ExpectedAction = 'hook-apply-plan'; ExpectedExit = 0 },
        @{ Mode = 'Success'; Operation = 'RevertPlan'; HookIds = @('cf.fixture.callback'); ExpectedAction = 'hook-revert-plan'; ExpectedExit = 0 },
        @{ Mode = 'Success'; Operation = 'ConfirmApply'; Token = 'fixture-plan-token'; ExpectedAction = 'hook-apply-confirm'; ExpectedExit = 0 },
        @{ Mode = 'Success'; Operation = 'ConfirmRevert'; Token = 'fixture-plan-token'; ExpectedAction = 'hook-revert-confirm'; ExpectedExit = 0 },
        @{ Mode = 'InvalidToken'; Operation = 'ConfirmApply'; Token = 'wrong-plan-token'; ExpectedAction = 'hook-apply-confirm'; ExpectedExit = 1; ExpectedError = 'Confirmation token is unknown, already used, or for another operation.' },
        @{ Mode = 'ExpiredToken'; Operation = 'ConfirmApply'; Token = 'expired-plan-token'; ExpectedAction = 'hook-apply-confirm'; ExpectedExit = 1; ExpectedError = 'Confirmation token expired; create a new plan.' },
        @{ Mode = 'InvalidToken'; Operation = 'ConfirmRevert'; Token = 'wrong-plan-token'; ExpectedAction = 'hook-revert-confirm'; ExpectedExit = 1; ExpectedError = 'Confirmation token is unknown, already used, or for another operation.' },
        @{ Mode = 'ExpiredToken'; Operation = 'ConfirmRevert'; Token = 'expired-plan-token'; ExpectedAction = 'hook-revert-confirm'; ExpectedExit = 1; ExpectedError = 'Confirmation token expired; create a new plan.' },
        @{ Mode = 'Timeout'; Operation = 'ConfirmApply'; Token = 'reviewed-token'; ExpectedAction = 'hook-apply-confirm'; ExpectedExit = 1; ExpectedErrorPrefix = 'Response timed out; outcome is uncertain.' },
        @{ Mode = 'WrongId'; Operation = 'RevertPlan'; HookIds = @('cf.fixture.callback'); ExpectedAction = 'hook-revert-plan'; ExpectedExit = 1 },
        @{ Mode = 'WrongVersion'; Operation = 'ConfirmRevert'; Token = 'reviewed-token'; ExpectedAction = 'hook-revert-confirm'; ExpectedExit = 1 },
        @{ Mode = 'StringVersion'; Operation = 'Status'; ExpectedAction = 'hook-snapshots'; ExpectedExit = 1 },
        @{ Mode = 'BooleanVersion'; Operation = 'Status'; ExpectedAction = 'hook-snapshots'; ExpectedExit = 1 },
        @{ Mode = 'NullVersion'; Operation = 'Status'; ExpectedAction = 'hook-snapshots'; ExpectedExit = 1 },
        @{ Mode = 'StringSuccess'; Operation = 'Status'; ExpectedAction = 'hook-snapshots'; ExpectedExit = 1 },
        @{ Mode = 'Truncated'; Operation = 'Status'; ExpectedAction = 'hook-snapshots'; ExpectedExit = 1 },
        @{ Mode = 'Oversize'; Operation = 'Status'; ExpectedAction = 'hook-snapshots'; ExpectedExit = 1 },
        @{ Mode = 'ExactBound'; Operation = 'Status'; ExpectedAction = 'hook-snapshots'; ExpectedExit = 0 },
        @{ Mode = 'DecimalVersion'; Operation = 'Status'; ExpectedAction = 'hook-snapshots'; ExpectedExit = 1 },
        @{ Mode = 'InvalidUtf8'; Operation = 'ConfirmApply'; Token = 'reviewed-token'; ExpectedAction = 'hook-apply-confirm'; ExpectedExit = 1 }
    )
    # Invalid/expired token rows assert client serialization and preserve a simulated host rejection.
    # This loopback fixture cannot invoke Runtime.CommitHookPlan or certify Bannerlord session,
    # screen-context, single-use, or clock-expiry behavior; those require a runtime test seam.
    foreach ($transportCase in $transportCases) {
        $mode = $transportCase.Mode
        $caseRoot = Join-Path $outputRoot ([Guid]::NewGuid().ToString('N'))
        [IO.Directory]::CreateDirectory($caseRoot) | Out-Null
        $ready = Join-Path $caseRoot 'ready'
        $count = Join-Path $caseRoot 'count'
        $requestPath = Join-Path $caseRoot 'request.json'
        $arguments = '-NoProfile -NonInteractive -ExecutionPolicy Bypass -File "' + $fixture + '" -Mode ' + $mode + ' -ReadyPath "' + $ready + '" -CountPath "' + $count + '" -RequestPath "' + $requestPath + '"'
        $server = Start-Process -FilePath powershell.exe -ArgumentList $arguments -WindowStyle Hidden -PassThru
        try {
            $wait = [Diagnostics.Stopwatch]::StartNew()
            while (!(Test-Path -LiteralPath $ready)) {
                if ($server.HasExited -or $wait.ElapsedMilliseconds -gt 4000) { throw "Fixture startup failed: $mode" }
                Start-Sleep -Milliseconds 20
            }
            $parameters = @{ Operation = $transportCase.Operation; ProcessId = $server.Id }
            if ($transportCase.ContainsKey('HookIds')) { $parameters.HookIds = $transportCase.HookIds }
            if ($transportCase.ContainsKey('Token')) {
                $parameters.Token = $transportCase.Token
                $parameters.Confirm = $true
            }
            if ($mode -eq 'Timeout') { $parameters.ReadTimeoutMilliseconds = 100 }
            $output = & $utility @parameters
            $actualExit = $LASTEXITCODE
            $expectedExit = $transportCase.ExpectedExit
            if ($actualExit -ne $expectedExit) { throw "Transport $mode exit $actualExit; expected $expectedExit" }
            $parsed = ($output -join "`n") | ConvertFrom-Json
            if ($expectedExit -eq 1 -and $parsed.Success -ne $false) { throw "Missing structured transport failure: $mode" }
            if ($transportCase.ContainsKey('ExpectedError') -and $parsed.Error -ne $transportCase.ExpectedError) {
                throw "Transport $mode did not preserve the expected host error."
            }
            if ($transportCase.ContainsKey('ExpectedErrorPrefix') -and
                -not ([string]$parsed.Error).StartsWith($transportCase.ExpectedErrorPrefix, [StringComparison]::Ordinal)) {
                throw "Transport $mode did not preserve the expected transport error."
            }
            if (!$server.WaitForExit(4000)) { throw "Fixture did not finish: $mode" }
            if ($server.ExitCode -ne 0) { throw "Fixture server failed: $mode" }
            if ([IO.File]::ReadAllText($count) -ne '1') { throw "Unexpected retry: $mode" }
            $request = [IO.File]::ReadAllText($requestPath) | ConvertFrom-Json
            if ($request.Action -ne $transportCase.ExpectedAction) { throw "Utility sent unexpected action for $($transportCase.Operation)." }
            if ($transportCase.ContainsKey('HookIds')) {
                $argument = $request.Argument | ConvertFrom-Json
                if ($argument.HookIds.Count -ne $transportCase.HookIds.Count) { throw "Wrong selection count for $($transportCase.Operation)." }
                for ($index = 0; $index -lt $transportCase.HookIds.Count; $index++) {
                    if ($argument.HookIds[$index] -cne $transportCase.HookIds[$index]) { throw "Wrong selection order or ID for $($transportCase.Operation)." }
                }
            } elseif ($transportCase.ContainsKey('Token')) {
                $argument = $request.Argument | ConvertFrom-Json
                if ($argument.Token -cne $transportCase.Token) { throw "Wrong confirmation token for $($transportCase.Operation)." }
            }
            $transportPassed++
        } finally {
            if (!$server.HasExited) { Stop-Process -Id $server.Id -ErrorAction SilentlyContinue }
            $server.Dispose()
        }
    }
    @{ Success = $true; Passed = $passed; TransportPassed = $transportPassed; IpcUsed = 'isolated fixture only'; ConfirmationBoundary = 'Token rejection is simulated; Bannerlord Runtime lifecycle validation is not covered.' } | ConvertTo-Json -Compress
    exit 0
} catch {
    @{ Success = $false; Error = $_.Exception.Message; IpcUsed = $false } | ConvertTo-Json -Compress
    exit 1
}
