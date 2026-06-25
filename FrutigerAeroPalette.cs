using UnityEngine;

namespace FrutigerAeroRecolor;

internal static class FrutigerAeroPalette
{
    public const string TieredHillContext = "hill-foliage";

    public static readonly Color SkyBlue = new(0.55f, 0.86f, 0.98f);
    public static readonly Color AquaGlass = new(0.30f, 0.86f, 0.98f);
    public static readonly Color LushGreen = new(0.42f, 0.82f, 0.44f);
    public static readonly Color SoftWhite = new(0.96f, 0.99f, 1f);
    public static readonly Color CreamWhite = new(0.96f, 0.97f, 0.95f);
    public static readonly Color ChromeWhite = new(0.92f, 0.96f, 0.98f);
    public static readonly Color PathWhite = new(0.94f, 0.97f, 0.99f);
    public static readonly Color DeepAqua = new(0.28f, 0.68f, 0.82f);
    public static readonly Color GrassGreen = new(0.38f, 0.78f, 0.42f);
    public static readonly Color WarmWood = new(0.72f, 0.58f, 0.44f);
    public static readonly Color StoneCream = new(0.88f, 0.90f, 0.86f);
    public static readonly Color StoneAqua = new(0.72f, 0.84f, 0.88f);
    public static readonly Color WarmEarthBrown = new(0.62f, 0.52f, 0.42f);
    public static readonly Color TrunkGreyTeal = new(0.48f, 0.58f, 0.54f);
    public static readonly Color BranchGreen = new(0.28f, 0.62f, 0.32f);

    public static Color Remap(Color input, float strength = 1f, float vibrancy = 1f)
    {
        Color.RGBToHSV(input, out var h, out var s, out var v);
        var target = BoostVibrancy(Classify(h, s, v), vibrancy);
        return Color.Lerp(input, target, Mathf.Clamp01(strength));
    }

    public static Color RemapBatchColor(Color input, string componentType, float strength, RecolorSettings settings)
    {
        strength = Mathf.Clamp01(strength);

        return componentType switch
        {
            "TreeBasesInstancing" or "PalmCrownInstancing" or "BushInstancing" => RemapFoliageBatchColor(
                input,
                LushGreen,
                Mathf.Max(strength, settings.FoliageStrength),
                Mathf.Min(settings.VibrancyMultiplier, 1.15f),
                0.72f),
            "GrassbaseInstancing" => RemapFoliageBatchColor(
                input, GrassGreen, strength, Mathf.Min(settings.VibrancyMultiplier, 1.15f), 0.72f),
            "TrunkInstancing" => RemapFoliageBatchColor(input, TrunkGreyTeal, strength, 1.05f, 0.45f),
            "BranchInstancing" => RemapFoliageBatchColor(input, BranchGreen, strength, Mathf.Min(settings.VibrancyMultiplier, 1.15f), 0.68f),
            _ => Remap(input, strength, settings.VibrancyMultiplier),
        };
    }

    private static Color RemapFoliageBatchColor(Color input, Color target, float strength, float vibrancy, float maxSaturation)
    {
        Color.RGBToHSV(input, out _, out var inputSat, out var inputValue);
        var vibrantTarget = BoostVibrancy(target, vibrancy);
        Color.RGBToHSV(vibrantTarget, out var targetHue, out var targetSat, out _);

        var targetSatCap = Mathf.Min(targetSat, maxSaturation);
        var sat = Mathf.Lerp(inputSat, targetSatCap, strength);
        sat = Mathf.Max(sat, targetSatCap * strength * 0.45f);
        sat = Mathf.Min(sat, maxSaturation);
        var value = Mathf.Clamp01(inputValue);
        var shifted = Color.HSVToRGB(targetHue, sat, value);
        shifted.a = input.a;
        return shifted;
    }

    public static Color BoostVibrancy(Color color, float multiplier)
    {
        if (multiplier <= 1.001f)
            return color;

        Color.RGBToHSV(color, out var h, out var s, out var v);
        s = Mathf.Clamp01(s * multiplier);
        v = Mathf.Clamp01(v + (multiplier - 1f) * 0.12f);
        var result = Color.HSVToRGB(h, s, v);
        result.a = color.a;
        return result;
    }

