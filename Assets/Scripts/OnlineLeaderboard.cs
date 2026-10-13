using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

// =====================================================================
// PAPAN SKOR ONLINE — semua pemain, semua device, satu papan.
// Memakai layanan gratis dreamlo.com (tanpa daftar akun, tanpa server sendiri).
//
// CARA MENGAKTIFKAN (sekali saja, ±2 menit):
//   1. Buka  https://dreamlo.com
//   2. Klik "Create a new leaderboard" (gratis) → isi nama apa saja.
//   3. Kamu akan dapat dua kode: PUBLIC CODE dan PRIVATE CODE.
//   4. Tempel PUBLIC CODE ke PublicKey di bawah ini, lalu build ulang.
//
// Kalau PublicKey dibiarkan kosong -> fitur online otomatis NONAKTIF,
// game tetap jalan normal (hanya pakai skor lokal + daftar resmi).
// =====================================================================
public static class OnlineLeaderboard
{
    // >>> TEMPEL "PUBLIC CODE" DREAMLO DI SINI, contoh: "5f3a1b2c7d8e9f0012ab34cd"
    public const string PublicKey = "6acdf4328f40bc15a8566a28";

    const string CacheFile = "online_scores.txt";

    public static List<ScoreEntry> Entries { get; private set; } = new List<ScoreEntry>();
    public static string Status { get; private set; } = "";
    public static bool Enabled => !string.IsNullOrEmpty(PublicKey);

    static bool loadedCache;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Entries = new List<ScoreEntry>();
        Status = "";
        loadedCache = false;
    }

    static string CachePath { get { return Path.Combine(Application.persistentDataPath, CacheFile); } }

    // ---------------- KIRIM SKOR ----------------
    public static IEnumerator Submit(string name, int score, int kills, int bosses, int seconds)
    {
        if (!Enabled) yield break;

        string url = "https://dreamlo.com/lb/" + PublicKey + "/add/" +
                     UnityWebRequest.EscapeURL(name) + "/" + score + "/" + seconds + "/" +
                     UnityWebRequest.EscapeURL(kills + "-" + bosses);

        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 8;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogWarning("[SEA Chan] Gagal kirim skor online: " + req.error);
        }
    }

    // ---------------- AMBIL DAFTAR ----------------
    public static IEnumerator Fetch()
    {
        LoadCache();
        if (!Enabled) yield break;

        string url = "https://dreamlo.com/lb/" + PublicKey + "/pipe";
        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 8;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var list = ParsePipe(req.downloadHandler.text);
                if (list.Count > 0)
                {
                    Entries = list;
                    try { File.WriteAllText(CachePath, req.downloadHandler.text); } catch { }
                    Status = list.Count + " skor dari semua pemain";
                }
                else
                {
                    Status = "belum ada skor online";
                }
            }
            else
            {
                Status = "tidak bisa online (pakai data terakhir)";
                Debug.LogWarning("[SEA Chan] Gagal ambil papan online: " + req.error);
            }

            if (UIManager.Instance != null) UIManager.Instance.RefreshLeaderboard();
        }
    }

    // Kirim skor lalu langsung perbarui tampilan papan skor.
    public static IEnumerator SubmitAndRefresh(ScoreEntry e)
    {
        yield return Submit(e.name, e.score, e.kills, e.bosses, Mathf.FloorToInt(e.time));
        yield return Fetch();
        if (UIManager.Instance != null) UIManager.Instance.RefreshLeaderboard();
    }

    static void LoadCache()
    {
        if (loadedCache) return;
        loadedCache = true;
        try
        {
            if (File.Exists(CachePath)) Entries = ParsePipe(File.ReadAllText(CachePath));
        }
        catch { }
    }

    // Format "pipe" dari dreamlo:  nama|skor|detik|teks|nama|skor|detik|teks|...
    // (teks dipakai menyimpan jumlah musuh & boss dalam bentuk "musuh-boss")
    static List<ScoreEntry> ParsePipe(string raw)
    {
        var list = new List<ScoreEntry>();
        if (string.IsNullOrEmpty(raw)) return list;

        var parts = raw.Split('|');
        for (int i = 0; i + 2 < parts.Length; i += 4)
        {
            string name = parts[i].Trim();
            if (string.IsNullOrEmpty(name)) continue;

            int score = 0, secs = 0, kills = 0, bosses = 0;
            int.TryParse(parts[i + 1].Trim(), out score);
            int.TryParse(parts[i + 2].Trim(), out secs);

            if (i + 3 < parts.Length)
            {
                var t = parts[i + 3].Trim().Split('-');
                if (t.Length > 0) int.TryParse(t[0], out kills);
                if (t.Length > 1) int.TryParse(t[1], out bosses);
            }

            list.Add(new ScoreEntry
            {
                name = name,
                score = score,
                kills = kills,
                bosses = bosses,
                time = secs,
                online = true
            });
        }
        return list;
    }
}
