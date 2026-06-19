$ErrorActionPreference = "Stop"
if (Get-Variable -Name PSNativeCommandUseErrorActionPreference -ErrorAction SilentlyContinue) {
    $PSNativeCommandUseErrorActionPreference = $false
}

function Write-Decision {
    param(
        [bool]$Allow,
        [string]$Reason = ""
    )

    $decision = if ($Allow) { "allow" } else { "deny" }
    $result = @{
        continue           = $Allow
        hookSpecificOutput = @{
            hookEventName             = "PreToolUse"
            permissionDecision        = $decision
            permissionDecisionReason  = if ($Reason) { $Reason } else { "Policy check completed." }
        }
    }

    if (-not $Allow) {
        $result.stopReason = $Reason
    }

    $result | ConvertTo-Json -Compress
}

function Get-AllStringValues {
    param([object]$Value)

    if ($null -eq $Value) { return @() }
    if ($Value -is [string]) { return @($Value) }

    $values = @()

    if ($Value -is [psobject]) {
        foreach ($property in $Value.PSObject.Properties) {
            $values += Get-AllStringValues -Value $property.Value
        }
        return $values
    }

    if ($Value -is [System.Collections.IDictionary]) {
        foreach ($key in $Value.Keys) {
            $values += Get-AllStringValues -Value $Value[$key]
        }
        return $values
    }

    if ($Value -is [System.Collections.IEnumerable] -and -not ($Value -is [string])) {
        foreach ($item in $Value) {
            $values += Get-AllStringValues -Value $item
        }
        return $values
    }

    return @()
}

try {
    $rawInput = [Console]::In.ReadToEnd()
    if ([string]::IsNullOrWhiteSpace($rawInput)) {
        Write-Decision -Allow $true
        exit 0
    }

    $payload = $rawInput | ConvertFrom-Json
    $allStrings = Get-AllStringValues -Value $payload
    $combinedText = ($allStrings -join "`n")

    if ($combinedText -notmatch '(?i)\bgit\s+push\b') {
        Write-Decision -Allow $true
        exit 0
    }

    $targetsMainDirectly = $combinedText -match '(?i)\bgit\s+push\b[^\n\r]*\b(origin\s+main|main:main|head:main|refs/heads/main)\b'
    $plainPushCommand = $combinedText -match '(?i)^\s*git\s+push(?:\s+-[^\s]+|\s+--[^\s]+|\s+origin)?\s*$'

    if ($targetsMainDirectly) {
        Write-Decision -Allow $false -Reason "Pushing to 'main' is blocked by policy. Push to a feature branch and open a PR."
        exit 2
    }

    if ($plainPushCommand -and (Test-Path ".git")) {
        $currentBranch = (git branch --show-current 2>$null).Trim().ToLowerInvariant()
        if ($currentBranch -eq "main") {
            Write-Decision -Allow $false -Reason "Direct push from local 'main' is blocked by policy. Create/use a feature branch first."
            exit 2
        }
    }

    Write-Decision -Allow $true
    exit 0
}
catch {
    Write-Decision -Allow $false -Reason "Main push policy hook failed: $($_.Exception.Message)"
    exit 2
}
