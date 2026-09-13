using System.Globalization;

namespace Spg;

public static class App
{
    public const int ExitOk = 0;
    public const int ExitUsage = 2;

    private const double WeakBelowBits = 64;
    private const double VeryStrongFromBits = 100;

    /// <summary>
    /// Passwords go to <paramref name="output"/>, one per line; prompts, diagnostics and the entropy
    /// estimate go to <paramref name="error"/>, so piping stdout captures only the passwords.
    /// </summary>
    public static int Run(IReadOnlyList<string> args, TextReader input, TextWriter output, TextWriter error)
    {
        CliOptions options;
        switch (CliParser.Parse(args))
        {
            case ParseResult.Help:
                output.Write(CliParser.Usage);
                return ExitOk;
            case ParseResult.Error parseError:
                error.WriteLine($"spg: {parseError.Message}");
                error.WriteLine("Run 'spg --help' for usage.");
                return ExitUsage;
            case ParseResult.Interactive:
                options = new InteractivePrompt(input, error).Run();
                break;
            case ParseResult.Run run:
                options = run.Options;
                break;
            default:
                throw new InvalidOperationException("Unhandled parse result.");
        }

        Func<string> generate = options.Mode == GenerationMode.Password
            ? () => PasswordGenerator.Generate(options.Password)
            : () => PassphraseGenerator.Default.Generate(options.Passphrase);

        for (var i = 0; i < options.Count; i++)
            output.WriteLine(generate());

        if (!options.Quiet)
            error.WriteLine(DescribeStrength(options));

        return ExitOk;
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
