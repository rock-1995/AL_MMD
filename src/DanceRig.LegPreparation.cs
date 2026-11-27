using UnityEngine;

namespace ALMMD;

public sealed partial class DanceRig
{
    private struct LegPose
    {
        public Quaternion Hip, Knee, Ankle, Toe;
        public Vector3 KneePosition, AnklePosition, ToePosition;
        public byte IkState;
    }

    private void BeginLegPreprocessing(Leg leg)
    {
        if (!leg.HasIk) return;
        leg.Prepared = new LegPose[sampleCount];
        Vector3 restToe = nodes[leg.Toe].RestPosition - nodes[leg.Ankle].RestPosition;
        leg.ToeBasisInverse = restToe.sqrMagnitude > .000001f
            ? Quaternion.Inverse(Quaternion.LookRotation(restToe, Vector3.up)) : Quaternion.identity;
        leg.UpperBasisInverse = Quaternion.Inverse(Quaternion.LookRotation(
            nodes[leg.Knee].RestPosition - nodes[leg.Hip].RestPosition, Vector3.right));
        leg.LowerBasisInverse = Quaternion.Inverse(Quaternion.LookRotation(
            nodes[leg.Ankle].RestPosition - nodes[leg.Knee].RestPosition, Vector3.right));
    }

    private static byte LegIkState(Leg leg, float frame) => (byte)(
        (leg.Enabled == null || leg.Enabled.Sample(frame) ? 1 : 0) |
        (leg.HasToeIk && (leg.ToeEnabled == null || leg.ToeEnabled.Sample(frame)) ? 2 : 0));

    private void CaptureLeg(Leg leg, float frame)
    {
        if (leg.Prepared == null) return;
        leg.Prepared[sampleIndex] = new LegPose {
            Hip = nodes[leg.Hip].Rotation,
            Knee = Quaternion.Inverse(nodes[leg.Hip].Rotation) * nodes[leg.Knee].Rotation,
            Ankle = nodes[leg.Ankle].Rotation, Toe = nodes[leg.Toe].Rotation,
            KneePosition = nodes[leg.Knee].Position, AnklePosition = nodes[leg.Ankle].Position,
            ToePosition = nodes[leg.Toe].Position, IkState = LegIkState(leg, frame)
        };
    }

    private void ApplyPreparedLeg(Leg leg, float frame)
    {
        if (leg.Prepared == null) return;
        float at = Mathf.Clamp(frame / PreparedTwists.FrameStep, 0f, leg.Prepared.Length - 1);
        int i = (int)at, j = Math.Min(i + 1, leg.Prepared.Length - 1);
        ref readonly LegPose a = ref leg.Prepared[i];
        ref readonly LegPose b = ref leg.Prepared[j];
        // IK switches are steps on integer VMD frames, all present on this grid.
        // Do not blend a future switch into the preceding interval.
        float t = a.IkState == b.IkState ? at - i : 0f;
        nodes[leg.Hip].Rotation = Quaternion.SlerpUnclamped(a.Hip, b.Hip, t);
        // Interpolate the knee relative to its thigh. Independent world-space
        // blends can change the bend when the whole knee plane turns quickly.
        nodes[leg.Knee].Rotation = nodes[leg.Hip].Rotation * Quaternion.SlerpUnclamped(a.Knee, b.Knee, t);
        nodes[leg.Ankle].Rotation = Quaternion.SlerpUnclamped(a.Ankle, b.Ankle, t);
        nodes[leg.Toe].Rotation = Quaternion.SlerpUnclamped(a.Toe, b.Toe, t);
        nodes[leg.Knee].Position = Vector3.LerpUnclamped(a.KneePosition, b.KneePosition, t);
        nodes[leg.Ankle].Position = Vector3.LerpUnclamped(a.AnklePosition, b.AnklePosition, t);
        nodes[leg.Toe].Position = Vector3.LerpUnclamped(a.ToePosition, b.ToePosition, t);
    }

