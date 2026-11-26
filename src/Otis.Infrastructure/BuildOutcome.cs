// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Building;
using Otis.Core.Projects;

namespace Otis.Infrastructure;

public sealed record BuildOutcome(OtisProject Project, FontBuild Build, BuildRecord Record, string FontPath, string SpecimenPath);
