using AL;
using Character;
using UnityEngine;

namespace ALMMD;

internal sealed class DanceLighting
{
    private readonly RestoreScope state = new();

    public void Capture()
    {
        if (!DanceSettings.UseMapLighting.Value) return;
        var map = Manager.MapManager._instance;
        if (!map) return;
        var mapInfo = map._sceneInfo ? map._sceneInfo.CameraLightInfo : null;
        var cameraInfo = map._cameraLightCtrl ? map._cameraLightCtrl.CameraLightInfo : null;
        var mapLights = new HashSet<IntPtr>();
        ApplyInfo(mapInfo, true, mapLights);
        ApplyInfo(cameraInfo, false, null);
        // Some map character lights also have a view/actor angle correction.
        // Restore their authored local direction once, then stop that writer.
        foreach (var correction in UnityEngine.Object.FindObjectsOfType<CmpCorrectLightAngle>())
        {
            var light = correction.dlight;
            if (!light || !mapLights.Contains(light.Pointer)) continue;
            var transform = correction.LightTrans;
            if (!transform) continue;
            bool enabled = correction.enabled;
            Quaternion rotation = transform.localRotation;
            var captured = correction;
            state.Add(() => {
                if (transform) transform.localRotation = rotation;
                if (captured) captured.enabled = enabled;
            });
            correction.enabled = false;
            transform.localRotation = correction._initRot;
        }
        Plugin.Logger.LogInfo($"Dance lighting: map lights ({mapLights.Count}); camera fill disabled; native shadow settings retained.");
    }

    private void ApplyInfo(MapSceneInfoCtrl.CharaLightInfo info, bool on, HashSet<IntPtr> mapLights)
    {
        if (info?.Infos == null) return;
        foreach (var entry in info.Infos)
        {
            var light = entry?.Light;
            if (!light) continue;
            float intensity = light.intensity;
            bool enabled = light.enabled;
            state.Add(() => { if (light) { light.intensity = intensity; light.enabled = enabled; } });
            light.intensity = on ? entry.Intensity : 0f;
            if (!on) light.enabled = false;
            mapLights?.Add(light.Pointer);
        }
    }

    public void Restore() => state.Restore();
}
