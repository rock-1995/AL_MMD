using AL;
using Character;
using UnityEngine;

namespace ALMMD;

// A temporary native standing pose, owned only by the facing-confirmation step.
internal sealed class PlacementPreview : IDisposable
{
    private readonly RestoreScope restore = new();
    private Transform actorRoot, humanRoot;
    private Vector3 position;
    private Quaternion rotation;

    public void Begin(Heroine actor, Human human, Vector3 point, Quaternion facing)
    {
        actorRoot = actor.Transform; humanRoot = human.Transform;
        var animation = actor.Animation;
        var animator = animation.Animator;
        // Normal standing loop in the installed AL animation tables, independent
        // of the seated/lying/water activity that the conversation interrupted.
        int stand = Animator.StringToHash($"Pose_D_{animation.Personality:00}_Loop");
        if (!animator.HasState(0, stand)) stand = Animator.StringToHash("Pose_D_00_Loop");
        if (!animator.HasState(0, stand)) throw new InvalidOperationException("Native standing animation is unavailable");

        bool enabled = actor.Enabled;
        restore.Add(() => { if (actor.Transform) actor.Enabled = enabled; }); actor.Enabled = false;
        Disable(animation); Disable(actor.RootMotionObserver); Disable(actor.SelfObstacle);
        var agent = actor.Agent;
        if (agent && agent.enabled && agent.isOnNavMesh)
        {
            bool stopped = agent.isStopped, updatePosition = agent.updatePosition, updateRotation = agent.updateRotation;
            Vector3 next = agent.nextPosition;
            restore.Add(() => {
                if (!agent || !agent.enabled || !agent.isOnNavMesh) return;
                agent.nextPosition = next; agent.updatePosition = updatePosition;
                agent.updateRotation = updateRotation; agent.isStopped = stopped;
            });
            agent.isStopped = true; agent.updatePosition = false; agent.updateRotation = false;
        }
        SaveTransform(actorRoot);
        if (actorRoot != humanRoot) SaveTransform(humanRoot);
        var skeleton = human.Body.GetObjBone().transform;
        foreach (var bone in skeleton.GetComponentsInChildren<Transform>(true)) SaveTransform(bone);
        foreach (var component in humanRoot.GetComponentsInChildren<Behaviour>(true))
        {
            string type = component.GetIl2CppType().FullName;
            if (type == "NeckLookControllerVer2" || type == "EyeLookController" ||
                type.EndsWith(".NeckLookControllerVer2", StringComparison.Ordinal) ||
                type.EndsWith(".EyeLookController", StringComparison.Ordinal) ||
                type.StartsWith("RootMotion.FinalIK.", StringComparison.Ordinal)) Disable(component);
        }
        CaptureAnimator(animator, stand);
        SetPlacement(point, facing);
    }

    internal void CaptureAnimator(Animator animator, int standingHash)
    {
        var states = new AnimatorStateInfo[animator.layerCount];
        var weights = new float[states.Length];
        for (int i = 0; i < states.Length; i++) { states[i] = animator.GetCurrentAnimatorStateInfo(i); weights[i] = animator.GetLayerWeight(i); }
        bool enabled = animator.enabled, rootMotion = animator.applyRootMotion, events = animator.fireEvents;
        float speed = animator.speed;
        var culling = animator.cullingMode;
        restore.Add(() => {
            if (!animator) return;
            animator.enabled = true; animator.fireEvents = false;
            for (int i = 0; i < states.Length; i++)
            {
                animator.SetLayerWeight(i, weights[i]);
                if (states[i].fullPathHash != 0) animator.Play(states[i].fullPathHash, i, states[i].normalizedTime);
            }
            animator.Update(0);
            animator.speed = speed; animator.applyRootMotion = rootMotion;
            animator.fireEvents = events; animator.cullingMode = culling; animator.enabled = enabled;
        });
        animator.enabled = true; animator.applyRootMotion = false; animator.fireEvents = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        for (int i = 1; i < states.Length; i++) animator.SetLayerWeight(i, 0);
        animator.Play(standingHash, 0, 0); animator.Update(0); animator.speed = 0;
    }

    private void SaveTransform(Transform bone)
    {
        Vector3 p = bone.localPosition, s = bone.localScale; Quaternion r = bone.localRotation;
        restore.Add(() => { if (bone) { bone.localPosition = p; bone.localRotation = r; bone.localScale = s; } });
    }
    private void Disable(Behaviour component)
    {
        if (!component) return;
        bool enabled = component.enabled;
        restore.Add(() => { if (component) component.enabled = enabled; }); component.enabled = false;
    }
    public void SetPlacement(Vector3 point, Quaternion facing) { position = point; rotation = facing; Apply(); }
    public void Apply()
    {
        if (actorRoot) actorRoot.SetPositionAndRotation(position, rotation);
        if (humanRoot && humanRoot != actorRoot) humanRoot.SetPositionAndRotation(position, rotation);
    }
    public void Dispose() { restore.Restore(); actorRoot = humanRoot = null; }
}
