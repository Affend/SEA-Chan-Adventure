using System.Collections.Generic;
using UnityEngine;

// Keunikan serangan tiap Boss (masing-masing beda, tidak digabung).
public enum BossSkill { None, Gun, Aura, Laser }

// Satu jenis monster: kumpulan frame animasi + nama tampil.
public class MonsterDef
{
    public Sprite[] Frames;
    public string Name;
    public bool IsBoss;
    public bool Elite;          // boss khusus (punya cutscene & 2 serangan)
    public BossSkill Skill = BossSkill.None;
    // Peluang menjatuhkan kotak obat (RNG, jarang).
    public float HealthDropChance = 0.02f;
}

// Memuat sprite monster dari Assets/Resources/Monsters/.
// File berisi "boss" -> Boss. File strip (>1 frame) otomatis dianimasikan.
public static class MonsterArt
{
    static MonsterDef[] _regular;
    static MonsterDef[] _bosses;
    static MonsterDef[] _elite;
    static bool _loaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        _regular = null;
        _bosses = null;
        _elite = null;
        _loaded = false;
    }

    public static int RegularCount { get { Load(); return _regular.Length; } }
    public static int BossCount { get { Load(); return _bosses.Length; } }
    public static int EliteCount { get { Load(); return _elite.Length; } }

    public static MonsterDef GetRegular(int i)
    {
        Load();
        if (_regular.Length == 0) return null;
        int k = i % _regular.Length; if (k < 0) k += _regular.Length;
        return _regular[k];
    }

    public static MonsterDef GetBoss(int i)
    {
        Load();
        if (_bosses.Length == 0) return GetRegular(i);
        int k = i % _bosses.Length; if (k < 0) k += _bosses.Length;
        return _bosses[k];
    }

    // Boss ELITE (dipakai untuk boss yang punya cutscene).
    public static MonsterDef GetEliteBoss(int i)
    {
        Load();
        if (_elite.Length == 0) return GetBoss(i);
        int k = i % _elite.Length; if (k < 0) k += _elite.Length;
        return _elite[k];
    }

    static void Load()
    {
        if (_loaded) return;
        _loaded = true;

        var regular = new List<MonsterDef>();
        var bosses = new List<MonsterDef>();
        var elite = new List<MonsterDef>();

        var texs = Resources.LoadAll<Texture2D>("Monsters");
        System.Array.Sort(texs, (a, b) => string.CompareOrdinal(a.name, b.name));

        foreach (var t in texs)
        {
            if (t == null) continue;
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;

            bool boss = t.name.ToLowerInvariant().Contains("boss");
            var def = MakeDef(t, boss);
            if (def == null) continue;
            if (!boss) regular.Add(def);
            else if (def.Elite) elite.Add(def);
            else bosses.Add(def);
        }

        _regular = regular.ToArray();
        _bosses = bosses.ToArray();
        _elite = elite.ToArray();

        if (_regular.Length == 0 && _bosses.Length == 0)
            Debug.LogWarning("[SEA Chan] Tidak ada sprite monster di Assets/Resources/Monsters/.");
    }

    static MonsterDef MakeDef(Texture2D tex, bool boss)
    {
        string low = tex.name.ToLowerInvariant();
        bool elite = boss && low.Contains("elite");

        int size = tex.height;
        if (size <= 0) return null;

        int count = Mathf.Max(1, tex.width / size);
        count = Mathf.Min(count, 12);

        float targetHeight = boss ? (elite ? 3.4f : 2.6f) : 1.0f;
        float ppu = size / targetHeight;

        var frames = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            var rect = new Rect(i * size, 0, size, size);
            frames[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.08f), ppu);
        }

        // Peluang drop HP: dibuat JARANG supaya tidak terlalu mudah.
        float drop = 0.02f;                       // robot biasa
        if (boss) drop = 0.50f;                   // boss (kadang)
        else if (low.Contains("heavy") || low.Contains("hybrid") || low.Contains("tank")) drop = 0.07f;
        else if (low.Contains("drone")) drop = 0.012f;

        // Keunikan serangan tiap boss (dari namanya)
        BossSkill skill = BossSkill.None;
        if (boss)
        {
            if (low.Contains("mech")) skill = BossSkill.Gun;
            else if (low.Contains("brain")) skill = BossSkill.Aura;
            else if (low.Contains("spider")) skill = BossSkill.Laser;
            else skill = BossSkill.Gun;
        }

        return new MonsterDef
        {
            Frames = frames,
            Name = PrettyName(tex.name),
            IsBoss = boss,
            Elite = elite,
            Skill = skill,
            HealthDropChance = drop
        };
    }

    static string PrettyName(string raw)
    {
        string s = raw;
        int us = s.IndexOf('_');
        // buang awalan "mon_00_" / "boss_0_"
        if (us > 0)
        {
            var parts = s.Split('_');
            int start = 0;
            if (parts.Length > 1 && int.TryParse(parts[1], out _)) start = 2;
            else if (parts[0] == "mon" || parts[0] == "boss") start = 1;
            s = string.Join("_", parts, start, parts.Length - start);
        }
        s = s.Replace('_', ' ').Trim();
        if (s.Length == 0) s = raw;
        return char.ToUpperInvariant(s[0]) + (s.Length > 1 ? s.Substring(1) : "");
    }
}
