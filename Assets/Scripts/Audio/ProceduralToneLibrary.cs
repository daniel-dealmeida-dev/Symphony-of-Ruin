using UnityEngine;

/// <summary>
/// Gera clips de áudio simples em tempo de execução (sem arquivos .wav externos),
/// adequados como placeholders livres de licenciamento para SFX e loops curtos.
/// </summary>
public static class ProceduralToneLibrary
{
    private const int SampleRate = 44100;

    public static AudioClip JumpBlip()
    {
        return BuildClip("jump", 0.12f, 520f, 980f, 0.35f);
    }

    public static AudioClip HitDeath()
    {
        return BuildClip("death", 0.35f, 180f, 45f, 0.55f, noise: 0.12f);
    }

    public static AudioClip CoinPickup()
    {
        return BuildClip("coin", 0.18f, 880f, 1320f, 0.4f);
    }

    /// <summary>Loop curto para gameplay (arpejo simples).</summary>
    public static AudioClip GameplayLoop()
    {
        int samples = SampleRate * 3;
        float[] data = new float[samples];
        float[] freqs = { 196f, 246.94f, 293.66f, 329.63f };
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            int step = (i / (SampleRate / 6)) % 4;
            float f = freqs[step];
            float env = 0.22f + 0.08f * Mathf.Sin(t * 2.3f);
            data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * env;
        }
        return FinishClip("bg_gameplay", data);
    }

    /// <summary>Música mais lenta / menor para título.</summary>
    public static AudioClip TitleLoop()
    {
        int samples = SampleRate * 4;
        float[] data = new float[samples];
        float[] freqs = { 146.83f, 174.61f, 196f };
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            int step = (i / (SampleRate / 5)) % 3;
            float f = freqs[step];
            float env = 0.18f + 0.05f * Mathf.Sin(t * 1.7f);
            data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * env;
        }
        return FinishClip("bg_title", data);
    }

    /// <summary>Tom curto para game over (diferente do gameplay).</summary>
    public static AudioClip GameOverSting()
    {
        return BuildClip("gameover", 0.9f, 220f, 55f, 0.45f, noise: 0.05f);
    }

    private static AudioClip BuildClip(
        string name,
        float duration,
        float freqStart,
        float freqEnd,
        float amplitude,
        float noise = 0f)
    {
        int samples = Mathf.Max(256, (int)(SampleRate * duration));
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float k = i / (float)Mathf.Max(1, samples - 1);
            float f = Mathf.Lerp(freqStart, freqEnd, k);
            float t = i / (float)SampleRate;
            float env = Mathf.SmoothStep(1f, 0f, k);
            float s = Mathf.Sin(2f * Mathf.PI * f * t) * amplitude * env;
            if (noise > 0f)
            {
                s += (Random.value * 2f - 1f) * noise * env;
            }

            data[i] = Mathf.Clamp(s, -1f, 1f);
        }

        return FinishClip(name, data);
    }

    private static AudioClip FinishClip(string name, float[] data)
    {
        AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
