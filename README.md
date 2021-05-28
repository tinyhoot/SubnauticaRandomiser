# Subnautica Randomiser
A Subnautica Mod that randomises recipes for replayability, based on [the work of stephenengland](https://github.com/stephenengland/SubnauticaRandomizer).

This can make the game considerably more difficult, and the varying availability of ingredients may also make it harder to collect enough of what you need. Since ingredients can vary so wildly, use of a blueprint tracker mod like [this one on nexus](https://www.nexusmods.com/subnautica/mods/22) is recommended.

The randomisation persists between play sessions and save games. If you decide to stop playing for the day, everything will remain randomised as it was. In case you need it, there is a mod menu option for randomising everything from scratch again.

#### This mod randomises:
* All recipes for basic and advanced materials
* All recipes for tools, equipment, vehicles and upgrades
* Most ingredients required for base building
  * Decorative pieces like chairs or beds are unaffected

## Features
- ✔️ Randomise most items in the game
- ✔️ Include fish and seeds in recipes
- ✔️ Upgrades are independent from their basic variants, so you might acquire e.g. a Seamoth Depth Module 3 long before you ever manage to get Module 1 or 2
- ✔️ Most things you can make may also show up as an ingredient in other recipes. Do you really need that laser cutter, or do you craft it into Polyaniline?
- ⬜ Include fish eggs and any fish bred from aquariums
- ⬜ Balance items better based on some value system

## How to Use
1. Install [QModManager](https://www.nexusmods.com/subnautica/mods/201)
2. Install [SMLHelper](https://www.nexusmods.com/subnautica/mods/113)
3. Extract this mod into your Subnautica/QMods folder
4. (Optional) Edit the config in the in-game options menu to your liking
   * Press the "Randomise Again" button twice
5. Enjoy!

## How to Build
* Install QModManager and SMLHelper
* git clone
* In Visual Studio, update the project's assembly references to point to the correct locations on your computer.
  * For more information, see [QMod Wiki](https://github.com/SubnauticaModding/QModManager/wiki/Libraries)
* Building in the Release configuration should leave you with a `SubnauticaRandomiser.dll` in `SubnauticaRandomiser/bin/Release/`
