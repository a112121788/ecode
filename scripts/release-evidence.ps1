[CmdletBinding()]
param(
    [string] $OutputPath = "",
    [switch] $Json
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$evidence = @(
    [ordered]@{
        id = "build"
        command = ".\\.dotnet\\dotnet.exe build ECodex.sln -c Debug -p:NuGetAudit=false"
        evidencePath = "terminal log"
        windowsOnly = $false
        notes = "Confirms the solution compiles before release validation."
    },
    [ordered]@{
        id = "unit_tests"
        command = ".\\.dotnet\\dotnet.exe test tests\\ECodex.Tests\\ECodex.Tests.csproj -p:NuGetAudit=false"
        evidencePath = "terminal log"
        windowsOnly = $false
        notes = "Must pass with zero failed tests."
    },
    [ordered]@{
        id = "docs_build"
        command = "npm run docs:build"
        evidencePath = "docs/.vitepress/dist/"
        windowsOnly = $false
        notes = "Verifies VitePress documentation output."
    },
    [ordered]@{
        id = "release_ci_gate"
        command = "pwsh ./scripts/ci.ps1 -Config Release -IncludeSmoke"
        evidencePath = "terminal log"
        windowsOnly = $true
        notes = "Runs Release build/test gates plus Windows ConPTY smoke."
    },
    [ordered]@{
        id = "perf_report"
        command = "pwsh ./scripts/perf/measure.ps1 -OutputDir artifacts/perf -Samples 1"
        evidencePath = "artifacts/perf/perf-report.md; artifacts/perf/perf-report.json; ecodex-perf-report"
        windowsOnly = $false
        notes = "Synthetic perf runs everywhere; live status/browser/cold-start flags are Windows-only."
    },
    [ordered]@{
        id = "doctor"
        command = "ecodex doctor"
        evidencePath = "terminal log"
        windowsOnly = $true
        notes = "Requires the Windows CLI package or local Windows build."
    },
    [ordered]@{
        id = "release_workflow"
        command = "GitHub Actions: .github/workflows/release.yml"
        evidencePath = "ecodex-win-x64-sc; ecodex-win-x86-sc; ecodex-win-arm64-sc; ecodex-cli-win-x64; ecodex-perf-report"
        windowsOnly = $true
        notes = "Release workflow runs on windows-latest and uploads app, CLI, and perf artifacts."
    }
)

function ConvertTo-Markdown {
    param([object[]] $Items)

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# ECodex Release Evidence Checklist")
    $lines.Add("")
    $lines.Add("Generated: $((Get-Date).ToUniversalTime().ToString('o'))")
    $lines.Add("")
    $lines.Add("| ID | Command | Evidence path | Windows-only | Notes |")
    $lines.Add("|---|---|---|---|---|")
    foreach ($item in $Items) {
        $command = $item.command -replace "\|", "/"
        $path = $item.evidencePath -replace "\|", "/"
        $notes = $item.notes -replace "\|", "/"
        $windowsOnly = if ($item.windowsOnly) { "yes" } else { "no" }
        $lines.Add("| $($item.id) | ``$command`` | $path | $windowsOnly | $notes |")
    }

    $lines.Add("")
    $lines.Add("## Release workflow artifacts")
    $lines.Add("")
    $lines.Add("- ecodex-win-x64-sc")
    $lines.Add("- ecodex-win-x86-sc")
    $lines.Add("- ecodex-win-arm64-sc")
    $lines.Add("- ecodex-cli-win-x64")
    $lines.Add("- ecodex-perf-report")

    return ($lines -join [Environment]::NewLine)
}

$output = if ($Json) {
    [ordered]@{
        generatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
        items = $evidence
    } | ConvertTo-Json -Depth 6
} else {
    ConvertTo-Markdown -Items $evidence
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $output
} else {
    $directory = Split-Path -Parent $OutputPath
    if (-not [string]::IsNullOrWhiteSpace($directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    Set-Content -LiteralPath $OutputPath -Value $output -Encoding UTF8
    Write-Host "Release evidence checklist written to $OutputPath"
}
