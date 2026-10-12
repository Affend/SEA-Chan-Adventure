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
    public float HealthDropChance = 0.02f;
    public BossSkill Skill = BossSkill.None;
    public BossSkill Skill2 = BossSkill.None;   // serangan kedua (boss cutscene)
    float skillTimer;
    float skill2Timer;

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
            Skill = def.Skill;
            if (sr != null) sr.sprite = frames[0];
        }

        if (IsBoss) skillTimer = 2.5f;   // jeda sebelum serangan pertama
        if (sr != null) sr.color = tint;

        baseTint = tint;
        // boss (sprite putih) tetap terlihat berkedip kalau dipakai warna hangat
        flashColor = IsBoss ? new Color(1f, 0.42f, 0.42f) : Color.white;

        // HP bar di atas boss/elite supaya sisa darahnya kelihatan
        if (IsBoss) worldBar = EnemyWorldBar.Attach(new Color(0.95f, 0.35f, 0.32f));
    }

    // Dipakai senjata player: kurangi HP + tampilkan angka damage melayang.
    public void ApplyDamage(float amount)
    {
        if (Health == null || Health.IsDead) return;
        Health.TakeDamage(amount);
        hitFlash = HitFlashTime;   // berkedip putih sebentar

        if (amount >= 5f && Random.value < 0.55f)
        {
            var col = IsBoss ? new Color(1f, 0.6f, 0.5f) : new Color(1f, 0.95f, 0.55f);
            DamageText.Spawn(transform.position + Vector3.up * 0.7f,
                Mathf.RoundToInt(amount).ToString(), col, IsBoss ? 1.4f : 0.9f);
        }
    }

    // --- ancang-ancang serangan boss ---
    bool pending;
    float pendTimer;
    BossSkill pendSkill;
    EnemyWorldBar worldBar;

    // --- kilatan saat kena serangan (umpan balik) ---
    const float HitFlashTime = 0.09f;
    Color baseTint = Color.white;
    Color flashColor = Color.white;
    float hitFlash;

    // Boss cutscene (2 serangan) ditandai sebagai ELITE di UI.
    public bool IsElite => IsBoss && Skill2 != BossSkill.None;

    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    // Dipakai boss cutscene: menambahkan serangan kedua yang berbeda.
    public void SetSecondSkill(BossSkill s)
    {
        Skill2 = s;
        skill2Timer = 4.5f;
    }

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
            CameraFollow.DoShake(0.35f, 0.4f);                   // kamera bergetar
            // boss menjatuhkan power-up (acak antara Overdrive & Shield)
            BuffPickup.Spawn(transform.position, Random.value < 0.5f ? BuffType.Overdrive : BuffType.Shield);
        }
        else
        {
            Burst.Spawn(transform.position, burstColor, Random.Range(7, 12), 5f, 0.16f, 0.42f);
        }

        int drops = IsBoss ? 8 : 1;
        int each = Mathf.Max(1, XPDrop / drops);
        for (int i = 0; i < drops; i++)
            XPGem.Spawn(transform.position + (Vector3)(Random.insideUnitCircle * 0.5f), each);

        // Drop kotak obat (RNG) — jarang, dan tidak menyembuhkan penuh.
        if (Random.value < HealthDropChance)
        {
            float heal = 15f;
            if (player != null && player.Health != null)
                heal = Mathf.Max(8f, player.Health.Max * (IsBoss ? 0.25f : 0.12f));
            HealthPickup.Spawn(transform.position + (Vector3)(Random.insideUnitCircle * 0.6f), heal);
        }

        if (worldBar != null) Destroy(worldBar.gameObject);
        Destroy(gameObject);
    }

    void Update()
    {
        // kilatan putih saat baru kena serangan (umpan balik agar serangan terasa)
        if (hitFlash > 0f)
        {
            hitFlash -= Time.deltaTime;
            if (sr != null)
                sr.color = hitFlash > 0f
                    ? Color.Lerp(baseTint, flashColor, Mathf.Clamp01(hitFlash / HitFlashTime))
                    : baseTint;
        }

        var gm = GameManager.Instance;
        if (gm == null || !gm.IsRunning || player == null) return;

        Vector2 to = (Vector2)player.transform.position - (Vector2)transform.position;
        float dist = to.magnitude;
        Vector2 dir = dist > 0.0001f ? to / dist : Vector2.zero;

        transform.position += (Vector3)(dir * Speed * Time.deltaTime);

        if (dist <= Radius + 0.4f && player.Health != null)
            player.Hurt(ContactDamage);   // per-pukulan (i-frames yang mengatur jeda)

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

        // --- serangan khusus boss (tidak memakai senjata player) ---
        // Boss biasa: 1 serangan. Boss yang punya cutscene: 2 kombinasi serangan.
        // Setiap serangan didahului ancang-ancang (telegraph) supaya adil.
        if (IsBoss && Skill != BossSkill.None)
        {
            skillTimer -= Time.deltaTime;
            if (skillTimer <= 0f) { skillTimer = CooldownFor(Skill); Telegraph(Skill, 0f); }
        }
        if (IsBoss && Skill2 != BossSkill.None)
        {
            skill2Timer -= Time.deltaTime;
            if (skill2Timer <= 0f) { skill2Timer = CooldownFor(Skill2); Telegraph(Skill2, 0.2f); }
        }

        if (pending)
        {
            pendTimer -= Time.deltaTime;
            if (pendTimer <= 0f) { pending = false; Cast(pendSkill); }
        }

        // HP bar di atas boss
        if (worldBar != null && Health != null)
        {
            float w = sr != null ? Mathf.Max(0.9f, sr.bounds.size.x * 1.08f) : 1.4f;
            float h = sr != null ? Mathf.Clamp(sr.bounds.size.y * 0.08f, 0.12f, 0.34f) : 0.16f;
            float top = sr != null ? sr.bounds.extents.y + 0.38f : 1.1f;
            worldBar.Place(transform.position + Vector3.up * top, w, h);
            worldBar.SetRatio(Health.Max <= 0f ? 0f : Health.Current / Health.Max);
        }
    }

    // Ancang-ancang sebelum serangan: kasih peringatan visual + jeda singkat.
    void Telegraph(BossSkill s, float extraDelay)
    {
        pending = true;
        pendSkill = s;
        pendTimer = 0.5f + extraDelay;

        if (s == BossSkill.Laser)
        {
            Vector2 dir = (Vector2)player.transform.position - (Vector2)transform.position;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
            BossWarning.Line(transform.position, dir.normalized, 14f, 0.16f, pendTimer);
        }
        else if (s == BossSkill.Aura)
        {
            BossWarning.Ring(transform.position, 4.4f, pendTimer);
        }
        else
        {
            Burst.Spawn(transform.position, new Color(1f, 0.5f, 0.3f), 8, 3.5f, 0.14f, 0.5f);
        }

        AudioManager.Play("boss", 0.28f);
    }

    public static float CooldownFor(BossSkill s)
    {
        if (s == BossSkill.Gun) return 2.6f;
        if (s == BossSkill.Aura) return 3.2f;
        return 3.6f;
    }

    void Cast(BossSkill s)
    {
        switch (s)
        {
            case BossSkill.Gun: FireGunVolley(); break;
            case BossSkill.Aura: FireAura(); break;
            case BossSkill.Laser: FireLaser(); break;
        }
    }

    // Boss MECH: menembakkan beberapa peluru menyebar ke arah player.
    void FireGunVolley()
    {
        Vector2 origin = transform.position;
        Vector2 to = (Vector2)player.transform.position - origin;
        float baseAng = Mathf.Atan2(to.y, to.x);
        int n = 5;
        float dmg = (8f + GameManager.Instance.Tier * 2.2f) * (1f + Time.timeSinceLevelLoad / 300f);

        for (int i = 0; i < n; i++)
        {
            float off = Mathf.Lerp(-22f, 22f, i / (float)(n - 1));
            float a = baseAng + off * Mathf.Deg2Rad;
            EnemyShot.Spawn(origin, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 8.5f, dmg);
        }
        AudioManager.Play("shoot", 0.5f);
    }

    // Boss BRAIN: memancarkan aura (ledakan energi) di sekelilingnya.
    void FireAura()
    {
        float r = 4.4f;
        float dmg = 10f + GameManager.Instance.Tier * 2.6f;

        if (Vector2.Distance(player.transform.position, transform.position) <= r + 0.4f)
            player.Hurt(dmg);

        AuraVisual.Spawn(transform.position, r, new Color(1f, 0.45f, 0.35f, 0.28f));
        AudioManager.Play("laser", 0.3f);
    }

    // Boss SPIDER: menembakkan laser lurus ke arah player.
    void FireLaser()
    {
        Vector2 origin = transform.position;
        Vector2 dir = ((Vector2)player.transform.position - origin);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();

        float range = 14f, width = 0.9f;
        float dmg = 12f + GameManager.Instance.Tier * 3.2f;

        Vector2 to = (Vector2)player.transform.position - origin;
        float fwd = Vector2.Dot(to, dir);
        float side = Mathf.Abs(Vector2.Dot(to, new Vector2(-dir.y, dir.x)));
        if (fwd > 0f && fwd <= range && side <= width + 0.3f) player.Hurt(dmg);

        LaserVisual.Spawn(origin, dir, range, width, new Color(1f, 0.35f, 0.3f, 0.9f));
        AudioManager.Play("laser", 0.55f);
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

// Peluru MILIK MUSUH (beda dari senjata player) — warna merah, melukai player.
public class EnemyShot : MonoBehaviour
{
    float speed = 8.5f;
    float damage = 10f;
    float life = 5f;
    const float HitRadius = 0.55f;
    Vector2 dir;

    public static void Spawn(Vector2 pos, Vector2 direction, float spd, float dmg)
    {
        var sr = SpriteFactory.NewSprite("EnemyShot", GameBootstrap.WorldRoot,
            new Color(1f, 0.42f, 0.38f), true, 21, pos, 0.34f);
        var s = sr.gameObject.AddComponent<EnemyShot>();
        s.dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        s.speed = spd;
        s.damage = dmg;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsRunning) return;

        life -= Time.deltaTime;
        if (life <= 0f) { Destroy(gameObject); return; }

        transform.position += (Vector3)(dir * speed * Time.deltaTime);

        var p = gm.Player;
        if (p != null && p.Health != null &&
            Vector2.Distance(transform.position, p.transform.position) <= HitRadius)
        {
            p.Hurt(damage);
            Destroy(gameObject);
        }
    }
}

