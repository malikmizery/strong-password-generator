using System.Globalization;

namespace Spg;

public abstract record ParseResult
{
    public sealed record Run(CliOptions Options) : ParseResult;

    public sealed record Help : ParseResult;

    public sealed record Interactive : ParseResult;

    /// <summary>Print the Agent Skill to stdout.</summary>
    public sealed record Skill : ParseResult;

    /// <summary>Write the Agent Skill for <paramref name="Target"/> (a harness name or a directory).</summary>
    public sealed record InstallSkill(string Target, bool Global) : ParseResult;

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
          spg -c [options]         Copy to the clipboard instead of printing
          spg -k NAME [options]    Store as NAME in a .env file (or -u: dotnet user secrets) instead of printing
          spg --stdin -k NAME      Store a password you paste on stdin, quoted correctly for .env

        Password options:
          -l, --length <n>         Number of characters (default 20)
          -e, --env-safe           Leave out $ and #, which .env loaders expand or treat as comments
          -a, --no-ambiguous       Leave out look-alike characters (0 O 1 l I |)
          -x, --exclude <chars>    Leave out these characters, e.g. -x '&*'
          -S, --no-symbols         Leave out symbols
          -A, --all-symbols        Use all punctuation, not just the common !@#$%^&*
              --no-upper           Leave out uppercase letters
              --no-lower           Leave out lowercase letters
              --no-digits          Leave out digits

        Passphrase options:
          -p, --passphrase         Generate a passphrase instead of a password
          -w, --words <n>          Number of words (default 6)
          -s, --separator <text>   Text between words (default "-"; "" for none)
              --no-capitalize      Don't capitalize a random word
              --no-digit           Don't append a digit to a random word

        Storing instead of printing:
          -c, --copy               Copy the secret to the clipboard; nothing goes to stdout
          -k, --key <NAME>         Append NAME=<secret> to a .env file; nothing goes to stdout (implies -e)
          -f, --file <path>        The .env file (default ./.env; "-" prints the NAME=<secret> line)
              --force              Replace NAME if it is already there
          -u, --user-secrets       Store NAME with `dotnet user-secrets set` instead of a .env file
              --project <path>     Project for --user-secrets (default: the current folder)
              --id <id>            UserSecretsId for --user-secrets, instead of a project
              --stdin              Read the secret from stdin instead of generating one, so an existing
                                   password can be stored quoted correctly (never pass it as an argument)

