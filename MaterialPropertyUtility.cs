using System;

using System.Collections.Generic;

using UnityEngine;

using UnityEngine.Rendering;



namespace FrutigerAeroRecolor;



internal readonly struct CloneKey : IEquatable<CloneKey>

{

    public Material Source { get; }

    public string Context { get; }



    public CloneKey(Material source, string context)

    {

        Source = source;

        Context = context;

    }



    public bool Equals(CloneKey other) =>

        ReferenceEquals(Source, other.Source) && string.Equals(Context, other.Context, StringComparison.Ordinal);



    public override bool Equals(object? obj) => obj is CloneKey other && Equals(other);



    public override int GetHashCode() =>

        (Source != null ? Source.GetHashCode() : 0) ^ StringComparer.Ordinal.GetHashCode(Context);

}



internal static class MaterialPropertyUtility

{

    private static readonly string[] BaseColorProperties =

    {

        "_BaseColor",

        "_Color",

        "_MainColor",

        "_TintColor",

    };



    private static readonly string[] ShadeColorProperties =

    {

        "_1st_ShadeColor",

        "_2nd_ShadeColor",

    };



    private static readonly string[] RampColorProperties =
    {
        "_Color_1",
        "_Color_2",
        "_Color_3",
        "_Color_4",
    };

    private static readonly string[] BuildingTextureProperties =
    {
        "_MainTex",
        "_BaseMap",
        "_BaseColorMap",
        "_1st_ShadeMap",
        "_2nd_ShadeMap",
        "_ShadingGradeMap",
        "mountain texture",
    };



    private static readonly Dictionary<CloneKey, Material> ContextClones = new();

    private static readonly Dictionary<Material, Material> CloneToSource = new();



    public static IReadOnlyDictionary<CloneKey, Material> CloneMap => ContextClones;



    public static Material EnsureInstance(Material source, string context)

    {

        if (source == null)

            return null!;



        var resolvedSource = ResolveSourceMaterial(source);

        var key = new CloneKey(resolvedSource, context);



        if (ContextClones.TryGetValue(key, out var existing))

            return existing;



        var clone = new Material(resolvedSource)

        {

            name = resolvedSource.name + " (FrutigerAero:" + context + ")",

            hideFlags = HideFlags.DontSave,

        };

        ContextClones[key] = clone;

        CloneToSource[clone] = resolvedSource;

        return clone;

    }



    public static Material? TryGetClone(Material sourceOrClone, string context)

    {

        var source = ResolveSourceMaterial(sourceOrClone);

        return ContextClones.TryGetValue(new CloneKey(source, context), out var clone) ? clone : null;

    }



    public static Material ResolveSourceMaterial(Material material)

    {

        if (CloneToSource.TryGetValue(material, out var source))

            return source;



        foreach (var pair in ContextClones)

        {

            if (pair.Value == material)

                return pair.Key.Source;

        }



        return material;

    }



    public static void ApplyPalette(

        Material material,

        Color target,

        RecolorSettings settings,

        MaterialApplyMode mode = MaterialApplyMode.Standard,

        bool buildingGlassRamp = false)

    {

        if (material == null || material.shader == null)

            return;



        switch (mode)

        {

            case MaterialApplyMode.BuildingForce:
                ApplyBuildingForce(material, target, settings, buildingGlassRamp);
                break;
            case MaterialApplyMode.BuildingStructureForce:
                ApplyBuildingStructureForce(material, target, settings);
                break;
            case MaterialApplyMode.BuildingGlassSoft:
                ApplyBuildingGlassSoft(material, target, settings);
                break;
            case MaterialApplyMode.FoliageForce:
                ApplyFoliageForce(material, target, settings);
                break;
            case MaterialApplyMode.PathForce:
                ApplyPathForce(material, target, settings);
                break;
            case MaterialApplyMode.LightPreserving:

                ApplyLightPreservingShading(material, target, settings);

                break;

            case MaterialApplyMode.Standard when settings.PreserveToonShading:

                ApplyPreservingShading(material, target, settings);

                break;

            case MaterialApplyMode.Standard:

                ApplyFlat(material, target, settings.BaseStrength);

                break;

        }



        ApplyGloss(material);

    }



