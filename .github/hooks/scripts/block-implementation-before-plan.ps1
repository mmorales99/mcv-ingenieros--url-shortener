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
            permissionDecisionReason = if ($Reason) { $Reason } else { "Plan-first policy check completed." }
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

function Test-PlanExists {
    if (-not (Test-Path "openspec\\changes")) {
        return $false
    }

    $planFiles = Get-ChildItem -Path "openspec\\changes" -Recurse -File -Filter "tasks.md" -ErrorAction SilentlyContinue
    foreach ($planFile in $planFiles) {
        if ($planFile.Length -gt 0) {
            return $true
        }
    }

    return $false
}

function Test-PlanningOnlyPatch {
    param([string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $false
    }

    $pathMatches = [regex]::Matches($Text, '\*\*\*\s+(?:Add|Update|Delete)\s+File:\s+([^\r\n]+)')
    if ($pathMatches.Count -eq 0) {
        return $false
    }

    foreach ($match in $pathMatches) {
        $path = $match.Groups[1].Value.Trim().Replace("\", "/").ToLowerInvariant()

        $isPlanningPath = (
            $path.StartsWith("openspec/") -or
            $path.StartsWith(".github/") -or
            $path.StartsWith("docs/") -or
            $path.EndsWith(".md")
        )

        if (-not $isPlanningPath) {
            return $false
        }
    }

    return $true
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

    $looksLikeEdit = $combinedText -match '(?i)\b(apply_patch|edit)\b' -or $combinedText -match '(?m)^\*\*\*\s+Begin Patch$'
    if (-not $looksLikeEdit) {
        Write-Decision -Allow $true
        exit 0
    }

    if (Test-PlanExists) {
        Write-Decision -Allow $true
        exit 0
    }

    if (Test-PlanningOnlyPatch -Text $combinedText) {
        Write-Decision -Allow $true
        exit 0
    }

    Write-Decision -Allow $false -Reason "Plan-first policy: create and approve a plan (for example openspec/changes/<change>/tasks.md) before implementation edits."
    exit 2
}
catch {
    Write-Decision -Allow $false -Reason "Plan-first policy hook failed: $($_.Exception.Message)"
    exit 2
}
