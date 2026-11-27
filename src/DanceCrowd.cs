using AL;
using Character;
using ILLGAMES.Unity.Component;
using UnityEngine;

namespace ALMMD;

// Keep loaded characters in memory. Disabling their roots stops rendering,
// animation and cloth components; actor flags also stop the map's AI updates.
internal sealed class DanceCrowd
{
    private readonly Queue<RestoreScope> pending = new();
    public int PendingCount => pending.Count;

    public void Capture(IReadOnlyCollection<Human> dancers)
    {
        var actors = new Dictionary<IntPtr, Heroine>();
        var game = SingletonInitializer<Manager.Game>._instance;
        if (game)
            foreach (var actor in game._heroines)
                if (actor?.Human != null) actors[actor.Human.Pointer] = actor;
        var seen = new HashSet<IntPtr>();
        var camera = Camera.main;
        var humans = Human._list;
        for (int i = 0; i < humans.Count; ++i)
        {
            var human = humans[i];
            var root = human?.GameObject;
            if (human == null || human.Disposed || !root || !root.activeInHierarchy ||
                dancers.Any(dancer => human.Pointer == dancer.Pointer || dancer.Transform.IsChildOf(root.transform)) ||
                camera && camera.transform.IsChildOf(root.transform) || !seen.Add(root.Pointer)) continue;
            var state = new RestoreScope();
            pending.Enqueue(state); // Partial capture must also be recoverable.
            bool active = root.activeSelf;
            Action pauseActor = null;
            if (actors.TryGetValue(human.Pointer, out var actor))
            {
                bool enabled = actor.Enabled;
                var action = actor._state;
                bool paused = action?.IsPause == true;
                var agent = actor.Agent;
                bool hasAgent = agent && agent.enabled && agent.isOnNavMesh;
                if (hasAgent)
                {
                    bool stopped = agent.isStopped;
                    // Restart can itself change navigation: restore this last.
                    state.Add(() => { if (agent && agent.enabled && agent.isOnNavMesh) agent.isStopped = stopped; });
                }
                state.Add(() => {
                    if (!actor.Transform) return;
                    if (!paused && action != null && actor._state?.Pointer == action.Pointer)
                        actor.ActionRoutineReStart(true);
                    actor.Enabled = enabled;
                });
                pauseActor = () => {
                    if (!paused) actor.ActionRoutinePause(true);
                    actor.Enabled = false;
                    if (hasAgent && agent && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
                };
            }
            state.Add(() => { if (root) root.SetActive(active); });
            pauseActor?.Invoke();
            root.SetActive(false);
        }
        Plugin.Logger.LogInfo($"Dance crowd: suspended {pending.Count} other loaded characters; restore one per frame.");
    }

    public void RestoreNext()
    {
        if (pending.Count != 0) pending.Dequeue().Restore();
    }

    public void RestoreAll()
    {
        while (pending.Count != 0) RestoreNext();
    }
}
