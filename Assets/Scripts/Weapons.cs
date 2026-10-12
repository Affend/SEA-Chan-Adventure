using System.Collections.Generic;
using UnityEngine;

public enum WeaponType { Gun, Laser, Aura, Nova }

// ============================ PLAYER ============================

public class PlayerController : MonoBehaviour
{
    // hanya boleh ada SATU player di scene (mencegah SEA Chan jadi dua)
    public static PlayerController Active { get; private set; }

    public float moveSpeed = 6f;
    public float pickupRadius = 2.5f;
    public float regenPerSecond = 0f;

    public Vector2 MoveDir { get; private set; } = Vector2.down;
    public Vector2 AimPoint { get; private set; }
    public bool Moving { get; private set; }

    [Header("Dash")]
    public float dashSpeed = 17f;
    public float dashDuration = 0.16f;
    public float dashCooldown = 1.5f;
    float dashTime;
    float dashCd;

    float lastHP = -1f;
    float hurtTimer;

    public Health Health { get; private set; }
    public PlayerStats Stats { get; private set; }
    public WeaponManager Weapons { get; private set; }

    Rigidbody2D rb;

    void Awake()
    {
        // pengaman: kalau sudah ada player lain, hancurkan yang baru ini
        if (Active != null && Active != this)
        {
            Debug.LogWarning("[SEA Chan] Player duplikat terdeteksi -> dihapus otomatis.");
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }
        Active = this;

        Health = GetComponent<Health>();
        Stats = GetComponent<PlayerStats>();
        Weapons = GetComponent<WeaponManager>();
        rb = GetComponent<Rigidbody2D>();
        AimPoint = (Vector2)transform.position + Vector2.right;
        if (Weapons != null) Weapons.Bind(this);
    }

    void OnDestroy()
    {
        if (Active == this) Active = null;
    }

    // Dipakai semua sumber damage ke player: menghormati i-frames & shield.
    public void Hurt(float amount)
    {
        if (Health == null || Health.IsDead) return;
        if (Stats != null && !Stats.CanTakeDamage()) return;
        if (Stats != null) Stats.MarkHit();
        Health.TakeDamage(amount);
    }

    // Dipakai HUD untuk menampilkan kesiapan dash.
    public float DashCooldownLeft => Mathf.Max(0f, dashCd);
    public float DashCooldownMax => Mathf.Max(0.01f, dashCooldown);
    public bool DashReady => dashCd <= 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics() { Active = null; }

    void Update()
    {
        // arah bidik selalu mengikuti kursor
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 w = cam.ScreenToWorldPoint(InputHelper.MousePosition());
            AimPoint = new Vector2(w.x, w.y);
        }

        var gm = GameManager.Instance;
        if (gm == null || !gm.IsRunning)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            Moving = false;
            return;
        }

        Vector2 m = Vector2.zero;
        m += InputHelper.MoveAxis();   // keyboard (WASD/panah) + gamepad stick kiri
        if (m.sqrMagnitude > 1f) m = m.normalized;

        Moving = m.sqrMagnitude > 0.0001f;
        if (Moving) MoveDir = m.normalized;

        // --- dash (tombol Shift) ---
        if (dashCd > 0f) dashCd -= Time.deltaTime;
        if (dashTime > 0f) dashTime -= Time.deltaTime;

        bool dashKey = InputHelper.GetKeyDown(Keybinds.Get(GameAction.Dash))
                       || InputHelper.GetButtonDownSafe("Jump");   // gamepad: tombol A / bawah
        if (dashKey && dashCd <= 0f && MoveDir.sqrMagnitude > 0.01f)
        {
            dashTime = dashDuration;
            dashCd = dashCooldown;
            Burst.Spawn(transform.position, new Color(0.6f, 0.92f, 1f), 10, 3.5f, 0.15f, 0.3f);
            AudioManager.Play("shoot", 0.2f);
        }

        if (rb != null)
            rb.linearVelocity = (dashTime > 0f) ? MoveDir * dashSpeed : m * moveSpeed;

        if (regenPerSecond > 0f && Health != null) Health.Heal(regenPerSecond * Time.deltaTime);

        if (Health != null)
        {
            if (lastHP < 0f) lastHP = Health.Current;
            hurtTimer -= Time.deltaTime;
            if (Health.Current < lastHP - 0.5f)
            {
                // indikator kena damage (kilat merah di layar + kamera bergetar)
                if (UIManager.Instance != null) UIManager.Instance.DamageFlash();
                CameraFollow.DoShake(0.09f, 0.12f);

                if (hurtTimer <= 0f) { AudioManager.Play("hurt", 0.45f); hurtTimer = 0.6f; }
                lastHP = Health.Current;
            }
            else if (Health.Current > lastHP)
            {
                lastHP = Health.Current;
            }
        }
    }
}

