*******************************************************************************
******                SUBNAUTICA RANDOMISER DOCUMENTATION                ******
*******************************************************************************


-------------------------------------------------
 Table of Contents
-------------------------------------------------
 [1.1] How to install
 [1.2] How to update
 ------------------------------------------------
 [2.1] Using the in-game mod menu
 [2.2] Advanced settings
 ------------------------------------------------
 [3] How to add your own items to the randomiser
 [3.1] TechType
 [3.2] Category
 [3.3] Depth
 [3.4] Prerequisites
 [3.5] Value
 [3.6] MaxUsesPerGame
 [3.7] BlueprintUnlockCondition
 [3.8] BlueprintUnlockDepth
-------------------------------------------------



 [1.1] How to install
--------------------
- Extract the SubnauticaRandomiser.zip file.
- Look at the resulting SubnauticaRandomiser folder. If you can see a bunch of
  files contained within (like mod.json, SubnauticaRandomiser.dll), good. If you're
  instead looking at a second folder within the first one, go grab that second one instead.
- Take the folder with the files inside, and move the entire folder to your
  Subnautica/QMods directory.
- Done!


 [1.2] How to update
---------------------
- Follow the same instructions as above.
- When asked if you want to overwrite files, let it overwrite everything.



 [2.1] Using the in-game mod menu
-----------------------------------
The mod menu is accessible at any point under "Options --> Mods", even while
in-game. Changes you make will not take effect until the next time you click
one of the "Randomise with ___ seed" buttons.

| Randomiser Mode | Balanced / Chaotic |
--Default: Balanced
This setting affects how the randomiser chooses ingredients for recipes. Balanced
will try to provide a challenge, but avoid unnecessary grinding and tedium. This
is the recommended way to play the randomiser. Should you wish for a completely
off-the-rails experience, Chaotic has you covered. Chaotic will not softlock
you, but it provides very little protection from the ups and downs of random chance.

| Spawnpoint | Vanilla / Random / <Biome> / Void |
--Default: Vanilla
Change where the lifepod spawns. Random simply chooses one among all available options.
You can constrain the general area of where the lifepod will end up by choosing one
of the given surface biomes. It is also possible to spawn in the little lake of the
floating island by choosing Floating Island.

| Use fish in logic | Yes / No |
--Default: Yes
Enabling this setting will include all fish you can grab in the wild in the logic,
so that they may show up as ingredients. Note that this does NOT affect fish bred
from eggs, like Stalkers or Crashfish.

| Use eggs in logic | Yes / No |
--Default: No
Similarly to the setting above, this will include all eggs in the logic, and also
the fish that emerge from them. If you enable this setting, building an alien
containment will be a necessity.

| Use seeds in logic | Yes / No |
--Default: Yes
Enabling this setting will include all maritime seeds in the logic. This includes
things like Cave Bush or Sea Crown seeds, as well as mushroom tree pieces. Note
that land-based plants are unaffected by this setting.

| Randomise blueprints in databoxes | Yes / No |
--Default: Yes
When enabled, this setting will shuffle the blueprints found in databoxes. It does
not add any recipes to the boxes that are not already contained in them in vanilla,
and the boxes themselves will still be found in the same locations. However, the
blueprints you get from them will no longer be in the same boxes as they used to be.

| Randomise fragment locations | Yes / No |
--Default: Yes
When enabled, all fragments will be distributed throughout the game. Logic is in place
to prevent any softlocks from not being able to access a specific fragment. However,
note that this will not (yet) put fragments into biomes where the vanilla game didn't
spawn any.

| Randomise number of fragments needed | Yes / No |
--Default: Yes
When enabled, all fragments will require a random number of scans to unlock. Ties into
the setting "Max number of fragments needed" below.

| Randomise duplicate scan rewards | Yes / No |
--Default: Yes
Scanning a fragment you've already unlocked will yield a random raw material instead
of the normal two titanium.