    private static void ApplyBuildingForce(Material material, Color target, RecolorSettings settings, bool aquaRamp)

    {

        var strength = settings.BuildingStrength;

        var vibrantTarget = FrutigerAeroPalette.BoostVibrancy(target, settings.VibrancyMultiplier);

        var shade = SaturatedShade(vibrantTarget, 0.18f);

        var deepShade = SaturatedShade(vibrantTarget, 0.30f);

        if (aquaRamp)
            NeutralizeGreenAlbedoTexture(material);



        foreach (var property in BaseColorProperties)

        {

            if (!material.HasProperty(property))

                continue;

            material.SetColor(property, Color.Lerp(material.GetColor(property), vibrantTarget, strength));

        }



        if (material.HasProperty("_1st_ShadeColor"))

            material.SetColor("_1st_ShadeColor", Color.Lerp(material.GetColor("_1st_ShadeColor"), shade, strength * 0.85f));



        if (material.HasProperty("_2nd_ShadeColor"))

            material.SetColor("_2nd_ShadeColor", Color.Lerp(material.GetColor("_2nd_ShadeColor"), deepShade, strength * 0.85f));



        for (var i = 0; i < RampColorProperties.Length; i++)

        {

            var property = RampColorProperties[i];

            if (!material.HasProperty(property))

                continue;



            Color rampTarget;

            if (aquaRamp)

            {

                var t = (i + 1f) / RampColorProperties.Length;

                rampTarget = Color.Lerp(FrutigerAeroPalette.DeepAqua, vibrantTarget, t);

            }

            else

            {

                rampTarget = Color.Lerp(deepShade, vibrantTarget, (i + 1f) / RampColorProperties.Length);

            }



            material.SetColor(property, Color.Lerp(material.GetColor(property), rampTarget, strength));

        }

    }



    private static void ApplyBuildingStructureForce(Material material, Color target, RecolorSettings settings)
    {
        var strength = Mathf.Clamp01(settings.BuildingStrength);
        var white = EnforceMinLuminance(target, 0.90f);
        Color.RGBToHSV(white, out var hue, out var sat, out var value);
        white = Color.HSVToRGB(hue, Mathf.Min(sat, 0.06f), Mathf.Max(value, 0.93f));
        white.a = target.a;

        var shade = EnforceMinLuminance(new Color(0.86f, 0.88f, 0.85f), 0.72f);
        var deepShade = EnforceMinLuminance(new Color(0.74f, 0.77f, 0.73f), 0.58f);

        NeutralizeBuildingTextures(material);
        DisableBuildingTextureBlends(material);
        ForceBuildingShaderColors(material, white, shade, deepShade, strength);

        foreach (var property in BaseColorProperties)
        {
            if (!material.HasProperty(property))
                continue;

            material.SetColor(property, strength >= 0.99f ? white : Color.Lerp(material.GetColor(property), white, strength));
        }

        foreach (var property in ShadeColorProperties)
        {
            if (!material.HasProperty(property))
                continue;

            var shadeTarget = property.Contains("2nd") ? deepShade : shade;
            material.SetColor(property, strength >= 0.99f ? shadeTarget : Color.Lerp(material.GetColor(property), shadeTarget, strength));
        }

        for (var i = 0; i < RampColorProperties.Length; i++)
        {
            var property = RampColorProperties[i];
            if (!material.HasProperty(property))
                continue;

            var rampTarget = Color.Lerp(deepShade, white, (i + 1f) / RampColorProperties.Length);
            material.SetColor(property, strength >= 0.99f ? rampTarget : Color.Lerp(material.GetColor(property), rampTarget, strength));
        }
    }

