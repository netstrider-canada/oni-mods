# Oxygen Not Included mods

Four mods for [Oxygen Not Included](https://store.steampowered.com/app/457140/), each in its own folder with its own README.

## The mods

- [Honest Instant Build](honest-instant-build/README.md): build orders complete instantly, without sandbox mode, but with some honest restrictions. Speeds up gameplay at the cost of some game challenge.
- [Honest Instant Deconstruction](honest-instant-deconstruction/README.md): deconstruct orders finish instantly, without sandbox mode, but with some honest restrictions. Speeds up gameplay at the cost of some game challenge.
- [Honest Instant Dig](honest-instant-dig/README.md): dig orders finish instantly, without sandbox mode, but with some honest restrictions. Speeds up gameplay at the cost of some game challenge.
- [Self-sealing Airlocks (Continued)](self-sealing-airlocks/README.md): doors on Auto or Locked stay airtight, even while a duplicant walks through.

## How they are tested

All four are tested by playing them in the real game before every release: an automated run loads real colonies, places orders, walks duplicants through doors, and checks the results, with each mod also switched off to prove the checks can tell the difference. Every release is security and privacy scanned: two antivirus engines and a check for personal details on the files that ship, and CodeQL on this source. The results and file hashes are in each [release's notes](https://github.com/netstrider-canada/oni-mods/releases).

## Building

All four target .NET Framework 4.8 and build with the .NET SDK (`dotnet build -c Release` in each mod's `src` folder) on Windows or Linux. They reference the game's own DLLs, which are Klei's and are not in this repository. Copy these from the game's `OxygenNotIncluded_Data/Managed` folder into `honest-instant-build/lib/` (every mod reads them from there):

```text
Assembly-CSharp.dll
Assembly-CSharp-firstpass.dll
0Harmony.dll
UnityEngine.dll
UnityEngine.CoreModule.dll
```

Then copy each mod's `bin/Release/net48/` DLL, `mod.yaml` and `mod_info.yaml` into a folder under the game's `mods/Dev/`.

## How to get support

Please raise an issue on [GitHub](https://github.com/netstrider-canada/oni-mods/issues) if you have any problems and I'll take a look.

## Credits and licensing

Each mod carries its own licence in its folder: Self-sealing Airlocks is GPL-3.0, inherited from the original (its README credits the authors it builds on); Honest Instant Build, Honest Instant Deconstruction and Honest Instant Dig are MIT.
