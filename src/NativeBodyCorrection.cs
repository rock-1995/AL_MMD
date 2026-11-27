using Character;
using UnityEngine;

namespace ALMMD;

/// <summary>Reconstructs the limb helper distribution baked into AL animation clips.</summary>
internal sealed class NativeBodyCorrection : IDisposable
{
    private readonly RestoreScope restore = new();
    private readonly Dictionary<string, Transform> bones = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Helper> helpers = new(StringComparer.Ordinal);
    private readonly List<TwistChannel> twists = new();
    private bool disposed;
    public string Source => "AL baked helper axes, direction blending and 35 prepared native pose-response channels";

    private sealed class Helper
    {
        public readonly Transform Bone;
        public readonly Vector3 Position, Scale;
        public readonly Quaternion Rotation;
        public ResponseCurve Response;
        public Helper(Transform bone, Vector3 position, Quaternion rotation, Vector3 scale)
        { Bone = bone; Position = position; Rotation = rotation; Scale = scale; }
        public void Reset()
        {
            if (!Bone) return;
            Bone.localPosition = Position; Bone.localRotation = Rotation; Bone.localScale = Scale;
        }
    }

    private sealed class TwistChannel
    {
        public Transform Source;
        public Helper Target;
        public Quaternion InputBasis;
        public Vector3 Axis;
        public float Weight, Sign, RestAngle;
        public TwistCurve Curve;
        public void Apply(float frame)
        {
            if (!Source || !Target.Bone) return;
            float angle = Curve?.Sample(frame) ?? 0f;
            Target.Bone.localRotation = Target.Rotation * Quaternion.AngleAxis(angle, Axis);
        }
    }

    public NativeBodyCorrection(Human human, Transform skeletonRoot)
    {
        try
        {
            // Do not construct the legacy writer: its absolute helper positions
            // use different units from the current AL assets.
            var expression = human.Body.expression?._bone;
            if (!expression) expression = human.Transform.GetComponentInChildren<HumanExpressionBone>(true);
            if (expression)
            {
                var component = expression; bool enabled = component.enabled;
                restore.Add(() => { if (component) component.enabled = enabled; });
                component.enabled = false;
            }
            foreach (var bone in skeletonRoot.GetComponentsInChildren<Transform>(true))
            {
                if (!bone) continue;
                if (!bones.ContainsKey(bone.name)) bones.Add(bone.name, bone);
                if (!bone.name.StartsWith("cf_j_shoulder02_", StringComparison.Ordinal) &&
                    !bone.name.StartsWith("cf_d_", StringComparison.Ordinal) &&
                    !bone.name.StartsWith("cf_dd_", StringComparison.Ordinal)) continue;
                if (!NativeRestPose.TryPose(bone.name, out var p, out var q, out var s)) continue;
                var saved = bone;
                Vector3 position = bone.localPosition, scale = bone.localScale;
                Quaternion rotation = bone.localRotation;
                restore.Add(() =>
                {
                    if (!saved) return;
                    saved.localPosition = position; saved.localRotation = rotation; saved.localScale = scale;
                });
                helpers[bone.name] = new Helper(bone, p, q, s);
            }
            foreach (string side in new[] { "L", "R" })
            {
                for (int i = 1; i <= 3; ++i)
                {
                    AddTwist("cf_j_arm00_" + side, "cf_d_arm0" + i + "_" + side,
                        Vector3.right, 1.1f - .3f * i, -1f, true);
                    AddTwist("cf_j_hand_" + side, "cf_d_forearm0" + i + "_" + side,
                        Vector3.right, .3f * i - .1f, 1f, false);
                    AddTwist("cf_j_thigh00_" + side, "cf_d_thigh0" + i + "_" + side,
                        Vector3.up, 1.1f - .3f * i, -1f, true);
                }
                AddTwist("cf_j_leg03_" + side, "cf_d_leg02_" + side, Vector3.up, .4f, 1f, false);
                AddTwist("cf_j_leg03_" + side, "cf_d_leg03_" + side, Vector3.up, .9f, 1f, false);
            }
            var hand = human.Body.Hand;
            if (hand != null)
                for (int side = 0; side < 2; ++side)
                {
                    int index = side; bool enabled = hand.GetEnableShapeHand(index);
                    restore.Add(() => hand.SetEnableShapeHand(index, enabled));
                    hand.SetEnableShapeHand(index, false);
                }
        }
        catch { Dispose(); throw; }
    }

