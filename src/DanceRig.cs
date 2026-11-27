using System;
using System.Collections.Generic;
using Character;
using UnityEngine;

namespace ALMMD;

/// <summary>AL's measured skeleton driven by a small, managed MMD hierarchy.</summary>
public sealed partial class DanceRig
{
    private readonly Human human;
    private readonly VmdMotion motion;
    private readonly VmdMotion facial, lips;
    private DanceFace danceFace;
    private DanceHeels danceHeels;
    private readonly List<Node> nodes = new();
    private readonly List<SavedBone> saved = new();
    private readonly List<SavedBehaviour> disabled = new();
    private readonly Dictionary<string, Transform> bones = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> nodeNames = new(StringComparer.Ordinal);
    private readonly HashSet<int> savedIds = new();
    private Transform root = null;
    private Leg left = null, right = null;
    private bool prepared;
    private int pelvisNode, hipsNode, upperBodyNode;
    private NativeBodyCorrection bodyCorrection;
    public float UnitScale { get; private set; } = 0.08f;
    public float CameraScale { get; private set; } = 0.08f;
    public int BoundBoneCount { get; private set; }
    public int RestPoseBoneCount { get; private set; }
    public Transform Root => root;
    internal Vector3 WaistPosition => Require("cf_j_waist01").position;
    public string CorrectionSource => bodyCorrection?.Source ?? "none";

    private sealed class Node
    {
        public string Name = "";
        public int Parent = -1, RotationSource = -1;
        public Transform Target;
        public BoneTrack Track;
        public Vector3 RestPosition, Offset, Position;
        public Quaternion RestRotation, Rotation;
    }
    private sealed class Leg
    {
        public int Hip, Knee, Ankle, Toe, Ik, ToeIk;
        public SwitchTrack Enabled, ToeEnabled;
        public bool HasIk, HasToeIk;
        public float UpperLength, LowerLength;
        public LegPose[] Prepared;
        public Quaternion ToeBasisInverse;
        public Quaternion UpperBasisInverse, LowerBasisInverse;
    }
    private readonly struct SavedBone
    {
        public readonly Transform Bone;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public SavedBone(Transform bone)
        { Bone = bone; Position = bone.localPosition; Rotation = bone.localRotation; }
    }
    private readonly struct SavedBehaviour
    {
        public readonly Behaviour Component;
        public readonly bool Enabled;
        public SavedBehaviour(Behaviour component) { Component = component; Enabled = component.enabled; }
    }

    public DanceRig(Human human, VmdMotion motion, VmdMotion facial = null, VmdMotion lips = null)
    { this.human = human; this.motion = motion; this.facial = facial; this.lips = lips; }

