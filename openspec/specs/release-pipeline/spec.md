# release-pipeline Specification

## Purpose
Ship tested builds automatically and keep installed apps up to date.

## Requirements

### Requirement: Pull request validation
Every pull request SHALL build the solution and run all tests on Windows.

#### Scenario: Failing test blocks merge
- **WHEN** a test fails in a pull request
- **THEN** the CI check fails

### Requirement: Single version source
`<Version>` in `Directory.Build.props` SHALL be the only place where the product version is defined. It MUST be `MAJOR.MINOR.PATCH`; stable tags and nightly versions are derived from it.

#### Scenario: Malformed version
- **WHEN** `<Version>` is `0.1`
- **THEN** every workflow that reads it fails with a message naming the expected format

### Requirement: Release gate on pull requests into main
A pull request into `main` SHALL fail CI when the tag `v<Version>` already exists.

#### Scenario: Forgotten version bump
- **WHEN** `v0.1.0` is released and a pull request into `main` still has `<Version>` 0.1.0
- **THEN** CI fails and asks to bump the version on `develop`

### Requirement: Nightly pre-release
A scheduled workflow SHALL build and test `develop` daily and, when it changed in the last 24 hours, publish a Velopack pre-release `v<Version>-nightly.<run_number>` on GitHub Releases. It SHALL refuse to publish when `v<Version>` is already released.

#### Scenario: Quiet day
- **WHEN** `develop` has no new commits in 24 hours
- **THEN** no nightly release is created

#### Scenario: Version not bumped after a release
- **WHEN** `v0.1.0` is released and `develop` still has `<Version>` 0.1.0
- **THEN** the nightly fails and asks to bump the version

### Requirement: Stable release
Every push to `main` SHALL read `<Version>` and, when the tag `v<Version>` does not exist, build and test, sign the binaries when a certificate secret is configured, create the tag and publish a stable GitHub Release with all Velopack assets. When the tag exists, the workflow SHALL skip without failing.

#### Scenario: Merge releases the new version
- **WHEN** a pull request with `<Version>` 0.2.0 is merged into `main` and `v0.2.0` does not exist
- **THEN** the release `v0.2.0` is published from the merge commit

### Requirement: Auto-update
The installed app SHALL check GitHub Releases through Velopack at startup and every four hours, download a newer release in the background, and install it automatically when the app exits. A banner SHALL name the downloaded version and offer "Restart now" to apply it immediately. The time and result of the last check, including errors, SHALL be shown in Settings, and errors SHALL be written to `logs/update.log`.

#### Scenario: Update ready
- **WHEN** a newer stable release exists
- **THEN** a banner shows "Otis 0.3.0 ist bereit und wird beim Schliessen installiert." with a restart action

#### Scenario: Update applied on exit
- **WHEN** an update was downloaded and the user closes Otis
- **THEN** the next start runs the new version without any further action
