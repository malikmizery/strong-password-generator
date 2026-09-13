using System.Globalization;

namespace Spg;

/// <summary>
/// Asks for each option in turn. Pressing Enter (or closing stdin) accepts the default shown in brackets.
/// </summary>
public sealed class InteractivePrompt(TextReader input, TextWriter output)
{
    public CliOptions Run()
    {
        output.WriteLine("Strong password generator - press Enter to accept the [default].");

        var mode = AskInt("Generate (1) random characters or (2) passphrase?", 1, 1, 2) == 2
            ? GenerationMode.Passphrase
            : GenerationMode.Password;

        var options = mode == GenerationMode.Password
            ? new CliOptions { Password = AskPasswordOptions() }
            : new CliOptions { Mode = GenerationMode.Passphrase, Passphrase = AskPassphraseOptions() };

        return options with { Count = AskInt("How many to generate", 1, 1, CliOptions.MaxCount) };
    }

    private PasswordOptions AskPasswordOptions()
    {
        while (true)
        {
            var options = new PasswordOptions
            {
                Length = AskInt("Length", PasswordOptions.DefaultLength, 1, PasswordOptions.MaxLength),
                Upper = AskYesNo("Include uppercase letters?", true),
                Lower = AskYesNo("Include lowercase letters?", true),
                Digits = AskYesNo("Include digits?", true),
                Symbols = AskYesNo("Include symbols?", true),
                ExcludeAmbiguous = AskYesNo("Leave out look-alike characters (0 O 1 l I |)?", false),
                EnvSafe = AskYesNo("Will it go in a .env file? (leaves out $ and #)", false),
                Exclude = AskText("Other characters to leave out", ""),
            };

            if (options.Validate() is not { } error)
                return options;
            output.WriteLine(error);
        }
    }

    private PassphraseOptions AskPassphraseOptions()
    {
        var words = AskInt("Number of words", PassphraseOptions.DefaultWords, 1, PassphraseOptions.MaxWords);

        output.Write($"Separator (type \"none\" for no separator) [{PassphraseOptions.DefaultSeparator}]: ");
        var separator = ReadAnswer() switch
        {
            null or "" => PassphraseOptions.DefaultSeparator,
            var answer when answer.Trim().Equals("none", StringComparison.OrdinalIgnoreCase) => "",
            var answer => answer,
        };

        return new PassphraseOptions
        {
            Words = words,
            Separator = separator,
            Capitalize = AskYesNo("Capitalize a random word?", true),
            AddDigit = AskYesNo("Append a digit to a random word?", true),
        };
    }

    private string AskText(string question, string defaultValue)
    {
        output.Write($"{question} [{(defaultValue.Length == 0 ? "none" : defaultValue)}]: ");
        return ReadAnswer() is { Length: > 0 } answer ? answer : defaultValue;
    }

    private int AskInt(string question, int defaultValue, int min, int max)
    {
        while (true)
        {
            output.Write($"{question} [{defaultValue}]: ");
            var answer = ReadAnswer()?.Trim();
            if (string.IsNullOrEmpty(answer))
                return defaultValue;
            if (int.TryParse(answer, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value >= min && value <= max)
                return value;
            output.WriteLine($"Please enter a whole number from {min} to {max}.");
        }
    }

    private bool AskYesNo(string question, bool defaultValue)
    {
        while (true)
        {
            output.Write($"{question} [{(defaultValue ? "Y/n" : "y/N")}]: ");
            switch (ReadAnswer()?.Trim().ToLowerInvariant())
            {
                case null or "":
                    return defaultValue;
                case "y" or "yes":
                    return true;
                case "n" or "no":
                    return false;
                default:
                    output.WriteLine("Please answer y or n.");
                    break;
            }
        }
    }

    private string? ReadAnswer()
    {
        var line = input.ReadLine();
        // At end of input the terminal echoes no newline, so end the prompt line ourselves.
        if (line is null)
            output.WriteLine();
        return line;
    }
}