    private static void NeutralizeBuildingTextures(Material material)
    {
        var white = Texture2D.whiteTexture;

        foreach (var property in BuildingTextureProperties)
        {
            if (material.HasProperty(property))
                material.SetTexture(property, white);
        }

        ForEachShaderTextureProperty(material, propertyName => material.SetTexture(propertyName, white));
    }

    private static void DisableBuildingTextureBlends(Material material)
    {
        if (material.HasProperty("_Is_BlendBaseColor"))
            material.SetFloat("_Is_BlendBaseColor", 0f);

        if (material.HasProperty("_Is_ColorShift"))
            material.SetFloat("_Is_ColorShift", 0f);
    }

    private static void ForceBuildingShaderColors(Material material, Color white, Color shade, Color deepShade, float strength)
    {
        ForEachShaderColorProperty(material, propertyName =>
        {
            if (ShouldSkipBuildingColorProperty(propertyName))
                return;

            var target = ResolveBuildingColorTarget(propertyName, white, shade, deepShade);
            if (strength >= 0.99f)
                material.SetColor(propertyName, target);
            else
                material.SetColor(propertyName, Color.Lerp(material.GetColor(propertyName), target, strength));
        });
    }

    private static bool ShouldSkipBuildingColorProperty(string propertyName)
    {
        var name = propertyName.ToLowerInvariant();
        return name.Contains("outline")
            || name.Contains("emissive")
            || name.Contains("emission")
            || name.Contains("mask")
            || name.Contains("diffusion")
            || name.Contains("remap")
            || name.Contains("mipinfo")
            || name.Contains("uv")
            || name.Contains("doublesided");
    }

    private static Color ResolveBuildingColorTarget(string propertyName, Color white, Color shade, Color deepShade)
    {
        var name = propertyName.ToLowerInvariant();
        if (name.Contains("2nd") || name.Contains("deep"))
            return deepShade;

        if (name.Contains("shade") || name.Contains("1st"))
            return shade;

        return white;
    }

    private static void ForEachShaderTextureProperty(Material material, Action<string> apply)
    {
        var shader = material.shader;
        if (shader == null)
            return;

        var count = shader.GetPropertyCount();
        for (var i = 0; i < count; i++)
        {
            if (shader.GetPropertyType(i) != ShaderPropertyType.Texture)
                continue;

            apply(shader.GetPropertyName(i));
        }
    }

    private static void ForEachShaderColorProperty(Material material, Action<string> apply)
    {
        var shader = material.shader;
        if (shader == null)
            return;

        var count = shader.GetPropertyCount();
        for (var i = 0; i < count; i++)
        {
            if (shader.GetPropertyType(i) != ShaderPropertyType.Color)
                continue;

            apply(shader.GetPropertyName(i));
        }
    }

    private static void NeutralizeGreenAlbedoTexture(Material material)
    {
        NeutralizeBuildingTextures(material);
    }



    private static void ApplyBuildingGlassSoft(Material material, Color target, RecolorSettings settings)
    {
        var boosted = new RecolorSettings
        {
            PreserveToonShading = settings.PreserveToonShading,
            BaseStrength = Mathf.Clamp01(settings.BuildingStrength * 0.78f),
            ShadeStrength = Mathf.Clamp01(settings.ShadeStrength * 0.85f),
            BatchStrength = settings.BatchStrength,
            BuildingStrength = settings.BuildingStrength,
            FoliageStrength = settings.FoliageStrength,
            VibrancyMultiplier = Mathf.Min(settings.VibrancyMultiplier, 1.15f),
            BuildingGlassBlue = settings.BuildingGlassBlue,
            BuildingStructureWhite = settings.BuildingStructureWhite,
            PathWhite = settings.PathWhite,
        };

        var vibrantTarget = FrutigerAeroPalette.BoostVibrancy(target, boosted.VibrancyMultiplier);
        ApplyPreservingShading(material, vibrantTarget, boosted);
    }

