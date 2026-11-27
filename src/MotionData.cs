using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace ALMMD;

// All positions/rotations remain in VMD coordinates. Conversion belongs to the rig/camera.
// Samples take an absolute VMD frame (audio seconds * 30), never accumulated delta time.
public readonly struct BonePose
{
    public readonly Vector3 Position;
    public readonly Quaternion Rotation;
    public BonePose(Vector3 position, Quaternion rotation) { Position = position; Rotation = rotation; }
}

public readonly struct CameraPose
{
    public readonly Vector3 Position; // MMD camera interest/target, not camera world position.
    public readonly Vector3 RotationRadians;
    public readonly float Distance;
    public readonly float FieldOfView; // Degrees.
    public readonly bool Perspective;
    public CameraPose(Vector3 position, Vector3 rotationRadians, float distance, float fieldOfView, bool perspective)
    { Position = position; RotationRadians = rotationRadians; Distance = distance; FieldOfView = fieldOfView; Perspective = perspective; }
}

internal readonly struct Bezier
{
    private readonly float x1, y1, x2, y2;
    private readonly bool linear;
    public Bezier(byte a, byte b, byte c, byte d)
    {
        x1 = a / 127f; y1 = b / 127f; x2 = c / 127f; y2 = d / 127f;
        linear = a == b && c == d;
    }
    // Solve x(t) using bracketed Newton steps; no allocations or sampled-curve drift.
    public float Evaluate(float x)
    {
        if (linear || x <= 0 || x >= 1) return Math.Clamp(x, 0, 1);
        float t = x, lo = 0, hi = 1;
        for (int i = 0; i < 12; i++)
        {
            float u = 1 - t;
            float value = 3 * u * u * t * x1 + 3 * u * t * t * x2 + t * t * t;
            float error = value - x;
            if (MathF.Abs(error) < 0.00001f) break;
            if (error > 0) hi = t; else lo = t;
            float derivative = 3 * u * u * x1 + 6 * u * t * (x2 - x1) + 3 * t * t * (1 - x2);
            float candidate = derivative > 0.000001f ? t - error / derivative : -1;
            t = candidate > lo && candidate < hi ? candidate : (lo + hi) * 0.5f;
        }
        float v = 1 - t;
        return 3 * v * v * t * y1 + 3 * v * t * t * y2 + t * t * t;
    }
}

internal readonly struct BoneKey
{
    public readonly uint Frame;
    public readonly BonePose Pose;
    public readonly Bezier X, Y, Z, R;
    public BoneKey(uint frame, BonePose pose, byte[] p)
    {
        Frame = frame; Pose = pose;
        X = new(p[0], p[4], p[8], p[12]); Y = new(p[1], p[5], p[9], p[13]);
        Z = new(p[2], p[6], p[10], p[14]); R = new(p[3], p[7], p[11], p[15]);
    }
}

internal readonly struct CameraKey
{
    public readonly uint Frame;
    public readonly CameraPose Pose;
    public readonly Bezier X, Y, Z, R, D, F;
    public CameraKey(uint frame, CameraPose pose, byte[] p)
    {
        Frame = frame; Pose = pose;
        X = Curve(p, 0); Y = Curve(p, 4); Z = Curve(p, 8);
        R = Curve(p, 12); D = Curve(p, 16); F = Curve(p, 20);
    }
    private static Bezier Curve(byte[] p, int i) => new(p[i], p[i + 2], p[i + 1], p[i + 3]);
}

internal static class FrameSearch
{
    // Last key at or before frame. At duplicate frames, loading retains the last file entry.
    public static int Lower(uint[] frames, float frame)
    {
        int lo = 0, hi = frames.Length;
        while (lo < hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            if (frames[mid] <= frame) lo = mid + 1; else hi = mid;
        }
        return lo - 1;
    }
    public static float Mix(float a, float b, float t) => a + (b - a) * t;
}