| Respect vanilla upgrade chains | Yes / No |
--Default: No
By default, the randomiser breaks the sequential upgrade chains present in the vanilla
game. It is thus possible to skip some items along the way and, e.g., grab a Seamoth
Depth Upgrade Mk.2 before Mk.1 was ever craftable.
When enabled, this setting instead forces the randomiser to respect that progression.

| Theme base parts around a common ingredient | Yes / No |
--Default: No
When enabled, this setting picks an ingredient to base habitats on. Basic pieces
like corridors or rooms will then always contain this ingredient, much like in vanilla
they always include titanium. The chosen ingredient is ensured to be common and
easily accessible.

| Include equipment as ingredients | Never / Top-level recipes only / Unrestricted |
| Include tools as ingredients     | Never / Top-level recipes only / Unrestricted |
| Include upgrades as ingredients  | Never / Top-level recipes only / Unrestricted |
--Default: Top-level recipes only
These three settings all do the same thing: allow some level of control over nesting
and repetition. With these set to 'Never', equipment, tools, and upgrades will never
be considered as valid ingredients, so you will never have to craft another scanner
because your Seamoth ate your current one.
If set to 'Top-level recipes only', these items may show up as ingredients for
things which cannot themselves feature in another recipe. This includes base pieces,
vehicles, and the rocket.
If set to 'Unrestricted', these items will not be treated any differently to any
other item in the game and may feature prominently in recipes. Note that on Unrestricted,
it is not uncommon to need to build a repair tool to build a laser cutter to build
a propulsion cannon to build a cyclops.

| Max number of a single ingredient | 1 - 10 |
This number determines the maximum which may be required of any ingredient within
the same recipe. Thus, setting this to 5 will never allow e.g. a knife to be made
from 6 titanium and 3 copper. The titanium would be capped at 5, and other ingredients
would be used in the 6th piece's stead.
Reducing this number leads to "flatter" recipes which require more diverse ingredients,
but only a few of them each. This setting is very strongly tied to the one below.

| Max ingredients per recipe | 1 - 10 |
This number determines how many different ingredient types a recipe can hold. Assume
you set this setting to 3, and you're trying to craft a knife. The recipe you get is
2 copper, 4 gold and 1 stalker tooth. The setting then prevents the randomiser from
adding e.g. 2 titanium to the list of necessary materials, which would bring the
total of diverse types up to 4. However, it does NOT influence how many of each
of these ingredients can be required at once.
This setting is very strongly tied to the one above.

| Max biomes to spawn each fragment in | 3 - 10 |
This number regulates within how many biomes any given fragment can show up in. Note
that these biomes do NOT refer to biomes in player terms, but rather in game terms,
which treats e.g. walls, ceilings, caves, wreckages, rooms behind locked doors inside
wreckages, etc. each as a different biome. Setting this too low will make it difficult
to find enough fragments.

| Randomise with new seed |
| Randomise with same seed |
Clicking one of these buttons will apply your changes and randomise anew. As the
buttons say, clicking the first one gives you a new seed, while the second one
will let you keep it. This is useful if you received a seed from someone else,
in which case you would enter the seed in your config.json in the mod directory
and then 'Randomise with same seed'.


 [2.2] Advanced settings
-------------------------
The randomiser allows you to change a few advanced settings which are not listed
in the mod menu. To find them, navigate to your Subnautica/QMods/SubnauticaRandomiser
directory and open the file config.json.

These settings are meant for advanced users, and changing them may greatly impact
your experience. Extreme values may even softlock you, or cause the randomiser to
get stuck trying to figure out a valid path. Proceed with caution.

| iDepthSearchTime | 0 - 45 |
--Default: 15
When calculating how deep you can go with your currently accessible equipment,
the randomiser will stop at a depth where you can remain for this many seconds
before having to return for air without dying. This number is capped at 45 seconds
since that is your maximum at the beginning of the game, without any tanks.

