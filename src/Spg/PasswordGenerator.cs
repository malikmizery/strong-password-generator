using System.Numerics;
using System.Security.Cryptography;

namespace Spg;

public static class PasswordGenerator
{
    public static string Generate(PasswordOptions options)
    {
        ThrowIfInvalid(options);
        var sets = options.EnabledSets();
        var pool = string.Concat(sets);
        var buffer = new char[options.Length];

        // Rejection sampling: draw every character uniformly from the whole pool and retry until each
        // enabled set is represented. Unlike "one from each set, then shuffle", this is uniform over all
        // valid passwords, which is what makes EntropyBits exact.
        do
        {
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = pool[RandomNumberGenerator.GetInt32(pool.Length)];
        }
        while (!sets.All(set => buffer.AsSpan().IndexOfAny(set) >= 0));

        return new string(buffer);
    }

    public static double EntropyBits(PasswordOptions options)
    {
        ThrowIfInvalid(options);
        var sizes = options.EnabledSets().Select(set => set.Length).ToArray();
        var poolSize = sizes.Sum();

        // Inclusion–exclusion: strings over the pool that contain at least one character from every set.
        var valid = BigInteger.Zero;
        for (var excludedMask = 0; excludedMask < 1 << sizes.Length; excludedMask++)
        {
            var excludedSize = 0;
            for (var i = 0; i < sizes.Length; i++)
                if ((excludedMask & (1 << i)) != 0)
                    excludedSize += sizes[i];

            var term = BigInteger.Pow(poolSize - excludedSize, options.Length);
            valid += BitOperations.PopCount((uint)excludedMask) % 2 == 0 ? term : -term;
        }

        return BigInteger.Log(valid, 2);
    }

    private static void ThrowIfInvalid(PasswordOptions options)
    {
        if (options.Validate() is { } error)
            throw new ArgumentException(error, nameof(options));
    }
}
