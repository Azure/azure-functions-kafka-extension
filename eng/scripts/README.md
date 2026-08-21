# Manual release preparation

## Motivation

Microsoft policy changes prevent the existing GitHub Actions release pipeline
from using the required credentials. `Prepare-Release.ps1` temporarily replaces
the release preparation workflow by running it locally with the operator's
GitHub credentials.

This is a transitional solution. The release process will move to an
Azure DevOps pipeline soon, and the `master` branch is expected to be retired.

## Usage

The script requires PowerShell 7.2+, Git, GitHub CLI authentication, and a Git
signing key configured in `user.signingkey`.

Validate the complete preparation locally without pushing:

```powershell
pwsh ./eng/scripts/Prepare-Release.ps1 -Version 4.3.3 -WhatIf
```

Create and push `release/4.3.3`, then open a pull request to `master`:

```powershell
pwsh ./eng/scripts/Prepare-Release.ps1 -Version 4.3.3
```

If the branch was pushed but pull request creation failed, resume from it:

```powershell
pwsh ./eng/scripts/Prepare-Release.ps1 -Version 4.3.3 -Resume
```