| iMaxBasicOutpostSize | 4 - 48 |
--Default: 24
The absolute essentials to establish a small scanning outpost all taken together
will not require ingredients which exceed this much space in your inventory. This
affects I-corridors, hatches, scanner rooms, windows, solar panels, and beacons.

| iMaxDuplicateScanYield | 1 - 10 |
--Default: 2
The maximum number of items you will be given upon scanning a fragment that is
already known. Setting this number too high will quickly clutter your inventory.

| iMaxEggsAsSingleIngredient | 1 - 10 |
--Default: 1
This setting does the same as the one above, but specialised for eggs. Because
eggs are relatively rare and difficult to obtain even with an alien containment,
this setting by default caps them at 1. Note that both eggs and the fish that are
bred from them are affected.

| iMaxInventorySizePerRecipe | 4 - 100 |
--Default: 24
Some recipes, particularly mid-late game ones like the cyclops, are valued so highly
that the total amount of ingredients you'd need to craft them would exceed the
amount you can physically carry in your inventory. Without an inventory-expanding
mod, this results in a softlock. The vanilla inventory is 6x8, resulting in 48
units of inventory space. The default value thus blocks any recipe from requiring
more than half your inventory at once.
This setting is included mostly for users of inventory-expanding mods. Increase
it at your own risk.

| dFuzziness | 0.0 - 1.0 |
--Default: 0.2
Every recipe in the game is assigned a value before randomising. This setting controls
how closely the randomiser tries to stick to that value before it declares a
recipe done.
The setting represents a percentage. Assume that Titanium Ingots had a value of 100.
With dFuzziness set to 0.2, the randomiser will try to reach 100 with a tolerance
range of 20%, or half of that in each direction. In practice, this means it will
be satisfied once the new ingredients reach any value between 90 and 110. Higher
values of this setting thus lead to a much more random experience.

| dIngredientRatio | 0.0 - 1.0 |
--Default: 0.45
While trying to find ingredients to fit into a recipe, the randomiser will always
attempt to find a major, high-value item first. This setting controls roughly how
valuable that first ingredient should be. It represents a percentage of the total
value of the recipe with a tolerance range of 10% of the total in each direction.
With the default value, this means that the randomiser will first try to find an
ingredient with 35%-55% of the total value before moving on to entirely random ones.
Set to 0.0 to disable this behaviour.

| fFragmentSpawnChanceMin | 0.01f - 10.0f |
--Default: 0.3f
This setting, tethered with the one below, provides a global modifier for the 
randomiser to decide how likely a fragment spawn should be within a biome. The value
it ultimately decides on is multiplied with the vanilla average fragment spawn rate
within that biome. Small adjustments can have large effects, particularly if combined
with the maximum number of fragments allowed to spawn in a single biome.

| fFragmentSpawnChanceMax | 0.01f - 10.0f |
--Default: 0.6f
See above.

| sBase64Seed |
This extremely long string represents your savegame. All recipes, databoxes,
everything that this mod changes is saved here. You should never change this
manually, unless you're using it to share seeds with someone else. In that case,
copying someone else's base seed to your own file allows you to skip synchronising
your settings with them and pressing the randomise button. The game will simply
load their game state next time you boot it up.

| iSaveVersion |
You should never change this manually. It is used to detect if you accidentally
updated into a save-breaking version of the randomiser, in which case the mod will
stop until you explicitly tell it to overwrite your old seed by randomising again.



 [3] How to add your own items
-------------------------------
The randomiser is unable to detect and randomise items from other mods, as it simply
lacks enough information about them. However, you can add them yourself by editing
the recipeInformation.csv. You can find this file in your Subnautica/QMods/SubnauticaRandomiser
folder. Here's a small excerpt:

