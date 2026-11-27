using UnityEngine;
using ILLGAMES.Unity.Animations;

namespace ALMMD;

public sealed partial class DanceRig
{
    private readonly List<EyeBinding> eyeBindings = new();
    private PreparedGaze gaze;
    private EyeLookCalc nativeEyes;
    private EyeLookController eyeController;
    private int targetEyePattern = -1, savedEyePattern;
    private Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<byte> savedEyeAngles;
    internal void SetGaze(PreparedGaze value) => gaze = value;
    private sealed class EyeBinding
    {
        public Transform Eye, Head;
        public BoneTrack Both, Individual;
        public Quaternion NativeRest, Saved;
        public bool Left;
    }
    private void BindEyes()
    {
        gaze ??= new PreparedGaze(motion, facial);
        eyeController = human.Face?.eyeLookCtrl;
        nativeEyes = eyeController ? eyeController.EyeLookScript : null;
        if (nativeEyes)
        {
            savedEyePattern = nativeEyes._nowPtnNo;
            savedEyeAngles = nativeEyes.SaveAngle();
            for (int i = 0; i < nativeEyes.EyeTypeStates.Length; i++)
                if (nativeEyes.EyeTypeStates[i].LookType == EYE_LOOK_TYPE.TARGET) { targetEyePattern = i; break; }
        }
        foreach (var eye in root.GetComponentsInChildren<Transform>(true))
        {
            bool left = eye.name == "EyeTargetL";
            if (!left && eye.name != "EyeTargetR") continue;
            var both = gaze.Both; var individual = left ? gaze.Left : gaze.Right;
            // Measured from the native head prefab; the socket's opposite
            // orientation is a separate shape transform and remains intact.
            eyeBindings.Add(new EyeBinding { Eye = eye, Head = Require("cf_j_head"), Both = both, Individual = individual, Left = left,
                Saved = eye.localRotation, NativeRest = left
                    ? new Quaternion(-.064110711f, .152999267f, -.009947196f, .986094415f)
                    : new Quaternion(-.064110711f, -.152999401f, .009947239f, .986094356f) });
        }
    }
    internal void ApplyGaze(double seconds, GazeMode mode, Transform camera)
    {
        float frame = (float)(seconds * 30);
        bool nativeLeft = mode == GazeMode.Camera || mode == GazeMode.Automatic && !gaze.UsesMmd(frame, true);
        bool nativeRight = mode == GazeMode.Camera || mode == GazeMode.Automatic && !gaze.UsesMmd(frame, false);
        bool nativeApplied = (nativeLeft || nativeRight) && nativeEyes && camera && targetEyePattern >= 0;
        if (nativeApplied)
            nativeEyes.EyeUpdateCalc(camera.position, targetEyePattern, Time.deltaTime, eyeController.MicroSaccade, false);
        foreach (var binding in eyeBindings)
        {
            if (!binding.Eye || !binding.Head) continue;
            if (nativeApplied && (binding.Left ? nativeLeft : nativeRight)) continue;
            bool useNative = binding.Left ? nativeLeft : nativeRight;
            Quaternion q = useNative || binding.Both == null ? Quaternion.identity : ConvertRotation(binding.Both.Sample(frame).Rotation);
            if (!useNative && binding.Individual != null) q *= ConvertRotation(binding.Individual.Sample(frame).Rotation);
            Quaternion basis = Quaternion.Inverse(binding.Eye.parent.rotation) * binding.Head.rotation;
            binding.Eye.localRotation = basis * q * Quaternion.Inverse(basis) * binding.NativeRest;
        }
    }
    private void RestoreEyes()
    {
        if (nativeEyes)
        {
            nativeEyes._nowPtnNo = savedEyePattern;
            if (savedEyeAngles != null) nativeEyes.LoadAngle(savedEyeAngles);
        }
        foreach (var binding in eyeBindings) if (binding.Eye) binding.Eye.localRotation = binding.Saved;
        eyeBindings.Clear();
        nativeEyes = null; eyeController = null; savedEyeAngles = null; gaze = null; targetEyePattern = -1;
    }
}
