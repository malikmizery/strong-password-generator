namespace Spg.Tests;

public class InteractivePromptTests
{
    private static (CliOptions Options, string Output) Run(string input)
    {
        var output = new StringWriter();
        var options = new InteractivePrompt(new StringReader(input), output).Run();
        return (options, output.ToString());
    }

    [Theory]
    [InlineData("\n\n\n\n\n\n\n\n")]  // Enter at every prompt
    [InlineData("")]                   // stdin closed immediately
    public void AcceptingDefaults_GivesDefaultPassword(string input)
    {
        var (options, _) = Run(input);

        Assert.Equal(GenerationMode.Password, options.Mode);
        Assert.Equal(20, options.Password.Length);
        Assert.True(options.Password.Upper && options.Password.Lower && options.Password.Digits && options.Password.Symbols);
        Assert.False(options.Password.ExcludeAmbiguous);
        Assert.Equal(1, options.Count);
    }

    [Fact]
    public void ClosedStdin_StillEndsEachPromptLine()
    {
        // Otherwise the password printed next lands on the same line as the last prompt.
        var (_, output) = Run("");

        Assert.EndsWith(Environment.NewLine, output);
    }

    [Fact]
    public void PasswordAnswers_AreApplied()
    {
        // mode, length, upper, lower, digits, symbols, exclude ambiguous, env-safe, exclude, count
        var (options, _) = Run("1\n32\nn\nY\nyes\nNO\ny\nn\n\n5\n");

        Assert.Equal(GenerationMode.Password, options.Mode);
        Assert.Equal(32, options.Password.Length);
        Assert.False(options.Password.Upper);
        Assert.True(options.Password.Lower);
        Assert.True(options.Password.Digits);
        Assert.False(options.Password.Symbols);
        Assert.True(options.Password.ExcludeAmbiguous);
        Assert.False(options.Password.EnvSafe);
        Assert.Equal("", options.Password.Exclude);
        Assert.Equal(5, options.Count);
    }

    [Fact]
    public void EnvSafeAndExcludeAnswers_AreApplied()
    {
        var (options, output) = Run("1\n\n\n\n\n\n\ny\n&*\n\n");

        Assert.True(options.Password.EnvSafe);
        Assert.Equal("&*", options.Password.Exclude);
        Assert.Contains(".env", output);
    }

    [Fact]
    public void PassphraseAnswers_AreApplied()
    {
        // mode, words, separator, capitalize, digit, count
        var (options, _) = Run("2\n8\n.\nn\nn\n3\n");

        Assert.Equal(GenerationMode.Passphrase, options.Mode);
        Assert.Equal(8, options.Passphrase.Words);
        Assert.Equal(".", options.Passphrase.Separator);
        Assert.False(options.Passphrase.Capitalize);
        Assert.False(options.Passphrase.AddDigit);
        Assert.Equal(3, options.Count);
    }

    [Theory]
    [InlineData("none", "")]
    [InlineData(" ", " ")]
    [InlineData("", "-")]
    public void Separator_SupportsNoneSpaceAndDefault(string answer, string expected) =>
        Assert.Equal(expected, Run($"2\n\n{answer}\n\n\n\n").Options.Passphrase.Separator);

    [Fact]
    public void InvalidAnswers_AreAskedAgain()
    {
        // mode "9" and "x", length "abc" and "0", yes/no "maybe" — each rejected, then answered properly
        var (options, output) = Run("9\nx\n1\nabc\n0\n30\nmaybe\nn\n\n\n\n\n\n");

        Assert.Equal(30, options.Password.Length);
        Assert.False(options.Password.Upper);
        Assert.True(CountOccurrences(output, "Length") >= 3);
    }

    [Fact]
    public void DisablingEverySet_ExplainsAndAsksAgain()
    {
        // first round turns every set off, second round accepts defaults
        var (options, output) = Run("1\n20\nn\nn\nn\nn\nn\n" + "\n\n\n\n\n\n" + "\n");

        Assert.True(options.Password.Upper && options.Password.Lower && options.Password.Digits && options.Password.Symbols);
        Assert.Contains("character set", output);
    }

    private static int CountOccurrences(string text, string value) =>
        (text.Length - text.Replace(value, "").Length) / value.Length;
}
