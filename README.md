# Oxygen Not Included mods

Three mods for [Oxygen Not Included](https://store.steampowered.com/app/457140/):

- [Honest Instant Build](honest-instant-build/README.md): build orders finish the moment you place them, but still cost their full materials. No sandbox, no debug mode, achievements stay on.
- [Honest Instant Deconstruction](honest-instant-deconstruction/README.md): deconstruct orders finish the moment you give them, and drop exactly what your duplicants would have dropped. No sandbox, no debug mode, achievements stay on.
- [Self-sealing Airlocks (Continued)](self-sealing-airlocks/README.md): doors on Auto or Locked stay airtight while duplicants walk through. A continuation of chromiumboy's original, rebuilt for the current game.

All three are tested by playing them in the real game before every release: an automated run loads real colonies, places orders, walks duplicants through doors, and checks the results, with each mod also switched off to prove the checks can tell the difference.

## Building

All three target .NET Framework 4.8 and build with the .NET SDK (`dotnet build -c Release` in each mod's `src` folder) on Windows or Linux. They reference the game's own DLLs, which are Klei's and are not in this repository. Copy these from the game's `OxygenNotIncluded_Data/Managed` folder into `honest-instant-build/lib/` (every mod reads them from there):

```text
Assembly-CSharp.dll
Assembly-CSharp-firstpass.dll
0Harmony.dll
UnityEngine.dll
UnityEngine.CoreModule.dll
```

Then copy each mod's `bin/Release/net48/` DLL, `mod.yaml` and `mod_info.yaml` into a folder under the game's `mods/Dev/`.

## Licences

Each mod carries its own licence: Self-sealing Airlocks is GPL-3.0 (inherited from the original); Honest Instant Build's and Honest Instant Deconstruction's are in their folders.
