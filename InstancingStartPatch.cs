using HarmonyLib;
using UnityEngine;

namespace FrutigerAeroRecolor;

internal static class InstancingStartPatch
{
    private static readonly HarmonyMethod PostfixMethod = new(typeof(InstancingStartPatch), nameof(Postfix));

    internal static int ApplyPatches(Harmony harmony)
    {
        var patched = 0;
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var name = assembly.GetName().Name;
            if (name == null || !name.Contains("Assembly-CSharp"))
                continue;

            System.Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (System.Reflection.ReflectionTypeLoadException ex)
            {
                types = ex.Types!;
            }

            foreach (var type in types)
            {
                if (type == null || !type.Name.EndsWith("Instancing", System.StringComparison.Ordinal))
                    continue;
                if (!typeof(MonoBehaviour).IsAssignableFrom(type))
                    continue;

                var start = AccessTools.Method(type, "Start");
                if (start == null)
                    continue;

                harmony.Patch(start, postfix: PostfixMethod);
                patched++;
            }
        }

        return patched;
    }

    internal static void Postfix(MonoBehaviour __instance)
    {
        Plugin.Instance?.OnInstancingStarted(__instance);
    }
}
