using System.Diagnostics;
using AL;
using Character;
using Cysharp.Threading.Tasks;
using ILLGAMES.Unity.Component;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ALMMD;

internal sealed partial class DanceSession : IDisposable
{
    private enum Phase { Idle, ChoosingSong, Selecting, Responding, Preparing, Dancing, Restoring }
    private Phase phase;
    private Task<(LoadedSong Song, Exception Error)> loading;
    private LoadedSong song;
    private string songName;
    private readonly DanceAudio audio = new();
    private readonly DanceCamera camera = new();
    private readonly DanceLighting lighting = new();
    private readonly DanceCrowd crowd = new();
    private readonly DancePhysics physics = new();
    private readonly SongMenu menu = new();
    private DanceRig rig;
    private DancePointPicker picker;
    private readonly RestoreScope conversation = new(), stage = new(), songChoiceInput = new();
    private Heroine heroine;
    private Human human;
    private CommandScene command;
    private CommandUI ui;
    private UniTask response;
    private bool responsePending, cancelled;
    private bool useFreeCamera;
    private Il2CppSystem.Threading.CancellationTokenSource responseCts;
    private Vector3 stagePosition;
    private Quaternion stageRotation = Quaternion.identity;
    private int rigFrame = -1, frames;
    private float seconds, began, reportTime;
    private long workTicks;
    private string stopReason;
    public bool Busy => phase != Phase.Idle;
    public bool Selecting => phase == Phase.Selecting;
    public bool Dancing => phase == Phase.Dancing;
    public bool OwnsRig => phase == Phase.Preparing || phase == Phase.Dancing;
    public Human Human => human;
    public Heroine Actor => heroine;

    private void LoadSong(SongCatalog.Song selection)
    {
        songName = selection.Name;
        string directory = selection.Directory;
        // A cancelled selection drops this task. Return errors as data so an abandoned
        // disk read cannot leave an unobserved task exception or install the old song.
        loading = Task.Run<(LoadedSong Song, Exception Error)>(() =>
        {
            try { return (LoadedSong.Read(directory), null); }
            catch (Exception ex) { return (null, ex); }
        });
    }

    public void DrawSongChoice()
    {
        if (phase == Phase.Restoring)
        {
            menu.DrawRestore(crowd.PendingCount);
            return;
        }
        if (phase == Phase.Preparing)
        {
            menu.DrawProgress(songName, PreparationProgress);
            return;
        }
        if (phase != Phase.ChoosingSong) return;
        menu.Draw(Plugin.Songs);
    }

    public void Update()
    {
        try
        {
            if (phase == Phase.Restoring)
            {
                crowd.RestoreNext();
                if (crowd.PendingCount == 0) FinishStop();
                return;
            }
            PollLoading();
            if (Dancing && Application.isFocused && DanceSettings.Pressed(DanceSettings.GazeKey))
            {
                DanceSettings.Gaze.Value = PreparedGaze.Toggle(DanceSettings.Gaze.Value);
                Plugin.Logger.LogInfo("Dance gaze: " + DanceSettings.Gaze.Value);
            }
            if (Application.isFocused && DanceSettings.Pressed(DanceSettings.ToggleKey))
            {
                if (Dancing) Stop("stop key pressed");
                else if (phase == Phase.Preparing) Stop("preprocessing cancelled by key");
                else if (phase == Phase.ChoosingSong) Stop("song selection cancelled by key");
                else if (Selecting) picker.Back();
                else if (Busy) { cancelled = true; Plugin.Logger.LogInfo("Cancelling after native response completes"); }
                else RequestDance();
                return;
            }
            if (phase == Phase.ChoosingSong)
            {
                if (human == null || !human.Transform || !command || !command._isOpen || command.Target?.Pointer != heroine?.Pointer)
                { Stop("song selection cancelled"); return; }
                if (!Application.isFocused) return;
                var selection = menu.TakeSelection();
                if (selection != null) SelectSong(selection);
            }
            if (Selecting)
            {
                if (cancelled || human == null || !human.Transform || !command || !command._isOpen || command.Target?.Pointer != heroine?.Pointer)
                { Stop("point selection cancelled"); return; }
                picker.Tick();
                if (!picker.Completed) return;
                if (picker.Cancelled) { Stop("point selection cancelled"); return; }
                stagePosition = picker.Position;
                stageRotation = picker.Rotation;
                picker.Dispose(); picker = null;
                BeginResponse();
            }
            if (phase == Phase.Responding)
            {
                if (responsePending)
                {
                    if (response.Status == UniTaskStatus.Pending) return;
                    responsePending = false;
                    response.GetAwaiter().GetResult();
                }
                if (cancelled) { Stop("cancelled before music"); return; }
                if (song != null) StartDance();
            }
            if (phase == Phase.Preparing)
            {
                if (human == null || !human.Transform || !command || !command._isOpen || command.Target?.Pointer != heroine?.Pointer)
                { Stop("target left during preprocessing"); return; }
                if (AdvancePerformers()) PlayPreparedDance();
            }
            if (Dancing)
            {
                if (human == null || !human.Transform || !command || !command._isOpen || command.Target?.Pointer != heroine?.Pointer) { Stop("conversation ended"); return; }
                if (!audio.Playing && Time.realtimeSinceStartup - began > 0.2f)
                { Stop("music complete"); return; }
                camera.Tick();
                if (Time.realtimeSinceStartup >= reportTime) Report();
            }
        }
        catch (Exception ex) { Plugin.LogError("MMD update", ex); Stop("error"); }
    }

