namespace ALMMD;

internal enum GazeMode { Automatic, Mmd, Camera }

internal sealed class PreparedGaze
{
    public readonly BoneTrack Both, Left, Right;
    private readonly byte[] coverage;
    private const float FrameStep = .5f;

    public PreparedGaze(VmdMotion motion, VmdMotion face)
    {
        BoneTrack Find(string name)
        {
            if (face != null && face.Bones.TryGetValue(name, out var track)) return track;
            return motion.Bones.TryGetValue(name, out track) ? track : null;
        }
        Both = Find("両目"); Left = Find("左目"); Right = Find("右目");
        uint last = Math.Max(motion.LastFrame, face?.LastFrame ?? 0);
        coverage = new byte[checked((int)Math.Ceiling(last / FrameStep) + 1)];
        for (int i = 0; i < coverage.Length; i++)
        {
            float frame = i * FrameStep;
            bool both = Both?.HasAuthoredRotation(frame) == true;
            coverage[i] = (byte)((both || Left?.HasAuthoredRotation(frame) == true ? 1 : 0) |
                (both || Right?.HasAuthoredRotation(frame) == true ? 2 : 0));
        }
    }

    public bool UsesMmd(float frame, bool left) =>
        (coverage[Math.Clamp((int)(frame / FrameStep), 0, coverage.Length - 1)] & (left ? 1 : 2)) != 0;

    public static GazeMode Toggle(GazeMode mode) => mode == GazeMode.Camera ? GazeMode.Mmd : GazeMode.Camera;
}
