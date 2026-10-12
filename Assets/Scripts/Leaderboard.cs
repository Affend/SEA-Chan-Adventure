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

    public static int Best()
    {
        var l = Load();
        return l.Count > 0 ? l[0].score : 0;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }

    public static string Format(int maxRows)
    {
        var list = Load();
        if (list.Count == 0) return "Belum ada skor. Jadilah yang pertama!";

        var sb = new System.Text.StringBuilder();
        int n = Mathf.Min(maxRows, list.Count);
        for (int i = 0; i < n; i++)
        {
            var e = list[i];
            sb.Append(i + 1).Append(".  ").Append(e.name)
              .Append("  —  ").Append(e.score)
              .Append("   (musuh ").Append(e.kills).Append(", boss ").Append(e.bosses).Append(")\n");
        }
        return sb.ToString();
    }
}
