using System.Collections.Generic;
using UnityEngine;

// Musuh: mengejar player, punya HP, dan bisa beranimasi bila sprite-nya berupa strip.
public class Enemy : MonoBehaviour
{
    public static readonly List<Enemy> All = new List<Enemy>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics() { All.Clear(); }

    public Health Health { get; private set; }
    public float Speed = 2f;
    public float ContactDamage = 8f;
    public float Radius = 0.35f;
    public int XPDrop = 3;
    public bool IsBoss;
    public string DisplayName = "Monster";
    public float HealthDropChance = 0.05f;

    PlayerController player;
    Sprite[] frames;
    SpriteRenderer sr;
    float animTimer;
    int animFrame;

    public void Configure(PlayerController target, float hp, float speed, float damage,
        float radius, int xp, bool boss, MonsterDef def, Color tint)
    {
        player = target;
        Speed = speed;
        ContactDamage = damage;
        Radius = radius;
        XPDrop = xp;
        IsBoss = boss;

        Health = GetComponent<Health>();
        if (Health == null) Health = gameObject.AddComponent<Health>();
        Health.Init(hp);
        Health.Died += OnDied;

        sr = GetComponent<SpriteRenderer>();

        if (def != null && def.Frames != null && def.Frames.Length > 0)
        {
            frames = def.Frames;
            DisplayName = def.Name;
            HealthDropChance = def.HealthDropChance;
            if (sr != null) sr.sprite = frames[0];
        }

        if (sr != null) sr.color = tint;
    }

    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void OnDied(Health h)
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.AddKill();
            if (IsBoss) gm.OnBossDefeated();
        }

        AudioManager.Play("hit", IsBoss ? 0.9f : 0.45f);

        // --- efek partikel saat musuh mati ---
        Color burstColor = sr != null ? sr.color : Color.white;
        if (IsBoss)
        {
            Burst.Spawn(transform.position, burstColor, 26, 8.5f, 0.3f, 0.8f);
            AuraVisual.Spawn(transform.position, 3.4f);          // gelombang ledakan
            Burst.Spawn(transform.position, new Color(1f, 0.85f, 0.5f), 14, 5f, 0.22f, 0.6f);
        }
        else
        {
            Burst.Spawn(transform.position, burstColor, Random.Range(7, 12), 5f, 0.16f, 0.42f);
        }

        int drops = IsBoss ? 8 : 1;
        int each = Mathf.Max(1, XPDrop / drops);
        for (int i = 0; i < drops; i++)
            XPGem.Spawn(transform.position + (Vector3)(Random.insideUnitCircle * 0.5f), each);

        // Drop kotak obat (RNG) — boss hampir selalu, robot biasa jarang.
        if (Random.value < HealthDropChance)
            HealthPickup.Spawn(transform.position + (Vector3)(Random.insideUnitCircle * 0.6f),
                IsBoss ? 50f : 25f);

        Destroy(gameObject);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsRunning || player == null) return;

        Vector2 to = (Vector2)player.transform.position - (Vector2)transform.position;
        float dist = to.magnitude;
        Vector2 dir = dist > 0.0001f ? to / dist : Vector2.zero;

        transform.position += (Vector3)(dir * Speed * Time.deltaTime);

        if (dist <= Radius + 0.4f && player.Health != null)
            player.Health.TakeDamage(ContactDamage * Time.deltaTime);

        if (frames != null && frames.Length > 1 && sr != null)
        {
            animTimer += Time.deltaTime;
            if (animTimer >= 1f / 8f)
            {
                animTimer = 0f;
                animFrame = (animFrame + 1) % frames.Length;
                sr.sprite = frames[animFrame];
            }
        }
    }

    public static Enemy Nearest(Vector3 pos, float maxDist)
    {
        Enemy best = null;
        float bestD = maxDist * maxDist;
        for (int i = 0; i < All.Count; i++)
        {
            var e = All[i];
            if (e == null) continue;
            float d = ((Vector2)(e.transform.position - pos)).sqrMagnitude;
            if (d <= bestD) { bestD = d; best = e; }
        }
        return best;
    }

    public static List<Enemy> NearestMany(Vector3 pos, int count)
    {
        var result = new List<Enemy>();
        for (int i = 0; i < All.Count; i++)
            if (All[i] != null) result.Add(All[i]);

        result.Sort((a, b) =>
            ((Vector2)(a.transform.position - pos)).sqrMagnitude
            .CompareTo(((Vector2)(b.transform.position - pos)).sqrMagnitude));

        if (result.Count > count) result.RemoveRange(count, result.Count - count);
        return result;
    }
}

// Kristal XP yang dijatuhkan musuh.
public class XPGem : MonoBehaviour
{
    public float Value = 3f;

    PlayerController player;
    float life = 30f;

    public static void Spawn(Vector3 pos, int value)
    {
        var sr = SpriteFactory.NewSprite("XP", GameBootstrap.WorldRoot,
            new Color(0.5f, 0.92f, 1f), false, 10, pos, 0.22f);
        var gem = sr.gameObject.AddComponent<XPGem>();
        gem.Value = value;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsRunning) return;

        if (player == null) { player = gm.Player; if (player == null) return; }

        life -= Time.deltaTime;
        if (life <= 0f) { Destroy(gameObject); return; }

        Vector2 to = (Vector2)player.transform.position - (Vector2)transform.position;
        float dist = to.magnitude;

        if (dist <= 0.6f)
        {
            player.Stats.AddXP(Value);
            AudioManager.Play("pickup", 0.5f);
            Destroy(gameObject);
            return;
        }

