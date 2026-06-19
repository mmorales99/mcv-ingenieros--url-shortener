$ErrorActionPreference = "Stop"
if (Get-Variable -Name PSNativeCommandUseErrorActionPreference -ErrorAction SilentlyContinue) {
    $PSNativeCommandUseErrorActionPreference = $false
}

function Write-HookResult {
    param(
        [bool]$Continue = $true,
        [string]$SystemMessage = "",
        [string]$StopReason = ""
    )

    $result = @{
        continue = $Continue
    }

    if ($SystemMessage) { $result.systemMessage = $SystemMessage }
    if ($StopReason) { $result.stopReason = $StopReason }

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
    $payload = $null
    if (-not [string]::IsNullOrWhiteSpace($rawInput)) {
        $payload = $rawInput | ConvertFrom-Json
    }

    $allStrings = Get-AllStringValues -Value $payload
    $combinedText = ($allStrings -join "`n")

    if ($combinedText -notmatch '(?i)\b(new|start)\s+feature\b') {
        Write-HookResult -Continue $true
        exit 0
    }

    $issueMatch = [regex]::Match($combinedText, '(?i)(?:#|issue\s*)(\d+)')
    if (-not $issueMatch.Success) {
        Write-HookResult -Continue $false -StopReason "Feature request detected, but no issue number found. Include '#123' or 'issue 123' in your request."
        exit 2
    }

    $issueNumber = $issueMatch.Groups[1].Value

    $descriptionMatch = [regex]::Match($combinedText, '(?i)(?:#|issue\s*)\d+\s*[:\-]\s*([a-z0-9][a-z0-9\-\s_]{2,})')
    if (-not $descriptionMatch.Success) {
        Write-HookResult -Continue $false -StopReason "Feature request detected, but no short description found. Use format like: 'start feature #123 - short description'."
        exit 2
    }

    $shortDescription = $descriptionMatch.Groups[1].Value.ToLowerInvariant()
    $shortDescription = ($shortDescription -replace '[^a-z0-9\s-]', '').Trim()
    $shortDescription = ($shortDescription -replace '\s+', '-').Trim('-')

    if ([string]::IsNullOrWhiteSpace($shortDescription)) {
        Write-HookResult -Continue $false -StopReason "The short description is empty after sanitization. Please provide a meaningful description."
        exit 2
    }

    $targetBranch = "feature/$issueNumber--$shortDescription"

    if (-not (Test-Path ".git")) {
        Write-HookResult -Continue $false -StopReason "Feature policy hook requires a git repository."
        exit 2
    }

    $status = git status --porcelain
    if ($LASTEXITCODE -ne 0) {
        Write-HookResult -Continue $false -StopReason "Could not read git status."
        exit 2
    }

    if (-not [string]::IsNullOrWhiteSpace(($status -join ""))) {
        Write-HookResult -Continue $false -StopReason "Working tree is not clean. Commit or stash changes before starting a new feature branch."
        exit 2
    }

    $currentBranch = (git branch --show-current).Trim()
    if ($currentBranch -eq $targetBranch) {
        Write-HookResult -Continue $true -SystemMessage "Already on branch '$targetBranch'."
        exit 0
    }

    git fetch origin main
    if ($LASTEXITCODE -ne 0) {
        Write-HookResult -Continue $false -StopReason "Failed to fetch origin/main."
        exit 2
    }

    git checkout main
    if ($LASTEXITCODE -ne 0) {
        Write-HookResult -Continue $false -StopReason "Failed to checkout main."
        exit 2
    }

    git pull --ff-only origin main
    if ($LASTEXITCODE -ne 0) {
        Write-HookResult -Continue $false -StopReason "Failed to pull latest main with --ff-only."
        exit 2
    }

    git show-ref --verify --quiet "refs/heads/$targetBranch"
    if ($LASTEXITCODE -eq 0) {
        git checkout $targetBranch
        if ($LASTEXITCODE -ne 0) {
            Write-HookResult -Continue $false -StopReason "Branch '$targetBranch' exists but checkout failed."
            exit 2
        }
    }
    else {
        git checkout -b $targetBranch
        if ($LASTEXITCODE -ne 0) {
            Write-HookResult -Continue $false -StopReason "Failed to create branch '$targetBranch'."
            exit 2
        }
    }

    Write-HookResult -Continue $true -SystemMessage "Feature branch ready: $targetBranch (main synced first)."
    exit 0
}
catch {
    Write-HookResult -Continue $false -StopReason "Feature policy hook failed: $($_.Exception.Message)"
    exit 2
}