    private void PollLoading()
    {
        if (loading == null || !loading.IsCompleted) return;
        var pending = loading; loading = null;
        var result = pending.Result;
        if (result.Error != null)
        {
            Plugin.LogError("Loading " + songName, result.Error);
            if (phase == Phase.Responding) cancelled = true;
            else Stop("song loading failed");
            return;
        }
        var loaded = result.Song;
        audio.Load(loaded.Audio);
        loaded.Audio = null; // AudioClip owns its PCM copy; allow decoding buffers to be collected.
        song = loaded;
        Plugin.Logger.LogInfo($"{songName} ready: music={audio.Duration:F3}s; parts={song.Parts.Length}; camera={song.Camera.Camera.Count} keys.");
    }

    private void RequestDance()
    {
        command = SingletonInitializer<CommandScene>._instance;
        ui = SingletonInitializer<CommandUI>._instance;
        if (!command || !ui || !command.IsOpen() || !CommandUI.IsOpenFlagOnly() || ui.IsTransitionUI || EventManager.IsEventPlay) return;
        heroine = command.Target;
        human = heroine?.Human;
        if (human == null || !human.Transform) return;
        Plugin.Songs.Refresh();
        menu.Reset();
        roster.Open(heroine);
        menu.SetRoster(roster);
        cancelled = false;
        phase = Phase.ChoosingSong;
        var counter = ui.TransitionCounter;
        counter.Add(); conversation.Add(() => counter.Sub());
        var mapUi = SingletonInitializer<ActionMapUI>._instance;
        if (mapUi)
        {
            var mapCounter = mapUi.TransitionCounter;
            mapCounter.Add(); conversation.Add(() => mapCounter.Sub());
        }
        LockUIInput(songChoiceInput);
        var cursorLock = Cursor.lockState;
        bool cursorVisible = Cursor.visible;
        songChoiceInput.Add(() => { Cursor.lockState = cursorLock; Cursor.visible = cursorVisible; });
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        Plugin.Logger.LogInfo("Click a song: " + Plugin.Songs.ChoiceSummary);
    }

    private void SelectSong(SongCatalog.Song selection)
    {
        useFreeCamera = DanceSettings.FreeCamera.Value;
        LoadSong(selection);
        menu.Dispose();
        Plugin.Songs.Clear();
        songChoiceInput.Restore();
        phase = Phase.Selecting;
        picker = new DancePointPicker();
        picker.Begin();
        Plugin.Logger.LogInfo(songName + ": click a surface, then its bed marker; D with no marker opens native positions.");
    }

    private void BeginResponse()
    {
        phase = Phase.Responding;
        LockUIInput(conversation);
        // Match the native response after confirming a destination.
        string asset = PropertyData.Instance._adv._preludes.NextMap.Asset;
        heroine.Data.GameCount.PlayCommand |= AL.User.NPCData.Count.CommandAction.Invite;
        responseCts = new Il2CppSystem.Threading.CancellationTokenSource();
        response = CommandScene.PlayVoiceADV(asset, new ILLGAMES.ADV.OpenData(), heroine, responseCts.Token);
        responsePending = true;
        Plugin.Logger.LogInfo($"Native destination response started; actor={human.Name}; asset={asset}");
    }

