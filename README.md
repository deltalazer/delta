<p align="center">
  <img src="https://raw.githubusercontent.com/deltalazer/delta/experimental/assets/osudelta.png" width="180" alt="DeltaLazer logo" />
</p>

# DeltaLazer

DeltaLazer (formerly called osu!v2 [Cliche name, we know]) is a community-driven fork of **osu!lazer**, made to be a playground for custom implementations with the lazer client!

Join our community discord!! (have fun and share stuff)
https://discord.gg/dfPwhRtGVZ

## What this fork adds (in simplest terms)

Modified In-game Logo

### (osu!standard only) 
Object Aware Star Rating and PP [For Forced Mods]
Extended Slider Velocity Cap of **1000x**!

Advanced Section-based Gimmicks with Hitobject-specific Attributes!
All Overrides:
<details open>
    <summary> DeltaLazer specific</summary>

    General:
    - Slider Tick Rate Overrides

    Mods:
    - Polyopia / Are those decoys? or are my eyes decieving me? Hitobjects that do nothing when hit, Typically used in conjunction with other hitobjects.
        - [ ] Avoid / Add poison to Decoys
    - Penalty Adjust / You feel your sins crawling on your back. Increase or decrease the consequences of imperfect hits.
        - Hitcount Tolerance
        - HP Reduction Multiplier
        - HP limit
    - Variate / Some drown, while others die of thirst. Heal players who make mistakes, while ignoring [or hurting] "perfectionists".
</details>

<details open>
  <summary>Difficulty Increase</summary>
    
    - Hidden
    - Flashlight [With Dynamic Sizing]
    - Hard Rock
    - Double Time
    - Sudden Death/No Miss
    - Accuracy Challenge
</details>

<details>
    <summary>Fun Mods</summary>
    
    - Wiggle
    - Transform
    - Spin-In
    - Grow
    - Deflate
    - Barrel Roll
    - Approach Different
    - Muted
    - No-Scope
    - Traceable
    - Magnetized
    - Repel
    - Freeze Frame
    - Bubbles
    - Synthesia
    - Depth
    - Bloom
</details>

<details>
    <summary>Conversion</summary>

    - Difficulty Adjust
        - Circle Size
        - HP Drain
        - Accuracy [Overall Difficulty]
        - Approach Rate
        - [DeltaLazer Specific] Stack Leniency
    - Single Tap
    - Alternate
</details>

## project base

- upstream project: https://github.com/ppy/osu
- this repository tracks and extends upstream osu!lazer behavior

## important server note

We have temporarily disabled logins as this client still connects to osu! servers.
When bypassed, osu!bancho prohibits score submission as this client is modified.

## Build Instructions (desktop)


Debug Builds
    Building from `root` of this repository:

```bash
  dotnet build osu.Desktop/osu.Desktop.csproj -c Debug
```

publish debug builds:

```bash
dotnet publish osu.Desktop/osu.Desktop.csproj -c Debug -r win-x64 --self-contained false -o ../builds/windows-debug-v2
dotnet publish osu.Desktop/osu.Desktop.csproj -c Debug -r linux-x64 --self-contained false -o ../builds/linux-debug-v2
```

## Bug Reports

If your issue is replicable in the standard client, Please [open an issue upstream instead.](https://github.com/ppy/osu/issues/new/choose)

Do not mention "Deltalazer" on your upstream bug report, as deltalazer (currently) has no affiliation with the osu!team. Doing so may be potentially disrespectful to them OR this fork.

If your issue is NOT replicable by the standard client, you may report the issue [here](https://github.com/deltalazer/delta/issues/new/choose), however keep in mind that:

1. This client is very experimental
2. Most issues are likely to already have a report, please check the Issues tab! 

### Please include:

- Your beatmap file and exact section/hitobject settings used
- The expected behavior compared to the actual behavior
- Your client's crash logs/stack trace if available
- Your build type [Debug or Release]

## Contributing

Community contributions are welcome and encouraged!!

- Open an issue for any bug reports
- Open a discussion for feature and/or design proposals
- Open a pull request for any changes to the code base
For Developers: Please include tests where possible for regression safety

## License

This fork uses the MIT license, which matches the license of the upstream client (osu!lazer)

See `LICENCE` for more information

## Additional Resources

In-Game Asset Overrides: [delta/delta-resources](https://github.com/deltalazer/delta-resources/tree/master)

Please note: DeltaLazer has its own branding guidelines!