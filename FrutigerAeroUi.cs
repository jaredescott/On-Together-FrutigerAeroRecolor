using UnityEngine;

namespace FrutigerAeroRecolor;

internal static class FrutigerAeroUi
{
    private static Rect _windowRect = new(40f, 40f, 380f, 560f);
    private static Vector2 _scroll;

    internal static void Draw(Plugin plugin)
    {
        _windowRect = GUILayout.Window(
            0xF4A7,
            _windowRect,
            id => DrawWindow(id, plugin),
            $"Frutiger Aero Recolor v{Plugin.PluginVersion}");
    }

    private static void DrawWindow(int id, Plugin plugin)
    {
        _scroll = GUILayout.BeginScrollView(_scroll);

        GUILayout.Label("Press Keypad 5 to toggle this panel.");

        plugin.Enabled = GUILayout.Toggle(plugin.Enabled, "Enabled");
        plugin.PreserveToonShading = GUILayout.Toggle(plugin.PreserveToonShading, "Preserve toon shading");

        plugin.BaseStrength = LabeledSlider("Base strength", plugin.BaseStrength);
        plugin.ShadeStrength = LabeledSlider("Shade strength", plugin.ShadeStrength);
        plugin.BatchStrength = LabeledSlider("Batch strength", plugin.BatchStrength);
        plugin.BuildingStrength = LabeledSlider("Building strength", plugin.BuildingStrength);
        plugin.FoliageStrength = LabeledSlider("Foliage strength", plugin.FoliageStrength);
        plugin.VibrancyMultiplier = LabeledSlider("Vibrancy boost", plugin.VibrancyMultiplier, 1f, 1.6f);

        GUILayout.Space(4f);
        GUILayout.Label($"Cliff face style: {plugin.CliffFaceStyle}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Cream stone"))
            plugin.CliffFaceStyle = CliffFaceStyle.CreamStone;
        if (GUILayout.Button("Soft aqua"))
            plugin.CliffFaceStyle = CliffFaceStyle.SoftAqua;
        if (GUILayout.Button("Warm brown"))
            plugin.CliffFaceStyle = CliffFaceStyle.WarmBrown;
        GUILayout.EndHorizontal();

        GUILayout.Space(6f);
        GUILayout.Label("Building glass blue (windows)");
        plugin.BuildingGlassBlue = DrawColorField(plugin.BuildingGlassBlue);

        GUILayout.Label("Building structure white (wall bands)");
        plugin.BuildingStructureWhite = DrawColorField(plugin.BuildingStructureWhite);

        GUILayout.Label("Path / footpath white");
        plugin.PathWhite = DrawColorField(plugin.PathWhite);

        GUILayout.Space(8f);
        if (GUILayout.Button("Eye Comfort Preset", GUILayout.Height(28f)))
            plugin.ApplyEyeComfortPreset();

        if (GUILayout.Button("Reset to Recommended Defaults (vibrant)", GUILayout.Height(28f)))
            plugin.ResetToRecommendedDefaults();

        if (GUILayout.Button("Apply + Recolor Now", GUILayout.Height(32f)))
            plugin.ApplyFromUi();

        GUILayout.Space(8f);
        GUILayout.Label("Rollback to older version:", GUI.skin.box);
        GUILayout.Label(
            "1. Close the game.\n" +
            "2. Rename plugins\\J4EGER-FrutigerAeroRecolor to J4EGER-FrutigerAeroRecolor-OFF.\n" +
            "3. Copy plugins\\J4EGER-FrutigerAeroRecolor-v1.4.2 (or older) to plugins\\J4EGER-FrutigerAeroRecolor.\n" +
            "4. Or restore DLL from releases\\FrutigerAeroRecolor-v1.x.x\\ into J4EGER\\ folder.\n" +
            "5. Restart On-Together.");

        GUILayout.EndScrollView();
        GUI.DragWindow();
    }

    private static float LabeledSlider(string label, float value, float min = 0f, float max = 1f)
    {
        GUILayout.Label($"{label}: {value:0.00}");
        return GUILayout.HorizontalSlider(value, min, max);
    }

    private static Color DrawColorField(Color color)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"#{ColorUtility.ToHtmlStringRGB(color)}", GUILayout.Width(80f));
        var r = GUILayout.HorizontalSlider(color.r, 0f, 1f);
        var g = GUILayout.HorizontalSlider(color.g, 0f, 1f);
        var b = GUILayout.HorizontalSlider(color.b, 0f, 1f);
        GUILayout.EndHorizontal();
        return new Color(r, g, b, color.a);
    }
}