    // This solver runs in the existing preprocessing pass, before helper curves
    // are derived, so the main joints and skin/cloth support share the same pose.
    private void SolveLeg(Leg leg, float frame)
    {
        if (!leg.HasIk || (leg.Enabled != null && !leg.Enabled.Sample(frame))) return;
        Node hip = nodes[leg.Hip], knee = nodes[leg.Knee], ankle = nodes[leg.Ankle];
        Node ik = nodes[leg.Ik], toe = nodes[leg.Toe];
        Quaternion ankleLocal = Quaternion.Inverse(knee.Rotation) * ankle.Rotation;
        Vector3 delta = ik.Position - hip.Position;
        float distance = delta.magnitude;
        if (distance < 0.00001f) return;
        Vector3 direction = delta / distance;
        float a = leg.UpperLength, b = leg.LowerLength;
        float length = a + b;
        // VMD has no source bind skeleton. Map only the final 3% of reach to
        // this body's measured leg length, with continuous distance/derivative.
        // Deep bends stay unchanged; no temporal filtering or song calibration.
        float soft = length * .03f;
        float reach = distance <= length - soft ? distance :
            length - soft * Mathf.Exp((length - soft - distance) / soft);
        reach = Mathf.Clamp(reach, Mathf.Abs(a - b) + length * 1e-6f, length * (1f - 1e-6f));
        float along = (a * a - b * b + reach * reach) / (2f * reach);
        float bend = Mathf.Sqrt(Mathf.Max(0f, a * a - along * along));
        // Transport the authored thigh frame to the reach direction before
        // choosing the knee plane. Directly projecting its forward axis becomes
        // singular at a 90-degree FK pitch even for an ordinary standing target.
        Vector3 thighDirection = (knee.Position - hip.Position).normalized;
        Quaternion align = Vector3.Dot(thighDirection, direction) < -.999999f
            ? Quaternion.AngleAxis(180f, Vector3.ProjectOnPlane(hip.Rotation * Vector3.right, thighDirection))
            : Quaternion.FromToRotation(thighDirection, direction);
        Vector3 pole = align * (hip.Rotation * Vector3.forward);
        Vector3 normal = pole - direction * Vector3.Dot(pole, direction);
        if (normal.sqrMagnitude < 0.000001f)
        {
            pole = hip.Rotation * Vector3.up;
            normal = pole - direction * Vector3.Dot(pole, direction);
        }
        normal.Normalize();
        Vector3 kneeTarget = hip.Position + direction * along + normal * bend;
        Vector3 end = hip.Position + direction * reach;
        // Resolve direction AND hinge axis. Two unrelated shortest swings leave
        // an arbitrary relative twist, whose cached interpolation can collapse
        // a bent knee through straight when the knee plane turns.
        Vector3 hinge = Vector3.Cross(normal, direction);
        hip.Rotation = Quaternion.LookRotation(kneeTarget - hip.Position, hinge) * leg.UpperBasisInverse;
        knee.Rotation = Quaternion.LookRotation(end - kneeTarget, hinge) * leg.LowerBasisInverse;
        knee.Position = kneeTarget;
        ankle.Position = end;
        // Leg IK rotates the two parents. Preserve ankle FK under those parents.
        // A toe target constrains direction, not the roll of its IK controller.
        ankle.Rotation = knee.Rotation * ankleLocal;
        Quaternion footReference = ankle.Rotation;
        Vector3 restToe = toe.RestPosition - ankle.RestPosition;
        if (leg.HasToeIk && (leg.ToeEnabled == null || leg.ToeEnabled.Sample(frame)))
        {
            // Keep the control-pair lever independent of an unreachable ankle.
            Vector3 target = nodes[leg.ToeIk].Position - ik.Position;
            if (target.sqrMagnitude > 0.000001f && restToe.sqrMagnitude > 0.000001f)
            {
                // Resolve both axes together. A second unrestricted FromTo can
                // reverse the toe when the sole normals oppose one another.
                // Use the authored FK sole, not the IK controller's
                // up vector. This keeps authored foot rolls instead of forcing
                // every foot upright or inheriting an unrelated control roll.
                Vector3 up = Vector3.ProjectOnPlane(footReference * Vector3.up, target);
                float strength = up.sqrMagnitude;
                if (strength < 1f)
                {
                    // Near a sole/target parallelism, its projected up axis is
                    // ill-conditioned. A toe swing from FK is well-conditioned
                    // there (the sole and toe axes are distinct). Blend only
                    // this roll reference, without changing the toe constraint.
                    Quaternion swing = Quaternion.FromToRotation(footReference * restToe, target);
                    Vector3 carried = Vector3.ProjectOnPlane(swing * (footReference * Vector3.up), target).normalized;
                    Vector3 forward = target.normalized;
                    float angle = Mathf.Atan2(Vector3.Dot(forward, Vector3.Cross(carried, up)),
                        Vector3.Dot(carried, up)) * Mathf.Rad2Deg;
                    // Squared projected length is the conditioning of this
                    // axis: full influence when perpendicular, none at a pole.
                    up = Quaternion.AngleAxis(angle * strength, forward) * carried;
                }
                ankle.Rotation = Quaternion.LookRotation(target, up) * leg.ToeBasisInverse;
                // A toe target leaves bank unconstrained. Recover the foot
                // controller's lateral tilt, measured relative to the shared
                // motion root. Its right-axis elevation is independent of yaw
                // and does not import the upside-down sole of a sagittal flip.
                // Apply only around the toe axis, preserving the solved aim.
                Quaternion control = Quaternion.Inverse(nodes[0].Rotation) * ik.Rotation;
                float bank = Mathf.Asin(Mathf.Clamp((control * Vector3.right).y, -1f, 1f)) * Mathf.Rad2Deg;
                ankle.Rotation = Quaternion.AngleAxis(bank, target) * ankle.Rotation;
            }
        }
        toe.Position = ankle.Position + ankle.Rotation * restToe;
        Quaternion toeLocal = toe.Track != null ? ConvertRotation(toe.Track.Sample(frame).Rotation) : Quaternion.identity;
        toe.Rotation = ankle.Rotation * toeLocal;
    }

}
