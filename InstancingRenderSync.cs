using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace FrutigerAeroRecolor;

internal static class InstancingRenderSync
{
    private static readonly HashSet<int> SyncedBushInstances = new();

    public static void AfterInstancerRecolor(MonoBehaviour behaviour, string componentType)
    {
        RebuildRenderParams(behaviour, componentType);
        if (componentType == "BushInstancing")
            SyncBushMatProp(behaviour, forceRemap: true);
    }

    public static void EnsureBushBeforeRender(MonoBehaviour behaviour, RecolorSettings settings)
    {
        if (behaviour == null || behaviour.GetType().Name != "BushInstancing")
            return;

        var id = behaviour.GetInstanceID();
        if (!SyncedBushInstances.Contains(id))
        {
            Plugin.Instance?.Recolorer?.RecolorSingleInstancer(behaviour);
            SyncedBushInstances.Add(id);
        }

        SyncBushMatProp(behaviour, forceRemap: false);
        RebuildRenderParams(behaviour, "BushInstancing");
    }

    public static void EnsureCircleMountainBeforeRender(MonoBehaviour behaviour)
    {
        if (behaviour == null || behaviour.GetType().Name != "CircleMountainInstancing")
            return;

        Plugin.Instance?.Recolorer?.RecolorSingleInstancer(behaviour);
        RebuildRenderParams(behaviour, "CircleMountainInstancing");
    }

    public static void Clear() => SyncedBushInstances.Clear();

    private static void SyncBushMatProp(MonoBehaviour behaviour, bool forceRemap)
    {
        var type = behaviour.GetType();
        var batchesField = type.GetField("BushBatches", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var matPropField = type.GetField("MatProp", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (batchesField?.GetValue(behaviour) is not object batches || matPropField?.GetValue(behaviour) is not MaterialPropertyBlock block)
            return;

        var count = GetBatchCount(batches);
        var settings = Plugin.Instance?.BuildSettingsForSync() ?? new RecolorSettings();
        var strength = Mathf.Max(settings.FoliageStrength, settings.BatchStrength);

        if (forceRemap)
        {
            RemapArray(batches, "Color1", count, strength, settings);
            RemapArray(batches, "Color2", count, strength, settings);
            RemapArray(batches, "Color3", count, strength, settings);
            RemapArray(batches, "Color4", count, strength, settings);
        }

        SetVectorArray(block, batches, "_Color_1", "Color1");
        SetVectorArray(block, batches, "_Color_2", "Color2");
        SetVectorArray(block, batches, "_Color_3", "Color3");
        SetVectorArray(block, batches, "_Color_4", "Color4");
    }

    private static void RebuildRenderParams(MonoBehaviour behaviour, string componentType)
    {
        var type = behaviour.GetType();

        if (componentType == "BushInstancing")
        {
            RebuildRenderParamsField(behaviour, type, "_renderParams", "ToonInstanceMat", "MatProp", shadowCast: true);
            RebuildRenderParamsField(behaviour, type, "_renderParamsOutline", "ToonOutlineInstanceMat", "MatPropOutline", shadowCast: false);
            return;
        }

        if (componentType == "CircleMountainInstancing")
        {
            RebuildRenderParamsField(behaviour, type, "_renderParams", "MountainMat", "MatProp", shadowCast: true);
            RebuildRenderParamsField(behaviour, type, "_renderParamsTop", "TopMat", "MatProp", shadowCast: true);
            return;
        }

        if (componentType is "GrassbaseInstancing" or "TreeBasesInstancing" or "PalmCrownInstancing" or "CircleBushPotInstancing")
            RebuildRenderParamsField(behaviour, type, "_renderParams", "ToonInstanceMat", "MatProp", shadowCast: componentType != "GrassbaseInstancing");

        if (componentType == "CircleBushPotInstancing")
        {
            RebuildRenderParamsField(behaviour, type, "_renderParamsBushes", "BushMat", "MatPropBush", shadowCast: false);
            RebuildRenderParamsField(behaviour, type, "_renderParamsPots", "PotMat", "MatPropPots", shadowCast: false);
        }
    }

    private static void RebuildRenderParamsField(
        MonoBehaviour behaviour,
        Type type,
        string renderParamsFieldName,
        string materialFieldName,
        string matPropFieldName,
        bool shadowCast)
    {
        var renderField = type.GetField(renderParamsFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var materialField = type.GetField(materialFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (renderField == null || materialField?.GetValue(behaviour) is not Material material)
            return;

        var renderParams = new RenderParams(material) { receiveShadows = true };
        if (shadowCast)
            renderParams.shadowCastingMode = ShadowCastingMode.On;

        var matPropField = type.GetField(matPropFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (matPropField?.GetValue(behaviour) is MaterialPropertyBlock block)
            renderParams.matProps = block;

        renderField.SetValue(behaviour, renderParams);
    }

    private static int GetBatchCount(object batches)
    {
        var countField = batches.GetType().GetField("Count", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return countField?.GetValue(batches) is int count && count > 0 ? count : 0;
    }

    private static void RemapArray(object batches, string arrayName, int count, float strength, RecolorSettings settings)
    {
        var arrayField = batches.GetType().GetField(arrayName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (arrayField?.GetValue(batches) is not Vector4[] colors)
            return;

        var limit = count > 0 ? Math.Min(count, colors.Length) : colors.Length;
        for (var i = 0; i < limit; i++)
        {
            var color = colors[i];
            var rgb = FrutigerAeroPalette.RemapBatchColor(
                new Color(color.x, color.y, color.z, color.w),
                "BushInstancing",
                strength,
                settings);
            colors[i] = new Vector4(rgb.r, rgb.g, rgb.b, color.w);
        }

        arrayField.SetValue(batches, colors);
    }

    private static void SetVectorArray(MaterialPropertyBlock block, object batches, string shaderProperty, string arrayName)
    {
        var arrayField = batches.GetType().GetField(arrayName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (arrayField?.GetValue(batches) is not Vector4[] colors)
            return;

        block.SetVectorArray(Shader.PropertyToID(shaderProperty), colors);
    }
}
