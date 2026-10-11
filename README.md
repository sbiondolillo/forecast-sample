# forecast

`forecast` is a console program that tracks the places whose weather a person follows.
It shows the current weather at one of them.
It calls the Open-Meteo geocoding API and the Open-Meteo forecast API.
Neither service needs a key.

## Commands

Restore the packages in locked mode:

```sh
dotnet restore --locked-mode
```

Build the program:

```sh
dotnet build --no-restore
```

Check the format of the code:

```sh
dotnet format --verify-no-changes --no-restore
```

Run the build suite:

```sh
dotnet test --no-restore
```

Run the integration suite against the real services:

```sh
dotnet run --no-restore --project tests/Integration
```

## Usage

Add the place of a name to the tracked locations:

```sh
dotnet run --project src/App -- add <name>
```

List the tracked locations:

```sh
dotnet run --project src/App -- list
```

Show the current weather at a tracked location:

```sh
dotnet run --project src/App -- forecast <id>
```

Each subcommand takes the option `--database`.
It names the SQLite file that holds the tracked locations.
Its default value is `forecast.db`.

## How this repo is built

[`sbiondolillo/dotnet-software-factory`](https://github.com/sbiondolillo/dotnet-software-factory) builds this repo from its issues.
It opens one pull request for each issue that the owner labels `factory`.
The owner reviews and merges each pull request.
The file `docs/write-an-issue.md` holds the rules for an issue.
