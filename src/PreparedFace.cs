namespace ALMMD;

// Managed-only preparation. Playback reads two adjacent samples and never
// searches source keys, computes envelopes or normalizes competing morphs.
internal sealed class PreparedFace
{
    internal const float Step = .5f;
    internal readonly record struct Input(MorphTrack Track, float Gain, bool Lip, bool Open);
    public readonly float[][] Channels;
    public readonly float[] Neutral;

    public PreparedFace(Input[] inputs, uint lastFrame, CancellationToken cancel)
    {
        int count = checked((int)Math.Ceiling(lastFrame / Step) + 7);
        Channels = new float[inputs.Length][];
        Neutral = new float[count];
        for (int c = 0; c < inputs.Length; ++c)
        {
            cancel.ThrowIfCancellationRequested();
            var input = inputs[c];
            var raw = new float[count];
            for (int i = 0; i < count; ++i)
                raw[i] = Math.Clamp(input.Track.Sample(i * Step) * input.Gain, 0f, 1f);
            var curve = Channels[c] = (float[])raw.Clone();
            if (!input.Lip || !input.Open) continue;
            // Keep the authored onset. Retain up to 67 ms of each vowel, then
            // release over 33 ms; no look-ahead and no change to the audio clock.
            for (int i = 0; i < count; ++i)
                for (int back = 1; back <= 6 && back <= i; ++back)
                {
                    float retention = back <= 4 ? 1f : (6 - back) / 2f;
                    curve[i] = Math.Max(curve[i], raw[i - back] * retention);
                }
        }
        for (int i = 0; i < count; ++i)
        {
            if ((i & 1023) == 0) cancel.ThrowIfCancellationRequested();
            float closed = 0f;
            for (int c = 0; c < inputs.Length; ++c)
                if (inputs[c].Lip && !inputs[c].Open) closed = Math.Max(closed, Channels[c][i]);
            float lips = 0f, expressions = 0f;
            for (int c = 0; c < inputs.Length; ++c)
            {
                if (inputs[c].Lip && inputs[c].Open) Channels[c][i] *= 1f - closed;
                if (inputs[c].Lip) lips += Channels[c][i];
                else expressions += Channels[c][i];
            }
            float lipScale = lips > 1f ? 1f / lips : 1f;
            float remaining = Math.Max(0f, 1f - lips);
            float expressionScale = remaining * (expressions > 1f ? 1f / expressions : 1f);
            Neutral[i] = remaining * Math.Max(0f, 1f - expressions);
            for (int c = 0; c < inputs.Length; ++c)
                Channels[c][i] *= inputs[c].Lip ? lipScale : expressionScale;
        }
    }

    public void Locate(float frame, out int first, out int second, out float blend)
    {
        float time = Math.Clamp(frame / Step, 0f, Neutral.Length - 1);
        first = (int)time;
        second = Math.Min(first + 1, Neutral.Length - 1);
        blend = time - first;
    }
    public static float Mix(float[] values, int a, int b, float t) => values[a] + (values[b] - values[a]) * t;
}
