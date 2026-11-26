# Project Context

## Purpose
Otis turns hand lettering into an installable font. Users open scanned pages (PDF or images),
mark one good example of each letter, and Otis cuts it out, traces it into curves and builds a
TrueType font that works in Word and every other Windows program.

Otis is made for people who are not type designers: a family chronicle written by a calligrapher,
a grandmother's handwriting, a sign painter's alphabet. The scans stay where they are and are never
modified; the user's work lives in one small, readable project file next to them.

## Tech Stack
- .NET 11 (`net11.0`, `net11.0-windows10.0.26100`), C# latest, nullable enabled
- Uno Platform (`Uno.Sdk`, single project, Windows App SDK head), CommunityToolkit.Mvvm
- PDFium through PDFtoImage for rendering PDF pages; SkiaSharp for image decoding, drawing the
  page canvas and removing outline overlaps
- A C# port of Potrace for tracing bitmaps into curves (see Licensing)
- An own TrueType writer in `Otis.Core`; no font library dependency
- Inter as the only UI typeface
- Velopack for installers and auto-update from GitHub Releases
- xUnit v3 on Microsoft.Testing.Platform

## Architecture (CUPID)
The codebase follows the CUPID properties rather than layered ceremony:

- **Composable** – small types with one purpose and few dependencies. `OtisRuntime` wires them
  together; nothing else knows how the graph is built.
- **Unix philosophy** – each type does one thing: `InkDetector` finds ink, `CutoutExtractor`
  applies the outline and erasures, `PotraceTracer` turns a bitmap into curves, `GlyphScaler`
  places curves on the font's grid, `DerivedGlyphs` builds umlauts and ligatures,
  `TrueTypeWriter` writes the file, `FontInstaller` installs it.
- **Predictable** – the whole path from page bitmap to font bytes is pure and deterministic in
  `Otis.Core`: the same project and pages produce byte-identical fonts (timestamps come from
  `TimeProvider`). Side effects (files, PDF rendering, registry, updates) live in
  `Otis.Infrastructure`.
- **Idiomatic** – modern C# (records, primary constructors, collection expressions), MVVM with
  source generators, `x:Bind` in XAML, analyzers at `latest-all` with warnings as errors.
- **Domain-based** – names come from the domain: source, page, cutout, baseline, x-line, ink,
  outline, derived glyph, coverage, specimen. Folders are organised by domain concept, not by
  technical layer.

Projects:
- `src/Otis.Core` – domain records, interfaces and pure logic: project model, ink detection,
  morphology, Potrace port, glyph metrics, derived glyphs, TrueType writer. No third-party
  dependencies.
- `src/Otis.Infrastructure` – PDF and image rendering, project file store, page cache, path
  overlap removal, Windows font installation, settings, Velopack updates.
- `src/Otis.App` – Uno Platform UI (letter table, page canvas, preview, build and install).
- `tests/Otis.ApiTests` – tests against the public API of Core and Infrastructure with real
  files: generated sample pages, real tracing and real font files loaded back through Windows.

## Project Conventions

### Code Style
- `.editorconfig` follows the bbvch/DotNetTemplates baseline: file-scoped namespaces,
  `this.` qualification, UTF-8 with BOM for C# files, StyleCop + Sonar analyzers.
- `TreatWarningsAsErrors` everywhere. Suppress a rule only with a `Justification`.
- `BannedSymbols.txt` bans `DateTime`, the `Now` clocks and sync-over-async; use `TimeProvider`.
- Comments explain *why* only (a constraint or quirk the code cannot show). No XML doc
  summaries that restate names.
- One type per file; the file name matches the type.
- UI text in English and Swiss German (`Strings/en`, `Strings/de-CH`); no literals in XAML.

### Testing Strategy
- API-level tests: exercise public types end to end with temporary folders.
- Test pages are generated at test time by drawing letters with SkiaSharp into PNG and PDF
  files, so the repository contains no scans of real documents.
- Every built font in tests is loaded back through Windows GDI and must be offered for Western
  text under its own family name (see font-building).
- Golden values are geometric (bounds, contour counts, advance widths), not pixel snapshots.
- No network in tests.

### Git Workflow
- `develop` is the integration branch; a nightly pre-release is built from it.
- `main` holds released code; a `vMAJOR.MINOR.PATCH` tag on `main` produces a stable release.
- Feature branches merge into `develop` via pull request; CI must be green.
- Rename files with `git mv`, committing the pure rename separately from content changes.

### Commit Messages
- Commit messages carry no co-author trailers and no tool-generated footers.
- Nothing personal goes into the repository: no names, e-mail addresses, local paths or machine
  details of individual contributors, and no real documents or the people in them.

## Domain Context
- **Project** – one `*.otis.json` file next to the scans. It lists the sources, the cutouts and
  the font settings. Paths inside it are relative to the file, so a folder can be moved or shared.
- **Source** – a PDF or image file (PNG, JPEG, TIFF). It is opened read-only.
- **Page** – one page of a source, rendered at a fixed resolution (default 400 dpi) and cached.
  All coordinates in a project are in pixels of that rendering.
- **Cutout** – the user's selection for one glyph: page, outline polygon, erasures, baseline and
  x-line.
- **Outline polygon** – the area that belongs to the glyph, usually a rectangle, with extra corners
  where a neighbouring letter must be cut away.
- **Erasure** – a rectangle inside the cutout whose ink is ignored (a piece of the next letter).
- **Baseline / x-line** – the bottom and top of lowercase letters such as a, n, e next to the
  cutout. Their distance sets the glyph's scale, so letters from different lines and pages end up
  the same size. Capitals use the lines of the lowercase letters beside them.
- **Ink** – the dark pixels of a cutout after smoothing, closing small gaps and dropping specks.
- **Outline** – the traced curves of the ink, in font units.
- **Body** – the part of the ink without hairline flourishes; it determines the advance width, so
  flourishes may overhang their neighbours as in the original.
- **Derived glyph** – a glyph built from others: umlauts (base + two dots), ligatures (ß = ſ + z)
  and aliases (I = J in Fraktur). A cutout for the same character always wins over derivation.
- **Coverage** – for every target character: cut out, derived or missing.
- **Specimen** – an image of all glyphs and a sample text, produced with every build.

## Important Constraints
- Sources are opened read-only and never modified.
- The project file is the only place the user's work is stored. It is plain, indented JSON with a
  `formatVersion`, so it diffs well and survives Otis. Before each save the previous version is
  kept in a `.otis-backups` folder next to it.
- Built fonts must work in Microsoft Word on Windows: Western code page and Unicode ranges set,
  name records on the Windows platform, installable embedding, Windows metrics that cover every
  glyph so flourishes are not clipped.
- The app works fully offline. Only the update check uses the network.
- No telemetry.

## Licensing
- Otis is licensed under GPL-3.0-or-later (`LICENSE`). Its tracer is a port of Potrace
  (Peter Selinger, GPL-2.0-or-later), which permits use under GPL-3.0. The port keeps Potrace's
  copyright notice in `src/Otis.Core/Tracing` and is listed in `THIRD-PARTY-NOTICES.md`.
- C# files carry `// SPDX-License-Identifier: GPL-3.0-or-later` as their first line.
- Fonts built with Otis belong to their makers. The font data is not a derivative of Otis, and Otis
  adds no license to it; the font's license fields are left for the maker to fill in.
- Third-party packages must be compatible with GPL-3.0: MIT, BSD, Apache-2.0 (Uno Platform) and
  OFL-1.1 (Inter) are fine; GPL-2.0-only packages are not.

## External Dependencies
- GitHub Releases as the Velopack update feed.
