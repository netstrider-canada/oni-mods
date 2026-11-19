# Honest Instant Dig

Install: [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3842568184) | [GitHub Releases](https://github.com/netstrider-canada/oni-mods/releases)

Dig orders finish instantly, without sandbox mode, but with some honest restrictions. Speeds up gameplay at the cost of some game challenge.

## What this mod does

- Dig orders finish instantly for tiles if you have at least one duplicant on the asteroid with the prerequisite skills to dig those tiles.
- Drops the same amount of resources a duplicant would get by digging it (half the mass, same temperature, same germs), with the usual "+ mass" pop-up and sound.
- Pairs well with my [Honest Instant Build](../honest-instant-build/README.md) mod by allowing instant building over natural tiles (assuming you meet all conditions). Each mod works on its own; they only work together when both are on.
- Works with the game paused or running.
- Creates a non-instant dig task if you don't have a duplicant on that asteroid who has the skills to dig the tile. Once one there has the skill, cancel the order and give it again: the new one is instant.

## What this mod doesn't do

- Disable achievements.
- Let you instantly dig any tiles if you don't have a duplicant on that asteroid (a rocket landed there counts).
- Let you instantly dig any tiles that no duplicant on that asteroid has the skills for (without Hard Digging you cannot dig Granite, for example). A skilled duplicant on another asteroid does not count.
- Dig Neutronium. That might break the universe even harder.
- Instantly dig any dig orders you had on your save before turning the mod on. Cancel one and give it again to make it instant.
- Uproot plants.
- Give any duplicant Mining experience for an instant dig.
- With Honest Instant Build, pay for or dig any of a building over ground nobody on that asteroid can dig yet: it stays a normal order in full.

## How it works

The Dig tool and a build order over natural ground both place the same dig marker. The mod remembers the markers made in the running game (never one a loaded save brings back), and a Harmony patch runs each time the game checks a marker's cell. If a duplicant on that asteroid could dig it now (not Neutronium, and the game's digging skill for that hardness present on a living duplicant there), the mod does what a duplicant's last swing does: it applies the cell's remaining damage in one go. The game's own code then digs the cell, drops the pile and removes the marker. Each dig writes one `[HonestInstantDig]` line to the game log, and so does each one left to duplicants, with the reason.

Honest Instant Build checks whether this mod is loaded when an order over natural ground is placed. It never references this mod's code, and this mod never references it. When this mod is loaded and every cell can be dug now, Honest Instant Build pays for the order, lets this mod dig it, and finishes the build the moment the game would hand it to a builder.

## How it was tested

Tested using automated tests on various saves, Spaced Out and Vanilla. The automated run plays the real game on several real Spaced Out colonies and on the base game, covering: a dig dropping exactly half the cell's mass at the cell's temperature; germs carried into the pile; eight cells dug in one drag; digging while paused; Neutronium refused; hard ground waiting for a duplicant with the skill on that asteroid, and dug at once when one has it; a skilled duplicant on another asteroid not counting; a dig order loaded from a save staying a duplicant job; with Honest Instant Build, a tile over rock dug and built at once with exact costs, and over ground nobody can dig left as a normal order; achievements still enabled; and, with the mod switched off, dig orders staying normal jobs. Every release is security and privacy scanned: two antivirus engines and a check for personal details on the files that ship, and CodeQL on this source. The results and file hashes are in each [release's notes](https://github.com/netstrider-canada/oni-mods/releases).

## Building

See the [top README](../README.md#building): `dotnet build -c Release` in `src`, reading the game's DLLs from `../honest-instant-build/lib/`.

## How to get support

Please raise an issue on [GitHub](https://github.com/netstrider-canada/oni-mods/issues) if you have any problems and I'll take a look.

## Other mods of mine

- [Honest Instant Build](../honest-instant-build/README.md): build orders complete instantly, without sandbox mode, but with some honest restrictions. Speeds up gameplay at the cost of some game challenge.
- [Honest Instant Deconstruction](../honest-instant-deconstruction/README.md): deconstruct orders finish instantly, without sandbox mode, but with some honest restrictions. Speeds up gameplay at the cost of some game challenge.
- [Self-sealing Airlocks (Continued)](../self-sealing-airlocks/README.md): doors on Auto or Locked stay airtight, even while a duplicant walks through.

## Credits and licensing

Suggested by ne_propheta on the [Honest Instant Deconstruction](https://steamcommunity.com/sharedfiles/filedetails/?id=3839803714) Workshop page. Thank you! MIT: see [LICENSE](LICENSE).
