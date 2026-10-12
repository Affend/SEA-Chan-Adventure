using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

// =====================================================================
// Sumber skor "resmi" (mis. rekor panitia / pemenang lomba).
//
// Dua cara memperbarui TANPA build ulang:
//   1. Taruh leaderboard.json baru di hosting (mis. GitHub raw / itch.io),
//      isi RemoteUrl di bawah -> game mengunduh sendiri saat dibuka.
//   2. Kalau RemoteUrl dikosongkan, dipakai file bawaan
//      Assets/StreamingAssets/leaderboard.json (ikut ter-bundle saat build).
//
// Kalau offline / gagal unduh, game tetap jalan memakai file bawaan/cache.
// =====================================================================
public static class LeaderboardSource
{
    // Contoh: "https://raw.githubusercontent.com/namamu/sea-chan/main/leaderboard.json"
    public const string RemoteUrl = "";

    public const string BundledFile = "leaderboard.json";
    const string CacheFile = "leaderboard_cache.json";

    public static List<ScoreEntry> Entries { get; private set; } = new List<ScoreEntry>();
    static bool loaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { loaded = false; Entries = new List<ScoreEntry>(); }

    public static void LoadLocal()
    {
        if (loaded) return;
        loaded = true;

        string raw;

        // 1) hasil unduhan terakhir (disimpan di folder data pemain)
        string cachePath = Path.Combine(Application.persistentDataPath, CacheFile);
        if (TryRead(cachePath, out raw))
        {
            var cached = Leaderboard.ParseItems(raw);
            if (cached.Count > 0) { Entries = cached; return; }
        }

        // 2) file bawaan yang ikut di-build
        string bundledPath = Path.Combine(Application.streamingAssetsPath, BundledFile);
        if (TryRead(bundledPath, out raw)) Entries = Leaderboard.ParseItems(raw);
    }

    static bool TryRead(string path, out string text)
    {
        text = null;
        try
        {
            if (File.Exists(path)) { text = File.ReadAllText(path); return true; }
        }
        catch { }
        return false;
    }

    // Dipanggil sebagai coroutine saat game mulai.
    public static IEnumerator RefreshRemote()
    {
        LoadLocal();
        if (string.IsNullOrEmpty(RemoteUrl)) yield break;

        var req = UnityWebRequest.Get(RemoteUrl);
        req.timeout = 8;
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var list = Leaderboard.ParseItems(req.downloadHandler.text);
            if (list.Count > 0)
            {
                Entries = list;
                loaded = true;
                try
                {
                    File.WriteAllText(Path.Combine(Application.persistentDataPath, CacheFile),
                        req.downloadHandler.text);
                }
                catch { }

                if (UIManager.Instance != null) UIManager.Instance.RefreshLeaderboard();
                Debug.Log("[SEA Chan] Leaderboard resmi diperbarui dari internet (" + list.Count + " entri).");
            }
        }
        req.Dispose();
    }
}