    private static void ApplyPathForce(Material material, Color target, RecolorSettings settings)
    {
        var strength = 0.92f;
        var vibrantTarget = EnforceMinLuminance(
            FrutigerAeroPalette.BoostVibrancy(target, Mathf.Min(settings.VibrancyMultiplier, 1.12f)),
            0.80f);
        var shade = EnforceMinLuminance(SaturatedShade(vibrantTarget, 0.14f), 0.55f);
        var deepShade = EnforceMinLuminance(SaturatedShade(vibrantTarget, 0.26f), 0.42f);

        foreach (var property in BaseColorProperties)
        {
            if (!material.HasProperty(property))
                continue;
            material.SetColor(property, Color.Lerp(material.GetColor(property), vibrantTarget, strength));
        }

        if (material.HasProperty("_1st_ShadeColor"))
            material.SetColor("_1st_ShadeColor", Color.Lerp(material.GetColor("_1st_ShadeColor"), shade, strength * 0.85f));

        if (material.HasProperty("_2nd_ShadeColor"))
            material.SetColor("_2nd_ShadeColor", Color.Lerp(material.GetColor("_2nd_ShadeColor"), deepShade, strength * 0.85f));

        for (var i = 0; i < RampColorProperties.Length; i++)
        {
            var property = RampColorProperties[i];
            if (!material.HasProperty(property))
                continue;

            var rampTarget = Color.Lerp(deepShade, vibrantTarget, (i + 1f) / RampColorProperties.Length);
            material.SetColor(property, Color.Lerp(material.GetColor(property), rampTarget, strength * 0.75f));
        }
    }

    private static Color EnforceMinLuminance(Color color, float minLuminance)
    {
        var lum = Luminance(color);
        if (lum >= minLuminance)
            return color;

        if (lum < 0.001f)
            return new Color(minLuminance, minLuminance, minLuminance, color.a);

        var scale = minLuminance / lum;
        return new Color(
            Mathf.Clamp01(color.r * scale),
            Mathf.Clamp01(color.g * scale),
            Mathf.Clamp01(color.b * scale),
            color.a);
    }

    private static void ApplyFoliageForce(Material material, Color target, RecolorSettings settings)

    {

        var strength = Mathf.Clamp01(settings.FoliageStrength * 0.88f);

        var foliageVibrancy = Mathf.Min(settings.VibrancyMultiplier, 1.15f);

        var vibrantTarget = CapSaturation(
            FrutigerAeroPalette.BoostVibrancy(target, foliageVibrancy),
            0.72f);

        var shade = CapSaturation(SaturatedShade(vibrantTarget, 0.22f), 0.68f);

        var deepShade = CapSaturation(SaturatedShade(vibrantTarget, 0.35f), 0.55f);



        foreach (var property in BaseColorProperties)

        {

            if (!material.HasProperty(property))

                continue;

            var current = material.GetColor(property);
            material.SetColor(property, CapSaturation(Color.Lerp(current, vibrantTarget, strength), 0.72f));

        }



        if (material.HasProperty("_1st_ShadeColor"))

            material.SetColor("_1st_ShadeColor", Color.Lerp(material.GetColor("_1st_ShadeColor"), shade, strength * 0.9f));



        if (material.HasProperty("_2nd_ShadeColor"))

            material.SetColor("_2nd_ShadeColor", Color.Lerp(material.GetColor("_2nd_ShadeColor"), deepShade, strength * 0.9f));



        for (var i = 0; i < RampColorProperties.Length; i++)

        {

            var property = RampColorProperties[i];

            if (!material.HasProperty(property))

                continue;



            var rampTarget = Color.Lerp(deepShade, vibrantTarget, (i + 1f) / RampColorProperties.Length);

            material.SetColor(property, Color.Lerp(material.GetColor(property), rampTarget, strength * 0.85f));

        }

    }



