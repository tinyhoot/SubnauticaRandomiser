# Subnautica Randomiser
A Subnautica Mod that randomises many aspects of the game for replayability, originally based on [the first Randomizer](https://github.com/stephenengland/SubnauticaRandomizer).

This can make the game considerably more difficult, and the varying availability of ingredients may also make it harder to collect enough of what you need. Since ingredients can vary so wildly, use of a blueprint tracker mod like [this one on nexus](https://www.nexusmods.com/subnautica/mods/22) is recommended.

The randomisation persists between play sessions and save games. If you decide to stop playing for the day, everything will remain randomised as it was. 

The mod will randomise using recommended settings on first startup. You can either start playing immediately, or customise your experience in the mod options menu. Note that, should you choose to re-randomise from the mod options menu, you must **restart your game** for all changes to properly take effect.

#### This mod randomises:
* Recipes for most craftable things in the game, excluding decorative base pieces.
* Blueprints found in databoxes
* Fragment spawn rates and locations
* Lifepod spawn location

## Features
- ✔️ No softlocks
- ✔️ Detailed mod options menu
- ✔️ (Probably) Don't spawn in the void
- ✔️ Randomise rewards from scanning a fragment you already know
- ✔️ Include fish, eggs and seeds in recipes
- ✔️ Out-of-order upgrades, so you might acquire e.g. a Seamoth Depth Module 3 long before you ever manage to get Module 1 or 2
- ✔️ Long recipe chains. Do you really need that laser cutter, or do you craft it into Polyaniline for a Rebreather?
- ✔️ Items are balanced on an underlying value logic
   - If you prefer pure chaos, simply turn on Chaotic mode!
- ✔️ Share your seed with friends

## How to Use
1. Install [QModManager](https://www.nexusmods.com/subnautica/mods/201)
2. Install [SMLHelper](https://www.nexusmods.com/subnautica/mods/113)
   1. (Optional) Install [BlueprintTracker](https://www.nexusmods.com/subnautica/mods/22)
3. Extract this mod into your Subnautica/QMods folder
   1. (Optional) Edit the config in the in-game options menu to your liking
   2. Press the "Randomise with new seed" button
   3. Restart the game to apply your changes
4. Enjoy!

## How to Build
* git clone
* Add a SUBNAUTICA_DIR variable to your PATH pointing to your install directory of Subnautica
* Install QModManager and SMLHelper
* In Visual Studio, update the project's assembly references to point to the correct locations on your computer.
  * For more information, see [QMod Wiki](https://github.com/SubnauticaModding/QModManager/wiki/Libraries)
  * In addition, you'll need a publicised version of Subnautica's `Assembly-CSharp.dll`. Start the game once using [the BepinEx plugin](https://github.com/MrPurple6411/Bepinex-Tools/releases/) for this.
* Building in the Release configuration should leave you with a `SubnauticaRandomiser.dll` in `SubnauticaRandomiser/bin/Release/`
