using UnityEngine;

public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance;
    AudioSource sfx;
    AudioSource music;
    AudioClip bed;

    void Awake()
    {
        Instance = this;
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.spatialBlend = 0f;
        sfx.playOnAwake = false;
        music = gameObject.AddComponent<AudioSource>();
        music.spatialBlend = 0f;
        music.loop = true;
        music.playOnAwake = false;
        music.volume = 0.18f;
        bed = BuildTrack();
        music.clip = bed;
    }

    public void StartMusic()
    {
        if (music && bed && !music.isPlaying) music.Play();
    }

    public void StopMusic()
    {
        if (music) music.Stop();
    }

    public void Gunshot() => Play(SynthShot(0.09f, 0.42f), 0.32f);
    public void EnemyShot() => Play(SynthShot(0.08f, 0.22f), 0.16f);
    public void Hit() => Play(SynthTone(980, 0.04f), 0.16f);
    public void Kill() => Play(SynthTone(220, 0.18f, -180), 0.28f);
    public void Damage() => Play(SynthTone(140, 0.22f, -40), 0.38f);
    public void Reload()
    {
        Play(SynthTone(520, 0.05f), 0.14f);
        Invoke(nameof(Click2), 0.12f);
    }
    void Click2() => Play(SynthTone(740, 0.04f), 0.14f);

    void Play(AudioClip clip, float vol) { if (sfx) sfx.PlayOneShot(clip, vol); }

    static AudioClip SynthShot(float dur, float power)
    {
        int rate = 22050;
        int n = Mathf.Max(8, (int)(rate * dur));
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float env = Mathf.Exp(-t * 20f);
            data[i] = (Mathf.Sin(i * 0.23f) * 0.35f + (Random.value * 2f - 1f) * 0.65f) * env * power;
        }
        var clip = AudioClip.Create("shot", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip SynthTone(float freq, float dur, float slide = 0)
    {
        int rate = 22050;
        int n = Mathf.Max(8, (int)(rate * dur));
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            float f = freq + slide * t / dur;
            float env = Mathf.Exp(-t * 9f);
            data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.55f;
        }
        var clip = AudioClip.Create("tone", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip BuildTrack()
    {
        const int rate = 22050;
        const float seconds = 21.333f;
        int n = (int)(rate * seconds);
        var data = new float[n];
        float[] bass = { 55f, 55f, 65.41f, 55f, 73.42f, 82.41f, 73.42f, 49f };
        float[] leadA = { 220f, 261.63f, 329.63f, 392f, 349.23f, 329.63f, 261.63f, 246.94f };
        float[] leadB = { 392f, 440f, 523.25f, 440f, 349.23f, 329.63f, 293.66f, 261.63f };
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            int step = (int)(t * 8f);
            float bassF = bass[step % bass.Length];
            float[] lead = ((step / 16) % 2 == 0) ? leadA : leadB;
            float leadF = lead[(step / 2) % lead.Length];
            float hat = ((step % 2) == 0) ? (Random.value * 0.04f) : 0f;
            float envB = 0.55f + 0.45f * Mathf.Sin(t * 6.283f * 4f);
            float envL = Mathf.Max(0f, Mathf.Sin(t * 6.283f * 2f));
            float sample =
                Mathf.Sin(2f * Mathf.PI * bassF * t) * 0.22f * envB +
                Mathf.Sin(2f * Mathf.PI * (bassF * 2f) * t) * 0.08f +
                (Mathf.Sin(2f * Mathf.PI * leadF * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * leadF * 2f * t)) * 0.07f * envL +
                hat;
            data[i] = Mathf.Clamp(sample, -0.9f, 0.9f);
        }
        var clip = AudioClip.Create("cobraBed", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
