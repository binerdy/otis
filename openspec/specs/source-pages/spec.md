# source-pages Specification

## Purpose
Show scanned pages sharply and consistently, whatever format they came in, so that coordinates in
the project mean the same thing on every computer.

## Requirements

### Requirement: Supported sources
Otis SHALL accept PDF files (every page) and PNG, JPEG and TIFF images (one page each). Several
sources MAY be added to one project; pages are numbered per source.

#### Scenario: Mixed sources
- **WHEN** a PDF with 25 pages and two JPEG files are added
- **THEN** the page picker lists 27 pages grouped by source

### Requirement: Fixed rendering resolution
PDF pages SHALL be rendered with PDFium at the project's resolution (default 400 dpi) in RGB.
Images SHALL be used at their native size; their dpi metadata SHALL be shown but SHALL NOT rescale
them. The resolution SHALL be stored in the project and SHALL NOT change after the first cutout.

#### Scenario: Same pixels everywhere
- **WHEN** the same project is opened on two computers
- **THEN** page 3 has identical width and height on both

### Requirement: Page cache
Rendered pages SHALL be cached in `%LOCALAPPDATA%\Otis.Data\pages`, keyed by the source's content
hash, page number and resolution, so that a changed source is rendered again and an unchanged one
is not. The cache MAY be deleted at any time.

#### Scenario: Second opening is fast
- **WHEN** a project is opened a second time
- **THEN** pages come from the cache without rendering

### Requirement: Merged overview
Otis SHALL be able to export all pages of a project's sources, in order, as one PDF for reading
and printing.

#### Scenario: Merge scans
- **WHEN** the user chooses "Export pages as PDF"
- **THEN** one PDF with every page in source order is written where the user chooses
