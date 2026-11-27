using UnityEngine;

namespace ALMMD;

public sealed partial class DanceRig
{
    private void PrepareCameraScale()
    {
        // VMD stores motion offsets, not bind-pose bone lengths. Use the shared
        // Tda reference PMD's upper-arm and elbow origins for the MMD-side estimate.
        // Raising each upper arm straight up puts its elbow at shoulder Y + length.
        var leftArm = new Vector3(1.093590f, 15.839610f, -0.135290f);
        var leftElbow = new Vector3(3.271710f, 14.172960f, -0.102240f);
        var rightArm = new Vector3(-1.093590f, 15.839610f, -0.135290f);
        var rightElbow = new Vector3(-3.271710f, 14.172960f, -0.102240f);
        float referenceHeadTop = 0.5f * (
            leftArm.y + Vector3.Distance(leftArm, leftElbow) +
            rightArm.y + Vector3.Distance(rightArm, rightElbow));

        // This native crown marker is under cf_s_Facescale, so head-size edits
        // affect it. The measured rest height also includes the prepared footwear.
        var headTop = root.GetComponentsInChildren<Transform>(true)
            .First(t => t && t.name == "a_n_headtop");
        // Blend the original head-joint calibration equally with the crown estimate.
        float headBoneScale = RootPoint(Require("cf_j_head")).y / 18f;
        float headTopScale = RootPoint(headTop).y / referenceHeadTop;
        CameraScale = (headBoneScale + headTopScale) * 0.5f;
    }
}
