$f = "tools/package.ps1"
$content = Get-Content $f -Raw

$xml_validation = @"
    # Pre-flight XML validation
    Write-Host "Validating XML syntax..."
    `$xmlFiles = Get-ChildItem -Path "modules" -Filter "*.xml" -Recurse
    foreach (`$xmlFile in `$xmlFiles) {
        try {
            [xml]`$testXml = Get-Content -LiteralPath `$xmlFile.FullName -Raw
        } catch {
            throw "XML Validation failed for `$(`$xmlFile.FullName): `$(`$_.Exception.Message)"
        }
    }
    Write-Host "All XML files valid."

"@

$content = $content -replace '(?s)Push-Location \$workspace\r?\ntry \{', ("Push-Location `$workspace`ntry {`n" + $xml_validation)
Set-Content $f -Value $content
