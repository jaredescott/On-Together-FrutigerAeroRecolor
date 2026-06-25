using HarmonyLib;
using UnityEngine;

namespace FrutigerAeroRecolor;

internal static class CircleMountainInstancingRenderPatch
{
    private static readonly HarmonyMethod PrefixMethod = new(typeof(CircleMountainInstancingRenderPatch), nameof(BeforeRenderBatches));

    internal static void ApplyPatches(Harmony harmony)
    {
        var mountainType = AccessTools.TypeByName("CircleMountainInstancing");
        if (mountainType == null)
            return;

        var renderBatches = AccessTools.Method(mountainType, "RenderBatches");
        if (renderBatches == null)
            return;

        harmony.Patch(renderBatches, prefix: PrefixMethod);
    }

    private static void BeforeRenderBatches(MonoBehaviour __instance)
    {
        if (!Plugin.IsEnabled)
            return;

        InstancingRenderSync.EnsureCircleMountainBeforeRender(__instance);
    }
}
