// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Infrastructure.Fonts;

public sealed record FontLocations(string FontsFolder, string UserKey, string MachineKey, bool AnnounceChanges);