    private void StartDance()
    {
        if (human == null || !human.Transform || !command || !command._isOpen || command.Target?.Pointer != heroine?.Pointer) { Stop("target left"); return; }
        phase = Phase.Preparing;
        BeginPerformers();
        began = Time.realtimeSinceStartup;
        Plugin.Logger.LogInfo($"{songName}: preprocessing {roster.Count} dancers from {song.Parts.Length} motion parts.");
    }

    private void PlayPreparedDance()
    {
        menu.Dispose();
        Plugin.Logger.LogInfo($"{songName}: motion preprocessing finished in {Time.realtimeSinceStartup - began:F2}s.");
        var cameraManager = SingletonInitializer<ActionMapCameraManager>._instance;
        if (!useFreeCamera) camera.Capture(cameraManager);
        lighting.Capture();
        DanceUI.Hide(stage);
        seconds = 0; rigFrame = -1; gazeFrame = -1; frames = 0; workTicks = 0;
        phase = Phase.Dancing;
        ApplyAllRigs();
        if (useFreeCamera)
            camera.CaptureFree(cameraManager, rig.WaistPosition, stageRotation, rig.CameraScale * 36f);
        // Initialization happens before audio starts. The pre-physics native
        // late pass will sample the playing audio and supply this frame's pose.
        rigFrame = -1;
        foreach (var dancer in performers) dancer.RigFrame = -1;
        ApplyCamera();
        audio.Start(Plugin.Volume.Value);
        began = Time.realtimeSinceStartup; reportTime = began + 5;
        Plugin.Logger.LogInfo($"{songName} started: stage={stagePosition.ToString("F3")}, MMD scale={rig.UnitScale:F5}, bones={rig.BoundBoneCount}, nativeRest={rig.RestPoseBoneCount}, correction={rig.CorrectionSource}, preprocessedTwists=true; audio timeSamples is master clock.");
    }

    private void Disable(Behaviour component)
    {
        if (!component) return;
        bool enabled = component.enabled;
        stage.Add(() => { if (component) component.enabled = enabled; });
        component.enabled = false;
    }

    private void CaptureTransform(Transform transform)
    {
        Vector3 p = transform.position;
        Quaternion r = transform.rotation;
        stage.Add(() => { if (transform) transform.SetPositionAndRotation(p, r); });
    }

    private void LockUIInput(RestoreScope restore)
    {
        foreach (var raycaster in UnityEngine.Object.FindObjectsOfType<GraphicRaycaster>())
        {
            bool enabled = raycaster.enabled;
            restore.Add(() => { if (raycaster) raycaster.enabled = enabled; });
            raycaster.enabled = false;
        }
        var events = EventSystem.current;
        if (events)
        {
            bool navigation = events.sendNavigationEvents;
            restore.Add(() => { if (events) events.sendNavigationEvents = navigation; });
            events.sendNavigationEvents = false;
        }
    }

    public void BeforeClothSimulation()
    {
        if (!Dancing) return;
        try
        {
            for (int i = 0; Dancing && i < performers.Count; i++)
            {
                var dancer = performers[i];
                if (dancer.Human.IsActiveAndEnabled && dancer.LateFrame != Time.frameCount) dancer.Human.LateUpdate();
            }
        }
        catch (Exception ex) { Plugin.LogError("MMD pre-physics native update", ex); Stop("native update error"); }
    }

    public bool ShouldRunScheduledHumanLate(Human instance) =>
        !Dancing || instance == null || !performerByHuman.TryGetValue(instance.Pointer, out var dancer) || dancer.LateFrame != Time.frameCount;

    public void AfterHumanLate(Human instance)
    {
        if (!Dancing || instance == null || !performerByHuman.TryGetValue(instance.Pointer, out var dancer)) return;
        dancer.LateFrame = Time.frameCount;
        ApplyRig(instance);
    }

