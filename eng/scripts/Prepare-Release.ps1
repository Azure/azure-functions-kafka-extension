#requires -Version 7.2

<#
.SYNOPSIS
Prepares a release branch and opens a pull request from a local workstation.

.DESCRIPTION
Replaces the GitHub Actions release-prep workflow without storing a PAT in
GitHub. The script uses the caller's existing git and GitHub CLI credentials.
All repository changes are made in a temporary worktree so the current checkout
is left unchanged. The release commit is signed with the key configured in
git user.signingkey. SSH and OpenPGP signing keys are supported.

Use -WhatIf to perform the complete local preparation, including the release
commit, without pushing a branch or creating a pull request.

.EXAMPLE
pwsh ./eng/scripts/Prepare-Release.ps1 -Version 4.3.3 -WhatIf

.EXAMPLE
pwsh ./eng/scripts/Prepare-Release.ps1 -Version 4.3.3

.EXAMPLE
pwsh ./eng/scripts/Prepare-Release.ps1 -Version 4.3.3 -Resume
#>

[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+(?:-[A-Za-z0-9.]+)?$')]
    [string] $Version,

    [Parameter()]
    [switch] $Resume
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory)]
        [string] $Command,

        [Parameter()]
        [string[]] $Arguments = @()
    )

    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "'$Command $($Arguments -join ' ')' failed with exit code $LASTEXITCODE."
    }
}

function Get-NativeCommandOutput {
    param(
        [Parameter(Mandatory)]
        [string] $Command,

        [Parameter()]
        [string[]] $Arguments = @()
    )

    $output = & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "'$Command $($Arguments -join ' ')' failed with exit code $LASTEXITCODE."
    }

    return @($output)
}

function Get-Changelog {
    $versionCommits = @(Get-NativeCommandOutput git @(
        'log',
        'origin/dev',
        '--first-parent',
        '--format=%H',
        '--',
        'build/common.props'
    ))

    if ($versionCommits.Count -eq 0) {
        return @('- Initial release')
    }

    $lastVersionCommit = $versionCommits[0]
    $entries = @(Get-NativeCommandOutput git @(
        'log',
        "$lastVersionCommit..origin/dev",
        '--first-parent',
        '--no-merges',
        '--pretty=format:- %s',
        '--grep=^Bump version',
        '--grep=^Update common.props',
        '--grep=^Updating extension version',
        '--grep=^Update version',
        '--invert-grep'
    ) | Select-Object -First 50)

    if ($entries.Count -eq 0) {
        return @('- No functional changes since last release')
    }

    return $entries
}

function Write-PullRequestBody {
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    $changelog = @(Get-Changelog)
    $bodyLines = @(
        "## Release $Version"
        ''
        "This PR syncs the ``dev`` branch content to ``master`` for release **$Version**."
        ''
        '### What this PR does'
        '- Replaces the master tree with dev content'
        '- Removes dev-only files'
        "- Updates ``build/common.props`` to ``$Version``"
        ''
        '### Changes since last release'
        $changelog
    )
    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllLines($Path, $bodyLines, $utf8NoBom)
}

function New-ReleasePullRequest {
    param(
        [Parameter(Mandatory)]
        [string] $BodyPath
    )

    Invoke-NativeCommand gh @(
        'label',
        'create',
        'release',
        '--repo',
        'Azure/azure-functions-kafka-extension',
        '--description',
        'Release PR',
        '--color',
        '0E8A16',
        '--force'
    )

    Invoke-NativeCommand gh @(
        'pr',
        'create',
        '--repo',
        'Azure/azure-functions-kafka-extension',
        '--base',
        'master',
        '--head',
        $releaseBranch,
        '--title',
        "[Release] $Version",
        '--body-file',
        $BodyPath,
        '--label',
        'release'
    )
}

foreach ($command in @('git', 'gh')) {
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
        throw "Required command '$command' was not found."
    }
}

$repoRoot = @(Get-NativeCommandOutput git @('rev-parse', '--show-toplevel'))[0]
$originUrl = @(Get-NativeCommandOutput git @('-C', $repoRoot, 'remote', 'get-url', 'origin'))[0]
if ($originUrl -notmatch '(^|[:/])Azure/azure-functions-kafka-extension(?:\.git)?$') {
    throw "Unexpected origin repository: $originUrl"
}

Invoke-NativeCommand gh @('auth', 'status', '--hostname', 'github.com')
Invoke-NativeCommand git @('-C', $repoRoot, 'fetch', 'origin', 'master', 'dev', '--tags')

