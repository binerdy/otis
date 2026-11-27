# Third-party notices

Otis includes or depends on the following third-party work. Package versions are listed in
`Directory.Packages.props`.

| Component | Use in Otis | License |
| --- | --- | --- |
| [Potrace](https://potrace.sourceforge.net) 1.16 by Peter Selinger | Ported to C# in `src/Otis.Core/Tracing` | GPL-2.0-or-later |
| [potracer](https://github.com/tatarize/potrace) by Tatarize | Structure guide for the port; generated the reference traces in `tests/Otis.ApiTests/TestData` | GPL-2.0-or-later |
| [Uno Platform](https://platform.uno) | UI framework | Apache-2.0 |
| [Windows App SDK](https://github.com/microsoft/WindowsAppSDK) | Windows head | MIT |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | View models | MIT |
| [SkiaSharp](https://github.com/mono/SkiaSharp) | Images, page canvas, overlap removal | MIT |
| [PDFtoImage](https://github.com/sungaila/PDFtoImage) | PDF page rendering | MIT |
| [PDFium](https://pdfium.googlesource.com/pdfium/) (via bblanchon/pdfium-binaries) | PDF engine | BSD-3-Clause / Apache-2.0 |
| [Anthropic C# SDK](https://github.com/anthropics/anthropic-sdk-csharp) | Optional "Read with Claude" | Apache-2.0 |
| [System.Security.Cryptography.ProtectedData](https://github.com/dotnet/runtime) | Encrypting the API key for the Windows user | MIT |
| [Inter](https://rsms.me/inter/) (Uno.Fonts.Inter) | UI typeface | OFL-1.1 |
| [Velopack](https://velopack.io) | Installer and updates | MIT |

Potrace copyright notice, kept in every ported file:

```
Copyright (C) 2001-2019 Peter Selinger.
This file is part of Potrace. It is free software and it is covered
by the GNU General Public License. See the file COPYING for details.
```
