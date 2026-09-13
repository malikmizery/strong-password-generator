using System.Globalization;

namespace Spg;

public static class App
{
    public const int ExitOk = 0;
    public const int ExitFailure = 1;
    public const int ExitUsage = 2;

    private const double WeakBelowBits = 64;
    private const double VeryStrongFromBits = 100;

    /// <summary>
    /// Passwords go to <paramref name="output"/>, one per line; prompts, diagnostics and the entropy
    /// estimate go to <paramref name="error"/>, so piping stdout captures only the passwords.
    /// With <c>-c</c> the secret goes to <paramref name="copy"/> and with <c>-k</c> to a .env file or
    /// user secrets; either way stdout stays empty.
    /// </summary>
    /// <param name="copy">The clipboard; defaults to <see cref="Clipboard.Copy"/>.</param>
    /// <param name="interactiveInput">True when <paramref name="input"/> is a terminal, so <c>--stdin</c> shows a prompt.</param>
    public static int Run(IReadOnlyList<string> args, TextReader input, TextWriter output, TextWriter error,
        Action<string>? copy = null, bool interactiveInput = false)
    {
        CliOptions options;
        switch (CliParser.Parse(args))
        {
            case ParseResult.Help:
                output.Write(CliParser.Usage);
                return ExitOk;
            case ParseResult.Error parseError:
                return UsageError(parseError.Message, error);
            case ParseResult.Interactive:
                options = new InteractivePrompt(input, error).Run();
                break;
            case ParseResult.Run run:
                options = run.Options;
                break;
            default:
                throw new InvalidOperationException("Unhandled parse result.");
        }

        try
        {
            Deliver(options, input, output, error, copy ?? Clipboard.Copy, interactiveInput);
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            error.WriteLine($"spg: {e.Message}");
            if (e is ArgumentException && options.FromStdin)
                error.WriteLine($"Consider replacing it with a generated one: spg -k {options.EnvKey} --force");
            return ExitFailure;
        }

        if (!options.Quiet && !options.FromStdin)
            error.WriteLine(DescribeStrength(options));

        return ExitOk;
    }

    private static void Deliver(CliOptions options, TextReader input, TextWriter output, TextWriter error, Action<string> copy, bool interactiveInput)
    {
        Func<string> secret = options.FromStdin
            ? () => ReadSecret(input, error, interactiveInput)
            : options.Mode == GenerationMode.Password
                ? () => PasswordGenerator.Generate(options.Password)
                : () => PassphraseGenerator.Default.Generate(options.Passphrase);

        if (options.EnvKey is { } key)
        {
            if (options.UserSecrets)
            {
                UserSecrets.Set(key, secret(), options.Project, options.SecretsId);
                error.WriteLine($"Stored {key} in user secrets");
            }
            else if (options.EnvFile == CliOptions.Stdout)
            {
                output.WriteLine(EnvFile.Format(key, secret()));
            }
            else
            {
                var result = EnvFile.Set(options.EnvFile, key, secret(), options.Force);
                error.WriteLine($"{result} {key} in {options.EnvFile}");
            }
        }
        else if (options.Copy)
        {
            copy(secret());
            error.WriteLine("Copied to the clipboard");
        }
        else
        {
            for (var i = 0; i < options.Count; i++)
                output.WriteLine(secret());
        }
    }

    private static string ReadSecret(TextReader input, TextWriter error, bool interactiveInput)
    {
        if (interactiveInput)
            error.Write("Paste the password and press Enter: ");
        var line = input.ReadLine();
        if (interactiveInput && line is null)
            error.WriteLine();
        // Only the line ending is stripped: a password may legitimately start or end with a space.
        return string.IsNullOrEmpty(line)
            ? throw new InvalidOperationException("Nothing to store: stdin was empty.")
            : line;
    }

    private static int UsageError(string message, TextWriter error)
    {
        error.WriteLine($"spg: {message}");
        error.WriteLine("Run 'spg --help' for usage.");
        return ExitUsage;
    }

    private static string DescribeStrength(CliOptions options)
    {
        var (bits, noun, remedy) = options.Mode == GenerationMode.Password
            ? (PasswordGenerator.EntropyBits(options.Password), "password", "use a longer length or more character sets")
            : (PassphraseGenerator.Default.EntropyBits(options.Passphrase), "passphrase", "use more words");

        var rating = bits switch
        {
            < WeakBelowBits => $"WEAK - {remedy}",
            < VeryStrongFromBits => "strong",
            _ => "very strong",
        };

        return string.Create(CultureInfo.InvariantCulture, $"Entropy: ~{bits:F0} bits per {noun} ({rating})");
    }
}
