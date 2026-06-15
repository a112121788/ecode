# ECodex PowerShell native argument completion.
# Usage:
#   . /path/to/scripts/completions/ecodex.ps1
# or:
#   ecodex completion powershell | Invoke-Expression

$script:ECodexCommands = @(
    'notify',
    'notification',
    'window',
    'workspace',
    'surface',
    'pane',
    'browser',
    'split',
    'reload-config',
    'config',
    'profile',
    'setup',
    'update',
    'restore-session',
    'status',
    'health',
    'doctor',
    'completion',
    'help',
    'version'
)

$script:ECodexSubcommands = @{
    notification = @('list', 'read', 'unread', 'jump-latest', 'clear')
    window = @('list', 'current', 'focus', 'create', 'close')
    workspace = @('list', 'create', 'select', 'close', 'rename', 'reorder')
    surface = @('list', 'create', 'select', 'close', 'rename', 'move', 'reorder', 'resume')
    pane = @('list', 'focus', 'write', 'read', 'split', 'close', 'resize', 'swap', 'zoom')
    browser = @('open', 'new', 'open-split', 'snapshot', 'click', 'fill', 'hover', 'press', 'eval', 'screenshot')
    split = @('right', 'down')
    config = @('reload', 'diagnostics', 'diag')
    profile = @('import', 'import-terminal', 'terminal')
    setup = @('install', 'status', 'uninstall')
    update = @('check', 'install')
    completion = @('powershell')
}

$script:ECodexResumeSubcommands = @('set', 'show', 'clear')
$script:ECodexGlobalOptions = @('--json', '--id-format')
$script:ECodexCommonOptions = @(
    '--id', '--ref', '--window', '--workspace', '--surface', '--pane',
    '--name', '--title', '--body', '--lines', '--text', '--value',
    '--url', '--surfaceRef', '--direction', '--submit',
    '--settings', '--write', '--commandline', '--shell', '--font-face',
    '--font-size', '--color-scheme', '--starting-directory', '--guid',
    '--timeout-ms', '--install-dir', '--profile', '--powershell-profile',
    '--feed-url', '--setup-url', '--installer-url', '--download-dir',
    '--download-only', '--pack-id', '--silent', '--wait'
)
$script:ECodexRefPrefixes = @('window:', 'workspace:', 'surface:', 'pane:')

function New-ECodexCompletionResult {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [string]$ToolTip = $Text
    )

    [System.Management.Automation.CompletionResult]::new(
        $Text,
        $Text,
        [System.Management.Automation.CompletionResultType]::ParameterValue,
        $ToolTip
    )
}

function Get-ECodexMatchingResults {
    param(
        [Parameter(Mandatory = $true)][string[]]$Items,
        [AllowNull()][string]$WordToComplete
    )

    $word = if ($null -eq $WordToComplete) { '' } else { $WordToComplete }
    $Items |
        Where-Object { $_ -like "$word*" } |
        Sort-Object -Unique |
        ForEach-Object { New-ECodexCompletionResult $_ }
}

function Get-ECodexRefResults {
    param([AllowNull()][string]$WordToComplete)

    $word = if ($null -eq $WordToComplete) { '' } else { $WordToComplete }
    foreach ($prefix in $script:ECodexRefPrefixes) {
        foreach ($index in 1..9) {
            $candidate = "$prefix$index"
            if ($candidate -like "$word*") {
                New-ECodexCompletionResult $candidate "ECodex short ref"
            }
        }
    }
}

Register-ArgumentCompleter -Native -CommandName ecodex -ScriptBlock {
    param($wordToComplete, $commandAst, $cursorPosition)

    $words = @(
        $commandAst.CommandElements |
            ForEach-Object { $_.Extent.Text.Trim('"', "'") }
    )

    if ($words.Count -le 1) {
        Get-ECodexMatchingResults $script:ECodexCommands $wordToComplete
        return
    }

    $command = $words[1].ToLowerInvariant()

    if ($wordToComplete -like '--*') {
        Get-ECodexMatchingResults ($script:ECodexGlobalOptions + $script:ECodexCommonOptions) $wordToComplete
        return
    }

    if ($words.Count -le 2 -and $script:ECodexCommands -contains $command) {
        Get-ECodexMatchingResults $script:ECodexCommands $wordToComplete
        return
    }

    if ($script:ECodexSubcommands.ContainsKey($command) -and $words.Count -le 3) {
        Get-ECodexMatchingResults $script:ECodexSubcommands[$command] $wordToComplete
        return
    }

    if ($command -eq 'surface' -and $words.Count -ge 3 -and $words[2].ToLowerInvariant() -eq 'resume' -and $words.Count -le 4) {
        Get-ECodexMatchingResults $script:ECodexResumeSubcommands $wordToComplete
        return
    }

    if ($wordToComplete -match '^(window|workspace|surface|pane):\d*$') {
        Get-ECodexRefResults $wordToComplete
        return
    }

    if ($words.Count -ge 3) {
        $subcommand = $words[2].ToLowerInvariant()
        $commandsThatAcceptRefs = @(
            'focus', 'select', 'close', 'rename', 'read',
            'resize', 'swap', 'move', 'reorder', 'unread', 'read'
        )

        if ($commandsThatAcceptRefs -contains $subcommand) {
            Get-ECodexRefResults $wordToComplete
            return
        }
    }

    Get-ECodexMatchingResults ($script:ECodexGlobalOptions + $script:ECodexCommonOptions) $wordToComplete
}
