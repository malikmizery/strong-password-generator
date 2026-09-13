using System.Globalization;

namespace Spg;

public abstract record ParseResult
{
    public sealed record Run(CliOptions Options) : ParseResult;

    public sealed record Help : ParseResult;

    public sealed record Interactive : ParseResult;

    public sealed record Error(string Message) : ParseResult;
}

public static class CliParser
{
    public const string Usage = """
        spg - strong password generator

        Usage:
          spg                      Interactive mode (prompts for every option)
          spg [options]            Random-character password
          spg --passphrase [opts]  Passphrase from the EFF long word list

        Password options:
          -l, --length <n>         Number of characters (default 20)
              --no-upper           Leave out uppercase letters
              --no-lower           Leave out lowercase letters
              --no-digits          Leave out digits
              --no-symbols         Leave out symbols
              --no-ambiguous       Leave out look-alike characters (0 O 1 l I |)

        Passphrase options:
          -p, --passphrase         Generate a passphrase instead of a password
          -w, --words <n>          Number of words (default 6)
          -s, --separator <text>   Text between words (default "-"; "" for none)
              --no-capitalize      Don't capitalize a random word
              --no-digit           Don't append a digit to a random word

        General:
          -n, --count <n>          How many to generate (default 1)
          -q, --quiet              Don't print the entropy estimate to stderr
          -h, --help               Show this help

        """;

    public static ParseResult Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
            return new ParseResult.Interactive();
        if (args.Any(arg => arg is "-h" or "--help"))
            return new ParseResult.Help();

        try
        {
            return new ParseResult.Run(ParseOptions(args));
        }
        catch (UsageException e)
        {
            return new ParseResult.Error(e.Message);
        }
    }

    private static CliOptions ParseOptions(IReadOnlyList<string> args)
    {
        var options = new CliOptions();
        string? passwordFlag = null;
        string? passphraseFlag = null;

        for (var i = 0; i < args.Count; i++)
        {
            var flag = args[i];
            string NextValue() => ++i < args.Count ? args[i] : throw new UsageException($"{flag} expects a value.");

            switch (flag)
            {
                case "-p" or "--passphrase":
                    options = options with { Mode = GenerationMode.Passphrase };
                    break;
                case "-n" or "--count":
                    options = options with { Count = ParseInt(flag, NextValue()) };
                    break;
                case "-q" or "--quiet":
                    options = options with { Quiet = true };
                    break;

                case "-l" or "--length":
                    options = options with { Password = options.Password with { Length = ParseInt(flag, NextValue()) } };
                    passwordFlag ??= flag;
                    break;
                case "--no-upper":
                    options = options with { Password = options.Password with { Upper = false } };
                    passwordFlag ??= flag;
                    break;
                case "--no-lower":
                    options = options with { Password = options.Password with { Lower = false } };
                    passwordFlag ??= flag;
                    break;
                case "--no-digits":
                    options = options with { Password = options.Password with { Digits = false } };
                    passwordFlag ??= flag;
                    break;
                case "--no-symbols":
                    options = options with { Password = options.Password with { Symbols = false } };
                    passwordFlag ??= flag;
                    break;
                case "--no-ambiguous":
                    options = options with { Password = options.Password with { ExcludeAmbiguous = true } };
                    passwordFlag ??= flag;
                    break;

                case "-w" or "--words":
                    options = options with { Passphrase = options.Passphrase with { Words = ParseInt(flag, NextValue()) } };
                    passphraseFlag ??= flag;
                    break;
                case "-s" or "--separator":
                    options = options with { Passphrase = options.Passphrase with { Separator = NextValue() } };
                    passphraseFlag ??= flag;
                    break;
                case "--no-capitalize":
                    options = options with { Passphrase = options.Passphrase with { Capitalize = false } };
                    passphraseFlag ??= flag;
                    break;
                case "--no-digit":
                    options = options with { Passphrase = options.Passphrase with { AddDigit = false } };
                    passphraseFlag ??= flag;
                    break;

                default:
                    throw new UsageException($"Unknown option '{flag}'.");
            }
        }

        // Silently ignoring a flag would leave the user believing they got, say, a 32-character password.
        if (options.Mode == GenerationMode.Passphrase && passwordFlag is not null)
            throw new UsageException($"{passwordFlag} cannot be used with --passphrase.");
        if (options.Mode == GenerationMode.Password && passphraseFlag is not null)
            throw new UsageException($"{passphraseFlag} requires --passphrase.");

        var error = options.Mode == GenerationMode.Password ? options.Password.Validate() : options.Passphrase.Validate();
        if (error is not null)
            throw new UsageException(error);
        if (options.Count is < 1 or > CliOptions.MaxCount)
            throw new UsageException($"Count must be between 1 and {CliOptions.MaxCount}.");

        return options;
    }

    private static int ParseInt(string flag, string value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            ? number
            : throw new UsageException($"{flag} expects a whole number, got '{value}'.");

    private sealed class UsageException(string message) : Exception(message);
}
