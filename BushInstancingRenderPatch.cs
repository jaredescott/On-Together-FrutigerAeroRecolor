using HarmonyLib;
using UnityEngine;

namespace FrutigerAeroRecolor;

internal static class BushInstancingRenderPatch
{
    private static readonly HarmonyMethod PrefixMethod = new(typeof(BushInstancingRenderPatch), nameof(BeforeRenderBatches));

    internal static void ApplyPatches(Harmony harmony)
    {
        var bushType = AccessTools.TypeByName("BushInstancing");
        if (bushType == null)
            return;

        var renderBatches = AccessTools.Method(bushType, "RenderBatches");
        if (renderBatches == null)
            return;

        harmony.Patch(renderBatches, prefix: PrefixMethod);
    }

    private static void BeforeRenderBatches(MonoBehaviour __instance)
    {
        if (!Plugin.IsEnabled)
            return;

        var settings = Plugin.Instance?.BuildSettingsForSync();
        if (settings == null)
            return;

        InstancingRenderSync.EnsureBushBeforeRender(__instance, settings);
    }
}