// ============================ STATS / LEVEL ============================

public class PlayerStats : MonoBehaviour
{
    public int Level { get; private set; } = 1;
    public float XP { get; private set; }
    public float XPToNext { get; private set; } = 14f;
    public float XPGrowth = 1.28f;

    public float DamageBonus = 0f;
    public float CooldownReduction = 0f;

    // --- kebal sesaat setelah kena pukul (i-frames) ---
    public float invulnTime = 0.5f;
    float invulnUntil;

    // --- buff dari power-up ---
    public float overdriveUntil;   // tembakan 2x lebih cepat
    public float shieldUntil;      // kebal total
    public bool Overdrive => Time.time < overdriveUntil;
    public bool Shield => Time.time < shieldUntil;

    public System.Action<float, float> XPChanged;
    public System.Action<int> LeveledUp;

    public void Init()
    {
        Level = 1;
        XP = 0f;
        XPToNext = 14f;
        invulnUntil = 0f;
        overdriveUntil = 0f;
        shieldUntil = 0f;
        XPChanged?.Invoke(XP, XPToNext);
    }

    // true kalau damage boleh masuk (tidak sedang kebal / shield).
    public bool CanTakeDamage()
    {
        if (Shield) return false;
        return Time.time >= invulnUntil;
    }

    public void MarkHit()
    {
        invulnUntil = Time.time + invulnTime;
    }

    public void GiveOverdrive(float dur) { overdriveUntil = Time.time + dur; }
    public void GiveShield(float dur) { shieldUntil = Time.time + dur; }

    public void AddXP(float amount)
    {
        XP += amount;
        while (XP >= XPToNext)
        {
            XP -= XPToNext;
            Level++;
            XPToNext = Mathf.Round(XPToNext * XPGrowth);
            LeveledUp?.Invoke(Level);
        }
        XPChanged?.Invoke(XP, XPToNext);
    }
}

// ============================ PROYEKTIL ============================

public class Projectile : MonoBehaviour
{
    public float speed = 15f;
    public float damage = 10f;
    public float life = 1.3f;
    public float hitRadius = 0.5f;

    Vector2 dir;

    public void Init(Vector2 direction, float dmg)
    {
        dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
        damage = dmg;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && !gm.IsRunning) return;

        life -= Time.deltaTime;
        if (life <= 0f) { Destroy(gameObject); return; }

        transform.position += (Vector3)(dir * speed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        var enemy = Enemy.Nearest(transform.position, hitRadius);
        if (enemy != null)
        {
            enemy.ApplyDamage(damage);
            Destroy(gameObject);
        }
    }
}

// ============================ SENJATA ============================

public abstract class Weapon : MonoBehaviour
{
    public abstract WeaponType Type { get; }
    public abstract string DisplayName { get; }

    public int Level = 1;
    public int MaxLevel = 6;

    protected PlayerController player;
    protected float timer;

    public bool CanLevelUp => Level < MaxLevel;

