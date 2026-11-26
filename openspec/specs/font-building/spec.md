# font-building Specification

## Purpose
Produce a TrueType font from a project that looks like the original lettering, spaces naturally and
works in Word and every other Windows program without surprises.

## Requirements

### Requirement: Scale from the lowercase lines
Each glyph SHALL be scaled so that the distance between its cutout's baseline and x-line equals the
font's x-height (default 400 of 1000 units per em), and placed so that the baseline is at y = 0.

#### Scenario: Letters from different lines match
- **WHEN** `a` was cut from a line with 90 px x-height and `n` from one with 100 px
- **THEN** both are 400 units high in the font

### Requirement: Spacing from the body
The advance width SHALL be the width of the glyph's body plus the sidebearing on both sides
(default 16 units). The body is the ink after an opening with a radius of 7 % of the x-height,
which removes hairlines. Flourishes outside the body SHALL overhang neighbouring letters as in the
original. Capitals MAY have their own right sidebearing; per-glyph overrides SHALL be stored in the
project. The word space SHALL default to 340 units.

#### Scenario: Flourished capital
- **WHEN** a capital has a swash reaching far to the left
- **THEN** the swash overhangs the preceding space and the following lowercase letter sits at body distance

### Requirement: Derived glyphs
Unless a cutout exists for the same glyph, the build SHALL derive:
- `Adieresis Odieresis Udieresis adieresis odieresis udieresis` from the base letter and two copies of
  the `dotaccent.src` cutout, scaled to 70 % and centred over the base's body;
- `germandbls` (ß) from `s.alt` (ſ) followed by `z`;
- `I` from `J`, as in Fraktur.
Derivation rules SHALL be listed in the project and MAY be changed there. Overlaps in derived
glyphs SHALL be removed.

#### Scenario: Own umlaut wins
- **WHEN** the project has a cutout for `adieresis`
- **THEN** the font uses that cutout and not a + dots

### Requirement: Valid TrueType
The writer SHALL produce a TrueType font (`glyf` outlines converted from cubic to quadratic curves
with at most 1 unit error) with the tables `head hhea maxp OS/2 hmtx cmap loca glyf name post gasp`,
correct checksums and 4-byte table alignment. `.notdef` SHALL be glyph 0 and `space` SHALL map U+0020.
The `gasp` table SHALL request smoothing at all sizes because the font is not hinted.

#### Scenario: Font round trip
- **WHEN** a built font is read back
- **THEN** every table's checksum is valid and every mapped character has its outline

### Requirement: Works in Word on Windows
Every built font SHALL:
- set `OS/2.ulCodePageRange1` bit 0 (Latin 1) and the Unicode range bits of the characters it maps;
- carry name records 1, 2, 3, 4, 5 and 6 on the Windows platform (3, 1, 0x409);
- set `OS/2.fsType` to 0 (installable) and `fsSelection` REGULAR;
- set `usWinAscent` and `usWinDescent` to cover the highest and lowest point of every glyph, so that
  flourishes are not clipped;
- take `head.created` and `head.modified` from `TimeProvider`.
Before writing, the build SHALL load the font through Windows GDI and verify that a request for the
family with `ANSI_CHARSET` returns that family, and fail with a clear message otherwise.

#### Scenario: Word gets the font, not Arial
- **WHEN** the font "Kanzlei Fraktur" is requested for Western text through GDI
- **THEN** GDI returns "Kanzlei Fraktur"

#### Scenario: Missing code page is caught
- **WHEN** a font without code page bits would be written
- **THEN** the build fails with "Windows would replace this font with a fallback for Western text."

### Requirement: Coverage report
Every build SHALL report which target characters are cut out, derived and missing. Missing
characters SHALL NOT be mapped, so Windows can fall back for them, and the report SHALL list them.

#### Scenario: Missing letters are named
- **WHEN** no cutouts exist for O, P and Q
- **THEN** the build succeeds and reports "Missing: O P Q"

### Requirement: Font version
The project SHALL hold the font version as `MAJOR.MINOR` with MAJOR 1–999 and MINOR 0–999, always
written with three minor digits (1.000, 1.001, 2.000). New projects start at 1.000. Every build
SHALL write the version into name ID 5 as `Version 1.001`, into name ID 3 as
`1.001;OTIS;<PostScript name>` and into `head.fontRevision` as the nearest 16.16 value, so that
Windows and other programs show it. The family name SHALL NOT contain the version, so documents
keep using the font across updates.

#### Scenario: Version visible in Windows
- **WHEN** version 1.004 is built and installed
- **THEN** the font's details in Windows show "Version 1.004"

#### Scenario: Documents follow the update
- **WHEN** a Word document uses "Kanzlei Fraktur" 1.000 and 1.001 is installed
- **THEN** after restarting Word the document shows the glyphs of 1.001

### Requirement: New version or rebuild
"Build font" SHALL propose the next version after the last build: MINOR + 1 (1.009 → 1.010,
1.999 → 2.000). The user MAY enter any higher version, for example 2.000 after a large change. A
version lower than or equal to an earlier build SHALL be refused, with one exception: the latest
version MAY be rebuilt as long as it was never installed. Once a version is installed it is frozen,
because Windows and Word treat two different files with the same version as the same font.

#### Scenario: Next version proposed
- **WHEN** the last build was 1.003
- **THEN** the build dialog proposes 1.004

#### Scenario: Lower version refused
- **WHEN** 1.003 was built and the user enters 1.002
- **THEN** the build is refused with "Version 1.002 is not newer than 1.003."

#### Scenario: Rebuild before installing
- **WHEN** 1.004 was built but not installed and the user fixes a letter
- **THEN** "Rebuild 1.004" replaces that build

#### Scenario: Installed version is frozen
- **WHEN** 1.004 was installed and the user fixes a letter
- **THEN** only 1.005 or higher is offered

### Requirement: Versioned output files
Each build SHALL write `<PostScript name>-<version>.ttf` (for example
`KanzleiFraktur-Regular-1.004.ttf`) and `<PostScript name>-<version>.png` (the specimen) into the
project's output folder, by default `fonts/` next to the project file. Earlier versions SHALL be kept
and never overwritten, except by a permitted rebuild of the same version.

#### Scenario: History on disk
- **WHEN** versions 1.000 to 1.002 were built
- **THEN** the output folder contains three font files, one per version

### Requirement: Reproducible
The same project, version, pages and build time SHALL produce byte-identical font files.

#### Scenario: Two builds
- **WHEN** a project is built twice with a fixed `TimeProvider`
- **THEN** both files are identical

### Requirement: Specimen
Every build SHALL render a specimen PNG with all mapped characters and the user's sample text,
using the built font file itself.

#### Scenario: Sample text
- **WHEN** the sample text is "Hamburgefonstiv"
- **THEN** the specimen shows that line in the new font below the character set
