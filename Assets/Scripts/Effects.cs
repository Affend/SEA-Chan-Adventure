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
