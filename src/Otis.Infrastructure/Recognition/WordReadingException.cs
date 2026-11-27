// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Infrastructure.Recognition;

public sealed class WordReadingException : Exception
{
    public WordReadingException()
    {
    }

    public WordReadingException(string message)
        : base(message)
    {
    }

    public WordReadingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public WordReadingException(WordReadingProblem problem, string message, Exception? innerException = null)
        : base(message, innerException) => this.Problem = problem;

    public WordReadingProblem Problem { get; } = WordReadingProblem.Other;
}
