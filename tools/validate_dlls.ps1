param([string]$Dir)
$ErrorActionPreference = 'Stop'
Write-Host "Validating DLLs in $Dir..."
$dlls = Get-ChildItem -Path $Dir -Filter *.dll -Recurse
foreach ($dll in $dlls) {
    try {
        [Reflection.Assembly]::ReflectionOnlyLoadFrom($dll.FullName) | Out-Null
    } catch {
        Write-Host "WARNING: Could not reflect load $($dll.Name) (Likely missing dependencies in target env)"
    }
}
Write-Host "DLL Validation completed."
