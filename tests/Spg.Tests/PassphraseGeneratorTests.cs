namespace Spg.Tests;

public class PassphraseGeneratorTests
{
    private static readonly string[] Words = ["alpha", "bravo", "charlie", "delta", "echo", "foxtrot", "golf", "hotel"];
    private static readonly PassphraseGenerator Generator = new(Words);
    private static readonly PassphraseOptions Plain = new() { Words = 5, Separator = "_", Capitalize = false, AddDigit = false };

    [Fact]
    public void Generate_JoinsRequestedNumberOfListWords()
    {
        var parts = Generator.Generate(Plain).Split('_');

        Assert.Equal(5, parts.Length);
        Assert.All(parts, part => Assert.Contains(part, Words));
    }

    [Fact]
    public void Generate_UsesSeparatorVerbatim()
    {
        var generator = new PassphraseGenerator(["cat"]);

        Assert.Equal("cat+cat+cat", generator.Generate(Plain with { Words = 3, Separator = "+" }));
        Assert.Equal("catcat", generator.Generate(Plain with { Words = 2, Separator = "" }));
    }

    [Fact]
    public void Generate_Capitalize_CapitalizesExactlyOneWord()
    {
        for (var i = 0; i < 200; i++)
        {
            var parts = Generator.Generate(Plain with { Capitalize = true }).Split('_');

            Assert.Single(parts, part => char.IsAsciiLetterUpper(part[0]));
            Assert.All(parts, part => Assert.Contains(part.ToLowerInvariant(), Words));
        }
    }

    [Fact]
    public void Generate_AddDigit_AppendsExactlyOneDigitToAWord()
    {
        for (var i = 0; i < 200; i++)
        {
            var parts = Generator.Generate(Plain with { AddDigit = true }).Split('_');

            var withDigit = Assert.Single(parts, part => part.Any(char.IsAsciiDigit));
            Assert.True(char.IsAsciiDigit(withDigit[^1]));
            Assert.Contains(withDigit[..^1], Words);
        }
    }

    [Fact]
    public void Generate_UsesEveryDigitPosition()
    {
        var digitWordIndexes = new HashSet<int>();
        for (var i = 0; i < 500; i++)
        {
            var parts = Generator.Generate(Plain with { AddDigit = true }).Split('_');
            digitWordIndexes.Add(Array.FindIndex(parts, part => char.IsAsciiDigit(part[^1])));
        }

        Assert.Equal([0, 1, 2, 3, 4], digitWordIndexes.Order());
    }

    [Fact]
    public void Generate_InvalidOptions_Throws() =>
        Assert.Throws<ArgumentException>(() => Generator.Generate(Plain with { Words = 0 }));

    [Fact]
    public void Constructor_RejectsEmptyWordList() =>
        Assert.Throws<ArgumentException>(() => new PassphraseGenerator([]));

    [Theory]
    [InlineData(false, false, 12.0)]     // 8^4 -> 4·3
    [InlineData(true, false, 14.0)]      // + which of 4 words is capitalized (2 bits)
    [InlineData(false, true, 17.3219)]   // + which word (2 bits) + which digit (log2 10)
    [InlineData(true, true, 19.3219)]
    public void EntropyBits_AccountsForEachRandomChoice(bool capitalize, bool addDigit, double expected)
    {
        var options = Plain with { Words = 4, Capitalize = capitalize, AddDigit = addDigit };

        Assert.Equal(expected, Generator.EntropyBits(options), precision: 3);
    }

    [Fact]
    public void Default_UsesTheFullEffList()
    {
        // 6·log2(7776) + log2(6) + log2(10) + log2(6)
        Assert.Equal(86.040, PassphraseGenerator.Default.EntropyBits(new PassphraseOptions()), precision: 2);
    }
}