    private static void ApplyLightPreservingShading(Material material, Color target, RecolorSettings settings)

    {

        Color? baseBefore = null;



        foreach (var property in BaseColorProperties)

        {

            if (!material.HasProperty(property))

                continue;



            var current = material.GetColor(property);

            baseBefore ??= current;

            var shifted = HueShiftPreservingDetail(current, target, settings.BaseStrength);

            material.SetColor(property, shifted);

        }



        if (baseBefore == null)

            baseBefore = target;



        var baseAfter = baseBefore.Value;

        foreach (var property in BaseColorProperties)

        {

            if (material.HasProperty(property))

            {

                baseAfter = material.GetColor(property);

                break;

            }

        }



        foreach (var property in ShadeColorProperties)

        {

            if (!material.HasProperty(property))

                continue;



            var shade = material.GetColor(property);

            var remappedShade = RemapShadeColor(baseBefore.Value, shade, baseAfter, target, settings.ShadeStrength, minRatio: 0.55f);

            material.SetColor(property, remappedShade);

        }



        foreach (var property in RampColorProperties)

        {

            if (!material.HasProperty(property))

                continue;



            var ramp = material.GetColor(property);

            var remappedRamp = RemapShadeColor(baseBefore.Value, ramp, baseAfter, target, settings.ShadeStrength * 0.85f, minRatio: 0.55f);

            material.SetColor(property, remappedRamp);

        }

    }



    private static void ApplyPreservingShading(Material material, Color target, RecolorSettings settings)

    {

        target = FrutigerAeroPalette.BoostVibrancy(target, settings.VibrancyMultiplier);

        Color? baseBefore = null;



        foreach (var property in BaseColorProperties)

        {

            if (!material.HasProperty(property))

                continue;



            var current = material.GetColor(property);

            baseBefore ??= current;

            material.SetColor(property, Color.Lerp(current, target, settings.BaseStrength));

        }



        if (baseBefore == null)

            baseBefore = target;



        var baseAfter = baseBefore.Value;

        foreach (var property in BaseColorProperties)

        {

            if (material.HasProperty(property))

            {

                baseAfter = material.GetColor(property);

                break;

            }

        }



        foreach (var property in ShadeColorProperties)

        {

            if (!material.HasProperty(property))

                continue;



            var shade = material.GetColor(property);

            var remappedShade = RemapShadeColor(baseBefore.Value, shade, baseAfter, target, settings.ShadeStrength);

            material.SetColor(property, remappedShade);

        }



        foreach (var property in RampColorProperties)

        {

            if (!material.HasProperty(property))

                continue;



            var ramp = material.GetColor(property);

            material.SetColor(property, FrutigerAeroPalette.Remap(ramp, settings.ShadeStrength, settings.VibrancyMultiplier));

        }

    }



    private static Color RemapShadeColor(

        Color originalBase,

        Color shade,

        Color newBase,

        Color target,

        float strength,

        float minRatio = 0.2f)

    {

        var baseLum = Luminance(originalBase);

        var shadeLum = Luminance(shade);

        var ratio = baseLum > 0.001f ? Mathf.Clamp(shadeLum / baseLum, minRatio, 1.2f) : 1f;



        var hueShifted = FrutigerAeroPalette.Remap(shade, strength, 1.2f);

        var adjusted = ScaleLuminance(hueShifted, Luminance(newBase) * ratio);

        return Color.Lerp(shade, adjusted, strength);

    }



    private static Color HueShiftPreservingDetail(Color input, Color target, float strength)

