# Honest Instant Build

Install: [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3839924729) | [GitHub Releases](https://github.com/netstrider-canada/oni-mods/releases)

Build orders complete instantly, without sandbox mode, but with some honest restrictions. Speeds up gameplay at the cost of some game challenge.

## What this mod does

- If your asteroid has the materials, the building is built instantly and materials are consumed instantly. If it cannot pay, the order is refused with the usual "No materials" message: nothing is ever built for free.
- Tile swaps, wires, and pipes are built instantly too. A swap drops the old material back, as the game does; long wire and pipe drags are joined up and each piece is charged its own cost.
- Materials are taken from the nearest piles first.
- Works with the game paused or running.
- Pairs well with my [Honest Instant Dig](../honest-instant-dig/README.md) mod: building over natural tiles happens instantly if both mods are active (assuming you meet all conditions: a duplicant on that asteroid could dig every tile of it).

## What this mod doesn't do

- Disable achievements.
- Instantly build anything if you don't have a duplicant on that asteroid (a rocket landed there counts).
- Use resources from other asteroids, you still need to figure out how to transport those.
- Instantly build over natural tiles: you must dig those first (unless [Honest Instant Dig](../honest-instant-dig/README.md) is also on!). Instead it places a normal, non-instant build order there as usual.
- Instantly build anything that needs a skill no duplicant on that asteroid has yet (conveyor rails, loaders, receptacles, chutes and bridges, the Auto-Sweeper, Steam Turbines and Heavi-Watt wires). Instead it places a normal, non-instant build order there as usual. Once a duplicant there has the skill, new orders for them are instant too.
- Instantly build rocket modules.
- Change research, discovered materials or placement rules: the mod lets the game place the order its usual way and only then finishes it.

## How it works

A Harmony patch runs right after the game creates any build order (buildings, tile swaps, wires and pipes all go through the same place). It asks the game's own "are the materials available?" check about the asteroid the order is on, before the order reserves anything. If the materials are there, it moves them from the nearest piles into the order and finishes it with the game's own completion code, so temperature, connections and refunds all work as in a normal build. If not, it cancels the order the same way the Cancel tool does. An order over natural ground is left to duplicants, unless Honest Instant Dig is loaded (checked by name when the order is placed; neither mod references the other): then the order is paid, the game's own delivery-complete step places its digs, Honest Instant Dig digs them, and the build is finished the moment the game would hand it to a builder. Each build, refusal or skip writes one `[HonestInstantBuild]` line to the game log.

## How it was tested

Tested using automated tests on various saves, Spaced Out and Vanilla, and played on a bunch of my saves. The automated run plays the real game on several real Spaced Out colonies and on the base game, covering: an ordinary build and its exact cost, refusal when the asteroid cannot pay, buildings that need a building skill left as normal orders until a duplicant on that asteroid has it, orders over rock left to duplicants (and, with Honest Instant Dig on, dug and built at once), tile swaps, wires through built tiles, dragged pipe and wire lines, building while paused, stock on another asteroid not paying, nothing left reserved afterwards, achievements still enabled, and, with the mod switched off, orders staying normal jobs. Every release is security and privacy scanned: two antivirus engines and a check for personal details on the files that ship, and CodeQL on this source. The results and file hashes are in each [release's notes](https://github.com/netstrider-canada/oni-mods/releases).

## Building

See the [top README](../README.md#building): `dotnet build -c Release` in `src`, with the game's DLLs in `lib/`.

## How to get support

Please raise an issue on [GitHub](https://github.com/netstrider-canada/oni-mods/issues) if you have any problems and I'll take a look.

## Other mods of mine

- [Honest Instant Deconstruction](../honest-instant-deconstruction/README.md): deconstruct orders finish instantly, without sandbox mode, but with some honest restrictions. Speeds up gameplay at the cost of some game challenge.
- [Honest Instant Dig](../honest-instant-dig/README.md): dig orders finish instantly, without sandbox mode, but with some honest restrictions. Speeds up gameplay at the cost of some game challenge.
- [Self-sealing Airlocks (Continued)](../self-sealing-airlocks/README.md): doors on Auto or Locked stay airtight, even while a duplicant walks through.

## Credits and licensing

MIT: see [LICENSE](LICENSE).
