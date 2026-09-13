namespace Spg.Tests;

public class WordlistTests
{
    [Fact]
    public void LoadEff_ReturnsAll7776DistinctWordsWithoutDiceNumbers()
    {
        var words = Wordlist.LoadEff();

        Assert.Equal(7776, words.Count);
        Assert.Equal(7776, words.Distinct().Count());
        Assert.Equal("abacus", words[0]);
        Assert.Equal("zoom", words[^1]);
        Assert.All(words, word => Assert.Matches("^[a-z]+(-[a-z]+)?$", word));
    }
}
