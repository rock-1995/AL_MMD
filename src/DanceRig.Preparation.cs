using System.Diagnostics;
using UnityEngine;
using NQ = System.Numerics.Quaternion;
using NV = System.Numerics.Vector3;

namespace ALMMD;

public sealed partial class DanceRig
{
    private readonly List<AxialTransfer> axialTransfers = new();
    private readonly List<SampleBinding> sampleBindings = new();
    private Dictionary<string, NQ[]> preparationSamples;
    private PreparedTwists.Helper[] helperDefinitions;
    private PreparedTwists.Transfer[] transferDefinitions;
    private CancellationTokenSource preparationCancel;
    private Task<PreparedTwists> preparationTask;
    private int sampleIndex, sampleCount;
    public bool PreprocessingComplete { get; private set; }
    public float PreprocessingProgress => PreprocessingComplete ? 1f : .95f * sampleIndex / Math.Max(1, sampleCount);

    private sealed class AxialTransfer
    {
        public Transform Source, End;
        public Vector3 Axis;
        public TwistCurve Curve;
        public void Apply(float frame)
        {
            if (Curve == null) return;
            var endWorld = End.rotation;
            Source.localRotation *= Quaternion.AngleAxis(Curve.Sample(frame), Axis);
            End.rotation = endWorld;
        }
    }

    private sealed class SampleBinding
    {
        public int Node, Parent = -1;
        public Quaternion ParentBasis;
        public NQ[] Samples;
    }

    private void BeginPreprocessing()
    {
        sampleCount = checked((int)Math.Ceiling(motion.LastFrame / PreparedTwists.FrameStep) + 1);
        sampleIndex = 0;
        BeginLegPreprocessing(left);
        BeginLegPreprocessing(right);
        PreprocessingComplete = false;
        preparationCancel = new CancellationTokenSource();
        var transfers = new List<PreparedTwists.Transfer>();
        foreach (string side in new[] { "L", "R" })
        {
            AddTransfer("cf_j_forearm01_" + side, "cf_j_hand_" + side, transfers);
            AddTransfer("cf_j_leg01_" + side, "cf_j_leg03_" + side, transfers);
        }
        transferDefinitions = transfers.ToArray();
        helperDefinitions = bodyCorrection.GetTwistDefinitions();
        preparationSamples = new Dictionary<string, NQ[]>(StringComparer.Ordinal);
        var targetNodes = new Dictionary<int, int>();
        for (int i = 0; i < nodes.Count; ++i)
            if (nodes[i].Target) targetNodes[nodes[i].Target.GetInstanceID()] = i;
        var inputs = helperDefinitions.Select(h => h.Source)
            .Concat(transfers.SelectMany(t => new[] { t.Source, t.End })).Concat(NativeResponse.Inputs).Distinct();
        foreach (string name in inputs)
        {
            var bone = Require(name);
            int index = targetNodes[bone.GetInstanceID()];
            var parent = bone.parent;
            var ancestor = parent;
            int parentNode = -1;
            while (ancestor && !targetNodes.TryGetValue(ancestor.GetInstanceID(), out parentNode))
            { parentNode = -1; ancestor = ancestor.parent; }
            var samples = new NQ[sampleCount];
            preparationSamples.Add(name, samples);
            sampleBindings.Add(new SampleBinding {
                Node = index, Parent = parentNode, Samples = samples,
                ParentBasis = Quaternion.Inverse(ancestor ? ancestor.rotation : root.rotation) * parent.rotation
            });
        }
    }

    private void AddTransfer(string sourceName, string endName, List<PreparedTwists.Transfer> definitions)
    {
        Transform source = Require(sourceName), end = Require(endName);
        // This identity is required to preserve the endpoint, not a song filter.
        if (end.parent != source) throw new InvalidOperationException("The AL joint chain has changed: " + sourceName + " → " + endName);
        Vector3 axis = Vector3.Scale(source.localScale, end.localPosition).normalized;
        NativeRestPose.TryRotation(sourceName, out var rest);
        axialTransfers.Add(new AxialTransfer { Source = source, End = end, Axis = axis });
        definitions.Add(new PreparedTwists.Transfer { Source = sourceName, End = endName,
            Axis = new NV(axis.x, axis.y, axis.z), Rest = Numeric(rest) });
    }

    private Quaternion SampleWorld(int index)
    {
        Node n = nodes[index];
        return (n.RotationSource >= 0 ? nodes[n.RotationSource].Rotation : n.Rotation) * n.RestRotation;
    }

    // Limit main-thread work to 4 ms/update so cancellation and the progress UI
    // remain responsive. Only the original sampler uses Unity's math functions.
    public bool AdvancePreprocessing()
    {
        if (PreprocessingComplete) return true;
        if (preparationTask != null)
        {
            if (!preparationTask.IsCompleted) return false;
            var result = preparationTask.GetAwaiter().GetResult();
            foreach (var transfer in axialTransfers) transfer.Curve = result.Transfers[transfer.Source.name];
            bodyCorrection.SetPreparedTwists(result);
            preparationTask = null;
            preparationSamples = null;
            sampleBindings.Clear();
            PreprocessingComplete = true;
            return true;
        }
        long start = Stopwatch.GetTimestamp();
        do
        {
            float frame = sampleIndex * PreparedTwists.FrameStep;
            SampleMotion(frame);
            CaptureLeg(left, frame);
            CaptureLeg(right, frame);
            foreach (var binding in sampleBindings)
            {
                Quaternion parent = binding.Parent >= 0 ? SampleWorld(binding.Parent) : Quaternion.identity;
                binding.Samples[sampleIndex] = Numeric(Quaternion.Inverse(parent * binding.ParentBasis) * SampleWorld(binding.Node));
            }
            ++sampleIndex;
        } while (sampleIndex < sampleCount && (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency < 4.0);
        if (sampleIndex == sampleCount)
        {
            var samples = preparationSamples;
            var transfers = transferDefinitions;
            var helpers = helperDefinitions;
            var cancel = preparationCancel.Token;
            var prepareFace = danceFace.CreatePreparation();
            preparationTask = Task.Run(() =>
            {
                var result = PreparedTwists.Build(samples, transfers, helpers, cancel);
                prepareFace(cancel);
                return result;
            }, cancel);
            // A cancelled/abandoned preparation must still observe worker errors.
            _ = preparationTask.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        }
        return false;
    }

    public bool OwnsFace(ILLGAMES.Unity.FaceBlendShape face) => danceFace?.Owns(face) == true;
    public void ApplyFace(double seconds) => danceFace?.Apply((float)(seconds * 30.0));
    public string FaceSummary => danceFace?.Summary ?? "none";
    public string HeelsSummary => danceHeels?.Summary ?? "off";
    private static NQ Numeric(Quaternion q) => new(q.x, q.y, q.z, q.w);
    private void CancelPreprocessing()
    {
        preparationCancel?.Cancel(); preparationCancel?.Dispose(); preparationCancel = null;
        preparationTask = null; preparationSamples = null;
        sampleBindings.Clear(); PreprocessingComplete = false;
        if (left != null) left.Prepared = null;
        if (right != null) right.Prepared = null;
    }
}
