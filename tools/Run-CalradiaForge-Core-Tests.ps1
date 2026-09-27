param(
    [Parameter(Mandatory = $true)][string]$TestAssembly,
    [string]$InstalledModulesPath
)

$ErrorActionPreference = 'Stop'

try {
    $assemblyPath = (Resolve-Path -LiteralPath $TestAssembly).Path
    $assemblyDirectory = Split-Path -Parent $assemblyPath
    [AppDomain]::CurrentDomain.SetData('APPBASE', ($assemblyDirectory.TrimEnd('\') + '\'))

    $assembly = [Reflection.Assembly]::LoadFrom($assemblyPath)
    $programType = $assembly.GetType('Program', $true, $false)
    $bindingFlags = [Reflection.BindingFlags]::Static -bor [Reflection.BindingFlags]::NonPublic -bor [Reflection.BindingFlags]::Public
    $entryPoint = $programType.GetMethod('Main', $bindingFlags)
    if ($null -eq $entryPoint) { throw 'The Core test library does not expose its internal test runner.' }

    $testArguments = [object[]]::new(1)
    if (-not [string]::IsNullOrWhiteSpace($InstalledModulesPath)) {
        $testArguments[0] = [string[]]@($InstalledModulesPath)
    } else {
        $testArguments[0] = [string[]]@()
    }

    Write-Host ('[Core] Hosted by {0}; the test library is invoked in-process and no test executable is started.' -f (Get-Process -Id $PID).ProcessName)
    try {
        $null = $entryPoint.Invoke($null, $testArguments)
    } catch {
        $exception = $_.Exception
        if ($exception -is [Reflection.TargetInvocationException] -and $null -ne $exception.InnerException) {
            $exception = $exception.InnerException
        }
        [Console]::Error.WriteLine('[Core] Unhandled test-host exception: ' + $exception.ToString())
        exit 1
    }

    exit [Environment]::ExitCode
} catch {
    [Console]::Error.WriteLine('[Core] Could not host the Core test assembly: ' + $_.Exception.ToString())
    exit 2
}
