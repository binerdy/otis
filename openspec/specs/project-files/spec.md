# project-files Specification

## Purpose
Keep the user's work in one small, readable file that lives next to the scans, outlasts Otis and
never touches the scans themselves.

## Requirements

### Requirement: One project file
A project SHALL be a single UTF-8 JSON file with the extension `.otis.json`. It SHALL contain a
`formatVersion`, the font settings (family name, font version, designer, x-height, cap height,
sidebearing, word space, output folder), the list of sources, the cutouts keyed by glyph name, the
derivation rules and the build history. It SHALL be indented and its keys SHALL be written in a stable order, so that two saves of
the same project are byte-identical.

#### Scenario: Unchanged project saves identically
- **WHEN** a project is opened and saved without changes
- **THEN** the file content is byte-identical to before

### Requirement: Build history
Every build SHALL be recorded in the project with its font version, UTC build time, file name,
SHA-256 of the font file, coverage counts (cut out, derived, missing) and whether it was installed.
The history SHALL be shown in the build pane, newest first, and SHALL decide which version comes
next (see font-building).

#### Scenario: History survives a lost output folder
- **WHEN** the `fonts/` folder was deleted
- **THEN** the history still lists every version, and the next build proposes the version after the last one

#### Scenario: Which file is installed
- **WHEN** 1.003 was installed and 1.004 was built afterwards
- **THEN** the history marks 1.003 as installed and 1.004 as not installed

### Requirement: Relative source paths
Source paths SHALL be stored relative to the project file with forward slashes. A source that
cannot be found SHALL be reported by name, and the cutouts on it SHALL be kept, not deleted.

#### Scenario: Moved folder
- **WHEN** the folder with the project and its scans is copied to another computer
- **THEN** the project opens there with all pages and cutouts

#### Scenario: Missing scan
- **WHEN** a listed source file was deleted
- **THEN** Otis names the missing file, shows its cutouts as unavailable and keeps them in the file

### Requirement: Glyph names as keys
Cutouts SHALL be keyed by their glyph name (`a`, `A`, `adieresis`, `s.alt`) inside the JSON, never by
file names on disk, so that names differing only in case stay distinct on case-insensitive file
systems.

#### Scenario: a and A coexist
- **WHEN** the project has cutouts for `a` and `A`
- **THEN** both are kept and the built font maps U+0061 and U+0041 to different glyphs

### Requirement: Cutout record
A cutout SHALL store the source, the page number (1-based), the outline polygon as integer page
pixels, the erasures as integer rectangles, the baseline and x-line as page rows, the optional
extraction overrides (gap closing, minimum speck size), and the optional adjustments (grey tracing,
smoothing level, ink and eraser strokes as width plus page-pixel points). Adjustments are optional fields, so
projects without them keep their format version and older Otis versions preserve them unread. All coordinates SHALL refer to the page
rendered at the project's resolution (default 400 dpi).

#### Scenario: Rectangle cutout
- **WHEN** the user draws a box from (700, 820) to (779, 955) on page 1
- **THEN** the cutout polygon is `[[700,820],[779,820],[779,955],[700,955]]` with page 1

### Requirement: Backups before saving
Before each save the previous file SHALL be copied to `.otis-backups/<name>-<UTC timestamp>.otis.json`
next to the project, and at most the 50 newest backups SHALL be kept.

#### Scenario: Undo a bad session
- **WHEN** the user saved a broken cutout
- **THEN** the previous version is available in `.otis-backups`

### Requirement: Forward compatibility
Otis SHALL refuse to save a project whose `formatVersion` is newer than it understands, and SHALL
open it read-only with a message to update Otis. Unknown fields in a supported version SHALL be
preserved on save.

#### Scenario: Project from a newer Otis
- **WHEN** a project with a higher `formatVersion` is opened
- **THEN** it is shown read-only with "This project was made with a newer Otis."

### Requirement: Read-only sources
Otis SHALL open sources read-only and SHALL never write, rename or delete them.

#### Scenario: Scan is untouched
- **WHEN** a project is edited, built and saved
- **THEN** every source file has the same bytes and modification time as before
