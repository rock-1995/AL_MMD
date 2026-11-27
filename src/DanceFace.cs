using Character;
using ILLGAMES.Unity;
using UnityEngine;

namespace ALMMD;

// Uses AL's own pattern tables for every affected renderer (face, eyelids,
// eyelashes, teeth and tongue), so vowel shapes stay consistent across meshes.
internal sealed class DanceFace : IDisposable
{
    private readonly FaceBlendShape controller;
    private readonly List<Group> groups = new();
    private readonly List<(SkinnedMeshRenderer Mesh, int Index, float Weight)> saved = new();
    private readonly HashSet<string> handled = new(StringComparer.Ordinal);
    private readonly VmdMotion motion, facial, lips;
    public string Summary { get; }

    private sealed class Channel
    {
        public MorphTrack Track;
        public int Pattern;
        public bool Open;
        public bool Lip;
        public float Gain;
        public float Weight;
    }
    private sealed class Target
    {
        public SkinnedMeshRenderer Mesh;
        public int[] Close, Open, Indices;
        public float[] Weights;
        public int NeutralIndex;
        public int[] ChannelIndices;
        public float[][] PreparedWeights;
        public void Prepare(PreparedFace curve, CancellationToken cancel)
        {
            var result = new float[Indices.Length][];
            for (int i = 0; i < Indices.Length; ++i)
            {
                cancel.ThrowIfCancellationRequested();
                var contributions = new List<float[]>();
                if (Indices[i] == NeutralIndex) contributions.Add(curve.Neutral);
                for (int c = 0; c < ChannelIndices.Length; ++c)
                    if (ChannelIndices[c] == Indices[i]) contributions.Add(curve.Channels[c]);
                // Most slots can directly share a channel's array across the
                // face, teeth and tongue; allocate only when slots are shared.
                if (contributions.Count == 1) result[i] = contributions[0];
                else if (contributions.Count > 1)
                {
                    var values = result[i] = new float[curve.Neutral.Length];
                    foreach (var input in contributions)
                        for (int sample = 0; sample < values.Length; ++sample) values[sample] += input[sample];
                }
            }
            PreparedWeights = result;
        }
    }
    private sealed class Group
    {
        public FBSBase Control;
        public bool NeutralOpen;
        public float Gain;
        public PreparedFace Curve;
        public readonly List<Channel> Channels = new();
        public readonly List<Target> Targets = new();
        public void Apply(float frame)
        {
            float neutral;
            if (Curve != null)
            {
                Curve.Locate(frame, out int a, out int b, out float t);
                foreach (var target in Targets)
                {
                    if (!target.Mesh) continue;
                    for (int i = 0; i < target.Indices.Length; ++i)
                    {
                        var values = target.PreparedWeights[i];
                        target.Mesh.SetBlendShapeWeight(target.Indices[i], values == null ? 0f :
                            100f * PreparedFace.Mix(values, a, b, t));
                    }
                }
                return;
            }
            else
            {
                // Only the static preparation preview uses the source tracks.
                float sum = 0;
                foreach (var channel in Channels)
                { channel.Weight = Mathf.Clamp01(channel.Track.Sample(frame)); sum += channel.Weight; }
                neutral = Mathf.Max(0f, 1f - sum);
                if (sum > 1f) foreach (var channel in Channels) channel.Weight /= sum;
            }
            foreach (var target in Targets)
            {
                if (!target.Mesh) continue;
                Array.Clear(target.Weights, 0, target.Weights.Length);
                int baseline = NeutralOpen ? target.Open[0] : target.Close[0];
                if (baseline >= 0) target.Weights[baseline] += neutral;
                foreach (var channel in Channels)
                {
                    int index = channel.Open ? target.Open[channel.Pattern] : target.Close[channel.Pattern];
                    if (index >= 0) target.Weights[index] += channel.Weight;
                }
                foreach (int index in target.Indices)
                    target.Mesh.SetBlendShapeWeight(index, 100f * target.Weights[index]);
            }
        }
    }

