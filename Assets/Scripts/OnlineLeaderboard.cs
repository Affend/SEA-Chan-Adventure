using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

// =====================================================================
// PAPAN SKOR ONLINE (semua pemain, semua device) memakai dreamlo.com
//
// PENTING — dari dokumentasi resmi dreamlo:
//   • Kode PUBLIC  = HANYA untuk MEMBACA papan skor.
//   • Kode PRIVATE = untuk MENGIRIM / menghapus skor  >>> WAJIB DIISI DI BAWAH.
//   dreamlo menyarankan kode private di-"compile in your code",
//   jadi aman ikut di dalam game.
//
// Cara dapat dua kode itu: https://dreamlo.com -> "Get Yours Now" -> buat leaderboard.
// =====================================================================
public static class OnlineLeaderboard
{
    // >>> KODE PUBLIC (untuk membaca daftar) <<<
    public const string PublicKey = "6acdf4328f40bc15a8566a28";

    // >>> KODE PRIVATE (untuk mengirim skor) <<<  <-- WAJIB DIISI; kalau kosong skor tidak pernah terkirim
    public const string PrivateKey = "s4cwNgooz0WP1M3NQT_Z0QlNZ4gRsOmki53Y7xes4LtQ";

    const string CacheFile = "online_scores.txt";

    public static List<ScoreEntry> Entries { get; private set; } = new List<ScoreEntry>();
    public static string Status { get; private set; } = "";
    public static bool ReadEnabled => !string.IsNullOrEmpty(PublicKey);
    public static bool WriteEnabled => !string.IsNullOrEmpty(PrivateKey);