    public static Color GetStoneFaceColor(RecolorSettings? settings) =>
        settings?.CliffFaceStyle switch
        {
            CliffFaceStyle.SoftAqua => StoneAqua,
            CliffFaceStyle.WarmBrown => WarmEarthBrown,
            _ => StoneCream,
        };

    public static bool LooksLikeAquaTerrainColor(Color color)
    {
        Color.RGBToHSV(color, out var h, out var s, out var v);
        return v > 0.28f && s > 0.18f && h >= 0.40f && h <= 0.62f;
    }

    public static bool IsTieredHillTerrain(string hierarchy, string objectName)
    {
        var blob = NormalizeTerrainBlob(hierarchy, objectName);
        if (blob.Contains("waterfall"))
            return false;

        return blob.Contains("circle_hill")
            || (blob.Contains("circlehill") && !blob.Contains("circlemountain"));
    }

    public static bool IsMainCircleBuilding(string hierarchy, string objectName)
    {
        if (IsTieredHillTerrain(hierarchy, objectName))
            return false;

        var blob = NormalizeTerrainBlob(hierarchy, objectName);
        return blob.Contains("circle_mountain")
            || blob.Contains("pr_circle_mountain")
            || blob.Contains("circlemountains");
    }

    public static bool IsTieredHillRenderer(Renderer renderer)
    {
        if (renderer == null)
            return false;

        var path = BuildHierarchyPath(renderer.transform);
        return IsTieredHillTerrain(path, renderer.gameObject.name);
    }

    public static string BuildRendererPath(Renderer renderer)
    {
        if (renderer == null)
            return string.Empty;

        return BuildHierarchyPath(renderer.transform);
    }

    private static string NormalizeTerrainBlob(string hierarchy, string objectName) =>
        (hierarchy + "/" + objectName).ToLowerInvariant().Replace(" ", string.Empty).Replace("_", string.Empty);

    public static bool IsHillTerrainContext(string hierarchy, string material, string field, string combined)
    {
        if (IsTieredHillTerrain(hierarchy, field) || IsTieredHillTerrain(hierarchy, string.Empty))
            return true;

        if (IsPathContext(hierarchy, material, field, combined) || IsPropContext(hierarchy, material, field, combined))
            return false;

        if (material.Contains("water") || field.Contains("water") || combined.Contains("/water/"))
            return false;

        var blob = (hierarchy + "/" + material + "/" + field + "/" + combined).ToLowerInvariant();

        var tokens = new[]
        {
            "meditationhill", "yogahill", "musichill", "listenmusic", "waterfallhill",
            "hillmovement", "collidertest",
        };

        foreach (var token in tokens)
        {
            if (blob.Contains(token))
                return true;
        }

        if (blob.Contains("hill") || blob.Contains("meditation") || blob.Contains("yoga"))
            return true;

        return blob.Contains("waterfall") && !material.Contains("water");
    }

    public static bool IsRendererOnHill(Renderer renderer)
    {
        if (renderer == null)
            return false;

        var t = renderer.transform;
        while (t != null)
        {
            var path = BuildHierarchyPath(t).ToLowerInvariant();
            var name = t.name.ToLowerInvariant();
            if (IsHillTerrainContext(path, string.Empty, name, path))
                return true;

            foreach (var comp in t.GetComponents<Component>())
            {
                if (comp != null && comp.GetType().Name == "ColliderTest")
                    return true;
            }

            t = t.parent;
        }

        return false;
    }

