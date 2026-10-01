# Hen Egg Pickup

**2.x targets Valheim 1.0**, built and checked against 1.0.16. Use the 1.x releases for Ashlands.

Automatically collect chicken eggs when your flock has enough adult hens nearby.

## Features

- Requires **12 living adult hens within 10 metres of you** by default.
- Configurable hen count and detection radius; chicks do not count.
- Leaves chicken eggs alone below the threshold, even if another mod enables their pickup flag.
- Uses the game's automatic pickup range, toggle, inventory capacity, and weight checks.
- Manual pickup remains available at any flock size.
- Client-side only, with local settings and no server synchronization.

## Configuration

Launch once to generate `BepInEx/config/com.ziluck.valheim.heneggpickup.cfg`.

| Setting | Default | Meaning |
| --- | --- | --- |
| Enabled | true | Enable conditional egg pickup. Disabling restores normal game/mod behavior. |
| Minimum Hens | 12 | Adult hens required; configurable from 1 to 1000. |
| Hen Detection Radius | 10 | Metres from the player to hens; configurable from 1 to 100. |

The count includes loaded, living `Hen` creatures, whether fed or hungry. Distance includes height; walls do not block detection. Only chicken eggs are affected. Eggs still need to be within your normal item pickup range. At the threshold, manually dropped eggs can also be collected automatically.

Install on each client that wants this behavior. Installing only on a dedicated server has no effect on clients. Each player controls their own settings.

## Optional mods

- [ConfigurationManager](https://thunderstore.io/c/valheim/p/shudnal/ConfigurationManager/) lets you edit settings in game.
- [Animal Feed Guard](https://thunderstore.io/c/valheim/p/DocZee/AnimalFeedGuard/) protects food near tamed animals. Its pickup filter remains in effect.
- [AutoPicker](https://thunderstore.io/c/valheim/p/Same/AutoPicker/) is optional; its installed harvesting loop is separate from ground-item pickup.

## Check out my other mods

- [Animal Feed Guard](https://thunderstore.io/c/valheim/p/DocZee/AnimalFeedGuard/)
- [RanchingChickAddon](https://thunderstore.io/c/valheim/p/DocZee/RanchingChickAddon/)

If you'd like to support ongoing modding work, [Ko-fi](https://ko-fi.com/doczee) is available.

Requires [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/).

## Links

- [Source, documentation, and issue tracker](https://github.com/Michael-Ziluck/HenEggPickup)