    public DanceFace(HumanFace face, VmdMotion motion, VmdMotion facial, VmdMotion lips)
    {
        this.motion = motion; this.facial = facial; this.lips = lips;
        controller = face?.fbsCtrl;
        if (!controller) { Summary = "no native face controller"; return; }
        // Resolve pattern IDs by actual mesh names; do not assume character head
        // variants use the same numeric blendshape slots.
        var eyes = new Group { Control = face.eyesCtrl, NeutralOpen = true, Gain = 1.5f };
        Add(eyes, "_def_cl", "_def_cl", true, false, "まばたき");
        Add(eyes, "_egao_cl", "_egao_cl", true, false, "笑い", "はぅ");
        Add(eyes, "_egao_op", "_egao_cl", true, false, "にっこり");
        Add(eyes, "_winkL", null, true, false, "ウィンク", "ウインク", "ウィンク２", "ウィンク2");
        Add(eyes, "_winkR", null, true, false, "ウィンク右", "ウインク右", "ｳｨﾝｸ２右", "ウィンク２右", "ウィンク2右", "ウィンク右２");
        Add(eyes, "_egao_s_op", null, true, false, "なごみ");
        Add(eyes, "_human_op", null, true, false, "じと目", "ジト目");
        Add(eyes, "_gimon_op", null, true, false, "びっくり", "見開き", "見開く");
        Add(eyes, "_ikari_op", "_def_cl", true, false, "キリッ", "ｷﾘｯ");
        Add(eyes, "_sabisi_op", null, true, false, "悲しい");
        Bind(eyes);

        var brow = new Group { Control = face.eyebrowCtrl, NeutralOpen = true, Gain = 1.7f };
        Add(brow, "_ikari", null, true, false, "真面目", "怒り", "怒る");
        Add(brow, "_komari", null, true, false, "困る", "困る2", "困る２", "困る３");
        Add(brow, "_egao", null, true, false, "にこり");
        Add(brow, "_doki", null, true, false, "上", "驚き");
        Add(brow, "_sabisi", null, true, false, "下");
        Bind(brow);

        var mouth = new Group { Control = face.mouthCtrl, Gain = 1.6f };
        string[] vowels = { "あ", "い", "う", "え", "お" };
        string[] native = { "a", "i", "u", "e", "o" };
        for (int i = 0; i < vowels.Length; ++i)
            Add(mouth, "_vo_" + native[i], "_def_cl", true, true, vowels[i]);
        Add(mouth, "_vo_a", "_def_cl", true, true, "あ２", "あ2");
        Add(mouth, "_vo_i", "_def_cl", true, true, "いー");
        Add(mouth, "_vo_o", "_def_cl", true, true, "お２", "お2");
        Add(mouth, "_def_op", "_def_cl", false, true, "ん");
        Add(mouth, "_egao_s_op", "_egao_s_cl", false, false, "にやり", "にやり２", "にやり2", "口角上げ", "口笑い", "にこ", "にこ２");
        Add(mouth, "_egao_op", "_egao_cl", true, false, "ワ", "わー");
        Add(mouth, "_neko_op", "_neko_cl", false, false, "ω");
        Add(mouth, "_neko_op", "_neko_cl", true, false, "ω□");
        Add(mouth, "_san", "_san", true, false, "▲", "△");
        Add(mouth, "_niko_op", "_egao_cl", true, false, "ニカッ");
        Add(mouth, "_pero", "_pero", true, false, "ぺろっ", "舌だし", "べろ出し");
        Add(mouth, "_sabisi_op", "_sabisi_cl", false, false, "口角下げ");
        Add(mouth, "_vo_o", "_human_cl", false, false, "∧");
        Bind(mouth);

        var present = new[] { motion, facial, lips }.Where(v => v != null)
            .SelectMany(v => v.Morphs.Keys).Distinct().OrderBy(v => v, StringComparer.Ordinal);
        Summary = "mapped=" + string.Join(",", handled.OrderBy(v => v, StringComparer.Ordinal)) +
            "; unmapped=" + string.Join(",", present.Where(v => !handled.Contains(v)));
    }

    private void Add(Group group, string openSuffix, string closeSuffix, bool open, bool mouthSource, params string[] names)
    {
        if (group.Control == null) return;
        int pattern = FindPattern(group.Control, openSuffix, closeSuffix);
        if (pattern < 0) return;
        foreach (string name in names)
        {
            MorphTrack track = null;
            var sources = mouthSource ? new[] { lips, motion, facial } : new[] { facial, motion, lips };
            foreach (var source in sources)
                if (source != null && source.Morphs.TryGetValue(name, out track)) break;
            if (track == null) continue;
            group.Channels.Add(new Channel { Track = track, Pattern = pattern, Open = open,
                Lip = mouthSource, Gain = mouthSource && open ? 2f : group.Gain });
            handled.Add(name);
        }
    }

