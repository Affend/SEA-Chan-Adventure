using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Mengatur musik & efek suara, termasuk volume master (disimpan dengan PlayerPrefs).
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    AudioSource music;
    AudioSource sfx;
    readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

    public float Master { get; private set; }
    public float MusicVol { get; private set; }
    public float SfxVol { get; private set; }

    void Awake()
    {
        // pengaman: hanya boleh ada satu AudioManager
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Master = PlayerPrefs.GetFloat("vol_master", 0.85f);
        MusicVol = PlayerPrefs.GetFloat("vol_music", 0.55f);
        SfxVol = PlayerPrefs.GetFloat("vol_sfx", 0.9f);

        var m = new GameObject("Music");
        m.transform.SetParent(transform, false);
        music = m.AddComponent<AudioSource>();
        music.loop = true;
        music.playOnAwake = false;

        var s = new GameObject("Sfx");
        s.transform.SetParent(transform, false);
        sfx = s.AddComponent<AudioSource>();
        sfx.playOnAwake = false;

        ApplyVolumes();
        PlayTrack("bgm_menu");   // mulai dari musik menu
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics() { Instance = null; }

    string _track;
    Coroutine _fade;

    // Ganti lagu dengan transisi halus (menu / dalam game / boss / kalah).
    public void PlayTrack(string name, bool loop = true)
    {
        if (_track == name) return;
        _track = name;
        if (music == null) return;

        if (_fade != null) StopCoroutine(_fade);
        _fade = StartCoroutine(SwitchRoutine(name, loop));
    }

    IEnumerator SwitchRoutine(string name, bool loop)
    {
        float t = 0f;
        float from = music.volume;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            music.volume = Mathf.Lerp(from, 0f, t / 0.3f);
            yield return null;
        }

        var clip = Get(name);
        music.clip = clip;
        music.loop = loop;
        if (clip != null) music.Play();

        t = 0f;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            music.volume = Mathf.Lerp(0f, MusicVol, t / 0.3f);
            yield return null;
        }
        music.volume = MusicVol;
        _fade = null;
    }

    AudioClip Get(string name)
    {
        if (cache.TryGetValue(name, out var c)) return c;
        c = Resources.Load<AudioClip>("Audio/" + name);
        cache[name] = c;
        return c;
    }

    void PlayMusic(string name)
    {
        var clip = Get(name);
        if (clip == null || music == null) return;
        music.clip = clip;
        music.Play();
    }

    // Pintasan aman untuk mengganti musik dari script lain.
    public static void Track(string name, bool loop = true)
    {
        if (Instance != null) Instance.PlayTrack(name, loop);
    }

    public static void Play(string name, float volumeScale = 1f)
    {
        if (Instance == null) return;
        var clip = Instance.Get(name);
        if (clip == null || Instance.sfx == null) return;
        Instance.sfx.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    void ApplyVolumes()
    {
        AudioListener.volume = Mathf.Clamp01(Master);
        if (music != null) music.volume = Mathf.Clamp01(MusicVol);
        if (sfx != null) sfx.volume = Mathf.Clamp01(SfxVol);
    }

    public void SetMaster(float v)
    {
        Master = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat("vol_master", Master);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    // Tombol cepat (bawaan M): matikan / nyalakan semua suara.
    public bool Muted { get; private set; }
    float beforeMute = 0.8f;

    public void ToggleMute()
    {
        Muted = !Muted;
        if (Muted)
        {
            beforeMute = Master > 0.01f ? Master : 0.8f;
            SetMaster(0f);
        }
        else
        {
            SetMaster(beforeMute);
        }

        if (UIManager.Instance != null)
            UIManager.Instance.ShowBanner(Muted ? "SUARA: MATI" : "SUARA: HIDUP");

        // ikutkan slider Master di menu pengaturan (kalau sedang terbuka)
        if (UIManager.Instance != null) UIManager.Instance.RefreshVolumeUI();
    }

    public void SetMusic(float v)
    {
        MusicVol = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat("vol_music", MusicVol);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void SetSfx(float v)
    {
        SfxVol = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat("vol_sfx", SfxVol);
        PlayerPrefs.Save();
        ApplyVolumes();
    }
}
