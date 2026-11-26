// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.ApiTests.Support;

internal sealed class FakeTime(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset now = start;

    public override DateTimeOffset GetUtcNow() => this.now;

    public void Advance(TimeSpan by) => this.now += by;
}
