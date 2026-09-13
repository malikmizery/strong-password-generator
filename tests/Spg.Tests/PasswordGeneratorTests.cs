namespace Spg.Tests;

public class PasswordGeneratorTests
{
    private const int Samples = 300;

    [Theory]
    [InlineData(4)]
    [InlineData(20)]
    [InlineData(128)]
    public void Generate_ReturnsRequestedLength(int length) =>
        Assert.Equal(length, PasswordGenerator.Generate(new PasswordOptions { Length = length }).Length);

    [Fact]
    public void Generate_AtMinimumLength_StillContainsEveryEnabledSet()
    {
        // Length 4 with 4 sets forces exactly one character per set; naive sampling would miss a set most of the time.
        var options = new PasswordOptions { Length = 4 };

        for (var i = 0; i < Samples; i++)
        {
            var password = PasswordGenerator.Generate(options);

            Assert.Contains(password, char.IsAsciiLetterUpper);
            Assert.Contains(password, char.IsAsciiLetterLower);
            Assert.Contains(password, char.IsAsciiDigit);
            Assert.Contains(password, c => !char.IsAsciiLetterOrDigit(c));
        }
    }

    [Fact]
    public void Generate_NeverUsesDisabledSets()
    {
        var options = new PasswordOptions { Length = 40, Digits = false, Symbols = false };

        for (var i = 0; i < Samples; i++)
            Assert.All(PasswordGenerator.Generate(options), c => Assert.True(char.IsAsciiLetter(c), $"unexpected '{c}'"));
    }

    [Fact]
    public void Generate_DigitsOnly_UsesOnlyDigits()
    {
        var options = new PasswordOptions { Length = 40, Upper = false, Lower = false, Symbols = false };

        Assert.All(PasswordGenerator.Generate(options), c => Assert.True(char.IsAsciiDigit(c)));
    }

    [Fact]
    public void Generate_ExcludeAmbiguous_OmitsLookAlikes()
    {
        var options = new PasswordOptions { Length = 60, ExcludeAmbiguous = true };

        for (var i = 0; i < Samples; i++)
            Assert.DoesNotContain(PasswordGenerator.Generate(options), c => "0O1lI|".Contains(c));
    }

    [Fact]
    public void Generate_IsUniformAcrossTheAlphabet()
    {
        var options = new PasswordOptions { Length = 20, Upper = false, Digits = false, Symbols = false };
        var counts = new int[26];

        for (var i = 0; i < 5000; i++)
            foreach (var c in PasswordGenerator.Generate(options))
                counts[c - 'a']++;

        // 100,000 draws over 26 letters: expected ~3846 each (σ ≈ 61); ±15% is ~9σ, so a real skew fails and noise never does.
        Assert.All(counts, count => Assert.InRange(count, 3269, 4423));
    }

    [Fact]
    public void Generate_ProducesDistinctPasswords()
    {
        var passwords = Enumerable.Range(0, 1000).Select(_ => PasswordGenerator.Generate(new PasswordOptions())).ToHashSet();

        Assert.Equal(1000, passwords.Count);
    }

    [Fact]
    public void Generate_InvalidOptions_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            PasswordGenerator.Generate(new PasswordOptions { Upper = false, Lower = false, Digits = false, Symbols = false }));

    [Fact]
    public void EntropyBits_SingleSet_IsLengthTimesLog2OfSetSize()
    {
        var options = new PasswordOptions { Length = 20, Upper = false, Digits = false, Symbols = false };

        Assert.Equal(94.0088, PasswordGenerator.EntropyBits(options), precision: 3);  // 20·log2(26)
    }

    [Fact]
    public void EntropyBits_ExcludeAmbiguous_ShrinksThePool()
    {
        var options = new PasswordOptions { Length = 10, Upper = false, Lower = false, Symbols = false, ExcludeAmbiguous = true };

        Assert.Equal(30.0, PasswordGenerator.EntropyBits(options), precision: 3);  // digits minus 0 and 1: 8^10
    }

    [Fact]
    public void EntropyBits_CountsOnlyPasswordsContainingEverySet()
    {
        // Two letters with one upper and one lower required: 52² − 2·26² = 1352 valid strings.
        var options = new PasswordOptions { Length = 2, Digits = false, Symbols = false };

        Assert.Equal(10.4009, PasswordGenerator.EntropyBits(options), precision: 3);
    }
}
