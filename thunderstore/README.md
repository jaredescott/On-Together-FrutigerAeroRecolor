# Frutiger Aero Recolor

Client-side BepInEx environment recolor for **On-Together** — Frutiger Aero palette (lush greens, cream-white buildings, aqua accents).

**Source & issues:** https://github.com/jaredescott/On-Together-FrutigerAeroRecolor

## Requirements

- [BepInEx Pack](https://thunderstore.io/c/on-together/p/BepInEx/BepInExPack/)

## Install

Install with Thunderstore Mod Manager, then launch On-Together from your mod profile.

**Manual:** Copy `J4EGER/FrutigerAeroRecolor.dll` into `BepInEx/plugins/J4EGER-FrutigerAeroRecolor/J4EGER/`.

Config: `BepInEx/config/io.j4eger.ontogether.frutigeraerorecolor.cfg`

Log should show: `FrutigerAeroRecolor 1.5.10 loaded`

## In-game settings

Press **Keypad 5** — try **Eye Comfort Preset** first.

## Multiplayer

Client-side only. Safe on public servers; only you see the recolor unless others install it too.

## Troubleshooting

| Issue | Try |
|-------|-----|
| No visible change | Full game restart. Check log for `1.5.10`. |
| Duplicate plugins | Remove extra `FrutigerAeroRecolor` folders under `BepInEx/plugins/`. |
| Buildings still green | Set `BuildingStrength = 1` in config, restart. |