    {

        Color.RGBToHSV(input, out var inH, out var inS, out var inV);

        Color.RGBToHSV(target, out var tgtH, out var tgtS, out _);



        var outH = Mathf.LerpAngle(inH * 360f, tgtH * 360f, strength) / 360f;

        var outS = Mathf.Lerp(inS, Mathf.Max(inS, tgtS * 0.65f), strength * 0.5f);

        var outV = inV;



        var result = Color.HSVToRGB(outH, outS, outV);

        result.a = input.a;

        return result;

    }



    private static void ApplyFlat(Material material, Color target, float strength)

    {

        foreach (var property in BaseColorProperties)

        {

            if (!material.HasProperty(property))

                continue;

            material.SetColor(property, Color.Lerp(material.GetColor(property), target, strength));

        }



        foreach (var property in ShadeColorProperties)

        {

            if (!material.HasProperty(property))

                continue;

            material.SetColor(property, Color.Lerp(material.GetColor(property), target, strength));

        }



        foreach (var property in RampColorProperties)

        {

            if (!material.HasProperty(property))

                continue;

            material.SetColor(property, Color.Lerp(material.GetColor(property), target, strength));

        }

    }



    private static void ApplyGloss(Material material)

    {

        if (material.HasProperty("_Smoothness"))

            material.SetFloat("_Smoothness", Mathf.Clamp(material.GetFloat("_Smoothness") + 0.12f, 0.40f, 0.72f));



        if (material.HasProperty("_Glossiness"))

            material.SetFloat("_Glossiness", Mathf.Clamp(material.GetFloat("_Glossiness") + 0.12f, 0.40f, 0.72f));

    }



    private static Color SaturatedShade(Color color, float amount)

    {

        return FrutigerAeroPalette.BoostVibrancy(Darken(color, amount), 1.12f);

    }

    private static Color CapSaturation(Color color, float maxSaturation)
    {
        Color.RGBToHSV(color, out var h, out var s, out var v);
        s = Mathf.Min(s, maxSaturation);
        var result = Color.HSVToRGB(h, s, v);
        result.a = color.a;
        return result;
    }



    public static int PropagateClonesToRenderers(

        Func<Renderer, bool> shouldSkip,

        bool includeDisabled,

        Func<Renderer, Material, string> resolveContext)

    {

        var renderers = UnityEngine.Object.FindObjectsOfType<Renderer>(true);

        var replacements = 0;



        foreach (var renderer in renderers)

        {

            if (renderer == null || shouldSkip(renderer))

                continue;



            if (!includeDisabled && !renderer.enabled)

                continue;



            var materials = renderer.sharedMaterials;

            if (materials == null)

                continue;



            var changed = false;

            for (var i = 0; i < materials.Length; i++)

            {

                var material = materials[i];

                if (material == null)

                    continue;



                var source = ResolveSourceMaterial(material);

                var context = resolveContext(renderer, source);

                if (context is "building-glass" or "building-structure")
                {
                    var path = FrutigerAeroPalette.BuildRendererPath(renderer);
                    if (!FrutigerAeroPalette.IsMainCircleBuilding(path, renderer.gameObject.name))
                        continue;
                }

                var clone = TryGetClone(source, context);

                if (clone == null)

                    continue;



                if (material == clone)

                    continue;



                materials[i] = clone;

                changed = true;

                replacements++;

            }



            if (changed)

                renderer.sharedMaterials = materials;

        }



        return replacements;

    }



    private static Color Darken(Color color, float amount) =>

        Color.Lerp(color, Color.black, Mathf.Clamp01(amount));



    private static float Luminance(Color color) =>

        color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;



    private static Color ScaleLuminance(Color color, float targetLum)

    {

        var current = Luminance(color);

        if (current < 0.001f)

            return color;



        var scale = targetLum / current;

        return new Color(

            Mathf.Clamp01(color.r * scale),

            Mathf.Clamp01(color.g * scale),

            Mathf.Clamp01(color.b * scale),

            color.a);

    }



    public static void ClearCache()

    {

        ContextClones.Clear();

        CloneToSource.Clear();

    }

}