    public void Bind(PlayerController p) { player = p; }

    public abstract void Tick(float dt, bool trigger, Vector2 aimPoint);
    public abstract void LevelUp();

    protected float Cd(float baseCd)
    {
        float cd = Mathf.Max(0.05f, baseCd * (1f - Mathf.Clamp01(player.Stats.CooldownReduction)));
        if (player.Stats.Overdrive) cd *= 0.5f;   // power-up Overdrive
        return Mathf.Max(0.03f, cd);
    }

    protected void ClearNodes(Transform holder)
    {
        if (holder == null) return;
        for (int i = holder.childCount - 1; i >= 0; i--)
            Destroy(holder.GetChild(i).gameObject);
    }
}

// --- Pistol: beberapa pistol melingkari player, menembak ke arah kursor ---
public class GunWeapon : Weapon
{
    public override WeaponType Type => WeaponType.Gun;
    public override string DisplayName => "Pistol Orbit";

    Transform holder;
    SpriteRenderer[] nodes = new SpriteRenderer[0];
    float angle;

    int GunCount => 1 + (Level - 1) / 2;
    int BulletsPerShot => 1 + (Level - 1) / 3;
    float Interval => 0.42f - 0.04f * (Level - 1);
    float Damage => (7f + 2.5f * (Level - 1)) * (1f + player.Stats.DamageBonus);
    float OrbitRadius => 1.15f + 0.05f * (Level - 1);

    void EnsureNodes()
    {
        int want = GunCount;
        if (nodes.Length == want) return;

        if (holder != null) ClearNodes(holder);
        else
        {
            var h = new GameObject("GunHolder");
            h.transform.SetParent(transform, false);
            holder = h.transform;
        }

        nodes = new SpriteRenderer[want];
        for (int i = 0; i < want; i++)
        {
            var go = new GameObject("Gun");
            go.transform.SetParent(holder, false);
            var s = go.AddComponent<SpriteRenderer>();
            s.sprite = SpriteFactory.Gun;
            s.color = new Color(0.75f, 0.95f, 1f);
            s.sortingOrder = 14;
            go.transform.localScale = Vector3.one * 0.85f;
            nodes[i] = s;
        }
    }

    public override void Tick(float dt, bool trigger, Vector2 aimPoint)
    {
        EnsureNodes();
        angle += 70f * dt;

        int n = nodes.Length;
        for (int i = 0; i < n; i++)
        {
            if (nodes[i] == null) continue;
            float a = (angle + i * (360f / n)) * Mathf.Deg2Rad;
            Vector3 pos = player.transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * OrbitRadius;
            nodes[i].transform.position = pos;

            Vector2 toAim = aimPoint - (Vector2)pos;
            nodes[i].transform.rotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(toAim.y, toAim.x) * Mathf.Rad2Deg);
        }

        timer -= dt;
        if (!trigger || timer > 0f) return;
        timer = Cd(Interval);

        for (int i = 0; i < n; i++)
        {
            if (nodes[i] == null) continue;
            Vector2 origin = nodes[i].transform.position;
            Vector2 toAim = (aimPoint - origin).normalized;
            if (toAim.sqrMagnitude < 0.0001f) toAim = Vector2.right;

            int shots = BulletsPerShot;
            for (int s = 0; s < shots; s++)
            {
                float spread = shots <= 1 ? 0f : (s - (shots - 1) / 2f) * 9f;
                Vector2 dir = Rotate(toAim, spread);
                var pr = SpriteFactory.NewSprite("Bullet", GameBootstrap.WorldRoot,
                    new Color(0.55f, 0.95f, 1f), true, 20, origin, 0.26f);
                var pb = pr.gameObject.AddComponent<Projectile>();
                pb.Init(dir, Damage);
            }
        }
        AudioManager.Play("shoot", 0.22f);
    }

    static Vector2 Rotate(Vector2 v, float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        float c = Mathf.Cos(r), s = Mathf.Sin(r);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    public override void LevelUp() { Level++; }
}

