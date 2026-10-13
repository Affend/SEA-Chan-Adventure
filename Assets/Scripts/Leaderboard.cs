using System;
using System.Collections.Generic;
using UnityEngine;

// Papan skor (leaderboard) sederhana yang disimpan di PlayerPrefs.
[Serializable]
public class ScoreEntry
{
    public string name;
    public int score;
    public int kills;
    public int bosses;
    public float time;
    // true = skor resmi dari leaderboard.json (bukan hasil main di komputer ini)
    public bool official;
    // true = skor dari papan online (semua pemain, semua device)
    public bool online;
}

[Serializable]
public class ScoreList
{
    public List<ScoreEntry> items = new List<ScoreEntry>();
}

public static class Leaderboard
{
    const string Key = "sea_leaderboard_v1";
    const int MaxEntries = 10;
    const string DefaultName = "Pemain";

    public static List<ScoreEntry> Load()
    {
        string raw = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(raw)) return new List<ScoreEntry>();
        try
        {
            var l = JsonUtility.FromJson<ScoreList>(raw);
            return (l != null && l.items != null) ? l.items : new List<ScoreEntry>();
        }
        catch { return new List<ScoreEntry>(); }
    }

    static void Save(List<ScoreEntry> list)
    {
        var l = new ScoreList();
        l.items = list;
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(l));
        PlayerPrefs.Save();
    }

    // Menambahkan skor; mengembalikan peringkat (0 = teratas) atau -1 kalau tidak masuk 10 besar.
    public static int Add(string playerName, int score, int kills, int bosses, float time)
    {
        var list = Load();

        string nm = string.IsNullOrEmpty(playerName) ? DefaultName : playerName.Trim();
        if (nm.Length == 0) nm = DefaultName;
        if (nm.Length > 14) nm = nm.Substring(0, 14);

        var e = new ScoreEntry { name = nm, score = score, kills = kills, bosses = bosses, time = time };
        list.Add(e);
        list.Sort((a, b) => b.score.CompareTo(a.score));
        if (list.Count > MaxEntries) list.RemoveRange(MaxEntries, list.Count - MaxEntries);
        Save(list);

        return list.IndexOf(e);
    }

    // Rekor milik pemain di device ini (bukan patokan resmi / skor pemain lain).
    public static int Best()
    {
        var l = Load();
        return l.Count > 0 ? l[0].score : 0;
    }

    // Membaca daftar skor dari teks JSON (dipakai leaderboard.json).
    public static List<ScoreEntry> ParseItems(string json)
    {
        var result = new List<ScoreEntry>();
        if (string.IsNullOrEmpty(json)) return result;
        try
        {
            var l = JsonUtility.FromJson<ScoreList>(json);
            if (l != null && l.items != null)
            {
                foreach (var e in l.items)
                {
                    if (e == null || string.IsNullOrEmpty(e.name)) continue;
                    e.official = true;
                    result.Add(e);
                }
            }
        }
        catch { }
        return result;
    }

    // Gabungan: skor pemain di device ini + skor online (semua pemain) + skor resmi dari file.
    public static List<ScoreEntry> Merged(int maxRows)
    {
        var all = new List<ScoreEntry>();
        all.AddRange(Load());

        // skor online — yang sama persis dengan skor lokal tidak ditampilkan dua kali
        var online = OnlineLeaderboard.Entries;
        for (int i = 0; i < online.Count; i++)
        {
            var o = online[i];
            if (o == null) continue;

            bool dup = false;
            for (int j = 0; j < all.Count; j++)
                if (all[j].name == o.name && all[j].score == o.score) { dup = true; break; }
            if (!dup) all.Add(o);
        }

        all.AddRange(LeaderboardSource.Entries);
        all.Sort((a, b) => b.score.CompareTo(a.score));
        if (maxRows > 0 && all.Count > maxRows) all.RemoveRange(maxRows, all.Count - maxRows);
        return all;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }

    // highlightScore/highlightName = isi yang baru saja disimpan pemain (diberi warna + tanda).
    public static string Format(int maxRows, string highlightName = null, int highlightScore = -1)
    {
        var list = Merged(maxRows);
        if (list.Count == 0) return "Belum ada skor. Jadilah yang pertama!";

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            bool mine = highlightName != null && e.name == highlightName && e.score == highlightScore;

            // warna medali untuk 3 besar
            string col;
            if (mine) col = "#8DEBFF";
            else if (i == 0) col = "#FFD34D";
            else if (i == 1) col = "#CFD8E6";
            else if (i == 2) col = "#E0A66A";
            else col = "#EAF0FA";

            sb.Append("<color=").Append(col).Append('>');
            sb.Append(i + 1).Append(".  ");
            if (e.official) sb.Append("[RESMI] ");    // patokan dari file leaderboard
            else if (e.online) sb.Append("[ONLINE] "); // skor pemain dari device lain
            if (mine) sb.Append("> ");
            sb.Append(e.name)
              .Append("  —  ").Append(e.score)
              .Append("   (musuh ").Append(e.kills).Append(", boss ").Append(e.bosses).Append(")");
            if (mine) sb.Append("   << KAMU");
            sb.Append("</color>\n");
        }
        return sb.ToString();
    }
}
