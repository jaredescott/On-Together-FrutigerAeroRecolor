using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrutigerAeroRecolor;

internal sealed class EnvironmentRecolorer
{
    private static readonly int ColorProp1 = Shader.PropertyToID("_Color_1");
    private static readonly int ColorProp2 = Shader.PropertyToID("_Color_2");
    private static readonly int ColorProp3 = Shader.PropertyToID("_Color_3");
    private static readonly int ColorProp4 = Shader.PropertyToID("_Color_4");
    private static readonly float[] RetryDelaysSeconds = { 0f, 1.5f, 4f, 8f, 15f, 25f, 40f };

    private readonly ManualLogSource _log;
    private RecolorSettings _settings;
    private readonly bool _skipPlayers;
    private readonly bool _verbose;

    public EnvironmentRecolorer(ManualLogSource log, RecolorSettings settings, bool skipPlayers, bool verbose)
    {
        _log = log;
        _settings = settings;
        _skipPlayers = skipPlayers;
        _verbose = verbose;
    }

    public IEnumerator RecolorWhenReady()
    {
        yield return null;
        yield return null;

        for (var i = 0; i < RetryDelaysSeconds.Length; i++)
        {
            if (i > 0)
                yield return new WaitForSeconds(RetryDelaysSeconds[i]);

            var instancers = RecolorInstancingComponents();
            var propagatedInstancer = MaterialPropertyUtility.PropagateClonesToRenderers(
                ShouldSkipRenderer,
                includeDisabled: true,
                ResolveRendererContext);
            var materials = RecolorRenderers();
            var propagatedFinal = MaterialPropertyUtility.PropagateClonesToRenderers(
                ShouldSkipRenderer,
                includeDisabled: true,
                ResolveRendererContext);

            _log.LogInfo(
                $"Frutiger Aero pass {i + 1}/{RetryDelaysSeconds.Length}: +{instancers} instancers, +{propagatedInstancer} instancer-propagated, +{materials} renderer materials, +{propagatedFinal} final-propagated.");
        }
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        MaterialPropertyUtility.ClearCache();
        InstancingRenderSync.Clear();
        Plugin.Instance?.StartCoroutine(RecolorWhenReady());
    }

    public void UpdateSettings(RecolorSettings settings) => _settings = settings;

    public void TriggerRecolor()
    {
        MaterialPropertyUtility.ClearCache();
        InstancingRenderSync.Clear();
        Plugin.Instance?.StartCoroutine(RecolorWhenReady());
    }

    public void RecolorSingleInstancer(MonoBehaviour behaviour)
    {
        if (behaviour == null)
            return;

        var typeName = behaviour.GetType().Name;
        if (!typeName.EndsWith("Instancing", StringComparison.Ordinal))
            return;

        RecolorInstancingComponent(behaviour, typeName);
        MaterialPropertyUtility.PropagateClonesToRenderers(ShouldSkipRenderer, includeDisabled: true, ResolveRendererContext);

        if (_verbose)
            _log.LogInfo($"Frutiger Aero: immediate recolor for {typeName}");
    }

    private int RecolorInstancingComponents()
    {
        var touched = 0;
        var behaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
        foreach (var behaviour in behaviours)
        {
            if (behaviour == null)
                continue;

            var typeName = behaviour.GetType().Name;
            if (!typeName.EndsWith("Instancing", StringComparison.Ordinal))
                continue;

            RecolorInstancingComponent(behaviour, typeName);
            touched++;
        }

        return touched;
    }

    private void RecolorInstancingComponent(MonoBehaviour behaviour, string componentType)
    {
        var type = behaviour.GetType();
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            if (field.FieldType != typeof(Material))
                continue;

            var material = field.GetValue(behaviour) as Material;
            if (material == null)
                continue;

            var source = MaterialPropertyUtility.ResolveSourceMaterial(material);
            var context = FrutigerAeroPalette.GetPaletteContext(componentType, field.Name, string.Empty, source.name);
            var clone = MaterialPropertyUtility.EnsureInstance(source, context);
            field.SetValue(behaviour, clone);

            var applyMode = FrutigerAeroPalette.GetApplyMode(componentType, field.Name);
            var palette = FrutigerAeroPalette.ForInstancingMaterial(
                componentType,
                field.Name,
                clone.shader != null ? clone.shader.name : string.Empty,
                source.name,
                _settings);

            var buildingGlassRamp = false;
            MaterialPropertyUtility.ApplyPalette(clone, palette, _settings, applyMode, buildingGlassRamp);

            if (_verbose)
            {
                var hasMainTex = clone.HasProperty("_MainTex") || clone.HasProperty("_BaseMap");
                _log.LogInfo(
                    $"Instancing material: {componentType}.{field.Name} ({source.name}) ctx={context} shader={clone.shader?.name} mainTex={hasMainTex} -> {palette}");
            }
        }

