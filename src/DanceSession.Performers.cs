using AL;
using Character;
using UnityEngine;

namespace ALMMD;

internal sealed partial class DanceSession
{
    private sealed class Performer
    {
        public Human Human;
        public DanceRig Rig;
        public bool Temporary;
        public int RigFrame = -1, LateFrame = -1;
    }
    private readonly DanceRoster roster = new();
    private readonly List<Performer> performers = new();
    private readonly Dictionary<IntPtr, Performer> performerByHuman = new();
    private int gazeFrame = -1;
    internal DanceRoster Roster => roster;
    internal bool OwnsHuman(Human value) => OwnsRig && value != null && performerByHuman.ContainsKey(value.Pointer);
    private float PreparationProgress => performers.Sum(p => p.Rig?.PreprocessingProgress ?? 0) / Math.Max(1, roster.Count);

    private void BeginPerformers()
    {
        var sources = roster.UniqueSources().ToArray();
        crowd.Capture(sources.Select(s => s.Human).ToArray());
        foreach (var source in sources)
        {
            var actor = source.Actor;
            bool enabled = actor.Enabled;
            var npc = actor.TryCast<Heroine>();
            var action = npc?._state;
            bool paused = action?.IsPause == true;
            bool pauseRoutine = npc != null && source.Human.Pointer != human.Pointer && !paused;
            var agent = actor.Agent;
            if (agent && agent.enabled && agent.isOnNavMesh)
            {
                bool stopped = agent.isStopped, updatePos = agent.updatePosition, updateRot = agent.updateRotation;
                Vector3 next = agent.nextPosition;
                stage.Add(() => {
                    if (!agent || !agent.enabled || !agent.isOnNavMesh) return;
                    agent.nextPosition = next;
                    agent.updatePosition = updatePos; agent.updateRotation = updateRot; agent.isStopped = stopped;
                });
                agent.isStopped = true; agent.updatePosition = false; agent.updateRotation = false;
            }
            stage.Add(() => {
                if (!actor.Transform) return;
                if (pauseRoutine && action != null && npc._state?.Pointer == action.Pointer) npc.ActionRoutineReStart(true);
                actor.Enabled = enabled;
            });
            if (pauseRoutine) npc.ActionRoutinePause(true);
            actor.Enabled = false;
            Disable(actor.SelfObstacle); Disable(actor.RootMotionObserver); Disable(actor.Animation);
            CaptureTransform(source.Human.Transform);
            if (actor.Transform != source.Human.Transform) CaptureTransform(actor.Transform);
            ActivateDancerRoot(actor.Transform.gameObject);
            if (actor.Transform != source.Human.Transform) ActivateDancerRoot(source.Human.GameObject);
            var selected = source.Human;
            bool visible = selected.VisibleAll;
            stage.Add(() => {
                if (selected.Disposed || !selected.GameObject) return;
                selected.VisibleAll = visible; selected.UpdateVisible();
            });
            var grid = actor.CharacterGrid;
            bool gridVisible = grid.Visible, gridConditions = grid.VisibleConditions;
            stage.Add(() => {
                if (selected.Disposed || !selected.GameObject) return;
                // Conditions must be restored while Visible is still true:
                // the native conditions setter ignores writes when hidden.
                grid.VisibleConditions = gridConditions;
                grid.VisibleImmediate = gridVisible;
            });
            // This cancels the native fade and restores dither/body visibility.
            // VisibleAll alone leaves a conversation-hidden actor transparent.
            grid.VisibleImmediate = true;
            grid.VisibleConditions = true;
            selected.VisibleAll = true;
            selected.UpdateVisible();
        }
    }

    private void ActivateDancerRoot(GameObject root)
    {
        bool active = root.activeSelf;
        stage.Add(() => { if (root) root.SetActive(active); });
        root.SetActive(true);
    }

    private bool AdvancePerformers()
    {
        // Instantiate/prepare one person per update; the existing 4 ms sampling
        // budget is shared by the cast, not multiplied by six.
        if (performers.Count < roster.Count)
        {
            int slot = performers.Count;
            var source = roster.At(slot);
            bool duplicate = performerByHuman.ContainsKey(source.Human.Pointer);
            var dancer = new Performer { Temporary = duplicate };
            if (duplicate)
            {
                var data = new HumanData(source.Human.Data);
                data.Coordinates[source.Human.FileStatus.coordinateType].Copy(source.Human.Coorde.Now);
                dancer.Human = Character.Human.Create(data);
            }
            else dancer.Human = source.Human;
            performers.Add(dancer); // Own the clone before any subsequent operation can fail.
            performerByHuman.Add(dancer.Human.Pointer, dancer);
            if (!duplicate)
            {
                source.Actor.CharacterGrid.VisibleImmediate = true;
                source.Actor.CharacterGrid.VisibleConditions = true;
            }
            dancer.Human.VisibleAll = true;
            dancer.Human.UpdateVisible();
            if (duplicate) PlacementClothes.CopyStates(source.Human, dancer.Human);
            int motionIndex = DanceFormation.MotionIndex(slot, song.Parts.Length);
            var part = song.Parts[motionIndex];
            dancer.Rig = new DanceRig(dancer.Human, part.Motion, part.Face, part.Lips);
            dancer.Rig.SetGaze(part.Gaze);
            if (slot == 0) rig = dancer.Rig;
            dancer.Rig.Prepare();
            var offset = DanceFormation.WorldOffset(slot, roster.Count, song.Parts.Length);
            Vector3 position = stagePosition + stageRotation * new Vector3(offset.X, offset.Y, offset.Z);
            if (!duplicate) source.Actor.Transform.SetPositionAndRotation(position, stageRotation);
            dancer.Human.Transform.SetPositionAndRotation(position, stageRotation);
            dancer.Rig.Apply(0);
            physics.Capture(dancer.Human);
            Plugin.Logger.LogInfo($"Dancer {slot}: {source.Name}; motion={motionIndex}; group={slot / song.Parts.Length}; clone={duplicate}; offset={offset}; heels={dancer.Rig.HeelsSummary}");
            return false;
        }
        foreach (var dancer in performers)
            if (!dancer.Rig.PreprocessingComplete) { dancer.Rig.AdvancePreprocessing(); return false; }
        return true;
    }

    private void ApplyAllRigs()
    {
        // A failed application stops and clears the cast; do not advance a
        // live enumerator through cleanup.
        for (int i = 0; Dancing && i < performers.Count; i++) ApplyRig(performers[i].Human);
    }

    private void RestorePerformerRigs()
    {
        foreach (var dancer in performers)
            try { dancer.Rig?.Restore(); } catch (Exception ex) { Plugin.LogError("Restore dancer rig", ex); }
        rig = null;
    }

    private void ReleasePerformers()
    {
        foreach (var dancer in performers)
            if (dancer.Temporary)
                try { dancer.Human?.Dispose(); } catch (Exception ex) { Plugin.LogError("Dispose dance clone", ex); }
        performers.Clear(); performerByHuman.Clear(); gazeFrame = -1;
    }
}