        if (dist <= player.pickupRadius)
            transform.position += (Vector3)(to.normalized * 12f * Time.deltaTime);
    }
}

// Kotak obat (health pack) — drop jarang (RNG) dari musuh tertentu / boss.
public class HealthPickup : MonoBehaviour
{
    public float Heal = 25f;

    PlayerController player;
    float life = 25f;

    public static void Spawn(Vector3 pos, float heal)
    {
        var sr = SpriteFactory.NewSprite("HealthPack", GameBootstrap.WorldRoot,
            Color.white, false, 11, pos, 0.5f);
        sr.sprite = SpriteFactory.Health;
        var hp = sr.gameObject.AddComponent<HealthPickup>();
        hp.Heal = heal;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsRunning) return;

        if (player == null) { player = gm.Player; if (player == null) return; }

        life -= Time.deltaTime;
        if (life <= 0f) { Destroy(gameObject); return; }

        Vector2 to = (Vector2)player.transform.position - (Vector2)transform.position;
        float dist = to.magnitude;

        if (dist <= 0.7f)
        {
            if (player.Health != null) player.Health.Heal(Heal);
            AudioManager.Play("pickup", 0.85f);
            Destroy(gameObject);
            return;
        }

        if (dist <= player.pickupRadius + 1.5f)
            transform.position += (Vector3)(to.normalized * 10f * Time.deltaTime);
    }
}

// Spawner endless: musuh makin kuat seiring waktu, Boss muncul berkala.
public class EnemySpawner : MonoBehaviour
{
    const int MaxEnemies = 170;

    PlayerController player;
    Camera cam;
    float spawnTimer;
    float bossTimer;
    float elapsed;
    int tier = 1;

    public void Init(PlayerController p, Camera camera)
    {
        player = p;
        cam = camera;
        spawnTimer = 0.4f;
        bossTimer = 26f;
    }

    public void OnTierUp(int newTier)
    {
        tier = newTier;
        bossTimer = 30f;
        elapsed += 6f;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsRunning || player == null) return;

        elapsed += Time.deltaTime;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            spawnTimer = Mathf.Max(0.14f, 0.9f - elapsed * 0.009f - (tier - 1) * 0.05f);
            int batch = 1 + tier / 2;
            for (int i = 0; i < batch; i++)
            {
                if (Enemy.All.Count >= MaxEnemies) break;
                SpawnNormal();
            }
        }

        bossTimer -= Time.deltaTime;
        if (bossTimer <= 0f && !BossAlive() && Enemy.All.Count < MaxEnemies)
        {
            bossTimer = 40f;
            SpawnBoss();
        }
    }

    bool BossAlive()
    {
        for (int i = 0; i < Enemy.All.Count; i++)
            if (Enemy.All[i] != null && Enemy.All[i].IsBoss) return true;
        return false;
    }

    Vector3 EdgePosition()
    {
        float half = cam != null ? cam.orthographicSize : 7f;
        float w = half * (cam != null ? cam.aspect : 1.78f);
        float pad = 1.5f;
        Vector2 p = player.transform.position;

        switch (Random.Range(0, 4))
        {
            case 0: return new Vector3(p.x - w - pad, p.y + Random.Range(-half, half), 0f);
            case 1: return new Vector3(p.x + w + pad, p.y + Random.Range(-half, half), 0f);
            case 2: return new Vector3(p.x + Random.Range(-w, w), p.y - half - pad, 0f);
            default: return new Vector3(p.x + Random.Range(-w, w), p.y + half + pad, 0f);
        }
    }

    static readonly Color[] Tints =
    {
        new Color(1.00f, 0.95f, 0.95f),
        new Color(1.00f, 0.78f, 0.70f),
        new Color(0.85f, 0.80f, 1.00f),
        new Color(0.78f, 1.00f, 0.85f),
        new Color(0.80f, 0.92f, 1.00f)
    };

    void SpawnNormal()
    {
        float scale = 1f + elapsed / 90f + (tier - 1) * 0.35f;
        float hp = (10f + 4f * tier) * scale;
        float spd = Random.Range(1.8f, 2.6f) + tier * 0.08f;
        float dmg = 6f + tier * 1.5f;

        Color tint = Tints[(tier - 1) % Tints.Length];
        var def = MonsterArt.GetRegular(Random.Range(0, Mathf.Max(1, MonsterArt.RegularCount)));

        var sr = SpriteFactory.NewSprite("Enemy", GameBootstrap.WorldRoot, tint, true, 8,
            EdgePosition(), 1f);
        var e = sr.gameObject.AddComponent<Enemy>();
        e.Configure(player, hp, spd, dmg, 0.35f, 3, false, def, tint);
    }

    void SpawnBoss()
    {
        float hp = (120f + 80f * tier) * (1f + elapsed / 150f);
        var def = MonsterArt.GetBoss(tier - 1);
        Color tint = Color.white;

        var sr = SpriteFactory.NewSprite("Boss", GameBootstrap.WorldRoot, tint, true, 9,
            EdgePosition(), 1f);
        var e = sr.gameObject.AddComponent<Enemy>();
        e.Configure(player, hp, 1.5f + tier * 0.05f, 14f + tier * 2f, 0.9f, 25 + tier * 5, true, def, tint);

        AudioManager.Play("boss");
        AudioManager.Track("bgm_boss");
        if (UIManager.Instance != null) UIManager.Instance.ShowBanner("BOSS MUNCUL!");
    }
}