    public void Prepare()
    {
        if (prepared) return;
        root = human.Transform;
        if (!root) throw new InvalidOperationException("The character root has been destroyed.");
        try
        {
            var skeleton = human.Body.GetObjBone();
            var boneRoot = skeleton ? skeleton.transform : root;
            foreach (var t in boneRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t && !bones.ContainsKey(t.name)) bones.Add(t.name, t);
            }
            // Validate the anatomical chain before modifying anything.
            Require("cf_j_hips"); Require("cf_j_waist01"); Require("cf_j_spine01"); Require("cf_j_head");
            foreach (string side in new[] { "L", "R" })
            {
                Require("cf_j_thigh00_" + side); Require("cf_j_leg01_" + side);
                Require("cf_j_leg03_" + side);
            }
            bodyCorrection = new NativeBodyCorrection(human, boneRoot);
            DisableAnimationWriters();
            // This list is anatomical AL joints, not every transform under the character.
            // Body shape scale/length nodes and hair/cloth simulation remain untouched.
            var reset = new List<Transform>();
            foreach (var pair in bones)
                if (IsDrivenJoint(pair.Key)) { Save(pair.Value); reset.Add(pair.Value); }
            reset.Sort((a, b) => Depth(a).CompareTo(Depth(b)));
            RestPoseBoneCount = 0;
            foreach (var t in reset)
            {
                if (!NativeRestPose.TryRotation(t.name, out var localRest)) continue;
                t.localRotation = localRest;
                RestPoseBoneCount++;
            }
            // The animation pelvis has a zero offset in the AL FBX. Height is on
            // cf_j_hips_length and the intervening shape nodes, which are preserved.
            Require("cf_j_hips").localPosition = Vector3.zero;
            // AL clips animate helper position, rotation and scale. Restore all
            // three before building the dance hierarchy from the native rest.
            bodyCorrection.ResetReferencePose();
            // Keep motion retargeting tied to the unlifted body. The camera
            // separately frames the complete footwear-adjusted height below.
            UnitScale = RootPoint(Require("cf_j_head")).y / 18f;
            if (UnitScale <= 0.001f || float.IsNaN(UnitScale))
                throw new InvalidOperationException("Could not calculate the MMD scale from the character's rest pose.");
            danceHeels = new DanceHeels();
            danceHeels.Bake(human, bones);
            // All camera lengths share one scale measured once in this rest pose.
            PrepareCameraScale();
            BuildHierarchy();
            CalibrateArmReferencePose();
            BoundBoneCount = 0;
            foreach (var n in nodes) if (n.Target) BoundBoneCount++;
            danceFace = new DanceFace(human.Face, motion, facial, lips);
            BindEyes();
            BeginPreprocessing();
            prepared = true;
        }
        catch { Restore(); throw; }
    }

    private void DisableAnimationWriters()
    {
        foreach (var component in root.GetComponentsInChildren<Behaviour>(true))
        {
            if (!component) continue;
            string type = component.GetIl2CppType().FullName ?? "";
            if (type == "UnityEngine.Animator" || type == "NeckLookControllerVer2" ||
                type.EndsWith(".NeckLookControllerVer2", StringComparison.Ordinal) ||
                type == "EyeLookController" || type.EndsWith(".EyeLookController", StringComparison.Ordinal) ||
                type.StartsWith("RootMotion.FinalIK.", StringComparison.Ordinal) ||
                component.TryCast<ILLGAMES.Unity.Correct.Process.BaseProcess>() != null)
            {
                disabled.Add(new SavedBehaviour(component));
                component.enabled = false;
            }
        }
    }

    private void BuildHierarchy()
    {
        Vector3 pelvis = RootPoint(Require("cf_j_hips"));
        Add("全ての親", -1, null, Vector3.zero);
        int center = Add("センター", 0, null, pelvis);
        int groove = Add("グルーブ", center, null, pelvis);
        int waist = Add("腰", groove, null, pelvis);
        int bodyCenter = pelvisNode = Add("体の重心", waist, "cf_j_hips");
        hipsNode = Add("下半身", bodyCenter, "cf_j_waist01");
        int upper1 = upperBodyNode = Add("上半身", bodyCenter, "cf_j_spine01");
        int upper2 = Add("上半身2", upper1, "cf_j_spine02");
        int upper3 = Add("上半身3", upper2, "cf_j_spine03");
        int neck = Add("首", upper3, "cf_j_neck");
        Add("頭", neck, "cf_j_head");
        AddArm("左", "L", upper3);
        AddArm("右", "R", upper3);
        left = AddLeg("左", "L");
        right = AddLeg("右", "R");
    }

    private void AddArm(string jp, string side, int parent)
    {
        int shoulder = Add(jp + "肩", parent, "cf_j_shoulder_" + side);
        int arm = Add(jp + "腕", shoulder, "cf_j_arm00_" + side);
        int twist = Add(jp + "腕捩", arm, null, nodes[arm].RestPosition);
        nodes[arm].RotationSource = twist;
        int elbow = Add(jp + "ひじ", twist, "cf_j_forearm01_" + side);
        int wristTwist = Add(jp + "手捩", elbow, null, nodes[elbow].RestPosition);
        nodes[elbow].RotationSource = wristTwist;
        int hand = Add(jp + "手首", wristTwist, "cf_j_hand_" + side);
        string[] jpNames = { "親指", "人指", "中指", "薬指", "小指" };
        string[] enNames = { "thumb", "index", "middle", "ring", "little" };
        const string digits = "０１２３";
        for (int finger = 0; finger < 5; finger++)
        {
            int p = hand;
            for (int joint = 1; joint <= 3; joint++)
            {
                int mmdJoint = finger == 0 ? joint - 1 : joint;
                p = Add(jp + jpNames[finger] + digits[mmdJoint], p,
                    "cf_j_" + enNames[finger] + "0" + joint + "_" + side);
            }
        }
    }

    private Leg AddLeg(string jp, string side)
    {
        int hip = Add(jp + "足", hipsNode, "cf_j_thigh00_" + side);
        int knee = Add(jp + "ひざ", hip, "cf_j_leg01_" + side);
        int ankle = Add(jp + "足首", knee, "cf_j_leg03_" + side);
        int toe = Add(jp + "つま先", ankle, "cf_j_toes_" + side);
        Vector3 ankleRest = nodes[ankle].RestPosition;
        Vector3 ikParentRest = new Vector3(ankleRest.x, 0f, ankleRest.z);
        int ikParent = Add(jp + "足IK親", 0, null, ikParentRest);
        int ik = Add(jp + "足ＩＫ", ikParent, null, ankleRest);
        int toeIk = Add(jp + "つま先ＩＫ", ik, null, nodes[toe].RestPosition);
        motion.Ik.TryGetValue(jp + "足ＩＫ", out var enabled);
        if (enabled == null) motion.Ik.TryGetValue(jp + "足IK", out enabled);
        motion.Ik.TryGetValue(jp + "つま先ＩＫ", out var toeEnabled);
        if (toeEnabled == null) motion.Ik.TryGetValue(jp + "つま先IK", out toeEnabled);
        return new Leg {
            Hip = hip, Knee = knee, Ankle = ankle, Toe = toe, Ik = ik, ToeIk = toeIk,
            Enabled = enabled, ToeEnabled = toeEnabled, HasIk = nodes[ik].Track != null,
            HasToeIk = nodes[toeIk].Track != null,
            UpperLength = Vector3.Distance(nodes[hip].RestPosition, nodes[knee].RestPosition),
            LowerLength = Vector3.Distance(nodes[knee].RestPosition, ankleRest)
        };
    }

    private int Add(string name, int parent, string realName, Vector3? virtualPosition = null)
    {
        Transform target = null;
        if (realName != null) bones.TryGetValue(realName, out target);
        Vector3 rest = target ? RootPoint(target) : virtualPosition ?? (parent >= 0 ? nodes[parent].RestPosition : Vector3.zero);
        motion.Bones.TryGetValue(name, out var track);
        if (track == null && name.Contains('Ｉ')) motion.Bones.TryGetValue(name.Replace("ＩＫ", "IK"), out track);
        if (track == null)
        {
            string half = name.Replace('０', '0').Replace('１', '1').Replace('２', '2').Replace('３', '3');
            if (half != name) motion.Bones.TryGetValue(half, out track);
        }
        var node = new Node {
            Name = name, Parent = parent, Target = target, Track = track,
            RestPosition = rest, Offset = rest - (parent >= 0 ? nodes[parent].RestPosition : Vector3.zero),
            RestRotation = target ? Quaternion.Inverse(root.rotation) * target.rotation : Quaternion.identity,
            Rotation = Quaternion.identity, Position = rest
        };
        int index = nodes.Count;
        nodes.Add(node); nodeNames[name] = index;
        return index;
    }

    public void Apply(double seconds)
    {
        if (!prepared || !root) return;
        float frame = (float)(seconds * 30.0);
        SampleMotion(frame);
        Quaternion stageRotation = root.rotation;
        Vector3 stagePosition = root.position;
        for (int i = 0; i < nodes.Count; i++)
        {
            Node n = nodes[i];
            if (!n.Target) continue;
            n.Target.rotation = stageRotation * SampleWorld(i);
            // AL already has separate waist01/spine01 branches below hips.
            // Move only their common root; never stretch the torso to emulate
            // an independent MMD upper-body pivot in world space.
            if (i == pelvisNode)
                n.Target.position = stagePosition + stageRotation * n.Position;
        }
        foreach (var transfer in axialTransfers) transfer.Apply(frame);
        bodyCorrection?.Apply(frame);
        danceFace?.Apply(frame);
    }

    // FK sampling stays shared. Leg IK is solved only while preparing; playback
    // reads those same results from the existing 60 Hz preparation grid.
    private void SampleMotion(float frame)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            Node n = nodes[i];
            Vector3 p = n.Offset;
            Quaternion q = Quaternion.identity;
            if (n.Track != null)
            {
                BonePose sample = n.Track.Sample(frame);
                p += ConvertPosition(sample.Position) * UnitScale;
                q = ConvertRotation(sample.Rotation);
            }
            if (n.Parent >= 0)
            {
                Node parent = nodes[n.Parent];
                n.Position = parent.Position + parent.Rotation * p;
                n.Rotation = parent.Rotation * q;
            }
            else { n.Position = p; n.Rotation = q; }
        }
        if (PreprocessingComplete)
        {
            ApplyPreparedLeg(left, frame);
            ApplyPreparedLeg(right, frame);
        }
        else
        {
            SolveLeg(left, frame);
            SolveLeg(right, frame);
        }
    }

    public void Restore()
    {
        bodyCorrection?.Dispose(); bodyCorrection = null;
        RestoreEyes();
        CancelPreprocessing();
        axialTransfers.Clear();
        danceFace?.Dispose(); danceFace = null;
        danceHeels?.Dispose(); danceHeels = null;
        // Restore pose before enabling the native writers. Individual destroyed
        // Unity objects must not prevent restoration of the surviving components.
        foreach (var state in saved)
        {
            try { if (state.Bone) { state.Bone.localPosition = state.Position; state.Bone.localRotation = state.Rotation; } }
            catch { }
        }
        foreach (var state in disabled)
        {
            try { if (state.Component) state.Component.enabled = state.Enabled; }
            catch { }
        }
        saved.Clear(); savedIds.Clear(); disabled.Clear(); nodes.Clear(); nodeNames.Clear();
        bones.Clear();
        prepared = false;
    }

    private void Save(Transform transform)
    { if (savedIds.Add(transform.GetInstanceID())) saved.Add(new SavedBone(transform)); }
    private Transform Require(string name) => bones.TryGetValue(name, out var t) && t ? t :
        throw new InvalidOperationException("The AL skeleton is missing " + name);
    private Vector3 RootPoint(Transform t) => Quaternion.Inverse(root.rotation) * (t.position - root.position);
    private static int Depth(Transform t) { int count = 0; while (t.parent) { count++; t = t.parent; } return count; }
    public static Vector3 ConvertPosition(System.Numerics.Vector3 p) => new(-p.X, p.Y, -p.Z);
    public static Quaternion ConvertRotation(System.Numerics.Quaternion q) => new(-q.X, q.Y, -q.Z, q.W);

    private static bool IsDrivenJoint(string name)
    {
        if (!name.StartsWith("cf_j_", StringComparison.Ordinal)) return false;
        string n = name.Substring(5);
        return n == "hips" || n == "waist01" || n == "waist02" || n == "spine01" || n == "spine02" ||
            n == "spine03" || n == "neck" || n == "head" || n.StartsWith("shoulder_") || n.StartsWith("shoulder02_") ||
            n.StartsWith("arm00_") || n.StartsWith("forearm01_") || n.StartsWith("hand_") ||
            n.StartsWith("thigh00_") || n.StartsWith("leg01_") || n.StartsWith("leg03_") ||
            n.StartsWith("foot_") || n.StartsWith("toes_") || n.StartsWith("thumb0") ||
            n.StartsWith("index0") || n.StartsWith("middle0") || n.StartsWith("ring0") || n.StartsWith("little0");
    }

}
