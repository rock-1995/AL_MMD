using System.Collections;
using System.Reflection;
using BepInEx.Configuration;
using Character;
using HarmonyLib;
using UnityEngine;

namespace ALMMD;

// Read AL_Heels once, while preparing. Its live Rig and configuration are never
// changed: restoring the saved transforms also restores its original baseline.
internal sealed class DanceHeels : IDisposable
{
    private readonly List<(Transform Bone, Vector3 Scale)> savedScales = new();
    public string Summary { get; private set; } = "off";

    public void Bake(Human human, IReadOnlyDictionary<string, Transform> bones)
    {
        var plugin = AccessTools.TypeByName("ALHeels.Plugin");
        if (plugin == null) return;
        var settings = AccessTools.TypeByName("ALHeels.ShoeSettings");
        var controller = AccessTools.TypeByName("ALHeels.HeelsController");
        bool enabled = ((ConfigEntry<bool>)Field(plugin, "Enabled").GetValue(null)).Value;
        object heel = enabled ? AccessTools.Method(settings, "For", new[] { typeof(Human) }).Invoke(null, new object[] { human }) : null;
        object tags = AccessTools.Method(settings, "TagsOf", new[] { typeof(Human) }).Invoke(null, new object[] { human });
        var foot = (Vector2?)Field(tags.GetType(), "foot").GetValue(tags);
        float toeScale = (float?)Field(tags.GetType(), "toe").GetValue(tags) ?? 1f;
        float heelScale = (float?)Field(tags.GetType(), "heel").GetValue(tags) ?? 1f;
        float width = foot?.x ?? 1f, height = foot?.y ?? 1f;

        // Existing shoe scalers may already include these multipliers. Recover
        // their cached baseline only when the live value is still that write.
        var rigs = (IDictionary)Field(controller, "Rigs").GetValue(null);
        object live = rigs[human.Pointer];
        Transform[] scalers = live == null ? null : (Transform[])Field(live.GetType(), "Scalers").GetValue(live);
        Vector3[] original = live == null ? null : (Vector3[])Field(live.GetType(), "sBase").GetValue(live);
        Vector3[] written = live == null ? null : (Vector3[])Field(live.GetType(), "sWritten").GetValue(live);
        bool scaled = live != null && (bool)Field(live.GetType(), "scaled").GetValue(live);
        foreach (string name in new[] { "cf_s_foot_L", "cf_s_foot_R", "cf_s_toes_L", "cf_s_toes_R", "cf_s_toed_L", "cf_s_toed_R" })
        {
            if (!bones.TryGetValue(name, out var bone) || !bone) continue;
            Vector3 current = bone.localScale, baseline = current;
            savedScales.Add((bone, current));
            if (scaled)
                for (int i = 0; i < scalers.Length; ++i)
                    if (scalers[i] && scalers[i].Pointer == bone.Pointer && Same(current, written[i]))
                    { baseline = original[i]; break; }
            Vector3 multiplier = name.StartsWith("cf_s_toe", StringComparison.Ordinal)
                ? new Vector3(width * toeScale, height * toeScale, toeScale)
                : new Vector3(width, height, heelScale);
            bone.localScale = Vector3.Scale(baseline, multiplier);
        }
        if (heel == null) return;
        float ankle = (float)Field(heel.GetType(), "Ankle").GetValue(heel);
        float toe = (float)Field(heel.GetType(), "Toe").GetValue(heel);
        float extra = (float)Field(heel.GetType(), "Height").GetValue(heel);
        float dropL = Tilt(bones["cf_j_foot_L"], bones["cf_j_toes_L"], bones["cf_j_leg01_L"], ankle, toe);
        float dropR = Tilt(bones["cf_j_foot_R"], bones["cf_j_toes_R"], bones["cf_j_leg01_R"], ankle, toe);
        float lift = Mathf.Max(0f, Mathf.Max(dropL, dropR)) + extra;
        bones["cf_j_hips"].position += Vector3.up * lift;
        Summary = $"baked ankle={ankle:F1}, toe={toe:F2}, lift={lift:F4}; no runtime heel solver";
    }

    // Same full-heel geometry as AL_Heels.Rig.Tilt; it becomes part of the
    // measured rest geometry, including the foot-to-toe IK lever arm.
    private static float Tilt(Transform foot, Transform toe, Transform knee, float ankle, float roll)
    {
        Vector3 axis = Vector3.Cross(knee.position - foot.position, toe.position - foot.position);
        if (axis.sqrMagnitude < 1e-10f) return 0f;
        axis.Normalize();
        float before = toe.position.y;
        foot.rotation = Quaternion.AngleAxis(ankle, axis) * foot.rotation;
        toe.rotation = Quaternion.AngleAxis(-ankle * roll, axis) * toe.rotation;
        return before - toe.position.y;
    }

    private static FieldInfo Field(Type type, string name) => AccessTools.Field(type, name)
        ?? throw new MissingFieldException(type?.FullName ?? "AL_Heels", name);
    private static bool Same(Vector3 a, Vector3 b) => a.x == b.x && a.y == b.y && a.z == b.z;
    public void Dispose()
    {
        foreach (var state in savedScales)
            if (state.Bone) state.Bone.localScale = state.Scale;
        savedScales.Clear();
    }
}
