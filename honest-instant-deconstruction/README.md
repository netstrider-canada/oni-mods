# Honest Instant Deconstruction

Deconstruct orders finish the moment you give them, and drop exactly what your duplicants would have dropped. No sandbox mode, no debug mode, and achievements stay on.

## What it does

- Drag the Deconstruct tool, press a building's Deconstruct button, or a rocket module's: the building is gone at once.
- What drops is the game's own: the same materials and amounts a duplicant's deconstruction gives back, at the building's temperature and with its germs.
- Whatever the building held comes out as the game drops it: a storage bin's contents land on the floor, and a pipe's contents spill into its cell.
- Works while paused.

## How it works

A Harmony patch runs when the game is about to queue a Deconstruct job for a player's order. The game has its own instant branch for that moment, used only in debug mode; the mod takes that same branch in a normal game. Before it does, it applies the game's own refusals: a building the game will not let you deconstruct never reaches it, an order already pending is left alone, and a tile with a swap waiting on it is left to the game. Everything after that (the drops, their temperature, stored items, pipe contents, the "+ mass" pop-ups and the sound) is the game's own code. Each finished order writes one `[HonestInstantDeconstruction]` line to the game log.

## Known limits

- Reconstruct orders (changing a building's material) and orders already pending when you load a save are still duplicant jobs. Click a pending order twice to cancel it and give it again: the second order finishes at once.
- Ruins that need the Demolition skill, and uprooting plants, are untouched.
- An instant deconstruction earns no duplicant any Building experience.

## Tested

Every release is play-tested by an automated run in the real game, on several real Spaced Out colonies and on the base game, covering: a tile and a ladder giving back exactly their materials, at the building's temperature; eight tiles taken in one drag; a pipe spilling its water; a storage bin's contents neither lost nor doubled; deconstructing while paused; Reconstruct orders and orders loaded from a save staying duplicant jobs; ruins left standing; achievements still enabled; and, with the mod switched off, a Deconstruct order staying a normal job.

## Building

It builds like the other mods here (see the top README): `dotnet build -c Release` in `src`, reading the game's DLLs from `../honest-instant-build/lib/`.

## Licence

See [LICENSE](LICENSE).
