using System.Numerics;

namespace ALMMD;

// No Unity objects or calls: the finishing pass can run on a worker.
internal sealed class PreparedTwists
{
    public const float FrameStep = .5f; // 60 Hz; VMD time remains 30 fps.
    public readonly Dictionary<string, TwistCurve> Transfers = new(StringComparer.Ordinal);
    public readonly Dictionary<string, TwistCurve> Helpers = new(StringComparer.Ordinal);
    public readonly Dictionary<string, ResponseCurve> Responses = new(StringComparer.Ordinal);

    internal sealed class Transfer
    {
        public string Source, End;
        public Vector3 Axis;
        public Quaternion Rest;
    }

    internal sealed class Helper
    {
        public string Source, Target;
        public Vector3 Axis;
        public Quaternion Basis;
        public float RestAngle, Weight, Sign;
    }

    public static PreparedTwists Build(Dictionary<string, Quaternion[]> rotations,
        Transfer[] transfers, Helper[] helpers, CancellationToken cancel)
    {
        var result = new PreparedTwists();
        foreach (var transfer in transfers)
        {
            cancel.ThrowIfCancellationRequested();
            var source = rotations[transfer.Source];
            var end = rotations[transfer.End];
            var angles = new float[source.Length];
            var confidence = new float[source.Length];
            float reference = TwistMath.Angle(transfer.Rest, transfer.Axis, out _);
            for (int i = 0; i < angles.Length; ++i)
                angles[i] = TwistMath.Wrap(reference - TwistMath.Angle(source[i], transfer.Axis, out confidence[i]));
            var curve = TwistCurve.Regularize(angles, confidence);
            result.Transfers.Add(transfer.Source, curve);
            for (int i = 0; i < angles.Length; ++i)
            {
                var rotation = Quaternion.CreateFromAxisAngle(transfer.Axis, curve.Values[i] * MathF.PI / 180f);
                source[i] = Quaternion.Normalize(source[i] * rotation);
                // Both AL forearm->hand and calf->foot are direct parent chains.
                end[i] = Quaternion.Normalize(Quaternion.Inverse(rotation) * end[i]);
            }
        }
        foreach (var helper in helpers)
        {
            cancel.ThrowIfCancellationRequested();
            var source = rotations[helper.Source];
            var angles = new float[source.Length];
            var confidence = new float[source.Length];
            for (int i = 0; i < angles.Length; ++i)
                angles[i] = TwistMath.Wrap(TwistMath.Angle(source[i] * helper.Basis, helper.Axis, out confidence[i]) - helper.RestAngle);
            var phase = TwistCurve.Regularize(angles, confidence);
            float restBlend = TwistMath.BlendDirection(helper.RestAngle, helper.Weight);
            for (int i = 0; i < angles.Length; ++i)
                angles[i] = helper.Sign * (TwistMath.BlendDirection(helper.RestAngle + phase.Values[i], helper.Weight) - restBlend);
            // Direction blending is nonlinear near the antipode; bound its
            // output too, rather than letting a slow input create a fast helper.
            result.Helpers.Add(helper.Target, new TwistCurve(TwistCurve.LimitSpeed(angles)));
        }
        NativeResponse.Build(rotations, result.Responses, cancel);
        return result;
    }
}

internal sealed class TwistCurve
{
    public readonly float[] Values;
    public TwistCurve(float[] values) { Values = values; }
    public float Sample(float frame)
    {
        float time = Math.Clamp(frame / PreparedTwists.FrameStep, 0f, Values.Length - 1);
        int i = (int)time;
        return i + 1 >= Values.Length ? Values[i] : Values[i] + (Values[i + 1] - Values[i]) * (time - i);
    }

    public static TwistCurve Regularize(float[] desired, float[] confidence)
    {
        int n = desired.Length;
        var diagonal = new double[n];
        var rhs = new double[n];
        // Least squares against twist, plus a 40 ms temporal regularizer.
        // The squared projection is the conditioning of swing/twist extraction:
        // uncertain folded poses defer to their neighbours in BOTH directions.
        double dt = PreparedTwists.FrameStep / 30.0, lambda = .04 * .04 / (dt * dt);
        for (int i = 0; i < n; ++i)
        {
            double weight = confidence[i];
            diagonal[i] = weight + 1e-8 + lambda * ((i > 0 ? 1 : 0) + (i + 1 < n ? 1 : 0));
            rhs[i] = weight * desired[i];
            if (i == 0) continue;
            double factor = -lambda / diagonal[i - 1];
            diagonal[i] += factor * lambda;
            rhs[i] -= factor * rhs[i - 1];
        }
        var smooth = new float[n];
        for (int i = n - 1; i >= 0; --i)
            smooth[i] = (float)((rhs[i] + (i + 1 < n ? lambda * smooth[i + 1] : 0)) / diagonal[i]);
        return new TwistCurve(LimitSpeed(smooth));
    }

    internal static float[] LimitSpeed(float[] desired)
    {
        // Axial redistribution may change at most one turn/second. This is a
        // bound on skin redistribution, NOT on the source animation or its IK.
        // Upper/lower Lipschitz envelopes use future as well as past samples;
        // their midpoint cannot accumulate an arbitrary extra 360-degree phase.
        float step = 360f * PreparedTwists.FrameStep / 30f;
        var lower = (float[])desired.Clone();
        var upper = (float[])desired.Clone();
        for (int i = 1; i < desired.Length; ++i)
        {
            lower[i] = Math.Max(lower[i], lower[i - 1] - step);
            upper[i] = Math.Min(upper[i], upper[i - 1] + step);
        }
        for (int i = desired.Length - 2; i >= 0; --i)
        {
            lower[i] = Math.Max(lower[i], lower[i + 1] - step);
            upper[i] = Math.Min(upper[i], upper[i + 1] + step);
        }
        for (int i = 0; i < desired.Length; ++i) lower[i] = (lower[i] + upper[i]) * .5f;
        return lower;
    }
}

internal static class TwistMath
{
    public static float Wrap(float degrees) => degrees - MathF.Floor((degrees + 180f) / 360f) * 360f;
    public static float Angle(Quaternion q, Vector3 axis, out float confidence)
    {
        float axial = q.X * axis.X + q.Y * axis.Y + q.Z * axis.Z;
        confidence = axial * axial + q.W * q.W;
        return confidence <= 1e-12f ? 0f : Wrap(2f * MathF.Atan2(axial, q.W) * 180f / MathF.PI);
    }
    public static float BlendDirection(float degrees, float weight)
    {
        if (MathF.Abs(weight - .5f) < 1e-6f) return .5f * degrees;
        float angle = degrees * MathF.PI / 180f;
        float result = MathF.Atan2(weight * MathF.Sin(angle), 1f - weight + weight * MathF.Cos(angle)) * 180f / MathF.PI;
        if (weight > .5f) result += 360f * MathF.Round((degrees - result) / 360f);
        return result;
    }
}
