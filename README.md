<p align="center">
  <img src="https://raw.githubusercontent.com/auroraongithub/delta/master/assets/osudelta.png" width="180" alt="osu! logo" />
</p>

# delta (deltalazer)

community-driven fork of **osu!lazer** with section and hitobject gimmick systems for mapping and gameplay experiments

join our community discord!! (have fun and share stuff)
https://discord.gg/dfPwhRtGVZ

## what this fork adds (in simplest terms)

- section gimmicks and per-hitobject attributes with editor UI
- control hp/judgment limits
- force mods and allow customization

# which means you can do a lot of things!!!

## project base

- upstream project: https://github.com/ppy/osu
- this repository tracks and extends upstream osu!lazer behavior

## important server note

- **debug builds** connect to osu!dev server
- **release builds** connect to official osu! server

i **DO NOT** recommend building for release as this currently doesn't have systems in place that make it safe to use in the official osu! server

## quick build (desktop)

from repository root:

```bash
dotnet build osu.Desktop/osu.Desktop.csproj -c Debug
```

publish debug builds:

```bash
dotnet publish osu.Desktop/osu.Desktop.csproj -c Debug -r win-x64 --self-contained false -o ../builds/windows-debug-v2
dotnet publish osu.Desktop/osu.Desktop.csproj -c Debug -r linux-x64 --self-contained false -o ../builds/linux-debug-v2
```

## reporting bugs

please include:

- map file and exact section/hitobject settings used
- expected vs actual behavior
- crash logs/stack trace if available
- whether you used debug or release build

## contributing

community contributions are welcome!!

- open issue for bug reports/feature proposals
- open pull request with focused changes
- include tests where possible for regression safety

## license

same license as upstream osu!lazer

see `LICENCE`
