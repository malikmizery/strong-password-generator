# spg — strong password generator

A .NET 10 console app that generates random-character passwords and passphrases using
`System.Security.Cryptography.RandomNumberGenerator` (a CSPRNG, unbiased).

```text
spg                                 # interactive: prompts for every option
spg -l 20               -> k#9Qv!zR2@mXe7}Lp_4w      (the defaults: 20 chars, all sets)
spg -l 32 -n 5 --no-symbols
spg --no-ambiguous                  # no 0 O 1 l I |
spg -p                  -> gravity-Unsaid-pelican-omen3-trilogy-shrank
spg -p -w 8 -s . --no-capitalize --no-digit
spg -q | clip                       # stdout carries only the passwords
spg --help
```

Passwords go to stdout, one per line. The entropy estimate (and interactive prompts) go to stderr;
anything under 64 bits is flagged **WEAK**.

## How it works

- **Passwords**: every character is drawn uniformly from the union of the enabled sets, and the draw is
  repeated until every enabled set is present (rejection sampling). This keeps the result uniform over
  all valid passwords, so the reported entropy is exact (computed by inclusion–exclusion).
- **Passphrases**: words are drawn uniformly from the EFF long word list (7776 words, ~12.9 bits each).
  By default one random word is capitalized and a random digit is appended to a random word, so the
  result satisfies "must contain upper/digit" rules. Default 6 words ≈ 86 bits.

## Build & test

```powershell
dotnet test spg.slnx
dotnet publish src/Spg -c Release -o out     # out/spg.exe
```

## Attribution

`src/Spg/eff_large_wordlist.txt` is the [EFF Large Wordlist](https://www.eff.org/dice) by the
Electronic Frontier Foundation, licensed under
[CC BY 3.0 US](https://creativecommons.org/licenses/by/3.0/us/).
