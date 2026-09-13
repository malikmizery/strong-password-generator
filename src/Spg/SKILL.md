---
name: spg
description: Generate a strong random password, secret, token or passphrase with the spg CLI and store it directly in a .env file or in dotnet user secrets, so the secret never appears in the conversation. Use whenever the user asks to create, set, or rotate a password, database/admin password, API key, secret, token, or passphrase, or when a .env file, compose file or appsettings needs a secret filled in.
---

# spg — generate secrets without seeing them

`spg` is a command-line password generator (CSPRNG, uniform, exact entropy). Use it instead of
inventing a password yourself: anything you write is not random and ends up in the transcript.

## The one rule

**Never let the secret enter the conversation.** Everything you run, read or print may be stored
and reviewed later. So, as an agent, you use exactly two forms of `spg` — both store the secret
directly and print nothing but the variable name:

```sh
spg -k NAME            # .env file
spg -k NAME -u         # dotnet user secrets
```

Never run `spg` any other way. In particular:

1. No bare `spg`, no `-c`, no `-f -`, no redirects or pipes: those put the secret on stdout or in
   your hands.
2. Never read the secret back afterwards: no `cat .env`, no `Get-Content`, no `grep`, no opening the
   file in an editor tool, no `dotnet user-secrets list`, no `echo $VAR`. If you must inspect a .env
   file, show names only: `cut -d= -f1 .env` (or `grep -v '^NAME=' .env`).
3. Report only the name and destination, e.g. "Stored `DB_PASSWORD` in `.env` (~121 bits)".
4. If the user wants to see or copy the secret, tell them to run `spg -c` (clipboard) or open the
   file themselves. Do not fetch it for them.
5. If the user pastes an existing password into the chat, it is already exposed. Do not store it;
   suggest rotating it with `spg -k NAME --force` (or they can store it themselves with
   `spg --stdin -k NAME` in their own terminal).

## .env file

```sh
spg -k DB_PASSWORD                    # appends DB_PASSWORD=<secret> to ./.env
spg -k API_TOKEN -f deploy/.env.prod  # another file
spg -k DB_PASSWORD --force            # rotate: replace an existing entry in place
spg -k SESSION_SECRET -l 48           # longer
spg -k RECOVERY_PHRASE -p -w 7        # a passphrase instead
```

The value is written without `$` or `#` (which .env loaders expand or treat as comments), so it
needs no quoting. `-k` refuses to overwrite an existing key unless `--force`. Files it creates are
mode 600 on Linux/macOS.

## dotnet user secrets

```sh
spg -k Db:Password -u                     # project in the current folder
spg -k Db:Password -u --project src/Web   # another project
spg -k Jwt:Key -u -l 64                   # longer
```

This runs `dotnet user-secrets set`, feeding the value through stdin so it never appears on a
command line. The project needs a `UserSecretsId` (`dotnet user-secrets init`).

## Tailoring the character set

| Flag | Effect |
|------|--------|
| `-l N` | length (default 20) |
| `-a` | leave out look-alikes `0 O 1 l I \|` |
| `-x CHARS` | leave out any characters a service rejects, e.g. `-x '&*'` |
| `-S` | no symbols at all (alphanumeric) |
| `-A` | all punctuation instead of the default `!@#$%^&*` |
| `-p -w N` | passphrase of N words (default 6) |

Defaults already avoid quotes, backslashes and backticks, so values are safe unquoted in
shells, YAML, JSON and .env files.

## Why this matters

Chats with AI assistants may be logged, reviewed, or used to improve models. A secret that
appears in a tool result or a reply is, from that moment, no longer a secret. Generating it
with `spg -k` keeps it out of the record entirely.
