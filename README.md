# Otis

Otis turns hand lettering into an installable font. Open scanned pages, mark one good example of
each letter, and Otis cuts it out, traces it into smooth curves and builds a TrueType font that
works in Word and every other Windows program.

> Status: specification. The capabilities are described in [`openspec/specs`](openspec/specs);
> the implementation follows.

## How it works

1. **Add sources**: PDFs (every page) or scans as PNG, JPEG or TIFF. They are opened read-only.
2. **Mark letters**: pick a character from the table and draw a box around a good example. Drag the
   box onto a better one, add corners to cut away a neighbouring letter, or erase stray pieces. Two
   guide lines (baseline and x-line) make letters from different lines the same size.
3. **Check the preview**: the right pane shows exactly what will be cut out.
4. **Build and install**: Otis traces every letter, derives umlauts and ligatures, writes the font
   with a version number (1.000, 1.001, …), checks that Windows will really use it, and installs it
   for you with one click, replacing the older version. Every version is kept, so you can go back.

## Your data stays yours

- Your work is one readable `*.otis.json` file next to your scans; copy the folder and it opens
  anywhere. Before every save the previous version is kept in `.otis-backups`.
- Scans are never modified. Rendered pages are cached in `%LOCALAPPDATA%\Otis.Data` and can be
  deleted at any time.
- Otis works offline and collects no telemetry.
- Fonts you build are yours. Otis adds no license to them.

## Building

Requirements: Windows 10 19041+ (Windows 11 recommended) and the .NET 11 SDK (`global.json` pins `11.0.100`).

```powershell
dotnet build Otis.slnx
dotnet test --solution Otis.slnx
dotnet run --project src/Otis.App
```

Set `OTIS_DATA_DIR` to keep a development data folder separate from your real one:

```powershell
$env:OTIS_DATA_DIR = "$env:TEMP\otis-dev"; dotnet run --project src/Otis.App
```

## Repository layout

| Path | Purpose |
| --- | --- |
| `src/Otis.Core` | Project model, ink detection, Potrace port, glyph metrics, derived glyphs, TrueType writer; pure and deterministic |
| `src/Otis.Infrastructure` | PDF and image rendering, project files, page cache, overlap removal, Windows font installation, Velopack |
| `src/Otis.App` | Uno Platform app (Windows App SDK head) |
| `tests/Otis.ApiTests` | API-level tests with generated sample pages and real font files |
| `openspec/` | Project conventions and capability specs |
| `.github/workflows/` | CI, nightly pre-releases, SemVer releases |

## Releases

`<Version>` in `Directory.Build.props` is the single source of truth.

- **Merge into `main`** → the Release workflow publishes `v<Version>` if that tag does not exist yet. Pull requests into `main` fail when `<Version>` is already released.
- **`develop`** → a nightly pre-release `v<Version>-nightly.<run>` when `develop` changed in the last 24 hours.
- **After a release**, bump `<Version>` on `develop` to the next version; nightlies refuse to build an already released version because updaters would ignore them.
- Set the `SIGNING_CERTIFICATE` (base64 PFX) and `SIGNING_PASSWORD` secrets to sign binaries.

## Privacy policy

This program will not transfer any information to other networked systems unless specifically
requested by the user or the person installing or operating it. The only network access is the
**update check**, which contacts GitHub Releases at startup and every four hours to download new
versions of Otis. No personal data, scans or fonts are sent.

## License

Otis is free software under the GNU General Public License, version 3 or (at your option) any later
version; see [`LICENSE`](LICENSE). Its tracer is a port of [Potrace](https://potrace.sourceforge.net)
by Peter Selinger (GPL-2.0-or-later). See [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md).

Fonts you build with Otis are yours: they are not covered by Otis's license.

## Contributing

Read [`openspec/project.md`](openspec/project.md) first. Analyzers run at `latest-all` with warnings
as errors, so build locally before opening a pull request against `develop`. Please do not add
scans of real documents to the repository; tests generate their own sample pages.
