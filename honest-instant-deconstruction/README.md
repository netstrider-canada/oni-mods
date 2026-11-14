# Honest Instant Deconstruction

Install: [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3839803714) | [GitHub Releases](https://github.com/netstrider-canada/oni-mods/releases)

Deconstruct orders finish instantly, without needing to be in sandbox mode. Achievements still remain on.

## What this mod does

- Drag the Deconstruct tool, press a building's Deconstruct button, or a rocket module's: the building is gone at once.
- What drops is the game's own: the same materials and amounts a duplicant's deconstruction gives back, at the building's temperature and with its germs.
- Whatever the building held comes out as the game drops it: a storage bin's contents land on the floor, and a pipe's contents spill into its cell.
- Also works when the game is paused.

## What this mod doesn't do

- Reconstruct orders (changing a building's material) and deconstruct orders already pending from existing saves stay duplicant jobs. Click a pending order twice to cancel it and give it again: the second order finishes at once.
- Ruins still require the Demolition skill.
- Uprooting plants is untouched.
- Anything the game will not let you deconstruct.
- An instant deconstruction earns no duplicant any Building experience.

## How it works

A Harmony patch runs when the game is about to queue a Deconstruct job for a player's order. The game has its own instant branch for that moment, used only in debug mode; the mod takes that same branch in a normal game. Before it does, it applies the game's own refusals: a building the game will not let you deconstruct never reaches it, an order already pending is left alone, and a tile with a swap waiting on it is left to the game. Everything after that (the drops, their temperature, stored items, pipe contents, the "+ mass" pop-ups and the sound) is the game's own code. Each finished order writes one `[HonestInstantDeconstruction]` line to the game log.

## How it was tested

Tested using automated and manual tests on various saves, DLCs on and off, Spaced Out and Vanilla. The automated run plays the real game on several real Spaced Out colonies and on the base game, covering: a tile and a ladder giving back exactly their materials, at the building's temperature; eight tiles taken in one drag; a pipe spilling its water; a storage bin's contents neither lost nor doubled; deconstructing while paused; Reconstruct orders and orders loaded from a save staying duplicant jobs; ruins left standing; achievements still enabled; and, with the mod switched off, a Deconstruct order staying a normal job. Every release is security scanned: two antivirus engines on the files that ship, and CodeQL on this source.

## Building

See the [top README](../README.md#building): `dotnet build -c Release` in `src`, reading the game's DLLs from `../honest-instant-build/lib/`.

## How to get support

Please raise an issue on [GitHub](https://github.com/netstrider-canada/oni-mods/issues) if you have any problems and I'll take a look.

## Other mods of mine

- [Honest Instant Build](../honest-instant-build/README.md): build orders finish instantly, without sandbox mode, and still cost their full materials.
- [Self-sealing Airlocks (Continued)](../self-sealing-airlocks/README.md): doors on Auto or Locked stay airtight, even while a duplicant walks through.

## Credits and licensing

MIT: see [LICENSE](LICENSE).