    private void AddTwist(string source, string target, Vector3 axis, float weight, float sign, bool helperBasis)
    {
        if (!bones.TryGetValue(source, out var input) || !helpers.TryGetValue(target, out var output)) return;
        NativeRestPose.TryRotation(source, out var rest);
        Quaternion basis = helperBasis ? output.Rotation : Quaternion.identity;
        twists.Add(new TwistChannel {
            Source = input, Target = output, Axis = axis, Weight = weight, Sign = sign,
            InputBasis = basis, RestAngle = TwistAngle(rest * basis, axis)
        });
    }

    // Native clips blend endpoint directions, rather than scaling an Euler angle.
    // The middle section uses the continuous half-angle at the antipode.
    internal static float BlendDirection(float degrees, float weight)
    {
        if (Mathf.Abs(weight - .5f) < 1e-6f) return .5f * degrees;
        float angle = degrees * Mathf.Deg2Rad;
        float result = Mathf.Atan2(weight * Mathf.Sin(angle), 1f - weight + weight * Mathf.Cos(angle)) * Mathf.Rad2Deg;
        if (weight > .5f) result += 360f * Mathf.Round((degrees - result) / 360f);
        return result;
    }

    private static float TwistAngle(Quaternion q, Vector3 axis) =>
        Mathf.DeltaAngle(0f, 2f * Mathf.Atan2(q.x * axis.x + q.y * axis.y + q.z * axis.z, q.w) * Mathf.Rad2Deg);

    public void ResetReferencePose()
    {
        foreach (var helper in helpers.Values) helper.Reset();
    }

    public PreparedTwists.Helper[] GetTwistDefinitions() => twists.Select(t => new PreparedTwists.Helper {
        Source = t.Source.name, Target = t.Target.Bone.name,
        Axis = new System.Numerics.Vector3(t.Axis.x, t.Axis.y, t.Axis.z),
        Basis = new System.Numerics.Quaternion(t.InputBasis.x, t.InputBasis.y, t.InputBasis.z, t.InputBasis.w),
        RestAngle = t.RestAngle, Weight = t.Weight, Sign = t.Sign
    }).ToArray();

    public void SetPreparedTwists(PreparedTwists prepared)
    {
        foreach (var channel in twists) channel.Curve = prepared.Helpers[channel.Target.Bone.name];
        foreach (var pair in prepared.Responses)
            if (helpers.TryGetValue(pair.Key, out var helper)) helper.Response = pair.Value;
    }

    public void Apply(float frame)
    {
        if (disposed) return;
        // Position and scale are animated too. Never retain the conversation
        // pose that happened to be playing when the dance was started.
        foreach (var helper in helpers.Values)
            if (helper.Bone && helper.Response != null) helper.Response.Apply(helper.Bone, frame);
            else helper.Reset();
        foreach (var channel in twists) channel.Apply(frame);
        ApplyElbow("L", 1f); ApplyElbow("R", -1f);
        ApplyWrist("L", 1f); ApplyWrist("R", -1f);
        ApplyKnee("L"); ApplyKnee("R");
    }

    private void ApplyWrist(string side, float sign)
    {
        if (!bones.TryGetValue("cf_j_hand_" + side, out var hand)) return;
        NativeRestPose.TryRotation(hand.name, out var rest);
        Quaternion delta = Quaternion.Inverse(rest) * hand.localRotation;
        float norm = Mathf.Sqrt(delta.x * delta.x + delta.w * delta.w);
        if (norm > 1e-6f && helpers.TryGetValue("cf_d_forearm_wrist_" + side, out var wrist))
        {
            // AL's wrist skin helper removes a LEFT-side X twist, then keeps
            // the XYZ Y/Z swing. Its Z response only covers the back of the hand.
            Quaternion twist = new Quaternion(delta.x / norm, 0f, 0f, delta.w / norm);
            Quaternion swing = Quaternion.Inverse(twist) * delta;
            float y = Mathf.Asin(Mathf.Clamp(2f * (swing.w * swing.y - swing.z * swing.x), -1f, 1f)) * Mathf.Rad2Deg;
            float z = Mathf.Atan2(2f * (swing.w * swing.z + swing.x * swing.y),
                1f - 2f * (swing.y * swing.y + swing.z * swing.z)) * Mathf.Rad2Deg;
            z = sign > 0f ? Mathf.Min(z, 0f) : Mathf.Max(z, 0f);
            wrist.Bone.localRotation = wrist.Rotation * Quaternion.AngleAxis(z, Vector3.forward) * Quaternion.AngleAxis(y, Vector3.up);
        }
        if (helpers.TryGetValue("cf_d_forearm02_" + side, out var middle))
        {
            // Smooth volume response fitted to the native animated scale curve.
            // TwistAngle returns the principal angle, so tan never approaches a pole.
            float angle = TwistAngle(hand.localRotation, Vector3.right);
            float reference = TwistAngle(rest, Vector3.right);
            float expansion = .367355f * (Mathf.Abs(Mathf.Tan(angle * Mathf.Deg2Rad * .25f)) -
                Mathf.Abs(Mathf.Tan(reference * Mathf.Deg2Rad * .25f)));
            middle.Bone.localScale = Vector3.Scale(middle.Scale, new Vector3(1f, 1f + expansion, 1f + expansion));
        }
    }

