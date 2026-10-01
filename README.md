# Hen Egg Pickup

**2.0.0 targets Valheim 1.0**, built and checked against 1.0.16. Use the 1.x releases for Ashlands.

A client-side Valheim mod that enables chicken egg automatic pickup once enough living adult hens are near the player. Defaults: 12 hens within 10 metres. All settings are local; there is no ServerSync or server-side requirement.

See [the mod page README](README.thunderstore.md) for settings and behavior.

## Build and package

```powershell
.\Build.ps1
```

An alternate game location can be supplied with `-GamePath`. The build uses your installed game's BepInEx and managed assemblies. It produces a validated Thunderstore ZIP under `artifacts/`, using `README.thunderstore.md` as the package README. AutoPicker and Animal Feed Guard are optional at build time and runtime.

The checks exercise the actual prefix/finalizer through Harmony against simulated game objects, including exception cleanup, other-player isolation, threshold boundaries, and compatibility with a simulated pickup filter. They also inspect the installed game's pickup method and the release DLL. Real gameplay testing remains necessary.

## Deploy locally

```powershell
# Preview the target without changing the profile.
.\Deploy.ps1 -WhatIf

# Build, check, package, and install into the r2modman Default profile.
.\Deploy.ps1 -Build
```

Exit Valheim before deployment. Use `-ProfilePath` to select another profile and `-GamePath`
to select another game installation. Deploying replaces the mod DLL, checks its SHA-256 hash,
and keeps any previous DLL under `.local/deploy-backups/`. Configuration files stay in place.

## Publish to Thunderstore

```powershell
.\Build.ps1
.\Publish.ps1 -WhatIf
.\Publish.ps1
```

`Publish.ps1` uploads the packaged ZIP to the **DocZee** team in the **Valheim** community.
It reads the package name and version from the ZIP, so it also works for future releases.
Use `-PackageFile` to select a specific ZIP. `-WhatIf` validates the package and previews the upload.
The script restores the pinned local Thunderstore CLI and reads `THUNDERSTORE_API_TOKEN`
from the process environment or the Windows user environment. Tokens do not belong in the repository.
See the [Thunderstore CLI documentation](https://github.com/thunderstore-io/thunderstore-cli/wiki).

The expected package page after the first upload is
[DocZee/HenEggPickup](https://thunderstore.io/c/valheim/p/DocZee/HenEggPickup/).
Increment the version in `HenEggPickup.csproj`, `src/Plugin.cs`, and `manifest.json` before each update;
Thunderstore versions cannot be uploaded twice.

## Install

Import the ZIP as a local mod in r2modman, or copy the DLL under your active profile's `BepInEx/plugins/HenEggPickup/`. Launch once to generate the configuration file. The game must have automatic pickup enabled.

## Implementation

Only `Player.AutoPickup` is patched. During the local player's pickup call, nearby `ChickenEgg` instances receive a temporary eligibility flag based on the count of living `Hen` prefabs. A Harmony finalizer restores the original flags even if an exception occurs. Egg hatching data, network state, and other items are not modified. Normal pickup checks and existing filters such as Animal Feed Guard still run.

The threshold applies to the player, not to each egg or pen. Loaded hens across nearby pens count together. Manually dropped eggs are eligible at the threshold. Below the threshold, chicken egg automatic pickup is blocked regardless of its original flag.

## Other mods

- [Animal Feed Guard](https://thunderstore.io/c/valheim/p/DocZee/AnimalFeedGuard/)
- [Ranching - Chick Addon](https://thunderstore.io/c/valheim/p/DocZee/Ranching_Chick_Addon/)

If you'd like to support ongoing modding work, [Ko-fi](https://ko-fi.com/doczee) is available.

Original code is MIT licensed. The reused Valheim chick icon is separately attributed in `ATTRIBUTION.md`.

## Automated builds and releases

See [ci/README.md](ci/README.md) for GitHub Actions builds, versioned releases, and automatic publishing to Thunderstore and Hexium. Builds run on each commit to `main`; Hexium publishing is disabled pending team approval.