// Peringatan visual sebelum serangan boss (biar player sempat menghindar).
public class BossWarning : MonoBehaviour
{
    SpriteRenderer sr;
    Vector3 baseScale;
    float t, dur;
    bool ring;

    public static void Ring(Vector3 pos, float radius, float dur)
    {
        var sr = SpriteFactory.NewSprite("WarnRing", GameBootstrap.WorldRoot,
            new Color(1f, 0.30f, 0.25f, 0.34f), true, 13, pos, radius * 2f);
        var w = sr.gameObject.AddComponent<BossWarning>();
        w.sr = sr; w.baseScale = sr.transform.localScale; w.dur = dur; w.ring = true;
    }

    public static void Line(Vector3 origin, Vector2 dir, float range, float width, float dur)
    {
        Vector3 mid = origin + (Vector3)(dir * (range * 0.5f));
        var sr = SpriteFactory.NewSprite("WarnLine", GameBootstrap.WorldRoot,
            new Color(1f, 0.30f, 0.22f, 0.34f), false, 13, mid, 1f);
        sr.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        sr.transform.localScale = new Vector3(range, width, 1f);
        var w = sr.gameObject.AddComponent<BossWarning>();
        w.sr = sr; w.baseScale = sr.transform.localScale; w.dur = dur;
    }

