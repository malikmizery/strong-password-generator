# spg — strong password generator

A .NET 10 console app that generates random-character passwords and passphrases using
`System.Security.Cryptography.RandomNumberGenerator` (a CSPRNG, unbiased).

```text
spg                                 # interactive: prompts for every option
spg -l 20               -> k#9Qv!zR2@mXe7*Lp_4w      (the defaults: 20 chars, all sets)
spg -l 32 -n 5 -S                   # no symbols
spg -a                              # no look-alikes: 0 O 1 l I |
spg -e                              # .env-safe: no $ or #
spg -x '&*'                         # leave out whatever a site rejects
spg -A                              # all punctuation, not just !@#$%^&*
spg -c                              # copy to the clipboard, print nothing
spg -k DB_PASSWORD                  # write DB_PASSWORD=... into ./.env, print nothing
spg -k Db:Password -u               # store in dotnet user secrets, print nothing
spg --stdin -k DB_PASSWORD          # store a password you already have, quoted for .env
spg -p                  -> gravity-Unsaid-pelican-omen3-trilogy-shrank
spg -p -w 8 -s . --no-capitalize --no-digit
spg -q                              # stdout carries only the passwords, nothing on stderr
spg --install-skill claude          # teach an AI coding agent to use spg safely
spg --help
```

Passwords go to stdout, one per line. The entropy estimate (and interactive prompts) go to stderr;
anything under 64 bits is flagged **WEAK**.

## Which characters

The default symbols are `!@#$%^&*` — the eight that password rules accept almost everywhere, and the
same set most password managers generate by default. `-A, --all-symbols` adds the rest of the
US-keyboard punctuation (`()-_=+[]{}<>?/~;:,.|`). Quotes, backslash and backtick are never used, so a
password is safe unquoted in a shell, YAML or JSON.

Three flags narrow the set further, and the entropy estimate accounts for them:

- `-a, --no-ambiguous` drops the look-alikes `0 O 1 l I |`.
- `-e, --env-safe` drops `$` and `#`. In a `.env` file an unquoted `$NAME` or `${NAME}` is expanded as
  a variable reference and `#` starts a comment, so a password containing either gets silently
  truncated or rewritten by whatever loads the file (docker compose, dotenv libraries, most PaaS
  dashboards). Use this whenever the password is going into a `.env` file or a compose file.
- `-x, --exclude <chars>` drops any characters you list, e.g. `-x '&*'` for a site that refuses them.
  A set that ends up empty is simply dropped.

## Storing instead of printing

By default the password is printed. Three flags send it somewhere else and leave stdout empty;
stderr then says only where it went, plus the entropy.

```text
spg -c                                # clipboard (clip / pbcopy / wl-copy / xclip)
spg -k DB_PASSWORD                    # appends DB_PASSWORD=<password> to ./.env
spg -k API_TOKEN -f deploy/.env.prod  # another file
spg -k DB_PASSWORD --force            # replace an existing entry in place
spg -k DB_PASSWORD -f -               # just print the DB_PASSWORD=... line
spg -k PHRASE -p -w 7                 # a passphrase works too
spg -k Db:Password -u                 # dotnet user secrets (project in the current folder)
spg -k Db:Password -u --project src/Web
```

`-k, --key` implies `-e`, so the value needs no quoting. It refuses to overwrite an existing key
unless you pass `--force`, a `.env` file it creates on Linux/macOS is mode `600`, and everything else
in the file is left byte-for-byte as it was. `-u, --user-secrets` runs `dotnet user-secrets set`,
feeding the value through stdin so it never appears on a command line (the project needs a
`UserSecretsId`; run `dotnet user-secrets init` once).

## Storing a password you already have

If a password with `$` or `#` in it broke your `.env` file, you do not have to change the password —
you have to quote it. `--stdin` reads a password from stdin and stores it with the quoting every
common loader agrees on: bare when nothing needs escaping, otherwise single quotes.

```text
spg --stdin -k DB_PASSWORD            # paste it at the prompt; appends DB_PASSWORD='pa$$w#rd' to ./.env
spg --stdin -k DB_PASSWORD -f -       # just print the quoted line
spg --stdin -k Db:Password -u         # or into dotnet user secrets
```

The password is read from stdin on purpose: as a command-line argument it would land in your shell
history and be visible to every process on the machine. A password containing both `'` and `$` is
refused, because no `.env` spelling of it is portable across loaders — generate a new one instead.

## AI coding agents

If you let an AI agent (Claude Code, Codex, Gemini CLI, Cursor, Copilot, …) set up your projects,
you probably do not want it inventing passwords — they are not random — and you definitely do not
want the real password sitting in a chat transcript that may be logged, reviewed, or used for training.

`spg --install-skill <harness>` installs an [Agent Skill](https://agentskills.io) (`SKILL.md`) that
allows the agent exactly two forms — `spg -k NAME` (`.env` file) and `spg -k NAME -u` (dotnet user
secrets) — and forbids the rest: no bare `spg`, no clipboard, no redirects, never reading the secret
back, and reporting only the variable name and destination. The agent uses the password without
ever seeing it. If you paste an existing password into the chat, the skill tells the agent to treat
it as exposed and suggest rotating it.

```text
spg --install-skill claude       # ./.claude/skills/spg/SKILL.md  (this project only)
spg --install-skill claude -g    # ~/.claude/skills/spg/SKILL.md  (every project)
spg --install-skill codex -g     # also: gemini, cursor, copilot
spg --install-skill some/dir     # any skills directory
spg --skill                      # print the skill to stdout
```

## Install (Windows)

```powershell
irm https://github.com/malikmizery/strong-password-generator/releases/latest/download/install.ps1 | iex
```

This downloads the latest `spg.exe`, checks it against the release's `SHA256SUMS.txt`, puts it in
`%USERPROFILE%\.local\bin` and adds that folder to your user `PATH` (open a new terminal afterwards).
Run it again to update; delete the file to uninstall. Set `SPG_INSTALL_DIR` to install somewhere else, or
`SPG_NO_MODIFY_PATH=1` to leave `PATH` untouched. You can also download `spg.exe` directly from the
[latest release](https://github.com/malikmizery/strong-password-generator/releases/latest).

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
dotnet publish src/Spg -c Release -o out     # out/spg.exe — one file, no .NET needed
dotnet publish src/Spg -c Release -o out -r linux-x64   # or osx-arm64, win-arm64, ...
```

Publishing produces a single self-contained, trimmed executable; copy it anywhere on your `PATH`.
The settings live in `src/Spg/Properties/PublishProfiles/SingleFile.pubxml`, which Visual Studio's
Publish dialog also uses. With plain MSBuild, name the profile:
`msbuild src/Spg -t:Publish -restore -p:Configuration=Release -p:PublishProfile=SingleFile`.

## Attribution

`src/Spg/eff_large_wordlist.txt` is the [EFF Large Wordlist](https://www.eff.org/dice) by the
Electronic Frontier Foundation, licensed under
[CC BY 3.0 US](https://creativecommons.org/licenses/by/3.0/us/).
