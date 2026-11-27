using System.Diagnostics;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using MagicaCloth2;
using UnityEngine;
using UnityEngine.Rendering;

namespace ALMMD;

[BepInPlugin(Id, "AL MMD", "0.4.3")]
[BepInProcess("AmanatsuLocation.exe")]
[BepInDependency("voxelize.amanatsu.heels", BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BasePlugin
{
    public const string Id = "local.al.mmd";
    internal static ManualLogSource Logger;
    internal static DanceSession Session;
    internal static SongCatalog Songs;
    internal static ConfigEntry<float> Volume;
    private Harmony harmony;
    private static MmdBehaviour behaviour;
    private Il2CppSystem.Action<ScriptableRenderContext, Il2CppSystem.Collections.Generic.List<Camera>> beforeRender;

    public override void Load()
    {
        Logger = Log;
        DanceSettings.Bind(Config);
        Songs = new SongCatalog(Config, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        Volume = Config.Bind("Girls", "MusicVolume", 0.8f, new ConfigDescription("Dance music volume.", new AcceptableValueRange<float>(0, 1)));
        Session = new DanceSession();
        harmony = new Harmony(Id);
        harmony.Patch(AccessTools.Method(typeof(AL.CommandUI), "HideProc"), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.CommandHide)));
        harmony.Patch(AccessTools.Method(typeof(AL.CommandUI), "OnClickCommonCondition"), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.CommandHide)));
        harmony.Patch(AccessTools.Method(typeof(AL.ActionMapCameraManager), "LateUpdateProc"), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.MapCamera)));
        harmony.Patch(AccessTools.Method(typeof(Character.Human), "LateUpdate"), postfix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.AfterHumanLate)));
        foreach (string property in new[] { "Visible", "VisibleImmediate", "VisibleConditions" })
            harmony.Patch(AccessTools.PropertySetter(typeof(Manager.Game.CharacterGrid), property),
                prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.DancerVisibility)));
        // AL schedules Human.LateUpdate in R3 PostLateUpdate, after Magica has
        // already sampled the skeleton. Move just the dancer's entire native
        // late pass before that sample, including the existing plugin postfixes.
        harmony.Patch(AccessTools.Method(typeof(ClothManager), "ClothUpdate"),
            prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.BeforeClothSimulation)) { priority = Priority.Last });
        // Gate the scheduled CALLER, not Human.LateUpdate itself: suppressing the
        // latter would still run other plugins' postfixes a second time.
        harmony.Patch(AccessTools.Method(typeof(Character.Human.__c), "__ctor_b__34_2"),
            prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.ScheduledHumanLate)));
        harmony.Patch(AccessTools.Method(typeof(ILLGAMES.Unity.FaceBlendShape), "OnLateUpdate"), postfix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.AfterFaceLate)));
        // Both AL_Heels timing paths converge here. Keep its other actors active,
        // but let the dance own the selected actor's pelvis, legs and feet.
        var heelsType = AccessTools.TypeByName("ALHeels.HeelsController");
        var heelsApply = heelsType == null ? null : AccessTools.Method(heelsType, "ApplyTo", new[] { typeof(Character.Human) });
        if (heelsApply != null)
            harmony.Patch(heelsApply, prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.HeelsApply)));
        // New sources that deliberately bypass pause must also be kept silent during the dance.
        foreach (var method in typeof(AudioSource).GetMethods(BindingFlags.Public | BindingFlags.Instance))
            if (method.Name is "Play" or "PlayOneShot" or "PlayDelayed" or "PlayScheduled")
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.AudioPlay)));
        beforeRender = (Action<ScriptableRenderContext, Il2CppSystem.Collections.Generic.List<Camera>>)((_, _) => Session?.BeforeRender());
        RenderPipelineManager.add_beginContextRendering(beforeRender);
        behaviour = AddComponent<MmdBehaviour>();
        Log.LogInfo("AL MMD 0.4.3: optional free camera and player dancer; " + Songs.ChoiceSummary);
    }

    public override bool Unload()
    {
        Session?.Dispose(); Session = null;
        if (beforeRender != null) RenderPipelineManager.remove_beginContextRendering(beforeRender);
        harmony?.UnpatchSelf();
        if (behaviour) behaviour.StartCoroutine(PlacementClothes.ReleaseAfterFrame(behaviour).WrapToIl2Cpp());
        return true;
    }

    internal static void ReleasePlacementAssetsAfterFrame() =>
        behaviour.StartCoroutine(PlacementClothes.ReleaseAfterFrame().WrapToIl2Cpp());

    internal static void LogError(string context, Exception ex) => Logger?.LogError(context + ": " + ex);
}

public sealed class MmdBehaviour : MonoBehaviour
{
    public MmdBehaviour(IntPtr pointer) : base(pointer) { }
    public void Update() => Plugin.Session?.Update();
    public void OnGUI() => Plugin.Session?.DrawSongChoice();
    public void OnDestroy() => Plugin.Session?.Dispose();
    public void OnApplicationQuit() => Plugin.Session?.Dispose();
}

internal static class Hooks
{
    public static bool CommandHide() => Plugin.Session?.Busy != true;
    public static bool MapCamera() => Plugin.Session?.Dancing != true && Plugin.Session?.Selecting != true;
    public static void BeforeClothSimulation() => Plugin.Session?.BeforeClothSimulation();
    public static bool ScheduledHumanLate(Character.Human __1) => Plugin.Session?.ShouldRunScheduledHumanLate(__1) != false;
    public static void AfterHumanLate(Character.Human __instance) => Plugin.Session?.AfterHumanLate(__instance);
    public static void DancerVisibility(Manager.Game.CharacterGrid __instance, ref bool __0)
    {
        if (Plugin.Session?.OwnsHuman(__instance.Chara) == true) __0 = true;
    }
    public static void AfterFaceLate(ILLGAMES.Unity.FaceBlendShape __instance) => Plugin.Session?.ApplyFace(__instance);
    public static bool HeelsApply(Character.Human __0)
    {
        var session = Plugin.Session;
        return session?.OwnsHuman(__0) != true;
    }
    public static void AudioPlay(AudioSource __instance) => Plugin.Session?.CaptureAudio(__instance);
}
