# Honest Instant Deconstruction

Install: [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3839803714) | [GitHub Releases](https://github.com/netstrider-canada/oni-mods/releases)

Deconstruct orders finish instantly, without sandbox mode, but with some honest restrictions. Speeds up gameplay at the cost of some game challenge.

## What this mod does

- Deconstruct orders finish instantly, whether given with the Deconstruct tool, from a building's own menu, or from a rocket module's.
- Gives back exactly what a duplicant would get by deconstructing it: the same materials and amounts, at the building's temperature and with its germs. A storage bin's contents land on the floor, and a pipe's contents spill into its cell, as usual.
- Works with the game paused or running.

## What this mod doesn't do

- Disable achievements.
- Instantly deconstruct anything if you don't have a duplicant on that asteroid (a rocket landed there counts).
- Instantly demolish ruins: they still need a duplicant with the Demolition skill, as usual.
- Instantly finish Reconstruct orders (changing a building's material).
- Instantly finish any deconstruct orders you had on your save before turning the mod on. Click a pending order twice to cancel it and give it again: the second order finishes at once.
- Uproot plants.
- Let you deconstruct anything the game won't.
- Give any duplicant Building experience for an instant deconstruction.

## How it works

A Harmony patch runs when the game is about to queue a Deconstruct job for a player's order. The game has its own instant branch for that moment, used only in debug mode; the mod takes that same branch in a normal game. Before it does, it applies the game's own refusals: a building the game will not let you deconstruct never reaches it, an order already pending is left alone, and a tile with a swap waiting on it is left to the game. Everything after that (the drops, their temperature, stored items, pipe contents, the "+ mass" pop-ups and the sound) is the game's own code. Each finished order writes one `[HonestInstantDeconstruction]` line to the game log.

## How it was tested

Tested using automated and manual tests on various saves, DLCs on and off, Spaced Out and Vanilla. The automated run plays the real game on several real Spaced Out colonies and on the base game, covering: a tile and a ladder giving back exactly their materials, at the building's temperature; eight tiles taken in one drag; a pipe spilling its water; a storage bin's contents neither lost nor doubled; deconstructing while paused; Reconstruct orders and orders loaded from a save staying duplicant jobs; ruins left standing; achievements still enabled; and, with the mod switched off, a Deconstruct order staying a normal job. Every release is security and privacy scanned: two antivirus engines and a check for personal details on the files that ship, and CodeQL on this source. The results and file hashes are in each [release's notes](https://github.com/netstrider-canada/oni-mods/releases).

## Building

See the [top README](../README.md#building): `dotnet build -c Release` in `src`, reading the game's DLLs from `../honest-instant-build/lib/`.

## How to get support

Please raise an issue on [GitHub](https://github.com/netstrider-canada/oni-mods/issues) if you have any problems and I'll take a look.

## Other mods of mine

- [Honest Instant Build](../honest-instant-build/README.md): build orders complete instantly, without sandbox mode, but with some honest restrictions. Speeds up gameplay at the cost of some game challenge.
- [Honest Instant Dig](../honest-instant-dig/README.md): dig orders finish instantly, without sandbox mode, but with some honest restrictions. Speeds up gameplay at the cost of some game challenge.
- [Self-sealing Airlocks (Continued)](../self-sealing-airlocks/README.md): doors on Auto or Locked stay airtight, even while a duplicant walks through.

## Credits and licensing

MIT: see [LICENSE](LICENSE).
