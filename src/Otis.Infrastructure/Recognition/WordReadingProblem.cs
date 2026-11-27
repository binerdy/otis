// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Infrastructure.Recognition;

public enum WordReadingProblem
{
    InvalidKey,
    NoAccess,
    TooManyRequests,
    ServiceBusy,
    NoConnection,
    Declined,
    Unreadable,
    Other,
}
