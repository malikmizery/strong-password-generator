using System.Security.Cryptography;

namespace Spg;

public sealed class PassphraseGenerator
{
    private static readonly Lazy<PassphraseGenerator> DefaultInstance = new(() => new PassphraseGenerator(Wordlist.LoadEff()));

    private readonly IReadOnlyList<string> _words;

    public PassphraseGenerator(IReadOnlyList<string> words)
    {
        if (words.Count == 0)
            throw new ArgumentException("The word list must not be empty.", nameof(words));
        _words = words;
    }

    /// <summary>A generator backed by the embedded EFF large word list.</summary>
    public static PassphraseGenerator Default => DefaultInstance.Value;

    public string Generate(PassphraseOptions options)
    {
        ThrowIfInvalid(options);
        var words = new string[options.Words];
        for (var i = 0; i < words.Length; i++)
            words[i] = _words[RandomNumberGenerator.GetInt32(_words.Count)];

        if (options.Capitalize)
        {
            var i = RandomNumberGenerator.GetInt32(words.Length);
            words[i] = char.ToUpperInvariant(words[i][0]) + words[i][1..];
        }

        if (options.AddDigit)
        {
            var i = RandomNumberGenerator.GetInt32(words.Length);
            words[i] += (char)('0' + RandomNumberGenerator.GetInt32(10));
        }

        return string.Join(options.Separator, words);
    }

    public double EntropyBits(PassphraseOptions options)
    {
        ThrowIfInvalid(options);
        var bits = options.Words * Math.Log2(_words.Count);
        if (options.Capitalize)
            bits += Math.Log2(options.Words);
        if (options.AddDigit)
            bits += Math.Log2(options.Words) + Math.Log2(10);
        return bits;
    }

    private static void ThrowIfInvalid(PassphraseOptions options)
    {
        if (options.Validate() is { } error)
            throw new ArgumentException(error, nameof(options));
    }
}