______________________________________________________________________________________________________________________________
| TechType | Category     | Depth | Prerequisites | Value | MaxUsesPerGame | BlueprintUnlockCondition | BlueprintUnlockDepth |
==============================================================================================================================
| Titanium | RawMaterials |     0 |               |    10 |                |                          |                      |
| Seamoth  | Vehicles     |   100 | Constructor   |   275 |                | SeamothFragment          |                   90 |
------------------------------------------------------------------------------------------------------------------------------

In general, any item you add MUST have a TechType, a Category, and a Value. If it
is an item which does not have a recipe (such as raw materials, fish, or seeds),
it should also provide a Depth. All other fields help the randomiser do its job,
but are not a hard requirement.
If the randomiser runs into any trouble parsing your custom items, it will log
the error(s) to qmodmanager_log-Subnautica.txt in your Subnautica folder.


 [3.1] TechType
----------------
This is the internal name of your item, NOT the one displayed in-game. For vanilla
items, the Subnautica wiki can tell you what name you're looking for under 'debugspawn'.
For modded items, ask the mod author what their modded item IDs are.


 [3.2] Category
----------------
Categories roughly follow the categories used by the PDA on the blueprint screen.
They tell the randomiser how to treat the item, and are thus an essential piece
of information. These are the categories the randomiser will accept:

- RawMaterials
  - These items do not have a recipe, but act as important baseline ingredients
    for all other items in the game, like titanium or coral pieces.
- BasicMaterials
  - These are simple crafted ingredients, like bleach or glass and usually available
    from the beginning of the game. They do have a recipe and may feature as an
    ingredient.
- AdvancedMaterials
  - Much more complex crafted ingredients with their own recipes, which must often
    be unlocked first.
- Electronics
  - Functionally not any different from Basic or Advanced materials.
- Tools
  - Things like laser cutters or scanners go in here.
  - Everything using this category is affected by the 'Use tools as ingredients?'
    setting.
  - These items also cannot be required as multiple ingredients. You will never
    find a recipe demanding three laser cutters, or two propulsion cannons.
- Tablets
  - Reserved for the three tablets in the base game. Functionally works like basic
    or advanced materials.
- Equipment
  - Things like radiation suits or compasses go in here.
  - Everything using this category is affected by the 'Use equipment as ingredients?'
    setting.
- Deployables
  - Things you put on your hotbar and then click to deploy go here, like the
    vehicle construction bay or beacons.
  - This category can never be required as an ingredient.
- Vehicles
  - Meant for the Seamoth, Prawn, and Cyclops. 
  - This category can never be required as an ingredient.
- Rocket
  - Meant for all the different rocket stages.
  - This category can never be required as an ingredient.
- ScannerRoom
  - Upgrades and items crafted at the fabricator in the scanner room go here.
- WorkbenchUpgrades
  - The workbench is the Modification Station. All upgrades crafted there, like
    the heatblade or Seamoth Depth Upgrade Mk. II, go here.
  - Everything in this category is affected by the 'Use upgrades as ingredients?'
    setting.
  - These items cannot be required as a multiple ingredient, only one at a time.
- VehicleUpgrades
  - Very similar to WorkbenchUpgrades, except these cover everything crafted at
    the Vehicle Upgrade Console in the Moonpool and the Cyclops fabricator.
  - Everything in this category is affected by the 'Use upgrades as ingredients?'
    setting.
  - These items cannot be required as a multiple ingredient, only one at a time.
- Torpedos
  - Torpedos. Functionally acts like basic or advanced materials.
- BaseGenerators
  - Generators for bases, like solar panels or thermal reactors.
  - This category can never be required as an ingredient.
- BaseBasePieces
  - Very simple base parts like corridors or rooms.
  - This category can never be required as an ingredient.
- BaseExternalModules
  - Base parts which are usually constructed outside, like growbeds.
  - This category can never be required as an ingredient.
- BaseInternalModules
  - Most things found on the 'Internal' tab of the building tool belong here.
  - This category can never be required as an ingredient.
