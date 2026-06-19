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
            hookEventName            = "PreToolUse"
            permissionDecision       = $decision
            permissionDecisionReason = if ($Reason) { $Reason } else { "Conventional commit policy check completed." }
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

function Test-ConventionalCommit {
    param([string]$Message)

    if ([string]::IsNullOrWhiteSpace($Message)) { return $false }
    return $Message -match '^(feat|fix|docs|style|refactor|perf|test|build|ci|chore|revert)(\([a-z0-9._\/-]+\))?(!)?:\s.+$'
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

    if ($combinedText -notmatch '(?i)\bgit\s+commit\b') {
        Write-Decision -Allow $true
        exit 0
    }

    # Block commits without explicit message to enforce deterministic validation.
    if ($combinedText -notmatch '(?i)\bgit\s+commit\b[^\n\r]*\s-m\s+"([^"]+)"' -and $combinedText -notmatch "(?i)\bgit\s+commit\b[^\n\r]*\s-m\s+'([^']+)'") {
        Write-Decision -Allow $false -Reason "Commit blocked: use an explicit Conventional Commit message with -m, for example 'feat(api): add short URL endpoint'."
        exit 2
    }

    $message = $null
    $doubleQuoteMatch = [regex]::Match($combinedText, '(?i)\bgit\s+commit\b[^\n\r]*\s-m\s+"([^"]+)"')
    if ($doubleQuoteMatch.Success) {
        $message = $doubleQuoteMatch.Groups[1].Value
    }
    else {
        $singleQuoteMatch = [regex]::Match($combinedText, "(?i)\bgit\s+commit\b[^\n\r]*\s-m\s+'([^']+)'")
        if ($singleQuoteMatch.Success) {
            $message = $singleQuoteMatch.Groups[1].Value
        }
    }

    if (-not (Test-ConventionalCommit -Message $message)) {
        Write-Decision -Allow $false -Reason "Commit blocked: message is not Conventional Commits compliant. Expected format 'type(scope): summary', e.g. 'fix(auth): validate token expiry'."
        exit 2
    }

    Write-Decision -Allow $true
    exit 0
}
catch {
    Write-Decision -Allow $false -Reason "Conventional commit hook failed: $($_.Exception.Message)"
    exit 2
}
