namespace Spg;

public enum GenerationMode
{
    Password,
    Passphrase,
}

public sealed record CliOptions
{
    public const int MaxCount = 1000;
    public const string DefaultEnvFile = ".env";

    /// <summary>Passing this as <see cref="EnvFile"/> prints the <c>KEY=value</c> line to stdout instead.</summary>
    public const string Stdout = "-";

    public GenerationMode Mode { get; init; } = GenerationMode.Password;
    public PasswordOptions Password { get; init; } = new();
    public PassphraseOptions Passphrase { get; init; } = new();
    public int Count { get; init; } = 1;
    public bool Quiet { get; init; }

    /// <summary>Send the secret to the clipboard instead of stdout.</summary>
    public bool Copy { get; init; }

    /// <summary>When set, the secret is stored under this name (in <see cref="EnvFile"/> or user secrets) instead of printed.</summary>
    public string? EnvKey { get; init; }

    public string EnvFile { get; init; } = DefaultEnvFile;

    /// <summary>Allow <see cref="EnvKey"/> to replace an existing entry.</summary>
    public bool Force { get; init; }

    /// <summary>Store with <c>dotnet user-secrets</c> instead of a .env file.</summary>
    public bool UserSecrets { get; init; }

    public string? Project { get; init; }

    public string? SecretsId { get; init; }

    /// <summary>Read the secret from stdin (a password the user already has) instead of generating one.</summary>
    public bool FromStdin { get; init; }
}
