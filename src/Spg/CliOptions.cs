namespace Spg;

public enum GenerationMode
{
    Password,
    Passphrase,
}

public sealed record CliOptions
{
    public const int MaxCount = 1000;

    public GenerationMode Mode { get; init; } = GenerationMode.Password;
    public PasswordOptions Password { get; init; } = new();
    public PassphraseOptions Passphrase { get; init; } = new();
    public int Count { get; init; } = 1;
    public bool Quiet { get; init; }
}
