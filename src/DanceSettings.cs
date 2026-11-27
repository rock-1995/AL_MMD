using BepInEx.Configuration;
using UnityEngine;

namespace ALMMD;

internal static class DanceSettings
{
    internal static ConfigEntry<KeyCode> ToggleKey;
    internal static ConfigEntry<KeyCode> GazeKey;
    internal static ConfigEntry<GazeMode> Gaze;
    internal static ConfigEntry<bool> FreeCamera;
    internal static ConfigEntry<int> SongsPerPage;
    internal static ConfigEntry<bool> UseMapLighting;

    internal static void Bind(ConfigFile config)
    {
        SongsPerPage = config.Bind("Library", "SongsPerPage", 30,
            new ConfigDescription("Maximum songs per page. Buttons fit the screen and wrap long names. Applies when the song menu opens.",
                new AcceptableValueRange<int>(1, 99)));
        UseMapLighting = config.Bind("Lighting", "UseMapLighting", true,
            "Use map lights and shadow directions during a dance instead of camera fill lights. Applies to the next dance.");
        ToggleKey = config.Bind("Keys", "OpenCancelStop", KeyCode.D,
            new ConfigDescription("Open song menu / clear surface point / switch to native points / cancel / stop. Escape belongs to the game.", new ControlKey(KeyCode.D)));
        GazeKey = config.Bind("Keys", "ToggleGaze", KeyCode.G,
            "During a dance: Automatic/Mmd -> Camera; Camera -> Mmd.");
        Gaze = config.Bind("Dance", "Gaze", GazeMode.Automatic,
            "Automatic: use authored eye motion, otherwise native camera gaze. Mmd: always use VMD. Camera: always look at the camera.");
        FreeCamera = config.Bind("Dance", "FreeCamera", false,
            "Start in front of dancer 0, looking at the waist, then control the camera with the mouse instead of VMD. Applies to the next dance.");
    }

    private sealed class ControlKey : AcceptableValueBase
    {
        private readonly KeyCode fallback;
        public ControlKey(KeyCode fallback) : base(typeof(KeyCode)) { this.fallback = fallback; }
        public override bool IsValid(object value) => value is KeyCode key && key != KeyCode.Escape;
        public override object Clamp(object value) => IsValid(value) ? value : fallback;
        public override string ToDescriptionString() => "# Escape is reserved for the game.";
    }

    internal static bool Pressed(ConfigEntry<KeyCode> key) => key.Value != KeyCode.None && Input.GetKeyDown(key.Value);
    internal static string Label(ConfigEntry<KeyCode> key) => key.Value switch {
        KeyCode.LeftArrow => "Left", KeyCode.RightArrow => "Right",
        KeyCode.PageUp => "PgUp", KeyCode.PageDown => "PgDn", KeyCode.Backspace => "Backspace",
        KeyCode.None => "-", _ => key.Value.ToString()
    };
}