    void Update()
    {
        t += Time.deltaTime;
        if (sr == null) { Destroy(gameObject); return; }

        float k = 1f - Mathf.Clamp01(t / dur);
        float throb = 1f + 0.07f * Mathf.Sin(Time.time * 26f);
        sr.transform.localScale = ring
            ? baseScale * throb
            : new Vector3(baseScale.x, baseScale.y * throb, 1f);

        var c = sr.color;
        c.a = 0.08f + 0.30f * k;
        sr.color = c;

        if (t >= dur) Destroy(gameObject);
    }
}

// Bar HP kecil di atas boss/elite (objek dunia, mengikuti musuh).
public class EnemyWorldBar : MonoBehaviour
{
    SpriteRenderer bg, fill;
    float ratio = 1f;

    public static EnemyWorldBar Attach(Color color)
    {
        var go = new GameObject("EnemyBar");
        if (GameBootstrap.WorldRoot != null) go.transform.SetParent(GameBootstrap.WorldRoot, false);

        var bar = go.AddComponent<EnemyWorldBar>();
        bar.bg = SpriteFactory.NewSprite("Bg", go.transform,
            new Color(0.05f, 0.06f, 0.10f, 0.92f), false, 19, Vector3.zero, 1f);
        bar.fill = SpriteFactory.NewSprite("Fill", go.transform, color, false, 20, Vector3.zero, 1f);
        return bar;
    }

