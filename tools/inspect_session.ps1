param(
    [Parameter(Mandatory=$true)][ValidateRange(1,2147483647)][int]$ProcessId,
    [ValidateSet('hello','summary','scan','modules','dependencies','logs','inspect','pin','compare','unpin','snapshots','tests','commands','metrics','report','patch-diagnostics','panel-open','panel-close','test-mode','run','confirm-copy')][string]$Action='summary',
    [string]$Argument='',
    [string]$OutputPath,
    [switch]$AllowTestChanges
)
$ErrorActionPreference='Stop'
if($Action -in @('test-mode','run','confirm-copy') -and -not $AllowTestChanges){throw 'Test actions require -AllowTestChanges. Game context and campaign-copy checks still apply.'}
$pipe=$null
$reader=$null
$writer=$null
try {
    # Inspect an existing session; never replay a request after a timeout.
    $pipe=[IO.Pipes.NamedPipeClientStream]::new('.',"CalradiaForge-$ProcessId",[IO.Pipes.PipeDirection]::InOut,[IO.Pipes.PipeOptions]::Asynchronous)
    $pipe.Connect(3000)
    $encoding=[Text.UTF8Encoding]::new($false)
    $reader=[IO.StreamReader]::new($pipe,$encoding,$false,4096,$true)
    $writer=[IO.StreamWriter]::new($pipe,$encoding,4096,$true)
    $writer.AutoFlush=$true
    $requestId=[Guid]::NewGuid().ToString('N')
    $request=@{Version=1;Id=$requestId;Action=$Action;Argument=$Argument;Seed=148}
    $writer.WriteLine(($request | ConvertTo-Json -Compress))
    $readTask=$reader.ReadLineAsync()
    if(-not $readTask.Wait(20000)){throw 'Session response timed out. The request was not retried.'}
    $responseText=$readTask.GetAwaiter().GetResult()
    if($null -eq $responseText){throw 'The session disconnected before returning a response.'}
    if($responseText.Length -gt 32MB){throw 'Session response exceeds 32 MB.'}
    $response=$responseText | ConvertFrom-Json
    if($response.Version -ne 1 -or $response.Id -ne $requestId){throw 'Session protocol or request ID mismatch.'}
    if(-not $response.Success){throw "Session rejected the request: $($response.Error)"}
    if($OutputPath){
        $fullOutputPath=[IO.Path]::GetFullPath($OutputPath)
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($fullOutputPath)) | Out-Null
        [IO.File]::WriteAllText($fullOutputPath,$response.Data,$encoding)
    }
    Write-Output $response.Data
} finally {
    if($null -ne $writer){$writer.Dispose()}
    if($null -ne $reader){$reader.Dispose()}
    if($null -ne $pipe){$pipe.Dispose()}
}
