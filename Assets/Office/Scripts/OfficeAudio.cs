using System;
using UnityEngine;

public sealed class OfficeAudio : MonoBehaviour
{
    public enum Cue { Click, Screen, Card, Footsteps, Warning, Win, Lose }

    private const string VolumeKey = "Office.MasterVolume";
    private const string MuteKey = "Office.Muted";
    private const int SampleRate = 24000;
    private static OfficeAudio instance;

    private AudioSource effects;
    private AudioSource ambience;
    private AudioClip[] clips;

    public static float Volume => Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 0.65f));
    public static bool Muted => PlayerPrefs.GetInt(MuteKey) == 1;

    public static void Initialize()
    {
        if (instance != null)
            return;

        var root = new GameObject("Office Audio");
        instance = root.AddComponent<OfficeAudio>();
        DontDestroyOnLoad(root);
        instance.Setup();
    }

    public static void SetVolume(float value)
    {
        PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
        Initialize();
        instance.ApplyVolume();
    }

    public static void ToggleMute()
    {
        PlayerPrefs.SetInt(MuteKey, Muted ? 0 : 1);
        Initialize();
        instance.ApplyVolume();
    }

    public static void Play(Cue cue)
    {
        Initialize();
        instance.effects.PlayOneShot(instance.clips[(int)cue]);
    }

    private void Setup()
    {
        effects = gameObject.AddComponent<AudioSource>();
        ambience = gameObject.AddComponent<AudioSource>();
        effects.playOnAwake = false;
        ambience.playOnAwake = false;
        clips = new[]
        {
            Create("Keyboard click", 0.065f, (t, n) => (0.4f * n + 0.35f * Square(240f, t)) * Envelope(t, 0.065f)),
            Notes("CRT switch", 0.065f, 320f, 480f),
            Create("Card shuffle", 0.11f, (t, n) => 0.65f * n * Envelope(t, 0.11f)),
            Create("Muffled footsteps", 0.56f, (t, n) => Footstep(t, n, 68f) + Footstep(t - 0.28f, n, 84f)),
            Notes("Reprimand", 0.14f, 260f, 190f, 120f),
            Notes("Shift complete", 0.14f, 392f, 494f, 587f, 784f),
            Notes("Shift failed", 0.16f, 330f, 247f, 165f)
        };
        ambience.clip = Create("Office hum", 4f, (t, _) =>
            (0.6f * Sine(50f, t) + 0.2f * Sine(100f, t) + 0.1f * Sine(137f, t)) *
            (0.85f + 0.15f * Sine(0.5f, t)));
        ambience.loop = true;
        ApplyVolume();
        ambience.Play();
    }

    private void ApplyVolume()
    {
        float volume = Muted ? 0f : Volume;
        effects.volume = volume * 0.42f;
        ambience.volume = volume * 0.065f;
    }

    private static AudioClip Notes(string name, float noteDuration, params float[] notes)
    {
        return Create(name, noteDuration * notes.Length, (time, _) =>
        {
            int index = Mathf.Min(notes.Length - 1, (int)(time / noteDuration));
            float t = time - index * noteDuration;
            return (0.44f * Sine(notes[index], t) + 0.16f * Square(notes[index], t)) * Envelope(t, noteDuration);
        });
    }

    private static AudioClip Create(string name, float seconds, Func<float, float, float> sample)
    {
        int count = Mathf.RoundToInt(seconds * SampleRate);
        var data = new float[count];
        var noise = new System.Random(23);
        for (int i = 0; i < count; i++)
            data[i] = sample(i / (float)SampleRate, (float)noise.NextDouble() * 2f - 1f);
        AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static float Sine(float frequency, float t) => Mathf.Sin(2f * Mathf.PI * frequency * t);
    private static float Square(float frequency, float t) => Sine(frequency, t) > 0f ? 1f : -1f;

    private static float Envelope(float t, float length)
    {
        return Mathf.Clamp01(t / 0.006f) * Mathf.Pow(Mathf.Clamp01(1f - t / length), 1.7f);
    }

    private static float Footstep(float t, float noise, float frequency)
    {
        if (t < 0f || t >= 0.23f)
            return 0f;

        float thump = 0.24f * Sine(frequency, t) + 0.12f * Sine(frequency * 1.7f, t);
        float scuff = 0.10f * noise * Envelope(t, 0.10f);
        return (thump + scuff) * Envelope(t, 0.23f);
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        foreach (AudioClip clip in clips)
            Destroy(clip);
        Destroy(ambience.clip);
        instance = null;
    }
}
