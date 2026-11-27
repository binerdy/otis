# font-installation Specification

## Purpose
Get a freshly built font into Word with one click, including the case that trips people up: an
older version of the same font is already installed.

## Requirements

### Requirement: Per-user install
"Install in Windows" SHALL install the built font for the current user only: copy it to
`%LOCALAPPDATA%\Microsoft\Windows\Fonts`, register it under
`HKCU\Software\Microsoft\Windows NT\CurrentVersion\Fonts` and broadcast `WM_FONTCHANGE`. It SHALL
NOT require administrator rights.

#### Scenario: First install
- **WHEN** the user installs "Kanzlei Fraktur" for the first time
- **THEN** the font appears in Word's font list after Word is restarted

### Requirement: Replace older versions
Installing SHALL first remove a per-user installation of the same family (same name ID 1 and 2),
including a file that is still loaded, by registering the new file under a versioned file name
and removing the old registration. The user SHALL be told to restart open programs such as Word.

#### Scenario: Update the font
- **WHEN** version 1.000 is installed and the user installs 1.001
- **THEN** only 1.001 is registered and Otis says "Restart Word to use the new version."

### Requirement: Installed version is shown
Otis SHALL read the version of the installed copy from its name ID 5 and show it next to the build
history ("Installed: 1.003"). Any version from the history MAY be installed, but installing a version
older than the installed one SHALL ask first ("Replace 1.004 with the older 1.002?"). Installing
SHALL mark the version as installed in the build history.

#### Scenario: Go back to an earlier version
- **WHEN** 1.004 is installed and the user installs 1.002 from the history and confirms
- **THEN** 1.002 is registered, 1.004 is removed and the history marks 1.002 as installed

#### Scenario: Font installed outside Otis
- **WHEN** a font with the same family but no Otis version tag in name ID 3 is installed
- **THEN** Otis shows "Installed: unknown version" and asks before replacing it

### Requirement: Machine-wide installs are left alone
If the same family is installed for all users, Otis SHALL NOT change it and SHALL explain that it
must be removed in Windows Settings first, because Windows prefers the machine-wide copy.

#### Scenario: Font installed for all users
- **WHEN** "Kanzlei Fraktur" exists in `C:\Windows\Fonts`
- **THEN** Otis does not install and shows how to remove the old copy

### Requirement: Uninstall
Otis SHALL offer to remove the per-user installation of the project's font.

#### Scenario: Remove the font
- **WHEN** the user chooses "Remove from Windows"
- **THEN** the registration and file are removed and the font disappears from new programs
