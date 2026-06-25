using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrutigerAeroRecolor;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "io.j4eger.ontogether.frutigeraerorecolor";
    public const string PluginName = "FrutigerAeroRecolor";
    public const string PluginVersion = "1.5.10";

    internal static Plugin? Instance { get; private set; }
    internal static bool IsEnabled => Instance != null && Instance.Enabled;
    internal EnvironmentRecolorer? Recolorer => _recolorer;

    private ConfigEntry<bool> _enabled = null!;
    private ConfigEntry<bool> _preserveToonShading = null!;
    private ConfigEntry<float> _baseStrength = null!;
    private ConfigEntry<float> _shadeStrength = null!;
    private ConfigEntry<float> _batchStrength = null!;
    private ConfigEntry<float> _buildingStrength = null!;
    private ConfigEntry<float> _foliageStrength = null!;
    private ConfigEntry<float> _vibrancyMultiplier = null!;
    private ConfigEntry<CliffFaceStyle> _cliffFaceStyle = null!;
    private ConfigEntry<string> _buildingGlassBlue = null!;
    private ConfigEntry<string> _buildingStructureWhite = null!;
    private ConfigEntry<string> _pathWhite = null!;
    private ConfigEntry<bool> _skipPlayers = null!;
    private ConfigEntry<bool> _verbose = null!;

    private Harmony? _harmony;
    private EnvironmentRecolorer? _recolorer;
    private bool _showUi;

    internal bool Enabled { get => _enabled.Value; set => _enabled.Value = value; }
    internal bool PreserveToonShading { get => _preserveToonShading.Value; set => _preserveToonShading.Value = value; }
    internal float BaseStrength { get => _baseStrength.Value; set => _baseStrength.Value = value; }
    internal float ShadeStrength { get => _shadeStrength.Value; set => _shadeStrength.Value = value; }
    internal float BatchStrength { get => _batchStrength.Value; set => _batchStrength.Value = value; }
    internal float BuildingStrength { get => _buildingStrength.Value; set => _buildingStrength.Value = value; }
    internal float FoliageStrength { get => _foliageStrength.Value; set => _foliageStrength.Value = value; }
    internal float VibrancyMultiplier { get => _vibrancyMultiplier.Value; set => _vibrancyMultiplier.Value = value; }
    internal CliffFaceStyle CliffFaceStyle { get => _cliffFaceStyle.Value; set => _cliffFaceStyle.Value = value; }

    internal Color BuildingGlassBlue
    {
        get => ParseColor(_buildingGlassBlue.Value, new Color(0.30f, 0.86f, 0.98f));
        set => _buildingGlassBlue.Value = "#" + ColorUtility.ToHtmlStringRGB(value);
    }

    internal Color BuildingStructureWhite
    {
        get => ParseColor(_buildingStructureWhite.Value, new Color(0.96f, 0.97f, 0.95f));
        set => _buildingStructureWhite.Value = "#" + ColorUtility.ToHtmlStringRGB(value);
    }

    internal Color PathWhite
    {
        get => ParseColor(_pathWhite.Value, new Color(0.94f, 0.97f, 0.99f));
        set => _pathWhite.Value = "#" + ColorUtility.ToHtmlStringRGB(value);
    }

    private void Awake()
    {
        Instance = this;

        _enabled = Config.Bind("General", "Enabled", true, "Apply Frutiger Aero palette to environment materials.");
        _preserveToonShading = Config.Bind("General", "PreserveToonShading", true, "Keep cel-shading detail by tinting base and shade colors separately.");
        _baseStrength = Config.Bind("General", "BaseStrength", 0.85f, "How strongly to tint base colors (0-1).");
        _shadeStrength = Config.Bind("General", "ShadeStrength", 0.58f, "How strongly to tint shade and ramp colors (0-1).");
        _batchStrength = Config.Bind("General", "BatchStrength", 0.75f, "How strongly to remap instanced batch colors (0-1).");
        _buildingStrength = Config.Bind("Building", "BuildingStrength", 1.0f, "How strongly to recolor the circular building (0-1).");
        _foliageStrength = Config.Bind("Foliage", "FoliageStrength", 0.82f, "How strongly to recolor grass and tree foliage (0-1).");
        _vibrancyMultiplier = Config.Bind("General", "VibrancyMultiplier", 1.05f, "Saturation/brightness boost for greens, blues, and building glass (1.0 = off).");
        _cliffFaceStyle = Config.Bind("Terrain", "CliffFaceStyle", CliffFaceStyle.CreamStone, "Cliff and terrace wall color style.");
        _buildingGlassBlue = Config.Bind("Building", "BuildingGlassBlue", "#4DDBFA", "Hex color for CircleMountain windows (MountainMat / submesh 0).");
        _buildingStructureWhite = Config.Bind("Building", "BuildingStructureWhite", "#F5F8F2", "Hex color for CircleMountain wall bands (TopMat / submesh 1).");
        _pathWhite = Config.Bind("Paths", "PathWhite", "#F0F7FC", "Hex color for footpaths, deck lines, and walkways.");
        _skipPlayers = Config.Bind("General", "SkipPlayers", true, "Do not recolor player avatars.");
        _verbose = Config.Bind("Debug", "VerboseLogging", false, "Log each material that gets recolored.");

        _recolorer = new EnvironmentRecolorer(Logger, BuildSettings(), _skipPlayers.Value, _verbose.Value);

        _harmony = new Harmony(PluginGuid);
        var patchedStarts = InstancingStartPatch.ApplyPatches(_harmony);
        BushInstancingRenderPatch.ApplyPatches(_harmony);
        CircleMountainInstancingRenderPatch.ApplyPatches(_harmony);
        Logger.LogInfo($"Harmony: patched {patchedStarts} Instancing.Start methods + BushInstancing.RenderBatches + CircleMountainInstancing.RenderBatches.");

        SceneManager.sceneLoaded += OnSceneLoaded;

        if (_enabled.Value)
            StartCoroutine(_recolorer.RecolorWhenReady());

        Logger.LogInfo($"{PluginName} {PluginVersion} loaded. Press Keypad 5 for settings UI.");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Keypad5))
            _showUi = !_showUi;
    }

    private void OnGUI()
    {
        if (!_showUi)
            return;

        FrutigerAeroUi.Draw(this);
    }

    internal void ApplyFromUi()
    {
        var settings = BuildSettings();
        _recolorer?.UpdateSettings(settings);
        Config.Save();

        if (_enabled.Value)
            _recolorer?.TriggerRecolor();
    }

    internal void ResetToRecommendedDefaults()
    {
        Enabled = true;
        PreserveToonShading = true;
        BaseStrength = 0.85f;
        ShadeStrength = 0.58f;
        BatchStrength = 0.75f;
        BuildingStrength = 1.0f;
        FoliageStrength = 0.95f;
        VibrancyMultiplier = 1.15f;
        CliffFaceStyle = CliffFaceStyle.CreamStone;
        BuildingGlassBlue = new Color(0.30f, 0.86f, 0.98f);
        BuildingStructureWhite = new Color(0.96f, 0.97f, 0.95f);
        PathWhite = new Color(0.94f, 0.97f, 0.99f);
        ApplyFromUi();
    }

    internal void ApplyEyeComfortPreset()
    {
        Enabled = true;
        PreserveToonShading = true;
        BaseStrength = 0.85f;
        ShadeStrength = 0.58f;
        BatchStrength = 0.72f;
        BuildingStrength = 1.0f;
        FoliageStrength = 0.82f;
        VibrancyMultiplier = 1.05f;
        CliffFaceStyle = CliffFaceStyle.CreamStone;
        BuildingGlassBlue = new Color(0.30f, 0.86f, 0.98f);
        BuildingStructureWhite = new Color(0.96f, 0.97f, 0.95f);
        PathWhite = new Color(0.94f, 0.97f, 0.99f);
        ApplyFromUi();
    }

    internal void OnInstancingStarted(MonoBehaviour instancer)
    {
        if (!_enabled.Value || _recolorer == null)
            return;

        _recolorer.UpdateSettings(BuildSettings());
        _recolorer.RecolorSingleInstancer(instancer);
    }

    internal RecolorSettings BuildSettingsForSync() => BuildSettings();

    private RecolorSettings BuildSettings()
    {
        return new RecolorSettings
        {
            PreserveToonShading = _preserveToonShading.Value,
            BaseStrength = _baseStrength.Value,
            ShadeStrength = _shadeStrength.Value,
            BatchStrength = _batchStrength.Value,
            BuildingStrength = _buildingStrength.Value,
            FoliageStrength = _foliageStrength.Value,
            VibrancyMultiplier = _vibrancyMultiplier.Value,
            CliffFaceStyle = _cliffFaceStyle.Value,
            BuildingGlassBlue = BuildingGlassBlue,
            BuildingStructureWhite = BuildingStructureWhite,
            PathWhite = PathWhite,
        };
    }

    private static Color ParseColor(string hex, Color fallback) =>
        ColorUtility.TryParseHtmlString(hex, out var color) ? color : fallback;

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        _harmony?.UnpatchSelf();
        MaterialPropertyUtility.ClearCache();
        Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_enabled.Value)
            return;

        _recolorer = new EnvironmentRecolorer(Logger, BuildSettings(), _skipPlayers.Value, _verbose.Value);
        _recolorer.OnSceneLoaded(scene, mode);
    }
}
