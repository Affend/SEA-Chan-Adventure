using System.Collections.Generic;
using UnityEngine;

// Sprite SEA Chan + animasi jalan.
// Catatan penting:
//  - Gambar "side" (baris WALK LEFT) sebenarnya MENGHADAP KANAN -> kanan = gambar asli,
//    kiri = dicerminkan (flipX).
//  - Baris sisi hanya dipakai 2 frame terbaik (idle + langkah) karena frame 4 di sheet
//    menggambar badan membalik, sehingga animasinya terlihat "kiri-kanan".
public class PlayerVisual : MonoBehaviour
{
    const float TargetHeight = 1.25f; // tinggi karakter di dunia (unit) — proporsional dgn dinding (1 unit)
    const float PivotY = 0.06f;
    const float FrameRate = 8f;

    static readonly int[] FrontCycle = { 0, 1, 2, 3 };
    static readonly int[] SideCycle = { 0, 1 };

    SpriteRenderer sr;
    PlayerController pc;

    Sprite[] front;   // seachan_front_*
    Sprite[] side;    // seachan_side_* (menghadap KANAN)

    int facing;       // 0 = depan, 1 = kiri, 2 = kanan
    int step;
    float timer;
    bool loaded;

    // indikator kena damage (kedip merah pada sprite)
    const float HitFlashTime = 0.5f;
    static readonly Color HitColor = new Color(1f, 0.28f, 0.32f);
    Color baseColor = Color.white;
    float hitFlash;
    float lastHP = -1f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        pc = GetComponent<PlayerController>();
        Load();
    }

    void Load()
    {
        front = LoadRow("front");
        side = LoadRow("side");
        if (side == null) side = LoadRow("left");
        if (side == null) side = LoadRow("right");

        loaded = (front != null && front.Length > 0) || (side != null && side.Length > 0);
        if (!loaded)
        {
            Debug.LogWarning("[SEA Chan] Sprite tidak ditemukan di Assets/Resources/SEAChan/.");
            return;
        }
        if (sr != null)
        {
            baseColor = sr.color;
            sr.sprite = Pick();
        }
        if (pc != null && pc.Health != null) lastHP = pc.Health.Current;
    }

    Sprite[] LoadRow(string key)
    {
        var list = new List<Sprite>();
        for (int i = 0; i < 12; i++)
        {
            var tex = Resources.Load<Texture2D>("SEAChan/seachan_" + key + "_" + i);
            if (tex == null) break;
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            float ppu = tex.height / TargetHeight;   // ukuran mengikuti tinggi, jadi proporsinya tetap
            list.Add(Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, PivotY), ppu));
        }
        return list.Count > 0 ? list.ToArray() : null;
    }

    Sprite Pick()
    {
        bool useSide = (facing != 0) && side != null && side.Length > 0;
        var row = useSide ? side : front;
        if (row == null || row.Length == 0) row = side;
        if (row == null || row.Length == 0) return null;

        var cycle = useSide ? SideCycle : FrontCycle;
        int idx = cycle[((step % cycle.Length) + cycle.Length) % cycle.Length];
        if (idx >= row.Length) idx = row.Length - 1;
        return row[idx];
    }

    void Update()
    {
        if (!loaded || sr == null) return;

        var gm = GameManager.Instance;
        if (gm != null && !gm.IsRunning) return;

        // --- indikator kena damage: kedip merah sebentar ---
        if (pc != null && pc.Health != null)
        {
            float hp = pc.Health.Current;
            if (lastHP >= 0f && hp < lastHP - 0.01f) hitFlash = HitFlashTime;
            lastHP = hp;
        }
        if (hitFlash > 0f)
        {
            hitFlash -= Time.deltaTime;
            bool on = Mathf.Repeat(Mathf.Max(hitFlash, 0f), 0.18f) > 0.09f;
            sr.color = (hitFlash > 0f && on) ? HitColor : baseColor;
        }
        else if (sr.color != baseColor)
        {
            sr.color = baseColor;
        }

        if (pc != null && pc.Moving)
        {
            Vector2 d = pc.MoveDir;
            if (Mathf.Abs(d.x) >= Mathf.Abs(d.y)) facing = d.x >= 0f ? 2 : 1;
            else facing = 0;

            timer += Time.deltaTime;
            float sp = 1f / FrameRate;
            if (timer >= sp) { timer -= sp; step++; }
        }
        else
        {
            timer = 0f;
            step = 0;
        }

        // gambar menghadap kanan -> arah kiri dicerminkan
        sr.flipX = (facing == 1);

        var s = Pick();
        if (s != null) sr.sprite = s;
    }
}