public sealed class BoneTrack
{
    private readonly BoneKey[] keys;
    private readonly uint[] frames;
    public string Name { get; }
    public int Count => keys.Length;
    public uint LastFrame => frames[^1];
    internal BoneTrack(string name, IEnumerable<BoneKey> source)
    {
        Name = name;
        keys = source.GroupBy(k => k.Frame).Select(g => g.Last()).OrderBy(k => k.Frame).ToArray();
        frames = keys.Select(k => k.Frame).ToArray();
    }
    public BonePose Sample(float frame)
    {
        int i = FrameSearch.Lower(frames, frame);
        if (i < 0) return keys[0].Pose;
        if (i >= keys.Length - 1) return keys[^1].Pose;
        ref readonly BoneKey a = ref keys[i]; ref readonly BoneKey b = ref keys[i + 1];
        float t = (frame - a.Frame) / (b.Frame - a.Frame);
        Vector3 p = new(FrameSearch.Mix(a.Pose.Position.X, b.Pose.Position.X, b.X.Evaluate(t)),
            FrameSearch.Mix(a.Pose.Position.Y, b.Pose.Position.Y, b.Y.Evaluate(t)),
            FrameSearch.Mix(a.Pose.Position.Z, b.Pose.Position.Z, b.Z.Evaluate(t)));
        return new(p, Quaternion.Slerp(a.Pose.Rotation, b.Pose.Rotation, b.R.Evaluate(t)));
    }

    // Retain interpolation into/out of an authored direction. A span with
    // two default rotations contains no authored gaze, even if keys exist.
    internal bool HasAuthoredRotation(float frame)
    {
        int i = FrameSearch.Lower(frames, frame);
        if (i < 0) return false;
        static bool Authored(Quaternion q) => q.X * q.X + q.Y * q.Y + q.Z * q.Z > 1e-12f;
        return Authored(keys[i].Pose.Rotation) ||
            i + 1 < keys.Length && Authored(keys[i + 1].Pose.Rotation);
    }
}

public sealed class MorphTrack
{
    private readonly uint[] frames;
    private readonly float[] weights;
    public string Name { get; }
    public int Count => frames.Length;
    public uint LastFrame => frames[^1];
    internal MorphTrack(string name, IEnumerable<(uint Frame, float Weight)> source)
    {
        Name = name;
        var sorted = source.GroupBy(k => k.Frame).Select(g => g.Last()).OrderBy(k => k.Frame).ToArray();
        frames = sorted.Select(k => k.Frame).ToArray(); weights = sorted.Select(k => k.Weight).ToArray();
    }
    public float Sample(float frame)
    {
        int i = FrameSearch.Lower(frames, frame);
        if (i < 0) return weights[0];
        if (i >= frames.Length - 1) return weights[^1];
        return FrameSearch.Mix(weights[i], weights[i + 1], (frame - frames[i]) / (frames[i + 1] - frames[i]));
    }
}

public sealed class SwitchTrack
{
    private readonly uint[] frames;
    private readonly bool[] enabled;
    public int Count => frames.Length;
    public uint LastFrame => frames.Length == 0 ? 0 : frames[^1];
    internal SwitchTrack(IEnumerable<(uint Frame, bool Enabled)> source)
    {
        var sorted = source.GroupBy(k => k.Frame).Select(g => g.Last()).OrderBy(k => k.Frame).ToArray();
        frames = sorted.Select(k => k.Frame).ToArray(); enabled = sorted.Select(k => k.Enabled).ToArray();
    }
    public bool Sample(float frame)
    {
        int i = FrameSearch.Lower(frames, frame);
        return i < 0 || enabled[i];
    }
}