    static bool loadedCache;
    static bool hostChecked;
    static string host = "https://dreamlo.com";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Entries = new List<ScoreEntry>();
        Status = "";
        loadedCache = false;
        hostChecked = false;
        host = "https://dreamlo.com";
    }

    static string CachePath { get { return Path.Combine(Application.persistentDataPath, CacheFile); } }

    // SSL dreamlo bisa belum aktif; kalau begitu https menjawab "ERROR:SSL not enabled".
    // Otomatis pindah ke http supaya tetap jalan.
    static IEnumerator EnsureHost()
    {
        if (hostChecked) yield break;
        hostChecked = true;
        if (!ReadEnabled) yield break;

        using (var req = UnityWebRequest.Get("https://dreamlo.com/lb/" + PublicKey + "/pipe"))
        {
            req.timeout = 8;
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success &&
                req.downloadHandler.text.Contains("SSL not enabled"))
            {
                host = "http://dreamlo.com";
                Debug.Log("[SEA Chan] SSL dreamlo belum aktif -> memakai http.");
            }
        }
    }

    // ---------------- AMBIL DAFTAR (kode PUBLIC) ----------------
    public static IEnumerator Fetch()
    {
        LoadCache();
        if (!ReadEnabled) yield break;
        yield return EnsureHost();

        using (var req = UnityWebRequest.Get(host + "/lb/" + PublicKey + "/pipe"))
        {
            req.timeout = 8;
            yield return req.SendWebRequest();
            HandleResponse(req, "daftar");
        }

        if (UIManager.Instance != null) UIManager.Instance.RefreshLeaderboard();
    }

    // ---------------- KIRIM SKOR (kode PRIVATE) ----------------
    // "add-pipe" = kirim skor SEKALIGUS minta daftar terbaru dalam satu permintaan
    // (hemat, karena dreamlo membatasi 1 permintaan per detik).
    public static IEnumerator Submit(string name, int score, int kills, int bosses, int seconds)
    {
        if (!WriteEnabled) yield break;
        yield return EnsureHost();

        string url = host + "/lb/" + PrivateKey + "/add-pipe/" +
                     UnityWebRequest.EscapeURL(name) + "/" + score + "/" + seconds + "/" +
                     UnityWebRequest.EscapeURL(kills + "-" + bosses);

        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 8;
            yield return req.SendWebRequest();
            HandleResponse(req, "kirim skor");
        }
    }

    public static IEnumerator SubmitAndRefresh(ScoreEntry e)
    {
        yield return Submit(e.name, e.score, e.kills, e.bosses, Mathf.FloorToInt(e.time));
        if (UIManager.Instance != null) UIManager.Instance.RefreshLeaderboard();
    }

    // Kirim semua skor yang tersimpan di laptop ini
    // (mis. skor lama yang didapat SEBELUM fitur online diaktifkan).
    public static IEnumerator SubmitAllLocal()
    {
        if (!WriteEnabled)
        {
            Status = "kode PRIVATE belum diisi";
            if (UIManager.Instance != null) UIManager.Instance.RefreshLeaderboard();
            yield break;
        }

        var local = Leaderboard.Load();
        if (local.Count == 0)
        {
            Status = "belum ada skor di laptop ini";
            if (UIManager.Instance != null) UIManager.Instance.RefreshLeaderboard();
            yield break;
        }

        for (int i = 0; i < local.Count; i++)
        {
            var e = local[i];
            Status = "mengirim " + (i + 1) + "/" + local.Count + " ...";
            if (UIManager.Instance != null) UIManager.Instance.RefreshLeaderboard();

            yield return Submit(e.name, e.score, e.kills, e.bosses, Mathf.FloorToInt(e.time));
            yield return new WaitForSecondsRealtime(1.3f);   // batas dreamlo: 1 permintaan / detik
        }

        Status = local.Count + " skor dari laptop ini sudah dikirim";
        if (UIManager.Instance != null) UIManager.Instance.RefreshLeaderboard();
    }

    // ---------------- PEMBANTU ----------------
    static void HandleResponse(UnityWebRequest req, string what)
    {
        if (req.result != UnityWebRequest.Result.Success)
        {
            Status = "tidak bisa online (pakai data terakhir)";
            Debug.LogWarning("[SEA Chan] Gagal " + what + " ke dreamlo: " + req.error);
            return;
        }

        string body = req.downloadHandler.text;
        if (body.StartsWith("ERROR"))
        {
            Status = body.Trim();
            Debug.LogWarning("[SEA Chan] dreamlo menolak " + what + ": " + Status);
            return;
        }

        var list = ParsePipe(body);
        if (list.Count > 0)
        {
            Entries = list;
            try { File.WriteAllText(CachePath, body); } catch { }
            Status = list.Count + " skor dari semua pemain";
        }
        else
        {
            Status = "belum ada skor online";
        }
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

    // Format dreamlo: nama|skor|detik|teks|...   (teks dipakai menyimpan "musuh-boss")
    static List<ScoreEntry> ParsePipe(string raw)
    {
        var list = new List<ScoreEntry>();
        if (string.IsNullOrEmpty(raw)) return list;

        string[] f = raw.Split('|');
        int n = f.Length;
        if (n > 0 && string.IsNullOrEmpty(f[n - 1].Trim())) n--;   // buang kolom kosong terakhir

        int stride = GuessStride(f, n);
        for (int i = 0; i + 2 < n; i += stride)
        {
            string name = f[i].Trim();
            if (string.IsNullOrEmpty(name)) continue;
            if (name.StartsWith("ERROR")) continue;     // pesan error dreamlo, bukan skor

            int score = 0, secs = 0, kills = 0, bosses = 0;
            int.TryParse(f[i + 1].Trim(), out score);
            int.TryParse(f[i + 2].Trim(), out secs);

            if (i + 3 < n)
            {
                var t = f[i + 3].Trim().Split('-');
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

    // Sebagian server dreamlo menambah kolom tanggal & flag (6 kolom, bukan 4).
    static int GuessStride(string[] f, int n)
    {
        if (n >= 6 && n % 6 == 0 && n % 4 != 0) return 6;
        if (n >= 4 && n % 4 == 0 && n % 6 != 0) return 4;
        if (n >= 6 && LooksLikeDate(f[4])) return 6;
        return 4;
    }

    static bool LooksLikeDate(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        if (s.IndexOf(':') < 0) return false;
        return s.IndexOf('/') >= 0 ||
               s.IndexOf("AM", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               s.IndexOf("PM", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
