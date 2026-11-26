// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.App.Controls;

public sealed class CanvasEventArgs<T>(T value) : EventArgs
{
    public T Value { get; } = value;
}