        RemapBatchColors(behaviour, fields, componentType);
        RefreshMaterialPropertyBlock(behaviour, type);
        InstancingRenderSync.AfterInstancerRecolor(behaviour, componentType);
    }

    private void RemapBatchColors(MonoBehaviour behaviour, FieldInfo[] fields, string componentType)
    {
        if (componentType == "CircleMountainInstancing")
            return;

        foreach (var field in fields)
        {
            if (!field.Name.EndsWith("Batches", StringComparison.Ordinal))
                continue;

            var batches = field.GetValue(behaviour);
            if (batches == null)
                continue;

            var strength = _settings.PreserveToonShading ? _settings.BatchStrength : _settings.BaseStrength;
            RemapColorArray(batches, "Color1", strength, componentType);
            RemapColorArray(batches, "Color2", strength, componentType);
            RemapColorArray(batches, "Color3", strength, componentType);
            RemapColorArray(batches, "Color4", strength, componentType);
        }
    }

    private void RemapColorArray(object batches, string arrayName, float strength, string componentType)
    {
        var arrayField = batches.GetType().GetField(arrayName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (arrayField?.GetValue(batches) is not Vector4[] colors)
            return;

        var limit = colors.Length;
        if (componentType == "BushInstancing")
        {
            var countField = batches.GetType().GetField("Count", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (countField?.GetValue(batches) is int count && count > 0)
                limit = Math.Min(count, colors.Length);
        }

        for (var i = 0; i < limit; i++)
        {
            var color = colors[i];
            var rgb = FrutigerAeroPalette.RemapBatchColor(
                new Color(color.x, color.y, color.z, color.w),
                componentType,
                strength,
                _settings);
            colors[i] = new Vector4(rgb.r, rgb.g, rgb.b, color.w);
        }

        arrayField.SetValue(batches, colors);
    }

    private static void RefreshMaterialPropertyBlock(MonoBehaviour behaviour, Type type)
    {
        RefreshMaterialPropertyBlockField(behaviour, type, "MatProp");
        RefreshMaterialPropertyBlockField(behaviour, type, "MatPropBush");
    }

    private static void RefreshMaterialPropertyBlockField(MonoBehaviour behaviour, Type type, string blockFieldName)
    {
        var matPropField = type.GetField(blockFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (matPropField?.GetValue(behaviour) is not MaterialPropertyBlock block)
            return;

        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!field.Name.EndsWith("Batches", StringComparison.Ordinal))
                continue;

            var batches = field.GetValue(behaviour);
            if (batches == null)
                continue;

            TrySetVectorArray(block, batches, ColorProp1, "Color1");
            TrySetVectorArray(block, batches, ColorProp2, "Color2");
            TrySetVectorArray(block, batches, ColorProp3, "Color3");
            TrySetVectorArray(block, batches, ColorProp4, "Color4");
        }
    }

    private static void TrySetVectorArray(MaterialPropertyBlock block, object batches, int propertyId, string arrayName)
    {
        var arrayField = batches.GetType().GetField(arrayName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (arrayField?.GetValue(batches) is Vector4[] colors)
            block.SetVectorArray(propertyId, colors);
    }

    private int RecolorRenderers()
    {
        var touched = 0;
        var renderers = UnityEngine.Object.FindObjectsOfType<Renderer>(true);
        foreach (var renderer in renderers)
        {
            if (renderer == null)
                continue;

            if (_skipPlayers && ShouldSkipRenderer(renderer))
                continue;

            if (renderer is SkinnedMeshRenderer)
                continue;

            var hierarchyPath = BuildHierarchyPath(renderer.transform);

            if (FrutigerAeroPalette.IsTieredHillRenderer(renderer))
            {
                if (RecolorTieredHillRenderer(renderer))
                    touched++;
                continue;
            }

            if (FrutigerAeroPalette.IsMainCircleBuilding(hierarchyPath, renderer.gameObject.name))
            {
                if (RecolorCircleMountainRenderer(renderer))
                    touched++;
                continue;
            }

            var owner = renderer.GetComponent<MonoBehaviour>();
            if (owner != null && owner.GetType().Name.EndsWith("Instancing", StringComparison.Ordinal))
                continue;

            var isOnHill = FrutigerAeroPalette.IsRendererOnHill(renderer);
            var isEnvironment = FrutigerAeroPalette.IsEnvironmentRenderer(renderer);
            var rendererMaterials = renderer.sharedMaterials;
            var hasDirtMaterial = RendererHasDirtMaterial(renderer, rendererMaterials);
            if (!renderer.enabled && !isEnvironment && !hasDirtMaterial && !isOnHill)
                continue;
            if (!isEnvironment && !hasDirtMaterial && !isOnHill)
                continue;

            var materials = rendererMaterials;
            if (materials == null)
                continue;

            var changed = false;
            for (var i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (material == null || material.shader == null)
                    continue;

                var source = MaterialPropertyUtility.ResolveSourceMaterial(material);
                var context = FrutigerAeroPalette.ResolveRendererContext(hierarchyPath, renderer.gameObject.name, source);
                var existingClone = MaterialPropertyUtility.TryGetClone(source, context);
                if (existingClone != null)
                {
                    if (material != existingClone)
                    {
                        materials[i] = existingClone;
                        changed = true;
                    }

                    continue;
                }

                var clone = MaterialPropertyUtility.EnsureInstance(source, context);
                var palette = FrutigerAeroPalette.ForMaterialField(
                    hierarchyPath,
                    source.shader.name,
                    source.name,
                    _settings);
                if (isOnHill)
                    palette = FrutigerAeroPalette.LushGreen;

                var applyMode = FrutigerAeroPalette.GetApplyModeForRenderer(hierarchyPath, source.name, renderer.gameObject.name);
                if (isOnHill)
                    applyMode = MaterialApplyMode.FoliageForce;
                var buildingGlassRamp = false;
                MaterialPropertyUtility.ApplyPalette(clone, palette, _settings, applyMode, buildingGlassRamp);
                materials[i] = clone;
                changed = true;
                touched++;

                if (_verbose)
                    _log.LogInfo($"Renderer material: {hierarchyPath} / {source.name} ctx={context}");
            }

            if (changed)
                renderer.sharedMaterials = materials;
        }

        return touched;
    }

    private static bool RendererHasDirtMaterial(Renderer renderer, Material[]? materials)
    {
        if (materials == null)
            return false;

        var hierarchyPath = BuildHierarchyPath(renderer.transform);
        var objectName = renderer.gameObject.name;

        foreach (var mat in materials)
        {
            if (mat == null)
                continue;

            var source = MaterialPropertyUtility.ResolveSourceMaterial(mat);
            var materialName = source.name.ToLowerInvariant();
            var field = objectName.ToLowerInvariant();
            var path = hierarchyPath.ToLowerInvariant();
            var combined = path + "/" + field + "/" + materialName;

            if (FrutigerAeroPalette.IsPropContext(path, materialName, field, combined))
                continue;

            if (FrutigerAeroPalette.IsRendererOnHill(renderer))
                return true;

            if (FrutigerAeroPalette.TryGetMaterialBaseColor(source, out var baseColor))
            {
                if (FrutigerAeroPalette.LooksLikeDirtColor(baseColor))
                    return true;

                if (FrutigerAeroPalette.LooksLikeAquaTerrainColor(baseColor) && !materialName.Contains("water"))
                    return true;
            }
        }

        return false;
    }

    private bool RecolorTieredHillRenderer(Renderer renderer)
    {
        var materials = renderer.sharedMaterials;
        if (materials == null)
            return false;

        var changed = false;
        for (var i = 0; i < materials.Length; i++)
        {
            var material = materials[i];
            if (material == null || material.shader == null)
                continue;

            var source = MaterialPropertyUtility.ResolveSourceMaterial(material);
            var clone = MaterialPropertyUtility.EnsureInstance(source, FrutigerAeroPalette.TieredHillContext);
            MaterialPropertyUtility.ApplyPalette(
                clone,
                FrutigerAeroPalette.LushGreen,
                _settings,
                MaterialApplyMode.FoliageForce,
                buildingGlassRamp: false);

            if (material != clone)
            {
                materials[i] = clone;
                changed = true;
            }
        }

        if (changed)
            renderer.sharedMaterials = materials;

        return changed;
    }

    private bool RecolorCircleMountainRenderer(Renderer renderer)
    {
        var materials = renderer.sharedMaterials;
        if (materials == null)
            return false;

        var changed = false;
        for (var i = 0; i < materials.Length; i++)
        {
            var material = materials[i];
            if (material == null || material.shader == null)
                continue;

            var source = MaterialPropertyUtility.ResolveSourceMaterial(material);
            var clone = MaterialPropertyUtility.EnsureInstance(source, "building-structure");
            MaterialPropertyUtility.ApplyPalette(
                clone,
                _settings.BuildingStructureWhite,
                _settings,
                MaterialApplyMode.BuildingStructureForce,
                buildingGlassRamp: false);

            if (material != clone)
            {
                materials[i] = clone;
                changed = true;
            }
        }

        if (changed)
            renderer.sharedMaterials = materials;

        return changed;
    }

    private static bool IsBuildingGlassContext(string context, string hierarchyPath, string materialName)
    {
        if (context == "building-glass")
            return true;

        return FrutigerAeroPalette.IsBuildingContext(hierarchyPath, materialName, string.Empty)
            && !materialName.ToLowerInvariant().Contains("top");
    }

    private static string ResolveRendererContext(Renderer renderer, Material source)
    {
        var hierarchyPath = BuildHierarchyPath(renderer.transform);
        return FrutigerAeroPalette.ResolveRendererContext(hierarchyPath, renderer.gameObject.name, source);
    }

    private bool ShouldSkipRenderer(Renderer renderer)
    {
        var transform = renderer.transform;
        while (transform != null)
        {
            var name = transform.name;
            if (name.IndexOf("player", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (transform.CompareTag("Player"))
                return true;
            transform = transform.parent;
        }

        return false;
    }

    private static string BuildHierarchyPath(Transform transform)
    {
        var parts = new List<string>();
        while (transform != null)
        {
            parts.Add(transform.name);
            transform = transform.parent;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }
}
