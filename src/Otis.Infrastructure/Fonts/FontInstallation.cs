// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Projects;

namespace Otis.Infrastructure.Fonts;

public sealed record FontInstallation(InstallScope Scope, string? Path, FontVersion? Version)
{
    public static FontInstallation None { get; } = new(InstallScope.None, null, null);
}
