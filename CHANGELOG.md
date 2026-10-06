# Changelog

## 2.0.2 - 2026-10-06

- Name saved egg pickup flags and player positions explicitly.
- Test mixed original flags, exception restoration across multiple eggs, and plugin teardown.
- Keep the simulated game's pickup range check independent of the mod's radius helper.
- Maintainer confirmed the current build works as intended in game.

## 2.0.1

- Replace the chick icon with Valheim's chicken egg artwork.
- Move build, package, publish, and deployment scripts into `ci` and normalize `tests/checks`.
- Add repository sponsorship links and automatic dependency update checks.

## 2.0.0

- Formally target Valheim 1.0; compiled and checked against 1.0.16.
- Require BepInExPack Valheim 5.4.2350.
- Add GitHub Actions builds and automatic publication of new versions from main.
- Add Hexium publishing scaffolding, disabled until DocZee is approved.
- Keep plugin IDs and configuration files stable for existing installations.

## 1.0.0

- Initial release with configurable adult hen threshold and detection radius.
- Local settings and scoped egg eligibility during automatic pickup.
