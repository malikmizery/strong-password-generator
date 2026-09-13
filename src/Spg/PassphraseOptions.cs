namespace Spg;

public sealed record PassphraseOptions
{
    public const int DefaultWords = 6;
    public const int MaxWords = 64;
    public const string DefaultSeparator = "-";

    public int Words { get; init; } = DefaultWords;
    public string Separator { get; init; } = DefaultSeparator;
    public bool Capitalize { get; init; } = true;
    public bool AddDigit { get; init; } = true;

    /// <returns>An error message, or <c>null</c> when the options are valid.</returns>
    public string? Validate() => Words switch
    {
        < 1 => "Word count must be at least 1.",
        > MaxWords => $"Word count must be at most {MaxWords}.",
        _ => null,
    };
}