$releaseBranch = "release/$Version"
$remoteBranch = & git -C $repoRoot ls-remote --exit-code --heads origin "refs/heads/$releaseBranch"
$remoteBranchExitCode = $LASTEXITCODE
if ($remoteBranchExitCode -eq 0) {
    if (-not $Resume) {
        throw "Remote branch '$releaseBranch' already exists. Use -Resume to create its pull request."
    }

    Invoke-NativeCommand git @(
        '-C',
        $repoRoot,
        'fetch',
        'origin',
        "$releaseBranch`:refs/remotes/origin/$releaseBranch"
    )

    $remoteVersionContent = @(
        Get-NativeCommandOutput git @(
            '-C',
            $repoRoot,
            'show',
            "origin/$releaseBranch`:build/common.props"
        )
    ) -join "`n"
    $remoteVersionMatch = [regex]::Match($remoteVersionContent, '<Version>([^<]+)</Version>')
    if (-not $remoteVersionMatch.Success -or $remoteVersionMatch.Groups[1].Value -ne $Version) {
        throw "Remote branch '$releaseBranch' does not contain version '$Version'."
    }

    $remoteCommit = @(
        Get-NativeCommandOutput git @('-C', $repoRoot, 'cat-file', 'commit', "origin/$releaseBranch")
    )
    if (-not ($remoteCommit | Where-Object { $_ -match '^gpgsig ' })) {
        throw "Remote branch '$releaseBranch' does not point to a signed commit."
    }

    $resumeBodyPath = Join-Path ([System.IO.Path]::GetTempPath()) "release-$Version-pr-body.md"
    try {
        Push-Location $repoRoot
        try {
            Write-PullRequestBody -Path $resumeBodyPath
        }
        finally {
            Pop-Location
        }

        if ($PSCmdlet.ShouldProcess(
            "GitHub pull request from $releaseBranch to master",
            'Create release pull request'
        )) {
            New-ReleasePullRequest -BodyPath $resumeBodyPath
        }
    }
    finally {
        [System.IO.File]::Delete($resumeBodyPath)
    }

    return
}
if ($remoteBranchExitCode -ne 2) {
    throw "Could not check whether remote branch '$releaseBranch' exists."
}
if ($Resume) {
    throw "Remote branch '$releaseBranch' does not exist, so there is nothing to resume."
}

$configuredName = @(Get-NativeCommandOutput git @('-C', $repoRoot, 'config', 'user.name'))
$configuredEmail = @(Get-NativeCommandOutput git @('-C', $repoRoot, 'config', 'user.email'))
if ($configuredName.Count -eq 0 -or $configuredEmail.Count -eq 0) {
    throw 'Configure git user.name and user.email before preparing a release.'
}

$signingKey = & git -C $repoRoot config --get user.signingkey
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($signingKey)) {
    throw 'Configure git user.signingkey before preparing a release.'
}

$signingFormat = & git -C $repoRoot config --get gpg.format
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($signingFormat)) {
    if ($signingKey.EndsWith('.pub', [StringComparison]::OrdinalIgnoreCase)) {
        $signingFormat = 'ssh'
    }
    else {
        $signingFormat = 'openpgp'
    }
}

if ($signingFormat -eq 'ssh' -and -not (Test-Path $signingKey -PathType Leaf)) {
    throw "SSH signing key '$signingKey' was not found."
}

$validationId = [Guid]::NewGuid().ToString('N')
$localBranch = if ($WhatIfPreference) { "validation/release-$Version-$validationId" } else { $releaseBranch }
$worktreePath = Join-Path ([System.IO.Path]::GetTempPath()) "kafka-release-$validationId"
$worktreeAdded = $false

try {
    Invoke-NativeCommand git @(
        '-C',
        $repoRoot,
        'worktree',
        'add',
        '-b',
        $localBranch,
        $worktreePath,
        'origin/master'
    )
    $worktreeAdded = $true

    Push-Location $worktreePath
    try {
        Invoke-NativeCommand git @('checkout', 'origin/dev', '--', '.')

        Invoke-NativeCommand git @(
            'rm',
            '-r',
            '--force',
            '--quiet',
            '--ignore-unmatch',
            'samples',
            'Architecture.md',
            'AGENT-RULES.md',
            'lint-architecture.ps1',
            'update-version.ps1',
            'CHECKLIST-*.md',
            'PLAN-*.md',
            'SOLUTION-*.md'
        )

        $propsPath = Join-Path $worktreePath 'build/common.props'
        $propsContent = Get-Content $propsPath -Raw
        $versionMatches = [regex]::Matches($propsContent, '<Version>[^<]*</Version>')
        if ($versionMatches.Count -ne 1) {
            throw "Expected one Version element in build/common.props, found $($versionMatches.Count)."
        }

        $updatedPropsContent = [regex]::Replace(
            $propsContent,
            '<Version>[^<]*</Version>',
            "<Version>$Version</Version>"
        )
        $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
        [System.IO.File]::WriteAllText($propsPath, $updatedPropsContent, $utf8NoBom)

        Invoke-NativeCommand git @('add', '--all')
        $stagedChanges = @(Get-NativeCommandOutput git @('diff', '--cached', '--name-only'))
        if ($stagedChanges.Count -eq 0) {
            throw 'Release preparation produced no changes.'
        }

        Invoke-NativeCommand git @(
            '-c',
            "gpg.format=$signingFormat",
            'commit',
            "--gpg-sign=$signingKey",
            '-m',
            "[Release] Prepare $Version from dev"
        )

        $commitObject = @(Get-NativeCommandOutput git @('cat-file', 'commit', 'HEAD'))
        if (-not ($commitObject | Where-Object { $_ -match '^gpgsig ' })) {
            throw 'The release commit was created without a signature.'
        }

        $bodyPath = Join-Path $worktreePath '.release-pr-body.md'
        Write-PullRequestBody -Path $bodyPath

        Write-Host ''
        Write-Host "Prepared release commit:"
        Invoke-NativeCommand git @('--no-pager', 'show', '--stat', '--oneline', '--summary', 'HEAD')

        if ($PSCmdlet.ShouldProcess(
            "origin/$releaseBranch and GitHub",
            "Push release branch and create pull request to master"
        )) {
            if ($localBranch -ne $releaseBranch) {
                throw 'Internal error: validation branch cannot be pushed.'
            }

            Invoke-NativeCommand git @('push', '--set-upstream', 'origin', $releaseBranch)
            New-ReleasePullRequest -BodyPath $bodyPath
        }
    }
    finally {
        Pop-Location
    }
}
finally {
    if ($worktreeAdded) {
        Invoke-NativeCommand git @('-C', $repoRoot, 'worktree', 'remove', '--force', $worktreePath)
    }

    & git -C $repoRoot branch -D $localBranch *> $null
}
