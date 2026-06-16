[CmdletBinding()]
param(
    [string] $CliPath = "",
    [string] $WorkspaceName = "smoke-ecodex-v2-$([DateTimeOffset]::UtcNow.ToString('yyyyMMdd-HHmmss'))",
    [string] $WorkspaceRoot = "",
    [string] $BrowserUrl = "data:text/html,%3Ctitle%3Esmoke-ecodex-v2%3C/title%3E%3Ch1%3Esmoke-ecodex-v2%3C/h1%3E"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$TmpRoot = Join-Path $RepoRoot "tmp"
if ([string]::IsNullOrWhiteSpace($WorkspaceRoot)) {
    $WorkspaceRoot = Join-Path $TmpRoot $WorkspaceName
}

function Write-SmokeStep {
    param([string] $Message)
    Write-Host "[smoke-ecodex-v2] $Message" -ForegroundColor Cyan
}

function Write-SmokeSkip {
    param([string] $Reason)
    Write-Host "[smoke-ecodex-v2] SKIP: $Reason" -ForegroundColor Yellow
    exit 0
}

function Test-IsWindows {
    return [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
        [System.Runtime.InteropServices.OSPlatform]::Windows)
}

function Test-IsUnderDirectory {
    param(
        [string] $Path,
        [string] $Root
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path).TrimEnd('\', '/')
    $fullRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    return $fullPath.Equals($fullRoot, [System.StringComparison]::OrdinalIgnoreCase) -or
        $fullPath.StartsWith($fullRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)
}

function Resolve-ECodexCli {
    if (-not [string]::IsNullOrWhiteSpace($CliPath)) {
        if (-not (Test-Path -LiteralPath $CliPath)) {
            throw "ECodex CLI was not found at CliPath: $CliPath"
        }

        return (Resolve-Path -LiteralPath $CliPath).Path
    }

    $repoCli = Join-Path $RepoRoot "src\ECodex.Cli\bin\Debug\net10.0-windows\ecodex.exe"
    if (Test-Path -LiteralPath $repoCli) {
        return (Resolve-Path -LiteralPath $repoCli).Path
    }

    $command = Get-Command ecodex -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    return $null
}

function Get-JsonProperty {
    param(
        [object] $Object,
        [string] $Name
    )

    if ($null -eq $Object) {
        return $null
    }

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}

function Invoke-ECodexJson {
    param([string[]] $Arguments)

    Write-SmokeStep "ecodex $($Arguments -join ' ')"
    $output = & $script:ResolvedCli @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    $text = ($output | Out-String).Trim()

    if ($exitCode -ne 0) {
        throw "ecodex exited with $exitCode for args: $($Arguments -join ' '). Output: $text"
    }

    if ([string]::IsNullOrWhiteSpace($text)) {
        throw "ecodex returned empty output for args: $($Arguments -join ' ')"
    }

    try {
        return $text | ConvertFrom-Json
    }
    catch {
        throw "ecodex returned non-JSON output for args: $($Arguments -join ' '). Output: $text"
    }
}

function Assert-ECodexSuccess {
    param(
        [object] $Response,
        [string] $StepName
    )

    $errorValue = Get-JsonProperty $Response "error"
    if ($null -ne $errorValue) {
        $details = $errorValue | ConvertTo-Json -Compress
        throw "$StepName failed: $details"
    }

    $okValue = Get-JsonProperty $Response "ok"
    if ($null -ne $okValue -and $okValue -eq $false) {
        $details = $Response | ConvertTo-Json -Compress
        throw "$StepName failed: $details"
    }
}

function Get-ECodexResult {
    param([object] $Response)

    $result = Get-JsonProperty $Response "result"
    if ($null -ne $result) {
        return $result
    }

    return $Response
}

function Wait-ForPaneText {
    param(
        [string] $WorkspaceTarget,
        [string] $ExpectedText
    )

    $lastText = ""
    for ($i = 0; $i -lt 20; $i++) {
        $read = Invoke-ECodexJson @('--json', 'pane', 'read', '--workspace', $WorkspaceTarget, '--lines', '80', '--maxChars', '12000')
        Assert-ECodexSuccess $read "pane.read"
        $readResult = Get-ECodexResult $read
        $lastText = [string] (Get-JsonProperty $readResult "text")
        if ($lastText.Contains($ExpectedText, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $lastText
        }

        Start-Sleep -Milliseconds 250
    }

    throw "pane.read did not include expected text '$ExpectedText'. Last text: $lastText"
}

function Wait-ForBrowserSnapshot {
    param([string] $SurfaceRef)

    $lastError = $null
    for ($i = 0; $i -lt 20; $i++) {
        try {
            $snapshot = Invoke-ECodexJson @('--json', 'browser', 'snapshot', '--surfaceRef', $SurfaceRef)
            Assert-ECodexSuccess $snapshot "browser.snapshot"
            return $snapshot
        }
        catch {
            $lastError = $_.Exception.Message
            Start-Sleep -Milliseconds 250
        }
    }

    throw "browser.snapshot did not become ready for $SurfaceRef. Last error: $lastError"
}

if (-not (Test-IsWindows)) {
    Write-SmokeSkip "smoke-ecodex-v2 requires Windows because it drives the WPF app pipe and WebView2."
}

$script:ResolvedCli = Resolve-ECodexCli
if ([string]::IsNullOrWhiteSpace($script:ResolvedCli)) {
    Write-SmokeSkip "ECodex CLI was not found. Build the CLI first or pass -CliPath."
}

$workspaceTarget = $null
$workspaceId = $null
$workspaceRootFull = [System.IO.Path]::GetFullPath($WorkspaceRoot)
$tmpRootFull = [System.IO.Path]::GetFullPath($TmpRoot)
$removeWorkspaceRoot = Test-IsUnderDirectory $workspaceRootFull $tmpRootFull

try {
    try {
        $status = Invoke-ECodexJson @('--json', 'status')
        Assert-ECodexSuccess $status "status"
    }
    catch {
        Write-SmokeSkip "ECodex app is not running or status failed: $($_.Exception.Message)"
    }

    New-Item -ItemType Directory -Path $workspaceRootFull -Force | Out-Null

    $workspaceCreate = Invoke-ECodexJson @('--json', 'workspace', 'create', '--name', $WorkspaceName, '--cwd', $workspaceRootFull)
    Assert-ECodexSuccess $workspaceCreate "workspace.create"
    $workspaceResult = Get-ECodexResult $workspaceCreate
    $workspace = Get-JsonProperty $workspaceResult "workspace"
    if ($null -eq $workspace) {
        throw "workspace.create did not return result.workspace."
    }

    $workspaceRef = Get-JsonProperty $workspace "ref"
    $workspaceId = Get-JsonProperty $workspace "id"
    $workspaceTarget = if (-not [string]::IsNullOrWhiteSpace($workspaceRef)) { $workspaceRef } else { $workspaceId }
    if ([string]::IsNullOrWhiteSpace($workspaceTarget)) {
        throw "workspace.create did not return workspace ref or id."
    }

    $marker = "smoke-ecodex-v2-$([Guid]::NewGuid().ToString('N'))"
    $paneWrite = Invoke-ECodexJson @('--json', 'pane', 'write', '--workspace', $workspaceTarget, "echo $marker", '--submit', 'true')
    Assert-ECodexSuccess $paneWrite "pane.write"
    $paneText = Wait-ForPaneText -WorkspaceTarget $workspaceTarget -ExpectedText $marker

    $browserOpen = Invoke-ECodexJson @('--json', 'browser', 'open', $BrowserUrl, '--workspaceId', $workspaceId, '--name', 'smoke-ecodex-v2')
    Assert-ECodexSuccess $browserOpen "browser.open"
    $browserResult = Get-ECodexResult $browserOpen
    $surfaceRef = Get-JsonProperty $browserResult "surfaceRef"
    if ([string]::IsNullOrWhiteSpace($surfaceRef)) {
        throw "browser.open did not return surfaceRef."
    }

    $snapshot = Wait-ForBrowserSnapshot -SurfaceRef $surfaceRef

    [ordered]@{
        status = "passed"
        workspace = $WorkspaceName
        workspaceTarget = $workspaceTarget
        paneTextMatched = $paneText.Contains($marker, [System.StringComparison]::OrdinalIgnoreCase)
        browserSurfaceRef = $surfaceRef
        browserSnapshotOk = ($null -eq (Get-JsonProperty $snapshot "error"))
    } | ConvertTo-Json -Depth 5
}
finally {
    if (-not [string]::IsNullOrWhiteSpace($workspaceId)) {
        try {
            Invoke-ECodexJson @('--json', 'workspace', 'close', $workspaceId) | Out-Null
        }
        catch {
            Write-Warning "Failed to close smoke workspace ${workspaceId}: $($_.Exception.Message)"
        }
    }

    if ($removeWorkspaceRoot -and (Test-Path -LiteralPath $workspaceRootFull)) {
        Remove-Item -LiteralPath $workspaceRootFull -Recurse -Force -ErrorAction SilentlyContinue
    }
}
