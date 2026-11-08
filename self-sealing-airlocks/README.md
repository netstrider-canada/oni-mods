# Self-sealing Airlocks (Continued)

Doors set to **Auto** or **Locked** stay airtight: gas and liquid cannot pass, even while a duplicant walks through. Doors set to **Open** let gas and liquid through as normal. Every door except the Pneumatic Door is covered.

This continues Self-sealing Airlocks, whose published copies stopped working with the 2026 game updates.

## What it does

- A Mechanized, Manual or Insulated Airlock on Auto or Locked keeps its solid cells while a duplicant passes, so nothing flows through.
- An Insulated Door keeps its insulation while a duplicant passes, so it does not leak heat.
- Switching a door to Open (even with a duplicant standing in it) opens it to gas and liquid.
- The Pneumatic Door is left exactly as the game has it.
- Deconstructing a door frees its cells.
- Duplicants can change a door's mode (Auto, Open, Locked) normally. Any mod that patches doors as it loads breaks this in the base game - the duplicant errors and never finishes - and this mod repairs it.

## How it works

1. When a door on Auto or Locked opens for a duplicant, a Harmony patch on `Door.SetSimState` keeps the door's solid cells instead of letting the game hollow them out. Duplicants still pass, because the game sets who can walk through separately.
2. When the player switches a door to Open with a duplicant in the doorway, a patch on `Door.RefreshControlState` opens the cells, since the game does not do that by itself then.
3. Patching doors at load runs the door code's one-time setup before the game has loaded its animations, which leaves the animation a duplicant uses to change a door's mode empty. A patch on `Door.OnPrefabInit` puts it back. The cause was traced in the Klei forums thread [Simple empty mod causes game to crash](https://kleiforums.com/forums/topic/149951-simple-empty-mod-causes-game-to-crash/) (2023).
4. Older versions saved a gas-blocking flag on door cells; a patch on `Door.OnSpawn` clears it so those saves clean themselves up.

Closing, temperature, insulation and falling sand stay with the game's own code.

Turn off any other self-sealing airlock mod first, or two copies will patch the same doors.

## Tested

Every release is play-tested by an automated run in the real game, on several real Spaced Out colonies and on the base game: doors stay sealed on Auto and Locked while a duplicant passes, open on Open (including with a duplicant in the doorway), seal again when set back to Auto, the Insulated Door keeps its insulation, the Pneumatic Door is unchanged, deconstruction frees the cells, and a duplicant changes a door's mode without error (proven in Spaced Out; in the small base-game test colony no duplicant took that job in the time given). The same checks run with the mod switched off show the game's own leaky doors, proving they can tell the difference.

## Credits

- **chromiumboy** wrote the original [Self-sealing Airlocks](https://github.com/chromiumboy/OxygenNotIncludedMods/tree/master/SelfSealingAirlocks).
- **LoneMarauder** ported it to Harmony 2 as [Self-sealing Airlocks Revived](https://github.com/LoneMarauder/ONI-Mods/tree/main/SelfSealingAirlocksRevived); **data54** contributed fixes.
- **yohan-id** rewrote it as [SelfSealingAirlocksRevivedV2](https://github.com/yohan-id/SelfSealingAirlocksRevivedV2), the code this copy starts from.

## Licence

GPL-3.0, as the original: see [LICENSE](LICENSE).
