using UnityEngine;

namespace ALMMD;

public sealed partial class DanceRig
{
    // VMD stores animated deltas, not the source model's bind pose. The generic
    // MMD reference has lowered arms; AL's measured native bind is a T pose.
    // 35 degrees is the common reference used here, not an inferred first frame
    // or a per-song offset. Exact author-model matching would require its PMX.
    private const float MmdArmRestDown = 35f;

    private void CalibrateArmReferencePose()
    {
        foreach (string jp in new[] { "左", "右" })
        {
            int arm = nodeNames[jp + "腕"], elbow = nodeNames[jp + "ひじ"];
            int hand = nodeNames[jp + "手首"], middle = nodeNames[jp + "中指１"];
            float side = jp == "左" ? -1f : 1f;
            Vector3 sourceAxis = new(side * Mathf.Cos(MmdArmRestDown * Mathf.Deg2Rad),
                -Mathf.Sin(MmdArmRestDown * Mathf.Deg2Rad), 0f);
            Quaternion upperBasis = Quaternion.FromToRotation(
                nodes[elbow].RestPosition - nodes[arm].RestPosition, sourceAxis);
            Quaternion lowerBasis = Quaternion.FromToRotation(
                nodes[hand].RestPosition - nodes[elbow].RestPosition, sourceAxis);
            Quaternion handBasis = Quaternion.FromToRotation(
                nodes[middle].RestPosition - nodes[hand].RestPosition, sourceAxis);
            // Fix the palm's second axis too. In the common MMD reference the
            // little-finger side is +Z; ConvertPosition turns that into AL -Z.
            Vector3 across = nodes[nodeNames[jp + "小指１"]].RestPosition -
                nodes[nodeNames[jp + "人指１"]].RestPosition;
            Vector3 width = Vector3.ProjectOnPlane(handBasis * across, sourceAxis);
            handBasis = Quaternion.FromToRotation(width, Vector3.forward * -1f) * handBasis;

            var bases = new Dictionary<int, Quaternion> {
                [arm] = upperBasis, [nodeNames[jp + "腕捩"]] = upperBasis,
                [elbow] = lowerBasis, [nodeNames[jp + "手捩"]] = lowerBasis,
                [hand] = handBasis
            };
            // Finger deltas remain in the MMD model coordinate system. Their
            // bind rotations receive the SAME palm basis, once, at the right
            // of the accumulated motion; prepending it to the hierarchy would
            // rotate every following motion axis and double-count the rest.
            for (int i = hand + 1; i < nodes.Count; ++i)
                if (bases.ContainsKey(nodes[i].Parent)) bases[i] = handBasis;
            foreach (var entry in bases)
                nodes[entry.Key].RestRotation = entry.Value * nodes[entry.Key].RestRotation;
            foreach (var n in nodes)
                if (bases.TryGetValue(n.Parent, out var parentBasis)) n.Offset = parentBasis * n.Offset;
        }
        // The clavicle receives only its authored motion. Arm rest correction
        // must not rotate the shoulder, torso, pelvis or the leg/IK hierarchy.
    }
}