    public void SetRatio(float r) { ratio = Mathf.Clamp01(r); }

    public void Place(Vector3 worldPos, float w, float h)
    {
        transform.position = worldPos;

        if (bg != null) bg.transform.localScale = new Vector3(w + 0.10f, h + 0.08f, 1f);

        if (fill != null)
        {
            float fw = Mathf.Max(0.001f, w * ratio);
            fill.transform.localPosition = new Vector3(-(w - fw) * 0.5f, 0f, 0f);
            fill.transform.localScale = new Vector3(fw, h, 1f);
        }
    }
}

// Power-up: buff sementara untuk player.
public enum BuffType { Overdrive, Shield }

public class BuffPickup : MonoBehaviour
{
    BuffType type;
    float life = 25f;
    PlayerController player;

    public static void Spawn(Vector3 pos, BuffType t)
    {
        Color col = t == BuffType.Overdrive ? new Color(1f, 0.85f, 0.3f) : new Color(0.5f, 0.92f, 1f);
        var sr = SpriteFactory.NewSprite("Buff", GameBootstrap.WorldRoot, col, true, 11, pos, 0.5f);
        var b = sr.gameObject.AddComponent<BuffPickup>();
        b.type = t;
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
            if (player.Stats != null)
            {
                if (type == BuffType.Overdrive) player.Stats.GiveOverdrive(8f);
                else player.Stats.GiveShield(6f);
            }
            AudioManager.Play("levelup", 0.6f);
            if (UIManager.Instance != null)
                UIManager.Instance.ShowBanner(type == BuffType.Overdrive
                    ? "OVERDRIVE!  tembakan 2x lebih cepat"
                    : "SHIELD!  kebal 6 detik");
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

    // Dipakai HUD: hitungan mundur menuju boss berikutnya.
    public float SecondsToBoss => Mathf.Max(0f, bossTimer);
    public bool BossAliveNow => BossAlive();

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
        // damage NAIK TAJAM tiap tingkat supaya makin menantang
        float dmg = (6f + tier * 2.2f) * (1f + elapsed / 240f);

        Color tint = Tints[(tier - 1) % Tints.Length];
        var def = MonsterArt.GetRegular(Random.Range(0, Mathf.Max(1, MonsterArt.RegularCount)));

        var sr = SpriteFactory.NewSprite("Enemy", GameBootstrap.WorldRoot, tint, true, 8,
            EdgePosition(), 1f);
        var e = sr.gameObject.AddComponent<Enemy>();
        e.Configure(player, hp, spd, dmg, 0.35f, 4, false, def, tint);
    }

    // Kombinasi serangan kedua untuk boss cutscene (selalu beda dari yang utama).
    static BossSkill SecondSkillFor(BossSkill primary)
    {
        switch (primary)
        {
            case BossSkill.Gun: return BossSkill.Laser;
            case BossSkill.Aura: return BossSkill.Gun;
            default: return BossSkill.Aura;
        }
    }

    void SpawnBoss()
    {
        bool cutsceneTier = (tier % 3 == 0);   // tingkat 3, 6, 9, ... -> boss ELITE + cutscene

        float hp = (120f + 80f * tier) * (1f + elapsed / 150f);
        if (cutsceneTier) hp *= 1.4f;          // boss cutscene lebih kuat

        var def = cutsceneTier ? MonsterArt.GetEliteBoss(tier / 3 - 1)
                               : MonsterArt.GetBoss(tier - 1);
        Color tint = Color.white;

        var sr = SpriteFactory.NewSprite("Boss", GameBootstrap.WorldRoot, tint, true, 9,
            EdgePosition(), 1f);
        var e = sr.gameObject.AddComponent<Enemy>();
        e.Configure(player, hp, 1.5f + tier * 0.05f, (14f + tier * 3f) * (1f + elapsed / 240f), 0.9f, 25 + tier * 5, true, def, tint);

        if (cutsceneTier) e.SetSecondSkill(SecondSkillFor(def.Skill));   // 2 serangan

        AudioManager.Play("boss");
        AudioManager.Track("bgm_boss");

        var gm = GameManager.Instance;
        if (gm != null && cutsceneTier)
        {
            gm.TriggerBossCutscene(e);   // cutscene + kamera mengarah ke boss
        }
        else if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowBanner("BOSS MUNCUL!");
        }
    }
}