    public void ApplyRig(Human instance)
    {
        if (!Dancing || instance == null || !performerByHuman.TryGetValue(instance.Pointer, out var dancer) || dancer.RigFrame == Time.frameCount) return;
        long start = Stopwatch.GetTimestamp();
        try
        {
            if (rigFrame != Time.frameCount)
            {
                seconds = audio.Seconds;
                rigFrame = Time.frameCount;
                ++frames;
            }
            dancer.Rig.Apply(seconds);
            dancer.RigFrame = Time.frameCount;
        }
        catch (Exception ex) { Plugin.LogError("MMD rig", ex); Stop("rig error"); }
        workTicks += Stopwatch.GetTimestamp() - start;
    }

    public void BeforeRender()
    {
        if (Selecting)
        {
            try { picker?.BeforeRender(); }
            catch (Exception ex) { Plugin.LogError("Point selection camera", ex); Stop("point selection error"); }
            return;
        }
        if (!Dancing) return;
        // Normally already sampled before cloth, once per frame. Retain the
        // fallback for a frame in which the actor's native late pass did not run.
        ApplyAllRigs();
        if (!Dancing) return;
        try
        {
            ApplyCamera();
            foreach (var dancer in performers)
            {
                dancer.Rig.ApplyFace(seconds);
                if (gazeFrame != Time.frameCount) dancer.Rig.ApplyGaze(seconds, DanceSettings.Gaze.Value, camera.Target);
            }
            gazeFrame = Time.frameCount;
        }
        catch (Exception ex) { Plugin.LogError("MMD camera", ex); Stop("camera error"); }
    }

    private void ApplyCamera()
    {
        long start = Stopwatch.GetTimestamp();
        if (camera.IsFree) camera.ApplyFree();
        else camera.Apply(song.Camera.Camera.Sample(seconds * 30f), stagePosition, stageRotation, rig.CameraScale);
        workTicks += Stopwatch.GetTimestamp() - start;
    }

    public void ApplyFace(ILLGAMES.Unity.FaceBlendShape instance)
    {
        if (!Dancing) return;
        try
        {
            foreach (var dancer in performers)
                if (dancer.Rig.OwnsFace(instance)) { dancer.Rig.ApplyFace(audio.Seconds); break; }
        }
        catch (Exception ex) { Plugin.LogError("MMD face", ex); Stop("face error"); }
    }

    public void CaptureAudio(AudioSource source) { if (Dancing) audio.CaptureGameSource(source); }

    private void Report()
    {
        float elapsed = Time.realtimeSinceStartup - (reportTime - 5);
        Plugin.Logger.LogInfo($"MMD perf: t={seconds:F3}s; fps={frames / Math.Max(0.01f, elapsed):F1}; rig+camera={workTicks * 1000.0 / Stopwatch.Frequency / Math.Max(1, frames):F3}ms/frame; frames={frames}");
        physics.Report();
        reportTime = Time.realtimeSinceStartup + 5; frames = 0; workTicks = 0;
    }

    public void Stop(string reason)
    {
        if (!Busy || phase == Phase.Restoring) return;
        phase = Phase.Restoring;
        stopReason = reason;
        menu.Dispose();
        Plugin.Songs.Clear();
        loading = null;
        song = null;
        try { picker?.Dispose(); } catch (Exception ex) { Plugin.LogError("Restore point selection", ex); }
        picker = null;
        try { audio.Dispose(); } catch (Exception ex) { Plugin.LogError("Restore audio", ex); }
        RestorePerformerRigs();
        stage.Restore();
        physics.Restore();
        ReleasePerformers();
        camera.Restore();
        lighting.Restore();
        songChoiceInput.Restore();
        if (crowd.PendingCount == 0) FinishStop();
    }

    private void FinishStop()
    {
        if (phase != Phase.Restoring) return;
        menu.Dispose();
        conversation.Restore();
        responseCts?.Dispose(); responseCts = null;
        responsePending = false; cancelled = false;
        phase = Phase.Idle;
        Plugin.Logger.LogInfo("Dance stopped and game state restored: " + stopReason);
        stopReason = null;
        heroine = null; human = null; command = null; ui = null;
        roster.Clear();
    }

    public void Dispose()
    {
        Stop("unloaded");
        crowd.RestoreAll();
        FinishStop();
        audio.Dispose();
        menu.Dispose();
    }
}