public sealed class CameraTrack
{
    private readonly CameraKey[] keys;
    private readonly uint[] frames;
    public int Count => keys.Length;
    public uint LastFrame => frames.Length == 0 ? 0 : frames[^1];
    internal CameraTrack(IEnumerable<CameraKey> source)
    {
        keys = source.GroupBy(k => k.Frame).Select(g => g.Last()).OrderBy(k => k.Frame).ToArray();
        frames = keys.Select(k => k.Frame).ToArray();
    }
    public CameraPose Sample(float frame)
    {
        if (keys.Length == 0) return new(Vector3.Zero, Vector3.Zero, -45, 30, true);
        int i = FrameSearch.Lower(frames, frame);
        if (i < 0) return keys[0].Pose;
        if (i >= keys.Length - 1) return keys[^1].Pose;
        ref readonly CameraKey a = ref keys[i]; ref readonly CameraKey b = ref keys[i + 1];
        // Adjacent keys are camera cuts, not a one-frame fly-through.
        if (b.Frame - a.Frame <= 1) return a.Pose;
        float t = (frame - a.Frame) / (b.Frame - a.Frame);
        Vector3 p = new(FrameSearch.Mix(a.Pose.Position.X, b.Pose.Position.X, b.X.Evaluate(t)),
            FrameSearch.Mix(a.Pose.Position.Y, b.Pose.Position.Y, b.Y.Evaluate(t)),
            FrameSearch.Mix(a.Pose.Position.Z, b.Pose.Position.Z, b.Z.Evaluate(t)));
        // Preserve unwrapped Euler rotations: e.g. a camera making several deliberate turns.
        return new(p, Vector3.Lerp(a.Pose.RotationRadians, b.Pose.RotationRadians, b.R.Evaluate(t)),
            FrameSearch.Mix(a.Pose.Distance, b.Pose.Distance, b.D.Evaluate(t)),
            FrameSearch.Mix(a.Pose.FieldOfView, b.Pose.FieldOfView, b.F.Evaluate(t)), a.Pose.Perspective);
    }
}

