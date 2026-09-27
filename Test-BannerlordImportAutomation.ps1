$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'BannerlordImportAutomation.csproj'
$app = Join-Path $PSScriptRoot 'bin\Release\net48\BannerlordImportAutomation.exe'
$policyProject = Join-Path $PSScriptRoot 'tests\BannerlordImportAutomation.Policy.Tests.csproj'
$policyAssemblyPath = Join-Path $PSScriptRoot 'tests\bin\Release\net48\BannerlordImportAutomation.Policy.Tests.dll'

function Assert([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Invoke-PolicyTests {
    $assembly = [Reflection.Assembly]::LoadFrom($policyAssemblyPath)
    $testType = $assembly.GetType('BannerlordImportAutomationPolicyTests', $true)
    $run = $testType.GetMethod('Run', [Reflection.BindingFlags]::Public -bor [Reflection.BindingFlags]::Static)
    Assert ($null -ne $run) 'The import policy test method was not found.'
    try { $null = $run.Invoke($null, $null) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException.Message }
}

dotnet build $project --configuration Release --nologo --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw 'Import automation build failed.' }
Assert (Test-Path -LiteralPath $app -PathType Leaf) 'The expected helper build output was not produced.'

dotnet build $policyProject --configuration Release --nologo --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw 'Import automation policy test build failed.' }
Assert (Test-Path -LiteralPath $policyAssemblyPath -PathType Leaf) 'The policy test assembly was not produced.'

Invoke-PolicyTests
Write-Host 'PASS: helper and isolated policy fixtures. No executable was launched; no Editor UI was accessed.'
