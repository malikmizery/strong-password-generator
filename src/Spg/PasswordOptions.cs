namespace Spg;

public sealed record PasswordOptions
{
    public const int DefaultLength = 20;
    public const int MaxLength = 1024;

    public int Length { get; init; } = DefaultLength;
    public bool Upper { get; init; } = true;
    public bool Lower { get; init; } = true;
    public bool Digits { get; init; } = true;
    public bool Symbols { get; init; } = true;
    public bool ExcludeAmbiguous { get; init; }

    public IReadOnlyList<string> EnabledSets()
    {
        var sets = new List<string>(4);
        if (Upper) sets.Add(CharacterSets.Upper);
        if (Lower) sets.Add(CharacterSets.Lower);
        if (Digits) sets.Add(CharacterSets.Digits);
        if (Symbols) sets.Add(CharacterSets.Symbols);

        return ExcludeAmbiguous
            ? sets.Select(set => string.Concat(set.Where(c => !CharacterSets.Ambiguous.Contains(c)))).ToList()
            : sets;
    }

    /// <returns>An error message, or <c>null</c> when the options are valid.</returns>
    public string? Validate()
    {
        var setCount = EnabledSets().Count;
        if (setCount == 0)
            return "At least one character set must be enabled.";
        if (Length < setCount)
            return $"Length must be at least {setCount} to include every enabled character set.";
        if (Length > MaxLength)
            return $"Length must be at most {MaxLength}.";
        return null;
    }
}
