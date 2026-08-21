using UnityEngine;

public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance;
    private AudioSource src;

    void Awake()
    {
        Instance = this;
        src = gameObject.AddComponent<AudioSource>();
        src.spatialBlend = 0f;
    }

    public void Gunshot()
    {
        PlayTone(SynthShot(0.12f, 0.4f), 0.35f);
    }

    public void EnemyShot()
    {
        PlayTone(SynthShot(0.1f, 0.25f), 0.18f);
    }

    public void Hit()
    {
        PlayTone(SynthTone(900, 0.05f), 0.2f);
    }

    public void Kill()
    {
        PlayTone(SynthTone(300, 0.2f, slide: -200), 0.3f);
    }

    public void Damage()
    {
        PlayTone(SynthTone(150, 0.25f, slide: -50), 0.4f);
    }

    public void Reload()
    {
        PlayTone(SynthTone(500, 0.06f), 0.15f);
        Invoke(nameof(Click2), 0.15f);
    }

    private void Click2() => PlayTone(SynthTone(700, 0.05f), 0.15f);

    void PlayTone(AudioClip clip, float vol) { if (src) src.PlayOneShot(clip, vol); }

    static AudioClip SynthShot(float dur, float power)
    {
        int rate = 22050;
        int n = (int)(rate * dur);
        var data = new float[n];
        var rng = new System.Random();
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float env = Mathf.Exp(-t * 18f);
            data[i] = ((float)rng.NextDouble() * 2f - 1f) * env * power + Mathf.Sin(i * 0.05f) * env * 0.2f;
        }
        var clip = AudioClip.Create("shot", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip SynthTone(float freq, float dur, float slide = 0)
    {
        int rate = 22050;
        int n = (int)(rate * dur);
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            float f = freq + slide * t / dur;
            float env = Mathf.Exp(-t * 8f);
            data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.6f;
        }
        var clip = AudioClip.Create("tone", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
