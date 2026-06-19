$ErrorActionPreference = "Stop"
if (Get-Variable -Name PSNativeCommandUseErrorActionPreference -ErrorAction SilentlyContinue) {
    $PSNativeCommandUseErrorActionPreference = $false
}

function Write-HookResult {
    param(
        [string]$SystemMessage = ""
    )

    $result = @{
        continue = $true
    }

    if ($SystemMessage) {
        $result.systemMessage = $SystemMessage
    }

    $result | ConvertTo-Json -Compress
}

function Invoke-ReindexAttempt {
    $commands = @(
        @("code", "--command", "github.copilot.chat.reindexWorkspace"),
        @("code", "--command", "github.copilot.reindexWorkspace")
    )

    foreach ($cmd in $commands) {
        try {
            & $cmd[0] $cmd[1] $cmd[2] *> $null
            if ($LASTEXITCODE -eq 0) {
                return $true
            }
        }
        catch {
            continue
        }
    }

    return $false
}

try {
    # Consume hook input to keep contract stable, even when not directly needed.
    $rawInput = [Console]::In.ReadToEnd()
    if ($null -eq $rawInput) { $rawInput = "" }

    if (-not (Test-Path ".git")) {
        Write-HookResult
        exit 0
    }

    $repoRoot = (git rev-parse --show-toplevel 2>$null)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repoRoot)) {
        Write-HookResult
        exit 0
    }

    $statusLines = git -C "$repoRoot" status --porcelain
    if ($LASTEXITCODE -ne 0) {
        Write-HookResult
        exit 0
    }

    if ([string]::IsNullOrWhiteSpace(($statusLines -join ""))) {
        Write-HookResult
        exit 0
    }

    $repoHashBytes = [System.Text.Encoding]::UTF8.GetBytes($repoRoot.Trim().ToLowerInvariant())
    $repoHash = [System.BitConverter]::ToString((New-Object Security.Cryptography.SHA256Managed).ComputeHash($repoHashBytes)).Replace("-", "").ToLowerInvariant()
    $statusFingerprint = [string]::Join("`n", $statusLines)
    $fingerprintPath = Join-Path $env:TEMP ("copilot-reindex-" + $repoHash + ".fingerprint")

    if (Test-Path $fingerprintPath) {
        $previousFingerprint = Get-Content -Raw $fingerprintPath
        if ($previousFingerprint -eq $statusFingerprint) {
            Write-HookResult
            exit 0
        }
    }

    Set-Content -Path $fingerprintPath -Value $statusFingerprint -NoNewline

    if (Invoke-ReindexAttempt) {
        Write-HookResult -SystemMessage "Code changes detected. Workspace reindex triggered."
        exit 0
    }

    Write-HookResult -SystemMessage "Code changes detected. Automatic workspace reindex command was not available; run your workspace reindex command manually."
    exit 0
}
catch {
    Write-HookResult -SystemMessage "Post-change reindex hook error: $($_.Exception.Message)"
    exit 0
}
