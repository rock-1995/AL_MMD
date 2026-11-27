using Character;
using MagicaCloth2;
using UnityEngine;

namespace ALMMD;

internal sealed class DancePhysics
{
    private readonly RestoreScope state = new();
    private readonly List<MagicaCloth> cloths = new();
    private readonly List<DynamicBone> bones = new();

    public void Capture(Human human)
    {
        var root = human.Transform;
        foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            bool before = renderer.updateWhenOffscreen;
            state.Add(() => { if (renderer) renderer.updateWhenOffscreen = before; });
            renderer.updateWhenOffscreen = true;
        }
        foreach (var animator in root.GetComponentsInChildren<Animator>(true))
        {
            var before = animator.cullingMode;
            state.Add(() => { if (animator) animator.cullingMode = before; });
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
        foreach (var cloth in root.GetComponentsInChildren<MagicaCloth>(true))
        {
            if (!cloth || !cloth.isActiveAndEnabled) continue; // Preserve deliberately disabled clothing/physics.
            var data = cloth.SerializeData;
            var culling = data.cullingSettings;
            var update = data.updateMode;
            var cameraMode = culling.cameraCullingMode;
            var distance = culling.distanceCullingLength;
            bool useDistance = distance.use;
            var process = cloth.Process;
            Plugin.Logger.LogInfo($"Dance physics before: {cloth.name}; update={update}; camera={cameraMode}; distance={useDistance}/{distance.value:F1}; culled={process.IsCameraCullingInvisible()}/{process.IsDistanceCullingInvisible()}; suspended={process.GetSuspendCounter()}");
            state.Add(() => {
                if (!cloth) return;
                data.updateMode = update;
                culling.cameraCullingMode = cameraMode;
                distance.use = useDistance;
                cloth.SetParameterChange();
                cloth.ResetCloth();
            });
            // The animation controller is intentionally disabled for the dance.
            // Its visibility/update mode must not govern the cloth simulation.
            data.updateMode = ClothUpdateMode.Normal;
            culling.cameraCullingMode = CullingSettings.CameraCullingMode.Off;
            distance.use = false;
            cloth.SetParameterChange();
            cloth.ResetCloth();
            cloths.Add(cloth);
        }
        foreach (var bone in root.GetComponentsInChildren<DynamicBone>(true))
        {
            if (!bone || !bone.isActiveAndEnabled) continue;
            bool distance = bone.m_DistantDisable;
            var mode = bone.m_UpdateMode;
            Plugin.Logger.LogInfo($"Dance physics before: {bone.name}; DynamicBone mode={mode}; rate={bone.m_UpdateRate:F1}; distance={distance}; culled={bone.m_DistantDisabled}");
            state.Add(() => {
                if (!bone) return;
                bone.m_DistantDisable = distance;
                bone.m_UpdateMode = mode;
                bone.ResetParticlesPosition();
                bone.CheckDistance();
            });
            bone.m_DistantDisable = false;
            // Native Default mode solves once per rendered frame, scaling the
            // integration by deltaTime * the existing authored update rate.
            // Normal/AnimatePhysics can instead call SkipUpdateParticles.
            bone.m_UpdateMode = DynamicBone.UpdateMode.Default;
            bone.ResetParticlesPosition();
            bones.Add(bone);
        }
        Plugin.Logger.LogInfo($"Dance physics: protected Magica={cloths.Count}, DynamicBone={bones.Count}; global solver frequency unchanged.");
    }

    public void Report()
    {
        int camera = 0, distance = 0, suspended = 0, running = 0, dynamic = 0;
        foreach (var cloth in cloths)
        {
            if (!cloth || !cloth.isActiveAndEnabled) continue;
            var process = cloth.Process;
            if (process.IsRunning()) ++running;
            if (process.IsCameraCullingInvisible()) ++camera;
            if (process.IsDistanceCullingInvisible()) ++distance;
            if (process.GetSuspendCounter() > 0) ++suspended;
        }
        foreach (var bone in bones) if (bone && bone.isActiveAndEnabled && bone.IsNeedUpdate()) ++dynamic;
        Plugin.Logger.LogInfo($"Dance physics status: Magica running={running}/{cloths.Count}, cameraCulled={camera}, distanceCulled={distance}, suspended={suspended}; DynamicBone updating={dynamic}/{bones.Count}; Magica frequency={MagicaManager.GetSimulationFrequency()}, maxSteps={MagicaManager.GetMaxSimulationCountPerFrame()}");
    }

    public void Restore()
    {
        state.Restore();
        cloths.Clear(); bones.Clear();
    }
}
