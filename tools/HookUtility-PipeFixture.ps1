param(
    [Parameter(Mandatory = $true)][string]$Mode,
    [Parameter(Mandatory = $true)][string]$ReadyPath,
    [Parameter(Mandatory = $true)][string]$CountPath,
    [Parameter(Mandatory = $true)][string]$RequestPath
)

# Disposable local server launched by Test-CalradiaForge-HookUtility.bat's harness.
# Its pipe name is its own process ID; it never connects to a game host.
$ErrorActionPreference = 'Stop'
$utf8 = New-Object System.Text.UTF8Encoding($false, $true)
$connectionCount = 0
$pipe = $null
$reader = $null
try {
    foreach ($attempt in 0..0) {
        $pipe = New-Object System.IO.Pipes.NamedPipeServerStream("CalradiaForge-$PID",
            [System.IO.Pipes.PipeDirection]::InOut, 1, [System.IO.Pipes.PipeTransmissionMode]::Byte,
            [System.IO.Pipes.PipeOptions]::Asynchronous, 4096, 4096)
        if ($attempt -eq 0) {
            [IO.File]::WriteAllText($CountPath, '0')
            [IO.File]::WriteAllText($ReadyPath, 'ready')
        }
        $connection = $pipe.WaitForConnectionAsync()
        if (-not $connection.Wait($(if ($attempt -eq 0) { 5000 } else { 250 }))) { break }
        [void]$connection.GetAwaiter().GetResult()
        $connectionCount++
        [IO.File]::WriteAllText($CountPath, [string]$connectionCount)
        $reader = New-Object System.IO.StreamReader($pipe, $utf8, $false, 1024, $true)
        $requestTask = $reader.ReadLineAsync()
        if (-not $requestTask.Wait(3000)) { throw 'Fixture request read timed out.' }
        $requestText = $requestTask.GetAwaiter().GetResult()
        [IO.File]::WriteAllText($RequestPath, $requestText)
        $request = $requestText | ConvertFrom-Json
        $response = @{ Version = 1; Id = $request.Id; Success = $true; Error = $null; Data = 'fixture' }
        switch ($Mode) {
            WrongId { $response.Id = 'another-request' }
            WrongVersion { $response.Version = 2 }
            StringVersion { $response.Version = '1' }
            BooleanVersion { $response.Version = $true }
            NullVersion { $response.Version = $null }
            Failure { $response.Success = $false; $response.Error = 'fixture rejection' }
            InvalidToken { $response.Success = $false; $response.Error = 'Confirmation token is unknown, already used, or for another operation.' }
            ExpiredToken { $response.Success = $false; $response.Error = 'Confirmation token expired; create a new plan.' }
            StringSuccess { $response.Success = 'true' }
            Timeout { Start-Sleep -Milliseconds 500 }
            Success { }
            Truncated { }
            Oversize { }
            ExactBound { }
            DecimalVersion { }
            InvalidUtf8 { }
            default { throw 'Unknown fixture mode.' }
        }
        $responseText = $response | ConvertTo-Json -Compress
        if ($Mode -eq 'DecimalVersion') { $responseText = $responseText -replace '"Version":1', '"Version":1.0' }
        if ($Mode -eq 'ExactBound') {
            $response.Data = 'x' * (65536 - $utf8.GetByteCount($responseText) + 'fixture'.Length)
            $responseText = $response | ConvertTo-Json -Compress
        }
        if ($Mode -eq 'Oversize') { $responseText = 'x' * 65537 }
        $bytes = if ($Mode -eq 'InvalidUtf8') { [byte[]]@(255, 10) }
            elseif ($Mode -eq 'Truncated') { $utf8.GetBytes($responseText) }
            else { $utf8.GetBytes($responseText + "`n") }
        try {
            $write = $pipe.WriteAsync($bytes, 0, $bytes.Length)
            if ($write.Wait(1000)) { [void]$write.GetAwaiter().GetResult() }
        } catch {
            # A timeout/oversize client closes its pipe. Connection counting below
            # records the one observed request; the single-connection fixture does not
            # certify detection of a later retry after this server exits.
            if ($Mode -notin @('Timeout', 'Oversize')) { throw }
        }
        $reader.Dispose(); $reader = $null
        $pipe.Dispose(); $pipe = $null
    }
    exit 0
} finally {
    if ($null -ne $reader) { $reader.Dispose() }
    if ($null -ne $pipe) { $pipe.Dispose() }
}
