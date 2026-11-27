using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace ALMMD;

internal sealed class DanceAudio : IDisposable
{
    private AudioClip clip;
    private AudioSource source;
    private GameObject root;
    private readonly RestoreScope state = new();
    private readonly HashSet<int> captured = new();
    private bool active;
    public float Seconds => source ? source.timeSamples / (float)clip.frequency : 0;
    public float Duration => clip ? clip.length : 0;
    public bool Playing => source && source.isPlaying;
    public AudioSource Source => source;

    public void Load(WavAudio wav)
    {
        clip = AudioClip.Create("Girls (AL MMD)", wav.FrameCount, wav.Channels, wav.SampleRate, false);
        // Transfer once. No disk access, decoding or buffer allocation during playback.
        var samples = new Il2CppStructArray<float>(wav.Samples);
        if (!clip.SetData(samples, 0)) throw new InvalidOperationException("AudioClip.SetData failed");
        root = new GameObject("AL MMD Music");
        UnityEngine.Object.DontDestroyOnLoad(root);
        source = root.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0;
        source.dopplerLevel = 0;
        source.priority = 0;
        source.bypassEffects = true;
        source.bypassListenerEffects = true;
        source.bypassReverbZones = true;
        source.ignoreListenerPause = true;
        source.clip = clip;
    }

    public void Start(float volume)
    {
        bool paused = AudioListener.pause;
        state.Add(() => AudioListener.pause = paused);
        active = true;
        foreach (var item in UnityEngine.Object.FindObjectsOfType<AudioSource>()) CaptureGameSource(item);
        AudioListener.pause = true;
        source.volume = volume;
        source.timeSamples = 0;
        source.Play();
    }

    public void CaptureGameSource(AudioSource item)
    {
        if (!active || !item || item == source || !item.ignoreListenerPause) return;
        int id = item.GetInstanceID();
        if (captured.Add(id)) state.Add(() => { if (item) item.ignoreListenerPause = true; });
        item.ignoreListenerPause = false;
    }

    public void Stop()
    {
        active = false;
        if (source) source.Stop();
        state.Restore();
        captured.Clear();
    }

    public void Dispose()
    {
        Stop();
        if (root) UnityEngine.Object.Destroy(root);
        if (clip) UnityEngine.Object.Destroy(clip);
        root = null; source = null; clip = null;
    }
}