    private static string BuildHierarchyPath(Transform transform)
    {
        var parts = new System.Collections.Generic.List<string>();
        while (transform != null)
        {
            parts.Add(transform.name);
            transform = transform.parent;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    public static bool IsGrassCapableTerrain(string hierarchy, string material, string field, string combined)
    {
        if (IsPathContext(hierarchy, material, field, combined) || IsPropContext(hierarchy, material, field, combined))
            return false;

        if (IsHillTerrainContext(hierarchy, material, field, combined))
            return true;

        if (IsLikelyVerticalTerrain(hierarchy, material, field))
            return false;

        if (IsGrassContext(hierarchy, material, string.Empty, field) || IsTerrainGrassContext(hierarchy, material, field, combined))
            return true;

        var tokens = new[]
        {
            "grassbase", "grass", "lawn", "meadow", "yard", "park", "terrace", "plateau", "hill", "island",
            "plain", "field", "greens", "turf", "shore", "bank", "rim", "ground",
        };

        foreach (var token in tokens)
        {
            if (hierarchy.Contains(token) || material.Contains(token) || field.Contains(token) || combined.Contains(token))
                return true;
        }

        return material.Contains("grass") || material.Contains("lawn") || material.Contains("ground");
    }

    public static bool IsLikelyVerticalTerrain(string hierarchy, string material, string field)
    {
        if (IsHillTerrainContext(hierarchy, material, field, hierarchy + "/" + material + "/" + field))
            return false;

        if (IsStoneContext(hierarchy, material, field, hierarchy + "/" + material + "/" + field))
            return true;

        var tokens = new[]
        {
            "wall", "face", "side", "cliff", "embankment", "retaining", "vertical", "rockface", "bluff", "strata",
        };

        foreach (var token in tokens)
        {
            if (hierarchy.Contains(token) || material.Contains(token) || field.Contains(token))
                return true;
        }

        return false;
    }

    public static Color ResolveAquaMisrouteColor(string hierarchy, string material, string field, RecolorSettings? settings)
    {
        var combined = hierarchy + "/" + material + "/" + field;
        if (IsHillTerrainContext(hierarchy, material, field, combined))
            return LushGreen;

        if (IsGrassCapableTerrain(hierarchy, material, field, combined))
            return GrassGreen;

        return GetStoneFaceColor(settings);
    }

    public static Color GetPropColor(string hierarchyPath, string materialName)
    {
        var path = hierarchyPath.ToLowerInvariant();
        var material = materialName.ToLowerInvariant();
        if (material.Contains("sign") || material.Contains("banner") || material.Contains("bunting")
            || material.Contains("roof") || material.Contains("canvas") || path.Contains("lemonade"))
            return CreamWhite;
        return WarmWood;
    }

    public static MaterialApplyMode GetApplyMode(string componentType, string fieldName)
    {
        if (componentType == "CircleMountainInstancing")
            return MaterialApplyMode.BuildingStructureForce;

        if (IsDeckInstancer(componentType))
            return MaterialApplyMode.PathForce;

        if (componentType == "CloudInstancing")
            return MaterialApplyMode.LightPreserving;

        if (componentType == "CircleBushPotInstancing" && fieldName == "PotMat")
            return MaterialApplyMode.LightPreserving;

        if (IsFoliageInstancer(componentType, fieldName))
            return MaterialApplyMode.FoliageForce;

        return MaterialApplyMode.Standard;
    }

    public static string GetPaletteContext(string componentType, string fieldName, string hierarchyPath = "", string materialName = "")
    {
        if (componentType == "CircleMountainInstancing")
            return "building-structure";

        if (IsDeckInstancer(componentType))
            return "path";

        if (componentType == "GrassbaseInstancing")
            return "grass";

        if (componentType is "TreeBasesInstancing" or "PalmCrownInstancing" or "BushInstancing")
            return "foliage";

        if (componentType == "CircleBushPotInstancing")
            return fieldName == "PotMat" ? "prop" : "foliage";

        if (componentType == "TrunkInstancing")
            return "trunk";

        if (componentType == "BranchInstancing")
            return "branch";

        if (componentType == "CloudInstancing")
            return "light";

        var path = hierarchyPath.ToLowerInvariant();
        var material = materialName.ToLowerInvariant();
        var field = fieldName.ToLowerInvariant();
        var combined = path + "/" + field + "/" + material;

        if (IsGrassStepsMaterial(material))
            return "grass";

        if (IsPathContext(path, material, field, combined))
            return "path";

        if (IsPropContext(path, material, field, combined))
            return "prop";

        if (IsTieredHillTerrain(path, field) || IsTieredHillTerrain(path, string.Empty))
            return TieredHillContext;

        if (IsHillTerrainContext(path, material, field, combined))
            return TieredHillContext;

        if (IsStoneContext(path, material, field, combined))
            return "stone";

        if (IsBuildingContext(path, material, string.Empty))
            return material.Contains("top") || field.Contains("trim") || field.Contains("band")
                ? "building-structure"
                : "building-glass";

        if (IsGrassContext(path, material, string.Empty, field))
            return "grass";

        if (IsTerrainGrassContext(path, material, field, combined))
            return "grass";

        if (material.Contains("tree") || material.Contains("palm") || material.Contains("crown") || path.Contains("treebase"))
            return "foliage";

        if (material.Contains("trunk"))
            return "trunk";

        if (material.Contains("branch"))
            return "branch";

        if (material.Contains("cloud") || material.Contains("lamp") || material.Contains("light"))
            return "light";

        return "default";
    }

    public static Color ForInstancingMaterial(
        string componentType,
        string fieldName,
        string shaderName,
        string materialName,
        RecolorSettings settings)
    {
        if (componentType == "CircleMountainInstancing")
            return settings.BuildingStructureWhite;

        if (IsDeckInstancer(componentType))
            return settings.PathWhite;

        if (componentType == "GrassbaseInstancing" && fieldName == "ToonInstanceMat")
            return GrassGreen;

        if (componentType == "TreeBasesInstancing" && fieldName == "ToonInstanceMat")
            return LushGreen;

        if (componentType == "BushInstancing" && (fieldName == "ToonInstanceMat" || fieldName == "ToonOutlineInstanceMat"))
            return LushGreen;

        if (componentType == "CircleBushPotInstancing" && fieldName == "BushMat")
            return LushGreen;

        if (componentType == "CircleBushPotInstancing" && fieldName == "PotMat")
            return WarmWood;

        if (componentType == "PalmCrownInstancing" && (fieldName == "ToonInstanceMat" || fieldName == "ToonInstanceMat1"))
            return LushGreen;

        if (componentType == "TrunkInstancing" && fieldName == "ToonInstanceMat")
            return TrunkGreyTeal;

        if (componentType == "BranchInstancing" && fieldName == "BranchMaterial")
            return BranchGreen;

        if (componentType == "CloudInstancing" && fieldName == "ToonInstanceMat")
            return SoftWhite;

        return ForMaterialField(string.Empty, shaderName, materialName, settings);
    }

    public static Color ForMaterialField(
        string hierarchyPath,
        string shaderName,
        string materialName,
        RecolorSettings? settings = null)
    {
        var path = hierarchyPath.ToLowerInvariant();
        var shader = shaderName.ToLowerInvariant();
        var material = materialName.ToLowerInvariant();
        var combined = path + "/" + material;

        if (IsGrassStepsMaterial(material))
            return GrassGreen;

        if (IsHillTerrainContext(path, material, string.Empty, combined))
            return LushGreen;

        if (IsPathContext(path, material, string.Empty, combined))
            return settings?.PathWhite ?? PathWhite;

        if (IsPropContext(path, material, string.Empty, combined))
            return GetPropColor(path, materialName);

        if (IsStoneContext(path, material, string.Empty, combined))
            return GetStoneFaceColor(settings);

        if ((material.Contains("aqua") || material.Contains("cyan") || material.Contains("teal") || material.Contains("turquoise"))
            && !material.Contains("water"))
            return ResolveAquaMisrouteColor(path, material, string.Empty, settings);

        if (IsBuildingContext(path, material, shader))
        {
            if (material.Contains("top") || path.Contains("trim") || path.Contains("band"))
                return settings?.BuildingStructureWhite ?? CreamWhite;
            return settings?.BuildingGlassBlue ?? AquaGlass;
        }

        if (IsTieredHillTerrain(path, string.Empty))
            return LushGreen;

        if (material.Contains("m_mountain"))
            return IsMainCircleBuilding(path, string.Empty)
                ? settings?.BuildingStructureWhite ?? CreamWhite
                : LushGreen;

        if (material.Contains("mountainmat") || material.Contains("topmat"))
            return settings?.BuildingStructureWhite ?? CreamWhite;

        if (material.Contains("glass") || material.Contains("window") || shader.Contains("glass"))
            return AquaGlass;

        if (material.Contains("water"))
            return DeepAqua;

        if (material.Contains("cloud"))
            return SoftWhite;

        if (IsGrassContext(path, material, shader, string.Empty) || IsTerrainGrassContext(path, material, string.Empty, combined))
            return GrassGreen;

        if (material.Contains("trunk"))
            return TrunkGreyTeal;

        if (material.Contains("branch"))
            return BranchGreen;

        if (material.Contains("bush") || material.Contains("palm") || material.Contains("crown") || material.Contains("tree"))
            return LushGreen;

        if (material.Contains("deck") || material.Contains("fence") || material.Contains("concrete"))
            return settings?.PathWhite ?? PathWhite;

        if (material.Contains("lamp") || material.Contains("light"))
            return SoftWhite;

        if (LooksLikeDirtMaterial(materialName) && !IsPropContext(path, material, string.Empty, combined))
            return GrassGreen;

        return Remap(Color.white, 0.35f, settings?.VibrancyMultiplier ?? 1f);
    }

    public static bool IsPropContext(string hierarchy, string material, string field, string combined)
    {
        if (IsPathContext(hierarchy, material, field, combined))
            return false;

        var tokens = new[]
        {
            "picnic", "table", "bench", "lemonade", "stand", "sign", "bunting", "banner",
            "counter", "board", "furniture", "vendor", "gazebo", "cotton", "shell", "stool",
            "chair", "prop", "umbrella", "kiosk", "stall", "flag", "pennant",
        };

        foreach (var token in tokens)
        {
            if (hierarchy.Contains(token) || material.Contains(token) || field.Contains(token) || combined.Contains(token))
                return true;
        }

        return false;
    }

    public static bool IsStoneContext(string hierarchy, string material, string field, string combined)
    {
        if (IsHillTerrainContext(hierarchy, material, field, combined))
            return false;

        if (IsPathContext(hierarchy, material, field, combined) || IsPropContext(hierarchy, material, field, combined))
            return false;

        var tokens = new[]
        {
            "cliff", "rock", "retaining", "ledge", "wallface", "tierface", "waterfallwall",
            "cliffside", "rockface", "bluff", "crag", "embankment", "escarpment", "geology",
            "sidewall", "side_wall", "tierwall", "terrasewall", "retainingwall", "strata",
        };

        foreach (var token in tokens)
        {
            if (hierarchy.Contains(token) || material.Contains(token) || field.Contains(token) || combined.Contains(token))
                return true;
        }

        if ((hierarchy.Contains("tier") || field.Contains("tier") || material.Contains("tier"))
            && (hierarchy.Contains("wall") || hierarchy.Contains("side") || hierarchy.Contains("face")
                || field.Contains("wall") || field.Contains("side") || field.Contains("face")
                || material.Contains("wall") || material.Contains("side") || material.Contains("face")))
            return true;

        if (hierarchy.Contains("wall") && !hierarchy.Contains("waterfall") && !material.Contains("glass"))
            return true;

        if (hierarchy.Contains("waterfall") && (material.Contains("rock") || material.Contains("cliff") || material.Contains("wall")))
            return true;

        return material.Contains("stone") || material.Contains("rock") || material.Contains("cliff");
    }

    public static bool IsGrassContext(string hierarchy, string material, string shader, string field)
    {
        return material.Contains("grass")
            || field.Contains("grass")
            || hierarchy.Contains("grassbase")
            || hierarchy.Contains("lawn");
    }

    public static bool IsTerrainGrassContext(string hierarchy, string material, string field, string combined)
    {
        if (IsPathContext(hierarchy, material, field, combined)
            || IsPropContext(hierarchy, material, field, combined)
            || IsStoneContext(hierarchy, material, field, combined))
            return false;

        var tokens = new[]
        {
            "hill", "terrace", "island", "yoga", "meditation", "plateau", "mound", "slope",
            "elevated", "upper", "raised", "earth", "dirt", "soil",
        };

        foreach (var token in tokens)
        {
            if (hierarchy.Contains(token) || field.Contains(token))
                return true;
        }

        return (material.Contains("earth") || material.Contains("dirt") || material.Contains("soil"))
            && !material.Contains("wood");
    }

    public static bool IsPathContext(string hierarchy, string material, string field, string combined)
    {
        if (IsGrassStepsMaterial(material))
            return false;

        var tokens = new[]
        {
            "path", "walk", "road", "deck", "deckline", "deckend", "deckpole", "sidewalk",
            "pavement", "footpath", "trail", "walkway", "bridge", "stair", "stairs", "tile",
            "concretesteps", "woodsteps", "sandsteps", "watersteps",
        };

        foreach (var token in tokens)
        {
            if (hierarchy.Contains(token) || material.Contains(token) || field.Contains(token) || combined.Contains(token))
                return true;
        }

        return false;
    }

    public static bool IsGrassStepsMaterial(string materialName)
    {
        var material = materialName.ToLowerInvariant();
        return material.Contains("grassstep") || material.Contains("grass_step");
    }

    public static bool IsBuildingContext(string hierarchy, string material, string shader)
    {
        var path = hierarchy.ToLowerInvariant();
        var mat = material.ToLowerInvariant();
        var objectName = string.Empty;

        if (IsTieredHillTerrain(path, objectName))
            return false;

        if (IsMainCircleBuilding(path, objectName))
            return true;

        if (path.Contains("circlemountains"))
            return true;

        return (path.Contains("mountain") || mat.Contains("mountainmat") || mat.Contains("topmat"))
            && !path.Contains("circle_hill")
            && !mat.Contains("m_mountain")
            && !IsGrassContext(path, mat, shader, string.Empty)
            && !IsTerrainGrassContext(path, mat, string.Empty, path);
    }

    public static bool IsEnvironmentRenderer(Renderer renderer)
    {
        if (renderer == null)
            return false;

        if (IsRendererOnHill(renderer))
            return true;

        var path = BuildHierarchyPath(renderer.transform).ToLowerInvariant();
        var objectName = renderer.gameObject.name.ToLowerInvariant();
        if (path.Contains("player"))
            return false;

        if (IsPathContext(path, string.Empty, objectName, path)
            || IsPropContext(path, string.Empty, objectName, path)
            || IsStoneContext(path, string.Empty, objectName, path)
            || IsGrassContext(path, string.Empty, string.Empty, string.Empty)
            || IsTerrainGrassContext(path, string.Empty, objectName, path)
            || IsBuildingContext(path, string.Empty, string.Empty))
            return true;

        var names = new[]
        {
            "deck", "fence", "concrete", "lamp", "light", "path", "walk", "trim",
            "bush", "tree", "trunk", "branch", "palm", "cloud", "water", "pool",
            "hill", "island", "terrace", "sand", "fountain", "playground",
            "picnic", "lemonade", "bunting", "bench", "cliff", "rock",
        };

        foreach (var token in names)
        {
            if (path.Contains(token) || objectName.Contains(token))
                return true;
        }

        var materials = renderer.sharedMaterials;
        if (materials == null)
            return false;

        foreach (var mat in materials)
        {
            if (mat == null || mat.shader == null)
                continue;

            var materialName = mat.name.ToLowerInvariant();
            var shaderName = mat.shader.name.ToLowerInvariant();
            var combined = path + "/" + objectName + "/" + materialName;

            if (IsGrassStepsMaterial(materialName)
                || IsPathContext(path, materialName, objectName, combined)
                || IsPropContext(path, materialName, objectName, combined)
                || IsStoneContext(path, materialName, objectName, combined)
                || IsGrassContext(path, materialName, shaderName, objectName)
                || IsTerrainGrassContext(path, materialName, objectName, combined)
                || IsBuildingContext(path, materialName, shaderName)
                || IsLikelyEnvironmentToon(path, objectName, materialName, shaderName)
                || materialName.Contains("water"))
                return true;
        }

        return false;
    }

    public static bool IsLikelyEnvironmentToon(string hierarchy, string objectName, string materialName, string shaderName)
    {
        if (!shaderName.Contains("toon"))
            return false;

        if (IsPropContext(hierarchy, materialName, objectName, hierarchy + "/" + objectName + "/" + materialName))
            return false;

        var terrainParents = new[]
        {
            "landscape", "environment", "terrain", "park", "island", "waterfall", "geology",
            "outdoor", "nature", "world", "scenery", "biome",
        };

        foreach (var token in terrainParents)
        {
            if (hierarchy.Contains(token) || objectName.Contains(token))
                return true;
        }

        var terrainMaterials = new[]
        {
            "earth", "dirt", "rock", "stone", "cliff", "grass", "sand", "mud", "ground", "soil", "geology",
        };

        foreach (var token in terrainMaterials)
        {
            if (materialName.Contains(token))
                return true;
        }

        return false;
    }

    public static MaterialApplyMode GetApplyModeForRenderer(string hierarchyPath, string materialName, string fieldName)
    {
        var path = hierarchyPath.ToLowerInvariant();
        var material = materialName.ToLowerInvariant();
        var combined = path + "/" + material + "/" + fieldName.ToLowerInvariant();

        if (IsPathContext(path, material, fieldName, combined))
            return MaterialApplyMode.PathForce;

        if (IsHillTerrainContext(path, material, fieldName, combined))
            return MaterialApplyMode.FoliageForce;

        if (IsPropContext(path, material, fieldName, combined) || IsStoneContext(path, material, fieldName, combined))
            return MaterialApplyMode.LightPreserving;

        if (IsGrassStepsMaterial(material))
            return MaterialApplyMode.FoliageForce;

        if (IsTieredHillTerrain(path, fieldName))
            return MaterialApplyMode.FoliageForce;

        if (material.Contains("m_mountain") && !IsMainCircleBuilding(path, fieldName))
            return MaterialApplyMode.FoliageForce;

        if (material.Contains("mountainmat") || material.Contains("m_mountain"))
            return MaterialApplyMode.BuildingStructureForce;

        if (material.Contains("topmat"))
            return MaterialApplyMode.BuildingStructureForce;

        if (IsBuildingContext(path, material, string.Empty))
            return material.Contains("top") || path.Contains("trim") || path.Contains("band")
                ? MaterialApplyMode.LightPreserving
                : MaterialApplyMode.BuildingGlassSoft;

        if (material.Contains("cloud") || path.Contains("cloud"))
            return MaterialApplyMode.LightPreserving;

        if (IsGrassCapableTerrain(path, material, fieldName, combined)
            || IsGrassContext(path, material, string.Empty, fieldName)
            || IsTerrainGrassContext(path, material, fieldName, combined)
            || material.Contains("grass"))
            return MaterialApplyMode.FoliageForce;

        if (material.Contains("tree") || material.Contains("palm") || material.Contains("crown") || material.Contains("bush"))
            return MaterialApplyMode.FoliageForce;

        return MaterialApplyMode.Standard;
    }

    private static bool IsDeckInstancer(string componentType) =>
        componentType is "DeckLineInstancing" or "DeckEndInstancing" or "DeckPoleInstancing" or "DeckLightInstancing";

    private static bool IsFoliageInstancer(string componentType, string fieldName) =>
        componentType is "GrassbaseInstancing" or "TreeBasesInstancing" or "PalmCrownInstancing" or "BushInstancing"
            || (componentType == "CircleBushPotInstancing" && fieldName == "BushMat")
            || (componentType == "BranchInstancing" && fieldName == "BranchMaterial");

    public static string ResolveRendererContext(string hierarchyPath, string objectName, Material source)
    {
        var materialName = source.name;
        var field = objectName.ToLowerInvariant();
        var path = hierarchyPath.ToLowerInvariant();
        var combined = path + "/" + field + "/" + materialName.ToLowerInvariant();

        if (IsGrassStepsMaterial(materialName))
            return "grass";

        var context = GetPaletteContext(string.Empty, objectName, hierarchyPath, materialName);
        if (context is "path" or "prop" or "stone" or "building-glass" or "building-structure" or "foliage" or "trunk" or "branch" or "light" or "grass" or TieredHillContext)
            return context;

        if (IsTieredHillTerrain(path, field))
            return TieredHillContext;

        if (IsPathContext(path, materialName.ToLowerInvariant(), field, combined))
            return "path";

        if (IsPropContext(path, materialName.ToLowerInvariant(), field, combined))
            return "prop";

        if (IsHillTerrainContext(path, materialName.ToLowerInvariant(), field, combined))
            return "grass";

        if (IsStoneContext(path, materialName.ToLowerInvariant(), field, combined))
            return "stone";

        if (!IsPropContext(path, materialName.ToLowerInvariant(), field, combined)
            && TryGetMaterialBaseColor(source, out var baseColor))
        {
            if (LooksLikeDirtColor(baseColor) || IsHillTerrainContext(path, materialName.ToLowerInvariant(), field, combined))
                return "grass";

            if (LooksLikeAquaTerrainColor(baseColor) && !materialName.ToLowerInvariant().Contains("water"))
            {
                var mat = materialName.ToLowerInvariant();
                return IsGrassCapableTerrain(path, mat, field, combined) ? "grass" : "stone";
            }
        }

        if (IsTerrainGrassContext(path, materialName.ToLowerInvariant(), field, combined))
            return "grass";

        return context;
    }

    public static bool LooksLikeDirtColor(Color color)
    {
        Color.RGBToHSV(color, out var h, out var s, out var v);
        return v > 0.15f && v < 0.75f && s > 0.08f && s < 0.65f && h > 0.05f && h < 0.20f;
    }

    private static bool TryGetMaterialBaseColorFromName(string materialName, out Color color)
    {
        color = GuessMaterialColor(materialName);
        var lower = materialName.ToLowerInvariant();
        return lower.Contains("aqua") || lower.Contains("cyan") || lower.Contains("teal") || lower.Contains("turquoise")
            || lower.Contains("grass") || lower.Contains("dirt") || lower.Contains("earth");
    }

    private static bool TryGetMaterialBaseColorFromMaterialNameHeuristic(string materialName, out Color color)
    {
        color = GuessMaterialColor(materialName);
        return TryGetMaterialBaseColorFromName(materialName, out _);
    }

    public static bool TryGetMaterialBaseColor(Material material, out Color color)
    {
        color = default;
        if (material == null)
            return false;

        foreach (var prop in new[] { "_BaseColor", "_Color", "_MainColor" })
        {
            if (!material.HasProperty(prop))
                continue;
            color = material.GetColor(prop);
            return true;
        }

        return false;
    }

    private static bool LooksLikeDirtMaterial(string materialName)
    {
        if (IsPropContext(string.Empty, materialName.ToLowerInvariant(), string.Empty, materialName))
            return false;

        Color.RGBToHSV(GuessMaterialColor(materialName), out var h, out var s, out var v);
        return v > 0.15f && v < 0.75f && s > 0.08f && s < 0.65f && h > 0.05f && h < 0.20f;
    }

    private static Color GuessMaterialColor(string materialName)
    {
        var lower = materialName.ToLowerInvariant();
        if (lower.Contains("wood") || lower.Contains("bench") || lower.Contains("table"))
            return WarmWood;
        if (lower.Contains("aqua") || lower.Contains("cyan") || lower.Contains("teal") || lower.Contains("turquoise"))
            return new Color(0.30f, 0.86f, 0.98f);
        if (lower.Contains("brown") || lower.Contains("dirt") || lower.Contains("earth") || lower.Contains("mud"))
            return new Color(0.45f, 0.35f, 0.22f);
        if (lower.Contains("sand"))
            return new Color(0.85f, 0.78f, 0.55f);
        if (lower.Contains("grass") || lower.Contains("lawn"))
            return GrassGreen;
        return new Color(0.5f, 0.45f, 0.3f);
    }

    private static Color Classify(float h, float s, float v)
    {
        if (v < 0.12f)
            return new Color(DeepAqua.r * 0.75f, DeepAqua.g * 0.75f, DeepAqua.b * 0.75f);

        if (v > 0.9f && s < 0.18f)
            return SoftWhite;

        if (h > 0.93f || h < 0.06f)
            return AquaGlass;

        if (h >= 0.06f && h < 0.14f)
            return WarmWood;

        if (h < 0.20f)
            return GrassGreen;

        if (h < 0.42f)
            return GrassGreen;

        if (h < 0.58f)
            return LushGreen;

        if (h < 0.72f)
            return GrassGreen;

        return SkyBlue;
    }
}
