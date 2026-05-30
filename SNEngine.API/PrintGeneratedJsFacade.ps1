param(
    [string]$IntermediateOutputPath,
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Continue"

Write-Host ""
Write-Host "=== GENERATED JS FACADE (from source generator) ===" -ForegroundColor Cyan

$searchRoot = $null

if (-not [string]::IsNullOrWhiteSpace($IntermediateOutputPath)) {
    $clean = $IntermediateOutputPath.Trim('"', ' ').TrimEnd('\', '/')
    if (Test-Path $clean -ErrorAction SilentlyContinue) {
        $searchRoot = $clean
    }
}

if (-not $searchRoot) {
    # Fallback: search from the script's directory upward
    $current = $PSScriptRoot
    while ($current -and -not (Test-Path (Join-Path $current "obj"))) {
        $parent = Split-Path $current -Parent
        if ($parent -eq $current) { break }
        $current = $parent
    }
    if ($current) {
        $searchRoot = Join-Path $current "obj"
    }
}

if (-not $searchRoot -or -not (Test-Path $searchRoot)) {
    # Final fallback: search the entire obj folder from project root
    $projectRoot = Split-Path $PSScriptRoot -Parent
    $objRoot = Join-Path $projectRoot "obj"
    if (Test-Path $objRoot) {
        $searchRoot = $objRoot
    }
}

# Always do a broad recursive search under the project obj as last resort
if (-not $searchRoot -or -not (Test-Path $searchRoot)) {
    $projectRoot = Split-Path $PSScriptRoot -Parent
    $broadSearch = Join-Path $projectRoot "obj"
    if (Test-Path $broadSearch) {
        $searchRoot = $broadSearch
    }
}

if (-not $searchRoot -or -not (Test-Path $searchRoot)) {
    Write-Host "Could not determine a valid search root for generated files." -ForegroundColor Yellow
    exit 0
}

Write-Host "Searching under: $searchRoot (recursive)" -ForegroundColor Gray

$found = Get-ChildItem -Path $searchRoot -Recurse -Filter "SNEngineJSBindings.g.cs" -ErrorAction SilentlyContinue | Select-Object -First 1

if ($found) {
    Write-Host "Found generated file: $($found.FullName)" -ForegroundColor Green
    $content = Get-Content -Raw -LiteralPath $found.FullName

    $regex = 'GeneratedFacade = @"(?<content>[\s\S]*?)"\s*;'
    $match = [regex]::Match($content, $regex)

    if ($match.Success) {
        $js = $match.Groups['content'].Value
        Write-Host ""
        Write-Host $js
        Write-Host ""
        Write-Host "=== END OF GENERATED JS FACADE ===" -ForegroundColor Cyan
    } else {
        Write-Host "Found the file but could not extract the GeneratedFacade constant." -ForegroundColor Yellow
    }
} else {
    Write-Host "Could not find SNEngineJSBindings.g.cs anywhere under $searchRoot" -ForegroundColor Yellow
    Write-Host "Tip: Try 'dotnet clean' then build again, or use --show-js when running SNEngine.Test"
}

$generatedFile = Get-ChildItem -Path $searchRoot -Recurse -Filter "SNEngineJSBindings.g.cs" -ErrorAction SilentlyContinue |
    Select-Object -First 1 -ExpandProperty FullName

if (-not $generatedFile) {
    Write-Host "Could not find SNEngineJSBindings.g.cs under $searchRoot" -ForegroundColor Yellow
    Write-Host "Make sure you are building SNEngine.API (not just the Generators project)."
    exit 0
}

Write-Host "Found: $generatedFile" -ForegroundColor Gray

$content = Get-Content -Raw -LiteralPath $generatedFile

# Extract content between GeneratedFacade = @" and ";
$regex = 'GeneratedFacade = @"(?<content>[\s\S]*?)"\s*;'
$match = [regex]::Match($content, $regex)

if (-not $match.Success) {
    Write-Host "Could not extract GeneratedFacade constant." -ForegroundColor Yellow
    exit 0
}

$js = $match.Groups['content'].Value

Write-Host ""
Write-Host $js
Write-Host ""
Write-Host "=== END OF GENERATED JS FACADE ===" -ForegroundColor Cyan
Write-Host ""
