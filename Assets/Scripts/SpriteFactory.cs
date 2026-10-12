using UnityEngine;

// Membuat sprite sederhana (lingkaran & kotak) langsung dari kode,
// supaya project tidak butuh file gambar apa pun agar bisa langsung jalan.
// Nanti tinggal diganti dengan sprite SEA Chan kalau sudah di-import.
public static class SpriteFactory
{
    static Sprite _circle;
    static Sprite _square;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        _circle = null;
        _square = null;
        _gun = null;
        _health = null;
        _arrow = null;
    }

    public static Sprite Circle
    {
        get
        {
            if (_circle == null) _circle = Make(64, true);
            return _circle;
        }
    }

    public static Sprite Square
    {
        get
        {
            if (_square == null) _square = Make(64, false);
            return _square;
        }
    }

    static Sprite _gun;
    public static Sprite Gun
    {
        get
        {
            if (_gun == null) _gun = MakeGun();
            return _gun;
        }
    }

    static Sprite _health;
    public static Sprite Health
    {
        get
        {
            if (_health == null) _health = MakeHealth();
            return _health;
        }
    }

    static Sprite _arrow;
    // Segitiga penunjuk arah (dipakai indikator boss di luar layar).
    public static Sprite Arrow
    {
        get
        {
            if (_arrow == null) _arrow = MakeArrow();
            return _arrow;
        }
    }

    static Sprite MakeArrow()
    {
        int s = 32;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[s * s];
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0);

        for (int y = 0; y < s; y++)
        {
            int w = Mathf.RoundToInt(Mathf.Lerp(1f, s * 0.42f, 1f - Mathf.Abs(y - s * 0.5f) / (s * 0.5f)));
            for (int x = 0; x < w; x++) px[y * s + x] = new Color32(255, 255, 255, 255);
        }
        // garis tepi gelap
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
                if (px[y * s + x].a > 0)
                {
                    bool edge = false;
                    if (x + 1 < s && px[y * s + x + 1].a == 0) edge = true;
                    if (y + 1 < s && px[(y + 1) * s + x].a == 0) edge = true;
                    if (edge) px[y * s + x] = new Color32(24, 26, 34, 255);
                }

        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
    }

    static Sprite Make(int size, bool round)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        float c = (size - 1) * 0.5f;
        float r = size * 0.5f - 1.5f;
        var px = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inside = true;
                if (round)
                {
                    float dx = x - c;
                    float dy = y - c;
                    inside = (dx * dx + dy * dy) <= r * r;
                }
                px[y * size + x] = inside
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(0, 0, 0, 0);
            }
        }

        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // Membuat GameObject berisi SpriteRenderer siap pakai.
    public static SpriteRenderer NewSprite(string name, Transform parent, Color color,
        bool round, int sortingOrder, Vector3 localPos, float scale)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = Vector3.one * scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = round ? Circle : Square;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return sr;
    }

    // Sprite blaster futuristik untuk senjata yang mengelilingi player.
    static Sprite MakeGun()
    {
        int w = 26, h = 14;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        var px = new Color32[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0);

        var body = new Color32(150, 162, 186, 255);
        var dark = new Color32(74, 84, 106, 255);
        var core = new Color32(86, 226, 240, 255);

        System.Action<int, int, Color32> set = (x, y, c) =>
        {
            if (x >= 0 && y >= 0 && x < w && y < h) px[y * w + x] = c;
        };

        for (int x = 9; x < 25; x++)
            for (int y = 6; y < 10; y++) set(x, y, body);          // laras
        for (int x = 2; x < 12; x++)
            for (int y = 4; y < 12; y++) set(x, y, body);          // badan
        for (int x = 3; x < 8; x++)
            for (int y = 1; y < 5; y++) set(x, y, dark);           // gagang
        for (int y = 4; y < 12; y++) set(11, y, dark);             // sambungan
        for (int x = 4; x < 10; x++) set(x, 7, core);              // inti energi
        for (int x = 4; x < 10; x++) set(x, 8, core);
        for (int y = 7; y < 9; y++) set(24, y, core);              // ujung laras

        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w * 0.9f);
    }

    // Kotak obat (health pack) untuk drop penyembuh.
    static Sprite MakeHealth()
    {
        int s = 24;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        var px = new Color32[s * s];
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0);

        var shell = new Color32(232, 238, 248, 255);
        var edge = new Color32(120, 132, 156, 255);
        var cross = new Color32(228, 62, 72, 255);

        System.Action<int, int, Color32> set = (x, y, c) =>
        {
            if (x >= 0 && y >= 0 && x < s && y < s) px[y * s + x] = c;
        };

        for (int y = 2; y < 22; y++)
            for (int x = 2; x < 22; x++) set(x, y, shell);
        for (int x = 2; x < 22; x++) { set(x, 2, edge); set(x, 21, edge); }
        for (int y = 2; y < 22; y++) { set(2, y, edge); set(21, y, edge); }

        for (int y = 7; y < 17; y++)
            for (int x = 10; x < 14; x++) set(x, y, cross);
        for (int y = 10; y < 14; y++)
            for (int x = 7; x < 17; x++) set(x, y, cross);

        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
    }
}
