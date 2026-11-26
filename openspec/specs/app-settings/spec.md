# app-settings Specification

## Purpose
Keep the few app preferences local and separate from projects, which belong to the user's folders.

## Requirements

### Requirement: Local storage
Settings and the list of recent projects SHALL be stored as plain JSON in `%LOCALAPPDATA%\Otis.Data`,
separate from the program folder `%LOCALAPPDATA%\Otis`, so that uninstalling never deletes them.
Otis stores no secrets. `OTIS_DATA_DIR` MAY override the folder for development.

#### Scenario: Uninstall keeps recent projects
- **WHEN** Otis is uninstalled and installed again
- **THEN** the recent projects are still listed

### Requirement: Recent projects
The start screen SHALL list recently opened projects with their font name and coverage, and offer
"New project" and "Open project".

#### Scenario: Reopen
- **WHEN** the user opened "Kanzlei.otis.json" yesterday
- **THEN** it is listed first on the start screen

### Requirement: Language
Settings SHALL offer the language (System, English, Deutsch (Schweiz)); it applies immediately.

#### Scenario: Language switch
- **WHEN** the user selects "Deutsch (Schweiz)"
- **THEN** the interface switches to Swiss German without a restart

### Requirement: Nightly opt-in
Users SHALL be able to opt in to nightly pre-releases; otherwise only stable releases are offered.

#### Scenario: Opted-in user
- **WHEN** nightly builds are enabled
- **THEN** the updater also considers GitHub pre-releases
