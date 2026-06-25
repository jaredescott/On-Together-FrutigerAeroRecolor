# Frutiger Aero Recolor

Client-side BepInEx environment recolor for **On-Together** — Frutiger Aero palette (lush greens, cream-white buildings, aqua accents).

**Source & issues:** https://github.com/jaredescott/On-Together-FrutigerAeroRecolor

![In-game screenshot with Frutiger Aero Recolor applied](screenshot.png)

> Store README template for Thunderstore — **not published yet**. Install from GitHub until the listing is live.

## Requirements

- [BepInEx Pack](https://thunderstore.io/c/on-together/p/BepInEx/BepInExPack/)

## Install

> **Not on Thunderstore yet.** Install from [GitHub](https://github.com/jaredescott/On-Together-FrutigerAeroRecolor) for now.

1. Build or obtain `FrutigerAeroRecolor.dll` (see the GitHub repo).
2. Copy it anywhere under your game's `BepInEx/plugins/` folder. For example:

   `BepInEx/plugins/FrutigerAeroRecolor/FrutigerAeroRecolor.dll`

3. Launch On-Together from your BepInEx / mod profile.

**When this listing is live:** install with Thunderstore Mod Manager — no manual folder setup needed.

Config (created on first launch): `BepInEx/config/io.j4eger.ontogether.frutigeraerorecolor.cfg`

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