    private static int FindPattern(FBSBase control, string openSuffix, string closeSuffix)
    {
        var targets = control.FBSTarget;
        if (targets == null) return -1;
        foreach (var target in targets)
        {
            var renderer = target?.GetSkinnedMeshRenderer();
            var mesh = renderer ? renderer.sharedMesh : null;
            if (!mesh || target.PtnSet == null) continue;
            for (int i = 0; i < target.PtnSet.Length; ++i)
            {
                var pattern = target.PtnSet[i];
                if (pattern.Open < 0 || pattern.Open >= mesh.blendShapeCount) continue;
                if (!mesh.GetBlendShapeName(pattern.Open).EndsWith(openSuffix, StringComparison.Ordinal)) continue;
                if (closeSuffix != null && (pattern.Close < 0 || pattern.Close >= mesh.blendShapeCount ||
                    !mesh.GetBlendShapeName(pattern.Close).EndsWith(closeSuffix, StringComparison.Ordinal))) continue;
                return i;
            }
        }
        return -1;
    }

    private void Bind(Group group)
    {
        if (group.Channels.Count == 0) return;
        foreach (var input in group.Control.FBSTarget)
        {
            var renderer = input?.GetSkinnedMeshRenderer();
            var mesh = renderer ? renderer.sharedMesh : null;
            if (!mesh || input.PtnSet == null) continue;
            int count = mesh.blendShapeCount;
            var target = new Target { Mesh = renderer, Weights = new float[count],
                Open = new int[input.PtnSet.Length], Close = new int[input.PtnSet.Length] };
            var owned = new HashSet<int>();
            for (int i = 0; i < input.PtnSet.Length; ++i)
            {
                int o = input.PtnSet[i].Open, c = input.PtnSet[i].Close;
                target.Open[i] = o >= 0 && o < count ? o : -1;
                target.Close[i] = c >= 0 && c < count ? c : -1;
                if (target.Open[i] >= 0) owned.Add(o);
                if (target.Close[i] >= 0) owned.Add(c);
            }
            // Native tables normally have equal pattern counts on every target.
            // If a modded secondary mesh omits a pattern, use its neutral slot.
            int required = group.Channels.Max(c => c.Pattern) + 1;
            if (required > target.Open.Length)
            {
                int old = target.Open.Length;
                Array.Resize(ref target.Open, required); Array.Resize(ref target.Close, required);
                for (int i = old; i < required; ++i) { target.Open[i] = target.Open[0]; target.Close[i] = target.Close[0]; }
            }
            target.Indices = owned.OrderBy(i => i).ToArray();
            target.NeutralIndex = group.NeutralOpen ? target.Open[0] : target.Close[0];
            target.ChannelIndices = group.Channels.Select(c => c.Open ? target.Open[c.Pattern] : target.Close[c.Pattern]).ToArray();
            foreach (int index in target.Indices)
                if (!saved.Any(v => v.Mesh.Pointer == renderer.Pointer && v.Index == index))
                    saved.Add((renderer, index, renderer.GetBlendShapeWeight(index)));
            group.Targets.Add(target);
        }
        groups.Add(group);
    }

    public bool Owns(FaceBlendShape other) => controller && other && controller.Pointer == other.Pointer;
    public Action<CancellationToken> CreatePreparation()
    {
        uint last = Math.Max(motion?.LastFrame ?? 0, Math.Max(facial?.LastFrame ?? 0, lips?.LastFrame ?? 0));
        // Snapshot managed inputs on the main thread. Cancellation/disposal may
        // clear the renderer bindings while this worker finishes its own data.
        var inputs = groups.Select(g => (Group: g, Targets: g.Targets.ToArray(), Inputs: g.Channels.Select(c =>
            new PreparedFace.Input(c.Track, c.Gain, c.Lip, c.Open)).ToArray())).ToArray();
        return cancel =>
        {
            foreach (var input in inputs)
            {
                var curve = new PreparedFace(input.Inputs, last, cancel);
                foreach (var target in input.Targets) target.Prepare(curve, cancel);
                input.Group.Curve = curve;
            }
        };
    }
    public void Apply(float frame) { foreach (var group in groups) group.Apply(frame); }
    public void Dispose()
    {
        foreach (var state in saved)
            if (state.Mesh) state.Mesh.SetBlendShapeWeight(state.Index, state.Weight);
        saved.Clear(); groups.Clear();
    }
}
