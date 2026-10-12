using UnityEngine;

// Efek partikel sederhana (dibuat dari kode, tanpa file aset).
// Dipakai untuk ledakan kecil saat musuh mati.
public class Burst : MonoBehaviour
{
    Transform[] parts;
    SpriteRenderer[] rends;
    Vector2[] vel;
    float[] life;
    float[] maxLife;
    float[] baseSize;

    public static void Spawn(Vector3 pos, Color color, int count, float speed, float size, float duration)
    {
        var go = new GameObject("Burst");
        if (GameBootstrap.WorldRoot != null) go.transform.SetParent(GameBootstrap.WorldRoot, false);
        go.transform.position = pos;
        var b = go.AddComponent<Burst>();
        b.Build(color, count, speed, size, duration);
    }

    void Build(Color color, int count, float speed, float size, float duration)
    {
        count = Mathf.Clamp(count, 3, 40);
        parts = new Transform[count];
        rends = new SpriteRenderer[count];
        vel = new Vector2[count];
        life = new float[count];
        maxLife = new float[count];
        baseSize = new float[count];

        for (int i = 0; i < count; i++)
        {
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float sp = speed * Random.Range(0.45f, 1.1f);

            var sr = SpriteFactory.NewSprite("Spark", transform, color, false, 22,
                Vector3.zero, size * Random.Range(0.6f, 1.4f));

            parts[i] = sr.transform;
            rends[i] = sr;
            vel[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * sp;
            maxLife[i] = duration * Random.Range(0.7f, 1.25f);
            life[i] = maxLife[i];
            baseSize[i] = sr.transform.localScale.x;
        }
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && !gm.IsRunning) return;

        float dt = Time.deltaTime;
        bool alive = false;

        for (int i = 0; i < parts.Length; i++)
        {
            if (life[i] <= 0f) continue;

            life[i] -= dt;
            float k = Mathf.Clamp01(life[i] / maxLife[i]);

            // melambat (seperti gesekan) lalu memudar & mengecil
            vel[i] *= 1f - Mathf.Clamp01(4f * dt);
            parts[i].position += (Vector3)(vel[i] * dt);

            var c = rends[i].color;
            c.a = k;
            rends[i].color = c;

            float s = baseSize[i] * (0.3f + 0.7f * k);
            parts[i].localScale = new Vector3(s, s, 1f);

            if (life[i] > 0f) alive = true;
            else rends[i].enabled = false;
        }

        if (!alive) Destroy(gameObject);
    }
}

// Angka damage melayang (naik lalu memudar) saat musuh kena serangan.
public class DamageText : MonoBehaviour
{
    static int live;

    TextMesh tm;
    MeshRenderer mr;
    float t;
    float life = 0.7f;
    Vector3 vel;

    public static void Spawn(Vector3 pos, string text, Color color, float scale = 1f)
    {
        if (live > 36) return;   // batasi supaya tidak berat

        var go = new GameObject("DmgText");
        if (GameBootstrap.WorldRoot != null) go.transform.SetParent(GameBootstrap.WorldRoot, false);
        go.transform.position = pos;

        var tm = go.AddComponent<TextMesh>();
        var font = UIFactory.UIFont;
        tm.font = font;
        if (font != null) tm.fontSize = 48;
        tm.text = text;
        tm.color = color;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = 0.055f * scale;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            if (font != null) mr.material = font.material;
            mr.sortingOrder = 30;
        }

        var d = go.AddComponent<DamageText>();
        d.tm = tm;
        d.mr = mr;
        d.vel = new Vector3(Random.Range(-0.9f, 0.9f), 2.3f, 0f);
        live++;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;

        transform.position += vel * dt;
        vel *= 1f - Mathf.Clamp01(1.6f * dt);

        float k = 1f - Mathf.Clamp01(t / life);
        if (tm != null) { var c = tm.color; c.a = k; tm.color = c; }

        if (t >= life)
        {
            live--;
            Destroy(gameObject);
        }
    }
}