// --- Laser: berkas cahaya lurus ke arah kursor, menembus semua musuh di garis ---
public class LaserWeapon : Weapon
{
    public override WeaponType Type => WeaponType.Laser;
    public override string DisplayName => "Laser";

    float Interval => 0.85f - 0.08f * (Level - 1);
    float Damage => (14f + 5f * (Level - 1)) * (1f + player.Stats.DamageBonus);
    float BeamRange => 10f + 1.2f * (Level - 1);
    float BeamWidth => 0.5f + 0.07f * (Level - 1);

    public override void Tick(float dt, bool trigger, Vector2 aimPoint)
    {
        timer -= dt;
        if (!trigger || timer > 0f) return;
        timer = Cd(Interval);

        Vector2 origin = player.transform.position;
        Vector2 dir = (aimPoint - origin);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();

        var normal = new Vector2(-dir.y, dir.x);
        var all = Enemy.All;
        for (int i = all.Count - 1; i >= 0; i--)
        {
            var e = all[i];
            if (e == null) continue;
            Vector2 to = (Vector2)e.transform.position - origin;
            float forward = Vector2.Dot(to, dir);
            if (forward < 0f || forward > BeamRange) continue;
            float side = Mathf.Abs(Vector2.Dot(to, normal));
            if (side <= BeamWidth + e.Radius) e.ApplyDamage(Damage);
        }

        LaserVisual.Spawn(origin, dir, BeamRange, BeamWidth);
        AudioManager.Play("laser", 0.35f);
    }

    public override void LevelUp() { Level++; }
}

// --- Aura: damage melingkar di sekitar player (pasif) ---
public class AuraWeapon : Weapon
{
    public override WeaponType Type => WeaponType.Aura;
    public override string DisplayName => "Aura";

    float Radius => 1.8f + 0.4f * (Level - 1);
    float Interval => 0.75f - 0.06f * (Level - 1);
    float Damage => (5f + 3f * (Level - 1)) * (1f + player.Stats.DamageBonus);

    public override void Tick(float dt, bool trigger, Vector2 aimPoint)
    {
        timer -= dt;
        if (timer > 0f) return;
        timer = Cd(Interval);

        float r = Radius;
        var list = Enemy.All;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var e = list[i];
            if (e == null) continue;
            if (Vector2.Distance(e.transform.position, player.transform.position) <= r)
                e.ApplyDamage(Damage);
        }
        AuraVisual.Spawn(player.transform.position, r);
    }

    public override void LevelUp() { Level++; }
}

// --- Nova: ledakan peluru ke segala arah saat tombol serang ditekan ---
public class NovaWeapon : Weapon
{
    public override WeaponType Type => WeaponType.Nova;
    public override string DisplayName => "Nova Burst";

    int Shots => 6 + 2 * (Level - 1);
    float Interval => 1.4f - 0.12f * (Level - 1);
    float Damage => (6f + 2f * (Level - 1)) * (1f + player.Stats.DamageBonus);

    public override void Tick(float dt, bool trigger, Vector2 aimPoint)
    {
        timer -= dt;
        if (!trigger || timer > 0f) return;
        timer = Cd(Interval);

        Vector2 origin = player.transform.position;
        int n = Shots;
        float offset = Random.Range(0f, 360f);
        for (int i = 0; i < n; i++)
        {
            float a = (offset + i * (360f / n)) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var pr = SpriteFactory.NewSprite("Nova", GameBootstrap.WorldRoot,
                new Color(0.7f, 0.98f, 1f), true, 20, origin, 0.3f);
            var pb = pr.gameObject.AddComponent<Projectile>();
            pb.Init(dir, Damage);
        }
        AuraVisual.Spawn(origin, 1.6f);
        AudioManager.Play("shoot", 0.3f);
    }

