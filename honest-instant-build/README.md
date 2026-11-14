# Honest Instant Build

Install: [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3839924729) | [GitHub Releases](https://github.com/netstrider-canada/oni-mods/releases)

Build orders complete instantly and materials are consumed instantly. Somewhere between vanilla and sandbox modes in terms of game pace and difficulty. Faster building, same resource cost.

## What this mod does

- If your asteroid has the materials, the building is built instantly and materials are consumed instantly. If it cannot pay, the order is refused with the usual "No materials" message: nothing is ever built for free.
- Tile swaps, wires, and pipes are instant too. A swap drops the old material back, as the game does; long wire and pipe drags are joined up and each piece is charged its own cost.
- Materials are taken from the nearest piles first.
- Works with game paused or running.
- Achievements still remain on.

## What this mod doesn't do

- Use resources from other asteroids, you still need to figure out how to transport those.
- Instant build over dirt, you still need to dig first.
- Rocket modules still build normally.
- Research, discovered materials and placement rules work as normal, because the mod lets the game place the order its usual way and only then finishes it.

## How it works

A Harmony patch runs right after the game creates any build order (buildings, tile swaps, wires and pipes all go through the same place). It asks the game's own "are the materials available?" check about the asteroid the order is on, before the order reserves anything. If the materials are there, it moves them from the nearest piles into the order and finishes it with the game's own completion code, so temperature, connections and refunds all work as in a normal build. If not, it cancels the order the same way the Cancel tool does. Each build, refusal or skip writes one `[HonestInstantBuild]` line to the game log.

## How it was tested

Tested using automated tests on various saves, Spaced Out and Vanilla, and played on a bunch of my saves. The automated run plays the real game on several real Spaced Out colonies and on the base game, covering: an ordinary build and its exact cost, refusal when the asteroid cannot pay, orders over rock left to duplicants, tile swaps, wires through built tiles, dragged pipe and wire lines, building while paused, stock on another asteroid not paying, nothing left reserved afterwards, achievements still enabled, and, with the mod switched off, orders staying normal jobs. Every release is security scanned: two antivirus engines on the files that ship, and CodeQL on this source.

## Building

See the [top README](../README.md#building): `dotnet build -c Release` in `src`, with the game's DLLs in `lib/`.

## How to get support

Please raise an issue on [GitHub](https://github.com/netstrider-canada/oni-mods/issues) if you have any problems and I'll take a look.

## Other mods of mine

- [Honest Instant Deconstruction](../honest-instant-deconstruction/README.md): deconstruct orders finish instantly, without sandbox mode, for a faster pace of play.
- [Self-sealing Airlocks (Continued)](../self-sealing-airlocks/README.md): doors on Auto or Locked stay airtight, even while a duplicant walks through.

## Credits and licensing

MIT: see [LICENSE](LICENSE).
