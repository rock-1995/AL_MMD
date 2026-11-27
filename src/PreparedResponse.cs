using System.Numerics;
using System.Reflection;

namespace ALMMD;

// A compact pose-response atlas sampled from AL's native animation assets.
// Matching happens only during preprocessing, on the worker. The main joints
// keep their authored MMD motion; these channels only restore skin/cloth support.
internal static class NativeResponse
{
    private sealed class Group
    {
        public string[] Inputs, Targets;
        public int Dimensions, Count;
        public float[] Features;
        public ResponsePose[] Poses;
    }
    private static readonly Lazy<Group[]> Atlas = new(Load);
    public static IEnumerable<string> Inputs => Atlas.Value.SelectMany(g => g.Inputs).Distinct();

    private static Group[] Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ALMMD.NativeResponse.bin")
            ?? throw new InvalidOperationException("AL native pose response data is missing.");
        using var reader = new BinaryReader(stream);
        string ReadName() => System.Text.Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
        string[] ReadNames() { var a = new string[reader.ReadInt32()]; for (int i = 0; i < a.Length; i++) a[i] = ReadName(); return a; }
        var groups = new Group[reader.ReadInt32()];
        for (int g = 0; g < groups.Length; g++)
        {
            var group = new Group { Inputs = ReadNames(), Targets = ReadNames(), Dimensions = reader.ReadInt32(), Count = reader.ReadInt32() };
            group.Features = new float[group.Count * group.Dimensions];
            for (int i = 0; i < group.Features.Length; i++) group.Features[i] = reader.ReadSingle();
            group.Poses = new ResponsePose[group.Count * group.Targets.Length];
            for (int i = 0; i < group.Poses.Length; i++)
                group.Poses[i] = new ResponsePose {
                    Position = new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                    Rotation = new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                    Scale = new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())
                };
            groups[g] = group;
        }
        return groups;
    }

    public static void Build(Dictionary<string, Quaternion[]> samples, Dictionary<string, ResponseCurve> output, CancellationToken cancel)
    {
        foreach (var group in Atlas.Value)
        {
            var inputs = group.Inputs.Select(n => samples[n]).ToArray();
            int count = (inputs[0].Length + 1) / 2;
            var curves = group.Targets.Select(_ => new ResponsePose[count]).ToArray();
            var feature = new float[group.Dimensions];
            var nearest = new int[9]; var distance = new float[9]; var weights = new float[8];
            for (int frame = 0; frame < count; frame++)
            {
                if ((frame & 31) == 0) cancel.ThrowIfCancellationRequested();
                int cursor = 0;
                for (int i = 0; i < inputs.Length; i++)
                {
                    Quaternion q = Quaternion.Normalize(inputs[i][frame * 2]);
                    if (group.Inputs[i].StartsWith("cf_j_thigh00_", StringComparison.Ordinal))
                        Put(Vector3.Transform(Vector3.UnitY, q), feature, ref cursor);
                    else
                    {
                        Put(Vector3.Transform(Vector3.UnitX, q), feature, ref cursor);
                        Put(Vector3.Transform(Vector3.UnitY, q), feature, ref cursor);
                    }
                }
                Array.Fill(distance, float.PositiveInfinity);
                for (int pose = 0; pose < group.Count; pose++)
                {
                    float d = 0f; int offset = pose * group.Dimensions;
                    for (int k = 0; k < feature.Length; k++) { float a = feature[k] - group.Features[offset + k]; d += a * a; }
                    if (d >= distance[8]) continue;
                    int slot = 8;
                    while (slot > 0 && d < distance[slot - 1]) { distance[slot] = distance[slot - 1]; nearest[slot] = nearest[slot - 1]; slot--; }
                    distance[slot] = d; nearest[slot] = pose;
                }
                float radius = distance[8] + 1e-8f, sum = 0f;
                for (int k = 0; k < 8; k++) { float t = 1f - distance[k] / radius; weights[k] = t * t / (distance[k] + 1e-8f); sum += weights[k]; }
                for (int k = 0; k < 8; k++) weights[k] /= sum;
                for (int target = 0; target < curves.Length; target++)
                {
                    ResponsePose result = default;
                    Quaternion reference = group.Poses[nearest[0] * curves.Length + target].Rotation;
                    for (int k = 0; k < 8; k++)
                    {
                        var pose = group.Poses[nearest[k] * curves.Length + target]; float weight = weights[k];
                        result.Position += pose.Position * weight; result.Scale += pose.Scale * weight;
                        result.Rotation += pose.Rotation * (Quaternion.Dot(reference, pose.Rotation) < 0f ? -weight : weight);
                    }
                    result.Rotation = Quaternion.Normalize(result.Rotation);
                    curves[target][frame] = result;
                }
            }
            for (int i = 0; i < curves.Length; i++) output.Add(group.Targets[i], new ResponseCurve(curves[i]));
        }
    }
    private static void Put(Vector3 v, float[] target, ref int i) { target[i++] = v.X; target[i++] = v.Y; target[i++] = v.Z; }
}

internal struct ResponsePose
{
    public Vector3 Position, Scale;
    public Quaternion Rotation;
}

internal sealed class ResponseCurve
{
    private readonly ResponsePose[] poses;
    public ResponseCurve(ResponsePose[] poses) { this.poses = poses; }
    public void Apply(UnityEngine.Transform bone, float frame)
    {
        float time = Math.Clamp(frame, 0f, poses.Length - 1); int i = (int)time;
        ResponsePose a = poses[i], b = poses[Math.Min(i + 1, poses.Length - 1)]; float t = time - i;
        Vector3 p = Vector3.Lerp(a.Position, b.Position, t), s = Vector3.Lerp(a.Scale, b.Scale, t);
        Quaternion q = Quaternion.Slerp(a.Rotation, b.Rotation, t);
        bone.localPosition = new(p.X, p.Y, p.Z); bone.localRotation = new(q.X, q.Y, q.Z, q.W); bone.localScale = new(s.X, s.Y, s.Z);
    }
}
