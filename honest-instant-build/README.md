# Honest Instant Build

Build orders finish instantly, without needing to be in sandbox mode, and still cost their full materials. Achievements still remain on.

## What this mod does

- Place a build order and, if the asteroid has the materials, it is built at once and the materials are taken from your stock.
- If the asteroid cannot pay, the order is refused: you see the usual "No materials" message and the ghost disappears. Nothing is ever built for free.
- Stock on another asteroid never pays for a build here.
- Tile swaps are instant too: the new material is taken and the old one dropped back, as the game does.
- Wires and pipes, including long drags, build at once, each segment joined to the next and charged its own cost.
- Materials are taken from the nearest piles first.
- Also works when the game is paused.

## What this mod doesn't do

- Anything that needs digging first (an order over natural rock) stays a normal job for your duplicants. Digging is not instant.
- Rocket modules are placed a different way and still build normally.
- Research, discovered materials and placement rules work as normal, because the mod lets the game place the order its usual way and only then finishes it.

## How it works

A Harmony patch runs right after the game creates any build order (buildings, tile swaps, wires and pipes all go through the same place). It asks the game's own "are the materials available?" check about the asteroid the order is on, before the order reserves anything. If the materials are there, it moves them from the nearest piles into the order and finishes it with the game's own completion code, so temperature, connections and refunds all work as in a normal build. If not, it cancels the order the same way the Cancel tool does. Each build, refusal or skip writes one `[HonestInstantBuild]` line to the game log.

## How it was tested

Every release is play-tested by an automated run in the real game, on several real Spaced Out colonies and on the base game, covering: an ordinary build and its exact cost, refusal when the asteroid cannot pay, orders over rock left to duplicants, tile swaps, wires through built tiles, dragged pipe and wire lines, building while paused, stock on another asteroid not paying, nothing left reserved afterwards, achievements still enabled, and, with the mod switched off, orders staying normal jobs. Every release is security scanned: two antivirus engines on the files that ship, and CodeQL on this source.

## Building

See the [top README](../README.md#building): `dotnet build -c Release` in `src`, with the game's DLLs in `lib/`.

## How to get support

Please raise an issue on [GitHub](https://github.com/netstrider-canada/oni-mods/issues) if you have any problems and I'll take a look.

## Other mods of mine

- [Honest Instant Deconstruction](../honest-instant-deconstruction/README.md): deconstruct orders finish instantly, without sandbox mode.
- [Self-sealing Airlocks (Continued)](../self-sealing-airlocks/README.md): doors on Auto or Locked stay airtight, even while a duplicant walks through.

## Credits and licensing

MIT: see [LICENSE](LICENSE).