    public override void LevelUp() { Level++; }
}

// ============================ VISUAL ============================

public class AuraVisual : MonoBehaviour
{
    SpriteRenderer sr;
    float t;
    float dur = 0.28f;
    float baseA = 0.20f;

    public static void Spawn(Vector3 pos, float radius)
    {
        Spawn(pos, radius, new Color(0.45f, 0.9f, 1f, 0.20f));
    }

    // Versi dengan warna (dipakai musuh/boss dengan warna berbeda).
    public static void Spawn(Vector3 pos, float radius, Color color)
    {
        var sr = SpriteFactory.NewSprite("Aura", GameBootstrap.WorldRoot,
            color, true, 6, pos, radius * 2f);
        var v = sr.gameObject.AddComponent<AuraVisual>();
        v.sr = sr;
        v.baseA = color.a;
    }

    void Update()
    {
        t += Time.unscaledDeltaTime;
        float k = 1f - Mathf.Clamp01(t / dur);
        if (sr != null) { var c = sr.color; c.a = baseA * k; sr.color = c; }
        if (t >= dur) Destroy(gameObject);
    }
}

public class LaserVisual : MonoBehaviour
{
    SpriteRenderer sr;
    float t;
    float dur = 0.16f;
    float baseA = 0.9f;

    public static void Spawn(Vector2 origin, Vector2 dir, float range, float width)
    {
        Spawn(origin, dir, range, width, new Color(0.5f, 1f, 1f, 0.9f));
    }

    // Versi dengan warna (dipakai boss dengan warna berbeda).
    public static void Spawn(Vector2 origin, Vector2 dir, float range, float width, Color color)
    {
        var sr = SpriteFactory.NewSprite("Laser", GameBootstrap.WorldRoot,
            color, false, 25, origin + dir * (range * 0.5f), 1f);
        var tr = sr.transform;
        tr.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        tr.localScale = new Vector3(range, width * 2f, 1f);
        var v = sr.gameObject.AddComponent<LaserVisual>();
        v.sr = sr;
        v.baseA = color.a;
    }

    void Update()
    {
        t += Time.unscaledDeltaTime;
        float k = 1f - Mathf.Clamp01(t / dur);
        if (sr != null) { var c = sr.color; c.a = baseA * k; sr.color = c; }
        if (t >= dur) Destroy(gameObject);
    }
}

// ============================ MANAGER ============================

public class WeaponManager : MonoBehaviour
{
    public readonly List<Weapon> weapons = new List<Weapon>();
    public PlayerController player;

    public void Bind(PlayerController p) { player = p; }

    public Weapon Get(WeaponType type)
    {
        for (int i = 0; i < weapons.Count; i++)
            if (weapons[i] != null && weapons[i].Type == type) return weapons[i];
        return null;
    }

    public Weapon Add(WeaponType type)
    {
        var existing = Get(type);
        if (existing != null) { if (existing.CanLevelUp) existing.LevelUp(); return existing; }

        var go = new GameObject(type + "Weapon");
        go.transform.SetParent(transform, false);

        Weapon w;
        switch (type)
        {
            case WeaponType.Laser: w = go.AddComponent<LaserWeapon>(); break;
            case WeaponType.Aura: w = go.AddComponent<AuraWeapon>(); break;
            case WeaponType.Nova: w = go.AddComponent<NovaWeapon>(); break;
            default: w = go.AddComponent<GunWeapon>(); break;
        }
        w.Bind(player);
        weapons.Add(w);
        return w;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsRunning || player == null) return;

        bool trigger = true; // tembakan OTOMATIS; arah tetap mengikuti kursor
        Vector2 aim = player.AimPoint;
        float dt = Time.deltaTime;

        for (int i = 0; i < weapons.Count; i++)
            if (weapons[i] != null) weapons[i].Tick(dt, trigger, aim);
    }
}