        AI agent skill:
              --skill              Print the Agent Skill (SKILL.md) that teaches an AI coding agent to
                                   use spg without the secret ever entering its transcript
              --install-skill <t>  Write it for a harness (claude, codex, gemini, cursor, copilot) under
                                   the current folder, or into any skills directory
          -g, --global             With --install-skill <harness>: install under your home folder instead

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
            return args.Any(arg => arg is "--skill" or "--install-skill" or "-g" or "--global")
                ? ParseSkill(args)
                : new ParseResult.Run(ParseOptions(args));
        }
        catch (UsageException e)
        {
            return new ParseResult.Error(e.Message);
        }
    }

    // --skill and --install-skill are commands of their own; mixing them with generation flags is a mistake.
    private static ParseResult ParseSkill(IReadOnlyList<string> args)
    {
        string? target = null;
        string? globalFlag = null;
        var print = false;
        string? stray = null;

        for (var i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--skill":
                    print = true;
                    break;
                case "--install-skill":
                    target = ++i < args.Count ? args[i] : throw new UsageException("--install-skill expects a harness name or a directory.");
                    break;
                case "-g" or "--global":
                    globalFlag = args[i];
                    break;
                default:
                    stray ??= args[i];
                    break;
            }
        }

        if (print && (target is not null || globalFlag is not null || stray is not null))
            throw new UsageException($"--skill takes no other options ('{stray ?? globalFlag ?? "--install-skill"}' given).");
        if (print)
            return new ParseResult.Skill();
        if (target is null)
            throw new UsageException($"{globalFlag} requires --install-skill <harness>.");
        if (stray is not null)
            throw new UsageException($"'{stray}' cannot be combined with --install-skill.");
        return new ParseResult.InstallSkill(target, globalFlag is not null);
    }

    private static CliOptions ParseOptions(IReadOnlyList<string> args)
    {
        var options = new CliOptions();
        string? passwordFlag = null;
        string? passphraseFlag = null;
        string? envFlag = null;
        string? userSecretsFlag = null;
        string? separatorFlag = null;
        string? generationFlag = null;
        string? countFlag = null;

        for (var i = 0; i < args.Count; i++)
        {
            var flag = args[i];
            string NextValue() => ++i < args.Count ? args[i] : throw new UsageException($"{flag} expects a value.");

            switch (flag)
            {
                case "-p" or "--passphrase":
                    options = options with { Mode = GenerationMode.Passphrase };
                    generationFlag ??= flag;
                    break;
                case "-n" or "--count":
                    options = options with { Count = ParseInt(flag, NextValue()) };
                    countFlag = flag;
                    break;
                case "-q" or "--quiet":
                    options = options with { Quiet = true };
                    generationFlag ??= flag;
                    break;
                case "-c" or "--copy":
                    options = options with { Copy = true };
                    break;
                case "--stdin":
                    options = options with { FromStdin = true };
                    break;

                case "-l" or "--length":
                    options = options with { Password = options.Password with { Length = ParseInt(flag, NextValue()) } };
                    passwordFlag ??= flag;
                    break;
                case "-e" or "--env-safe":
                    options = options with { Password = options.Password with { EnvSafe = true } };
                    passwordFlag ??= flag;
                    break;
                case "-a" or "--no-ambiguous":
                    options = options with { Password = options.Password with { ExcludeAmbiguous = true } };
                    passwordFlag ??= flag;
                    break;
                case "-x" or "--exclude":
                    options = options with { Password = options.Password with { Exclude = options.Password.Exclude + NextValue() } };
                    passwordFlag ??= flag;
                    break;
                case "-S" or "--no-symbols":
                    options = options with { Password = options.Password with { Symbols = false } };
                    passwordFlag ??= flag;
                    break;
                case "-A" or "--all-symbols":
                    options = options with { Password = options.Password with { AllSymbols = true } };
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

                case "-w" or "--words":
                    options = options with { Passphrase = options.Passphrase with { Words = ParseInt(flag, NextValue()) } };
                    passphraseFlag ??= flag;
                    break;
                case "-s" or "--separator":
                    options = options with { Passphrase = options.Passphrase with { Separator = NextValue() } };
                    passphraseFlag ??= flag;
                    separatorFlag = flag;
                    break;
                case "--no-capitalize":
                    options = options with { Passphrase = options.Passphrase with { Capitalize = false } };
                    passphraseFlag ??= flag;
                    break;
                case "--no-digit":
                    options = options with { Passphrase = options.Passphrase with { AddDigit = false } };
                    passphraseFlag ??= flag;
                    break;

                case "-k" or "--key":
                    options = options with { EnvKey = NextValue() };
                    break;
                case "-f" or "--file":
                    options = options with { EnvFile = NextValue() };
                    envFlag ??= flag;
                    break;
                case "--force":
                    options = options with { Force = true };
                    envFlag ??= flag;
                    break;
                case "-u" or "--user-secrets":
                    options = options with { UserSecrets = true };
                    userSecretsFlag ??= flag;
                    break;
                case "--project":
                    options = options with { Project = NextValue() };
                    userSecretsFlag ??= flag;
                    break;
                case "--id":
                    options = options with { SecretsId = NextValue() };
                    userSecretsFlag ??= flag;
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
        if (options.EnvKey is null && envFlag is not null)
            throw new UsageException($"{envFlag} requires --key.");
        if (options.EnvKey is null && userSecretsFlag is not null)
            throw new UsageException($"{userSecretsFlag} requires --key.");
        if (!options.UserSecrets && userSecretsFlag is not null)
            throw new UsageException($"{userSecretsFlag} requires --user-secrets.");
        if (options.UserSecrets && envFlag is not null)
            throw new UsageException($"{envFlag} only applies to a .env file, not --user-secrets.");
        if (options.Copy && options.EnvKey is not null)
            throw new UsageException("-c/--copy cannot be used with --key; pick one destination.");
        if ((options.Copy || options.EnvKey is not null) && options.Count != 1)
            throw new UsageException($"{countFlag} cannot be used when storing the secret; one destination holds one secret.");

        if (options.FromStdin)
        {
            if (options.EnvKey is null)
                throw new UsageException("--stdin requires --key: it stores a password you already have.");
            if ((generationFlag ?? passwordFlag ?? passphraseFlag ?? countFlag) is { } flag)
                throw new UsageException($"{flag} cannot be used with --stdin; nothing is generated.");
        }

        if (options.EnvKey is { } key)
        {
            var pattern = options.UserSecrets ? UserSecrets.KeyPattern : EnvFile.KeyPattern;
            if (!pattern.IsMatch(key))
                throw new UsageException(options.UserSecrets
                    ? $"'{key}' is not a valid configuration key."
                    : $"'{key}' is not a valid variable name (letters, digits and _, not starting with a digit; use -u for a configuration key such as Db:Password).");

            // A generated value is written unquoted, so it must not contain anything a .env loader would reinterpret.
            options = options with { Password = options.Password with { EnvSafe = true } };
            if (options.Mode == GenerationMode.Passphrase
                && options.Passphrase.Separator.Any(c => CharacterSets.EnvUnsafe.Contains(c) || char.IsWhiteSpace(c) || c is '"' or '\'' or '\\' or '`'))
                throw new UsageException($"{separatorFlag} must not contain $, #, quotes, backslash or whitespace when writing to a .env file.");
        }

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