    private void ApplyElbow(string side, float sign)
    {
        if (!bones.TryGetValue("cf_j_forearm01_" + side, out var elbow)) return;
        NativeRestPose.TryRotation(elbow.name, out var rest);
        float bend = sign * Mathf.DeltaAngle(TwistAngle(rest, Vector3.up), TwistAngle(elbow.localRotation, Vector3.up));
        float over90 = Mathf.Max(0f, bend - 90f), over50 = Mathf.Max(0f, bend - 50f);
        Adjust("cf_d_elbo_" + side, new Vector3(sign * (-.002f * bend - .003f * over90), 0, -.0003f * bend + .003f * over50),
            Vector3.up, sign * (.2f * bend + .6f * over90), Vector3.one);
        Adjust("cf_d_forearm00_" + side, new Vector3(0, 0, -.0033f * over90),
            Vector3.up, sign * .095f * over90, Vector3.one);
        NativeElbowBack.Sample(bend, sign > 0f, out var backOffset, out float backAngle);
        Adjust("cf_d_elboback_" + side, backOffset, Vector3.up, backAngle, Vector3.one);
    }

    private void ApplyKnee(string side)
    {
        if (!bones.TryGetValue("cf_j_leg01_" + side, out var knee)) return;
        NativeRestPose.TryRotation(knee.name, out var rest);
        float bend = Mathf.DeltaAngle(TwistAngle(rest, Vector3.right), TwistAngle(knee.localRotation, Vector3.right));
        float over90 = Mathf.Max(0f, bend - 90f), over60 = Mathf.Max(0f, bend - 60f);
        Adjust("cf_d_kneeF_" + side, new Vector3(0, -.003f * bend + .001f * over90, -.001f * bend + .001f * over90),
            Vector3.right, .6f * bend, Vector3.one);
        Adjust("cf_d_kneeB_" + side, new Vector3(0, .005f * (bend + over90), -.005f * bend + .01f * over90),
            Vector3.right, .25f * bend, new Vector3(1f - .002f * over90, 1, 1));
        Adjust("cf_d_leg00_" + side, Vector3.zero, Vector3.right, .05f * over90, Vector3.one);
        Adjust("cf_d_leg01_" + side, Vector3.zero, Vector3.right, -.05f * over90, new Vector3(1f + .002f * over90, 1, 1));
        if (helpers.TryGetValue("cf_d_thigh03_" + side, out var thigh))
        {
            thigh.Bone.localPosition = thigh.Position + new Vector3(0, 0, .0025f * over60);
            thigh.Bone.localScale = Vector3.Scale(thigh.Scale, new Vector3(1f + .004f * over90, 1, 1));
        }
    }

    private void Adjust(string name, Vector3 position, Vector3 axis, float angle, Vector3 scale)
    {
        if (!helpers.TryGetValue(name, out var helper) || !helper.Bone) return;
        helper.Bone.localPosition = helper.Position + position;
        helper.Bone.localRotation = helper.Rotation * Quaternion.AngleAxis(angle, axis);
        helper.Bone.localScale = Vector3.Scale(helper.Scale, scale);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        restore.Restore(); twists.Clear(); helpers.Clear(); bones.Clear();
    }
}
