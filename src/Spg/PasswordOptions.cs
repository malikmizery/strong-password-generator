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

    /// <summary>Use <see cref="CharacterSets.AllSymbols"/> instead of the eight common ones.</summary>
    public bool AllSymbols { get; init; }

    public bool ExcludeAmbiguous { get; init; }

    /// <summary>Leave out <see cref="CharacterSets.EnvUnsafe"/> so the password survives a .env loader unquoted.</summary>
    public bool EnvSafe { get; init; }

    /// <summary>Characters to leave out of every set.</summary>
    public string Exclude { get; init; } = "";

    /// <summary>The enabled sets with every exclusion applied; a set that ends up empty is dropped.</summary>
    public IReadOnlyList<string> EnabledSets()
    {
        var sets = new List<string>(4);
        if (Upper) sets.Add(CharacterSets.Upper);
        if (Lower) sets.Add(CharacterSets.Lower);
        if (Digits) sets.Add(CharacterSets.Digits);
        if (Symbols) sets.Add(AllSymbols ? CharacterSets.AllSymbols : CharacterSets.Symbols);

        var excluded = Exclude;
        if (ExcludeAmbiguous) excluded += CharacterSets.Ambiguous;
        if (EnvSafe) excluded += CharacterSets.EnvUnsafe;
        if (excluded.Length == 0)
            return sets;

        return sets
            .Select(set => string.Concat(set.Where(c => !excluded.Contains(c))))
            .Where(set => set.Length > 0)
            .ToList();
    }

    /// <returns>An error message, or <c>null</c> when the options are valid.</returns>
    public string? Validate()
    {
        var setCount = EnabledSets().Count;
        if (setCount == 0)
            return "At least one character set must be enabled (and not excluded entirely).";
        if (Length < setCount)
            return $"Length must be at least {setCount} to include every enabled character set.";
        if (Length > MaxLength)
            return $"Length must be at most {MaxLength}.";
        return null;
    }
}
