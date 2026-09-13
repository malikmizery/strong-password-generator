namespace Spg;

public static class Wordlist
{
    private const string EffResource = "Spg.eff_large_wordlist.txt";

    /// <summary>Loads the EFF large word list (7776 words, CC BY 3.0 US) embedded in this assembly.</summary>
    public static IReadOnlyList<string> LoadEff()
    {
        using var stream = typeof(Wordlist).Assembly.GetManifestResourceStream(EffResource)
            ?? throw new InvalidOperationException($"Embedded resource '{EffResource}' not found.");
        using var reader = new StreamReader(stream);

        var words = new List<string>(7776);
        while (reader.ReadLine() is { } line)
        {
            // Each line is "<five dice digits>\t<word>".
            var tab = line.IndexOf('\t');
            if (tab >= 0)
                words.Add(line[(tab + 1)..].Trim());
        }

        return words;
    }
}