- BaseInternalPieces
  - Base parts which snap into place, like the ladder or water filter, go here.
  - This category can never be required as an ingredient.
- Fish
  - Essentially act the same as RawMaterials. Can never have a recipe, require Depth.
  - Everything in this category is affected by the 'Use Fish in Recipes?' setting.
  - Note that only wildly occurring, lootable fish are part of this category. Fish
    acquired from eggs in the alien containment belong to the Eggs category.
- Seeds
  - Essentially act the same as RawMaterials. Can never have a recipe, require Depth.
  - Everything in this category is affected by the 'Use Seeds in Recipes?' setting.
  - Additionally, this category requires a knife to be unlocked in logic.
- Eggs
  - Essentially act the same as RawMaterials. Can never have a recipe, require Depth.
  - Everything in this category is affected by the 'Use Eggs in Recipes?' setting.
  - Note that fish bred from eggs are also listed in this category.
  - Additionally, this category requires an alien containment and a multipurpose
    room to be unlocked in logic.
- Fragments
  - Provides a base TechType for the randomiser to latch on to while looking through
    prefabs in the spawn registry.
  - Removing an entry here will cause it to no longer be randomised in a new location.
  - Adding an entry will likely do nothing, as the randomiser will probably fail to
    find any associated custom prefabs.
    
    
 [3.3] Depth
-------------
This column lists the depth at which the item reliably becomes available. Essential
for raw materials, fish and all other categories acting like them. The randomiser
will start requiring these materials as soon as it considers the listed Depth
reachable.


 [3.4] Prerequisites
---------------------
Some items in the game cannot be built or acquired until you have gathered, built
or crafted something else. For example, it is impossible to get a Seamoth without
a mobile vehicle bay, or seeds without a knife.
The item on this row will not be randomised until all its prerequisites are met,
ensuring continuity and no softlocks.


 [3.5] Value
-------------
This is an essential field without which the randomiser will complain and refuse
to add your item to the game. This column represents a rough approximation of
the item's rarity or obtainability. For example, titanium is an easily obtainable,
low-value item, whereas stalker teeth, being fairly hard and/or annoying to get,
are worth quite a bit more.
For crafted items (i.e., most categories aside from RawMaterials, Fish, etc.), Value
is the target the randomiser will try to reach with ingredients. For vanilla items,
I have calculated it as the value of all ingredients plus 5.


 [3.6] MaxUsesPerGame
----------------------
Some items, for whatever reason, love to show up over and over. A frequent early
game offender is stalker teeth. This column prevents an item from appearing in
more than this many recipes across the entire game.
Note that this does not affect nested recipes! If a stalker tooth reaches its
last recipe with, say, plasteel, this does not prevent plasteel from then being
used in other recipes.


 [3.7] BlueprintUnlockConditions
---------------------------------
Some items are uncraftable until you unlock their blueprint. This primarily applies
to items which require you to scan fragments, like most tools and vehicles, but
also to advanced materials like Polyaniline, which unlocks when you grab your
first Deepshroom.
This column is quite similar to Prerequisites. What's the difference? Let's take
the Seamoth as an example. You do not get access to the Seamoth's recipe until
after you have scanned enough fragments (blueprint, therefore this column), but
to actually make a Seamoth you first need a mobile vehicle bay (actual crafting,
therefore prerequisites).
Just like prerequisites, the TechType on this row will not get randomised until
all its blueprint unlocks are fulfilled.


 [3.8] BlueprintUnlockDepth
----------------------------
Some very few items in the game unlock only once you reach a specific depth, most
notably Hatching Enzymes. This column is meant for those.
In addition, this column gives the randomiser a rough idea of how deep you need
to go to be able to grab fragments or databoxes of something. Any item which
included fragments or databoxes in the previous column should provide an unlock
depth here. The randomiser will leave this item alone until its unlock depth is
considered reachable.
