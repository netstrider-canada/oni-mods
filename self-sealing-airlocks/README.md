# Self-sealing Airlocks (Continued)

Install: [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3839123854) | [GitHub Releases](https://github.com/netstrider-canada/oni-mods/releases)

All doors EXCEPT the Pneumatic Door, when set to **Auto** or **Locked**, prevent all gas and liquid from escaping through, even while a duplicant walks through. Setting the door to **Open** allows gas and liquid through as normal.

## What this mod does

- A Mechanized, Manual or Insulated Airlock on Auto or Locked keeps its solid cells while a duplicant passes, so nothing flows through.
- An Insulated Door keeps its insulation while a duplicant passes, so it does not leak heat.
- Switching a door to Open (even with a duplicant standing in it) opens it to gas and liquid.
- Deconstructing a door frees its cells.
- Saves from older versions clean themselves up.
- Duplicants can change a door's mode (Auto, Open, Locked) normally. Any mod that patches doors as it loads breaks this in the base game - the duplicant errors and never finishes - and this mod repairs it.

## What this mod doesn't do

- The Pneumatic Door is unchanged: it always lets gas and liquid through.
- It doesn't work alongside another self-sealing airlock mod. Turn any other off first, or two copies will patch the same doors.

## How it works

1. When a door on Auto or Locked opens for a duplicant, a Harmony patch on `Door.SetSimState` keeps the door's solid cells instead of letting the game hollow them out. Duplicants still pass, because the game sets who can walk through separately.
2. When the player switches a door to Open with a duplicant in the doorway, a patch on `Door.RefreshControlState` opens the cells, since the game does not do that by itself then.
3. Patching doors at load runs the door code's one-time setup before the game has loaded its animations, which leaves the animation a duplicant uses to change a door's mode empty. A patch on `Door.OnPrefabInit` puts it back. The cause was traced in the Klei forums thread [Simple empty mod causes game to crash](https://kleiforums.com/forums/topic/149951-simple-empty-mod-causes-game-to-crash/) (2023).
4. Older versions saved a gas-blocking flag on door cells; a patch on `Door.OnSpawn` clears it so those saves clean themselves up.

Closing, temperature, insulation and falling sand stay with the game's own code.

## How it was tested

Tested automatically by automated test scripts with many different saves, as well as played by myself first in my own games. Every release is play-tested by an automated run in the real game, on several real Spaced Out colonies and on the base game: doors stay sealed on Auto and Locked while a duplicant passes, open on Open (including with a duplicant in the doorway), seal again when set back to Auto, the Insulated Door keeps its insulation, the Pneumatic Door is unchanged, deconstruction frees the cells, and a duplicant changes a door's mode without error (proven in Spaced Out; in the small base-game test colony no duplicant took that job in the time given). The same checks run with the mod switched off show the game's own leaky doors, proving they can tell the difference. Every release is security scanned: two antivirus engines on the files that ship, and CodeQL on this source.

## Building

See the [top README](../README.md#building): `dotnet build -c Release` in `src`, reading the game's DLLs from `../honest-instant-build/lib/`.

## How to get support

Please raise an issue on [GitHub](https://github.com/netstrider-canada/oni-mods/issues) if you have any problems and I'll take a look.

## Other mods of mine

- [Honest Instant Build](../honest-instant-build/README.md): build orders finish instantly, without sandbox mode, and still cost their full materials.
- [Honest Instant Deconstruction](../honest-instant-deconstruction/README.md): deconstruct orders finish instantly, without sandbox mode.

## Credits and licensing

Previous versions of this mod have not been kept up to date and so I have continued it; their published copies stopped working with the 2026 game updates.

- **chromiumboy** wrote the original [Self-sealing Airlocks](https://github.com/chromiumboy/OxygenNotIncludedMods/tree/master/SelfSealingAirlocks).
- **LoneMarauder** ported it to Harmony 2 as [Self-sealing Airlocks Revived](https://github.com/LoneMarauder/ONI-Mods/tree/main/SelfSealingAirlocksRevived); **data54** contributed fixes.
- **yohan-id** rewrote it as [SelfSealingAirlocksRevivedV2](https://github.com/yohan-id/SelfSealingAirlocksRevivedV2), the code this copy starts from.

GPL-3.0, as the original: see [LICENSE](LICENSE).
