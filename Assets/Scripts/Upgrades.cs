using System;
using System.Collections.Generic;
using UnityEngine;

// Definisi satu pilihan upgrade.
public class UpgradeDef
{
    public string Title;
    public string Desc;
    public Action Apply;
}

// Membuat daftar upgrade acak untuk ditawarkan saat naik level.
public static class UpgradeManager
{
    public static List<UpgradeDef> Roll(PlayerController p, int count)
    {
        var pool = new List<UpgradeDef>();
        var wm = p.Weapons;

        // --- Senjata: senjata baru atau naikkan level ---
        foreach (WeaponType type in Enum.GetValues(typeof(WeaponType)))
        {
            var existing = wm.Get(type);

            if (existing == null)
            {
                var t = type;
                pool.Add(new UpgradeDef
                {
                    Title = "Senjata Baru: " + t,
                    Desc = "Dapatkan senjata " + t,
                    Apply = () => wm.Add(t)
                });
            }
            else if (existing.CanLevelUp)
            {
                var w = existing;
                pool.Add(new UpgradeDef
                {
                    Title = w.DisplayName + "  →  Lv " + (w.Level + 1),
                    Desc = "Perkuat senjata ini",
                    Apply = () => w.LevelUp()
                });
            }
        }

        // --- Upgrade pasif ---
        pool.Add(new UpgradeDef
        {
            Title = "Max HP +20",
            Desc = "Darah maksimum bertambah",
            Apply = () => p.Health.AddMax(20f, true)
        });
        pool.Add(new UpgradeDef
        {
            Title = "Kecepatan +12%",
            Desc = "Bergerak lebih cepat",
            Apply = () => p.moveSpeed *= 1.12f
        });
        pool.Add(new UpgradeDef
        {
            Title = "Damage +15%",
            Desc = "Semua senjata lebih kuat",
            Apply = () => p.Stats.DamageBonus += 0.15f
        });
        pool.Add(new UpgradeDef
        {
            Title = "Cooldown -10%",
            Desc = "Senjata menyerang lebih sering",
            Apply = () => p.Stats.CooldownReduction = Mathf.Min(0.6f, p.Stats.CooldownReduction + 0.10f)
        });
        pool.Add(new UpgradeDef
        {
            Title = "Regen +0.7 / detik",
            Desc = "Darah pulih sendiri",
            Apply = () => p.regenPerSecond += 0.7f
        });
        pool.Add(new UpgradeDef
        {
            Title = "Radius Ambil +40%",
            Desc = "Menarik XP dari lebih jauh",
            Apply = () => p.pickupRadius *= 1.4f
        });

        // --- Acak & ambil sejumlah yang diminta ---
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
        }
        if (pool.Count > count) pool.RemoveRange(count, pool.Count - count);
        return pool;
    }
}
