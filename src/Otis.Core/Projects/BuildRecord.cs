// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Core.Projects;

public sealed record BuildRecord
{
    public required FontVersion Version { get; init; }

    public required DateTimeOffset BuiltAt { get; init; }

    public required string FileName { get; init; }

    public required string Sha256 { get; init; }

    public required int CutOut { get; init; }

    public required int Derived { get; init; }

    public required int Missing { get; init; }

    public bool Installed { get; init; }
}