public sealed class VmdMotion
{
    public const float FramesPerSecond = 30f;
    public string ModelName { get; }
    public IReadOnlyDictionary<string, BoneTrack> Bones { get; }
    public IReadOnlyDictionary<string, MorphTrack> Morphs { get; }
    public IReadOnlyDictionary<string, SwitchTrack> Ik { get; }
    public SwitchTrack Visibility { get; }
    public CameraTrack Camera { get; }
    public uint LastFrame { get; }
    public double DurationSeconds => LastFrame / 30.0;
    private VmdMotion(string modelName, Dictionary<string, BoneTrack> bones, Dictionary<string, MorphTrack> morphs,
        Dictionary<string, SwitchTrack> ik, SwitchTrack visibility, CameraTrack camera)
    {
        ModelName = modelName; Bones = new ReadOnlyDictionary<string, BoneTrack>(bones);
        Morphs = new ReadOnlyDictionary<string, MorphTrack>(morphs); Ik = new ReadOnlyDictionary<string, SwitchTrack>(ik);
        Visibility = visibility; Camera = camera;
        uint last = camera.LastFrame;
        foreach (var t in bones.Values) last = Math.Max(last, t.LastFrame);
        foreach (var t in morphs.Values) last = Math.Max(last, t.LastFrame);
        foreach (var t in ik.Values) last = Math.Max(last, t.LastFrame);
        LastFrame = Math.Max(last, visibility.LastFrame);
    }
    public static VmdMotion Load(string path)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var sjis = Encoding.GetEncoding(932);
        using var stream = File.OpenRead(path);
        using var r = new BinaryReader(stream, Encoding.UTF8, false);
        string signature = Encoding.ASCII.GetString(ReadBytes(r, 30)).TrimEnd('\0');
        if (!signature.StartsWith("Vocaloid Motion Data", StringComparison.Ordinal))
            throw new InvalidDataException("Not a VMD motion file: " + path);
        string model = Name(r, signature.Contains("0002", StringComparison.Ordinal) ? 20 : 10, sjis);
        var bones = new Dictionary<string, List<BoneKey>>(StringComparer.Ordinal);
        var morphs = new Dictionary<string, List<(uint, float)>>(StringComparer.Ordinal);
        var ik = new Dictionary<string, List<(uint, bool)>>(StringComparer.Ordinal);
        var visible = new List<(uint, bool)>(); var cameras = new List<CameraKey>();
        uint n = Count(r, 111);
        for (uint i = 0; i < n; i++)
        {
            string name = Name(r, 15, sjis); uint frame = r.ReadUInt32(); Vector3 position = Vector(r);
            var rotation = new Quaternion(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
            float length = rotation.LengthSquared();
            rotation = length > 0.000001f ? Quaternion.Normalize(rotation) : Quaternion.Identity;
            Add(bones, name, new BoneKey(frame, new BonePose(position, rotation), ReadBytes(r, 64)));
        }
        n = OptionalCount(r, 23);
        for (uint i = 0; i < n; i++)
        { string name = Name(r, 15, sjis); Add(morphs, name, (r.ReadUInt32(), r.ReadSingle())); }
        n = OptionalCount(r, 61);
        for (uint i = 0; i < n; i++)
        {
            uint frame = r.ReadUInt32(); float distance = r.ReadSingle(); Vector3 position = Vector(r), rotation = Vector(r);
            byte[] curve = ReadBytes(r, 24); uint fov = r.ReadUInt32(); bool perspective = r.ReadByte() == 0;
            cameras.Add(new CameraKey(frame, new CameraPose(position, rotation, distance, fov, perspective), curve));
        }
        n = OptionalCount(r, 28); Skip(r, n * 28L); // Light track is intentionally not used by this player.
        n = OptionalCount(r, 9); Skip(r, n * 9L); // Self shadow track.
        n = OptionalCount(r, 9);
        for (uint i = 0; i < n; i++)
        {
            uint frame = r.ReadUInt32(); visible.Add((frame, r.ReadByte() != 0)); uint count = Count(r, 21);
            for (uint j = 0; j < count; j++) { string name = Name(r, 20, sjis); Add(ik, name, (frame, r.ReadByte() != 0)); }
        }
        return new VmdMotion(model, bones.ToDictionary(p => p.Key, p => new BoneTrack(p.Key, p.Value), StringComparer.Ordinal),
            morphs.ToDictionary(p => p.Key, p => new MorphTrack(p.Key, p.Value), StringComparer.Ordinal),
            ik.ToDictionary(p => p.Key, p => new SwitchTrack(p.Value), StringComparer.Ordinal), new SwitchTrack(visible), new CameraTrack(cameras));
    }
    private static void Add<T>(Dictionary<string, List<T>> dict, string name, T value)
    { if (!dict.TryGetValue(name, out var list)) dict.Add(name, list = new List<T>()); list.Add(value); }
    private static Vector3 Vector(BinaryReader r) => new(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
    private static string Name(BinaryReader r, int count, Encoding encoding)
    { byte[] b = ReadBytes(r, count); int end = Array.IndexOf(b, (byte)0); return encoding.GetString(b, 0, end < 0 ? b.Length : end); }
    private static uint OptionalCount(BinaryReader r, int minimumRecordBytes) =>
        r.BaseStream.Position == r.BaseStream.Length ? 0 : Count(r, minimumRecordBytes);
    private static uint Count(BinaryReader r, int minimumRecordBytes)
    {
        uint count = r.ReadUInt32();
        if ((long)count * minimumRecordBytes > r.BaseStream.Length - r.BaseStream.Position)
            throw new InvalidDataException("Truncated VMD section.");
        return count;
    }
    private static void Skip(BinaryReader r, long count)
    { if (count > r.BaseStream.Length - r.BaseStream.Position) throw new EndOfStreamException(); r.BaseStream.Seek(count, SeekOrigin.Current); }
    private static byte[] ReadBytes(BinaryReader r, int count)
    { byte[] b = r.ReadBytes(count); if (b.Length != count) throw new EndOfStreamException(); return b; }
}

public sealed class WavAudio
{
    public int Channels { get; }
    public int SampleRate { get; }
    public float[] Samples { get; } // Interleaved, ready for Unity AudioClip.SetData.
    public int FrameCount => Samples.Length / Channels;
    public double DurationSeconds => FrameCount / (double)SampleRate;
    private WavAudio(int channels, int sampleRate, float[] samples)
    { Channels = channels; SampleRate = sampleRate; Samples = samples; }
    public static WavAudio Load(string path)
    {
        using var stream = File.OpenRead(path);
        using var r = new BinaryReader(stream, Encoding.ASCII, false);
        if (r.ReadUInt32() != 0x46464952) throw new InvalidDataException("Expected a RIFF WAV file.");
        uint riffSize = r.ReadUInt32();
        if (r.ReadUInt32() != 0x45564157) throw new InvalidDataException("Expected WAVE format.");
        long riffEnd = Math.Min(stream.Length, (long)riffSize + 8);
        ushort format = 0, channels = 0, bits = 0, blockAlign = 0; int rate = 0;
        long dataPosition = -1; int dataSize = 0;
        while (stream.Position + 8 <= riffEnd)
        {
            uint id = r.ReadUInt32(), size = r.ReadUInt32(); long end = stream.Position + size;
            if (end > stream.Length) throw new InvalidDataException("Truncated WAV chunk.");
            if (id == 0x20746d66) // fmt
            {
                if (size < 16) throw new InvalidDataException("Invalid WAV fmt chunk.");
                format = r.ReadUInt16(); channels = r.ReadUInt16(); rate = r.ReadInt32(); r.ReadUInt32();
                blockAlign = r.ReadUInt16(); bits = r.ReadUInt16();
                if (format == 0xfffe && size >= 40)
                {
                    r.ReadUInt16(); r.ReadUInt16(); r.ReadUInt32();
                    byte[] subtype = r.ReadBytes(16);
                    var guid = new Guid(subtype);
                    if (guid == new Guid("00000001-0000-0010-8000-00aa00389b71")) format = 1;
                    else if (guid == new Guid("00000003-0000-0010-8000-00aa00389b71")) format = 3;
                }
            }
            else if (id == 0x61746164 && dataPosition < 0) // data
            { dataPosition = stream.Position; dataSize = checked((int)size); }
            stream.Position = end + (size & 1);
        }
        if (dataPosition < 0 || channels == 0 || rate <= 0 ||
            !((format == 1 && (bits == 16 || bits == 24 || bits == 32)) || (format == 3 && bits == 32)))
            throw new InvalidDataException("WAV must contain PCM16/24/32 or IEEE float32 audio.");
        int bytesPerSample = bits / 8;
        if (blockAlign != channels * bytesPerSample || dataSize % blockAlign != 0)
            throw new InvalidDataException("WAV block alignment is invalid.");
        stream.Position = dataPosition;
        var samples = new float[dataSize / bytesPerSample];
        // Decode in bounded chunks instead of allocating a second entire music-file buffer.
        byte[] buffer = new byte[bytesPerSample * 16384]; int offset = 0;
        while (offset < samples.Length)
        {
            int count = Math.Min(16384, samples.Length - offset), bytes = count * bytesPerSample, read = 0;
            while (read < bytes)
            { int got = stream.Read(buffer, read, bytes - read); if (got == 0) throw new EndOfStreamException(); read += got; }
            for (int i = 0, p = 0; i < count; i++, p += bytesPerSample)
            {
                float value;
                if (format == 3) value = BitConverter.ToSingle(buffer, p);
                else if (bits == 16) value = (short)(buffer[p] | buffer[p + 1] << 8) / 32768f;
                else if (bits == 24)
                { int value24 = buffer[p] | buffer[p + 1] << 8 | buffer[p + 2] << 16; value = (value24 << 8 >> 8) / 8388608f; }
                else value = BitConverter.ToInt32(buffer, p) / 2147483648f;
                samples[offset + i] = value;
            }
            offset += count;
        }
        return new WavAudio(channels, rate, samples);
    }
}
