// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Imaging;

namespace Otis.Core.Ink;

public sealed record CutoutInk(BitGrid Mask, int Left, int Top);
