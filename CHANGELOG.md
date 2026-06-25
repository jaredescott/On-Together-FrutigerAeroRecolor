# Changelog

## 1.5.10
- Plugin GUID/config namespace is now `io.j4eger` (J4EGER)
- Release builds no longer embed local Windows PDB paths in the DLL

## 1.5.9
- Renamed plugin GUID and config namespace from `io.jesco` to `io.j4eger`

## 1.5.8
- Force circle buildings mostly white/cream by neutralizing `mountain texture` on `_MainTex` and `_1st_ShadeMap`
- Override all building shader colors and texture blends for structure bands
- Keep tiered hills lush green (isolated from building material clones)

## 1.5.7
- Separate building vs hill contexts for shared `M_Mountain` material
- Fix blue hills regression from building-glass clone propagation
- CircleMountain instancing render patch

## 1.5.0 – 1.5.6
- Eye Comfort palette, bush/tree batch fixes, cliff cream stone, path white, hill detection iterations

## 1.0.0 – 1.4.x
- Initial GPU instancing recolor support, building glass/structure palette, toon shading preservation
