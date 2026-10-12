using System.Collections.Generic;
using UnityEngine;

// Membangun seluruh game dari kode:
//   - root: kamera, AudioManager, UI (menu/pengaturan/HUD)
//   - gameplay: lantai, dekorasi, player, spawner (dibuat saat mulai main)
public class GameBootstrap : MonoBehaviour
{
    static GameBootstrap _self;

    // true hanya jika ada instance yang benar-benar hidup (objek yang sudah
    // dihancurkan otomatis dianggap null oleh Unity).
    public static bool Initialized => _self != null;

    static GameObject root;
    static GameObject gameplay;
    static PlayerController _authoringTemplate;

    public static Transform WorldRoot => gameplay != null ? gameplay.transform : null;

    // Reset semua variabel static setiap kali Play.
    // (Penting kalau opsi "Enter Play Mode Options" tanpa Reload Domain diaktifkan.)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        _self = null;
        root = null;
        gameplay = null;
        _authoringTemplate = null;
    }

    public class GameplayRefs
    {
        public PlayerController player;
        public EnemySpawner spawner;
    }

    // Root scene "authoring" (dibuat lewat menu editor) yang bisa diedit bebas.
    public const string AuthoringRootName = "SEA Chan World (Authoring)";

    void Awake()
    {
        // pengaman: hanya boleh ada satu bootstrap
        if (_self != null && _self != this)
        {
            Destroy(gameObject);
            return;
        }
        _self = this;
        Build();
    }

    void OnDestroy()
    {
        if (_self == this) _self = null;
    }

    public static void Build()
    {
        Time.timeScale = 1f;

        // baca template "authoring" sekali (kalau ada) lalu sembunyikan
        var authored = GameObject.Find(AuthoringRootName);
        if (authored != null)
        {
            _authoringTemplate = authored.GetComponentInChildren<PlayerController>(true);
            authored.SetActive(false);
        }

        // buang sisa root/pratinjau dari sesi sebelumnya supaya tidak dobel
        // (nama "SEA Chan World (Authoring)" TIDAK terhapus karena pencocokan persis)
        CleanupByName("SEA Chan World");
        CleanupByName("PREVIEW_World");

        // reset singleton & cache: objek lama yang belum sempat terhapus jangan
        // sampai menghalangi pembuatan objek baru (ini yang bikin menu tak muncul)
        UIManager.ResetStatics();
        GameManager.ResetStatics();
        AudioManager.ResetStatics();
        PlayerController.ResetStatics();
        SpriteFactory.ResetStatics();
        UIFactory.ResetStatics();
        MonsterArt.ResetStatics();
        Enemy.ResetStatics();

        gameplay = null;
        root = new GameObject("SEA Chan World");

        // ---------- Kamera ----------
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(root.transform, false);
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 7f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.05f, 0.09f);
        camGo.AddComponent<AudioListener>();   // penting: agar suara terdengar
        camGo.AddComponent<CameraFollow>();

        var others = Object.FindObjectsByType<Camera>();
        for (int i = 0; i < others.Length; i++)
            if (others[i] != null && others[i].gameObject != camGo)
                others[i].gameObject.SetActive(false);

        // ---------- Audio ----------
        var audioGo = new GameObject("AudioManager");
        audioGo.transform.SetParent(root.transform, false);
        audioGo.AddComponent<AudioManager>();

        // ---------- UI ----------
        UIManager.Create(root.transform);

        // ---------- GameManager ----------
        var gmGo = new GameObject("GameManager");
        gmGo.transform.SetParent(root.transform, false);
        gmGo.AddComponent<GameManager>();

        // mulai layar opening (intro) -> setelah selesai otomatis ke menu utama
        if (UIManager.Instance != null) UIManager.Instance.PlayIntro();
    }

    static void CleanupByName(string name)
    {
        var go = GameObject.Find(name);
        while (go != null)
        {
            go.name = name + " (dibuang)";
            Object.Destroy(go);
            go = GameObject.Find(name);
        }
    }

    // =====================================================================
    // Penanda container dunia. Dipakai untuk memastikan hanya ada SATU dunia
    // (mencegah isi game terduplikasi, mis. SEA Chan muncul dua kali).
    public class GameplayMarker : MonoBehaviour { }

    public static GameplayRefs CreateGameplay()
    {
        // Kalau scene punya struktur "authoring", pakai pengaturan dari situ
        // (posisi awal, kecepatan, dll) supaya hasil editan di Inspector terpakai.
        var tpl = _authoringTemplate;

        // hapus semua container dunia yang tersisa (kalau ada)
        var strays = Object.FindObjectsByType<GameplayMarker>();
        for (int i = 0; i < strays.Length; i++)
            if (strays[i] != null) Object.Destroy(strays[i].gameObject);

        if (gameplay != null) Object.Destroy(gameplay);

        gameplay = new GameObject("Gameplay");
        gameplay.AddComponent<GameplayMarker>();

        // PENTING: lupakan penanda player lama. Tanpa ini, player baru akan
        // dianggap "duplikat" lalu menghapus dirinya sendiri -> layar macet.
        PlayerController.ResetStatics();

        var groundGo = new GameObject("Ground");
        groundGo.transform.SetParent(gameplay.transform, false);
        var ground = groundGo.AddComponent<GroundLayer>();

        var propsGo = new GameObject("Props");
        propsGo.transform.SetParent(gameplay.transform, false);
        var props = propsGo.AddComponent<PropsLayer>();

        var wallsGo = new GameObject("Walls");
        wallsGo.transform.SetParent(gameplay.transform, false);
        var walls = wallsGo.AddComponent<WallLayer>();

        var player = BuildPlayer(gameplay.transform);

        // terapkan pengaturan dari scene authoring (kalau ada)
        if (tpl != null)
        {
            player.transform.position = tpl.transform.position;
            player.moveSpeed = tpl.moveSpeed;
            player.pickupRadius = tpl.pickupRadius;
            player.dashSpeed = tpl.dashSpeed;
            player.dashCooldown = tpl.dashCooldown;
        }

        ground.Init(player.transform, LoadFloorSprites());
        props.Init(player.transform, LoadPropSprites());
        walls.Init(player.transform, LoadWallSprites());

        var spGo = new GameObject("EnemySpawner");
        spGo.transform.SetParent(gameplay.transform, false);
        var spawner = spGo.AddComponent<EnemySpawner>();
        spawner.Init(player, Camera.main);

        if (Camera.main != null)
        {
            var follow = Camera.main.GetComponent<CameraFollow>();
            if (follow != null) follow.target = player.transform;
            Camera.main.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10f);
        }

        var r = new GameplayRefs();
        r.player = player;
        r.spawner = spawner;
        return r;
    }

    // =====================================================================
    // Struktur ringkas (dipakai tool editor) supaya susunan objek & komponen
    // game terlihat di Hierarchy tanpa harus menekan Play.
    public static GameObject BuildStructurePreview(Transform parent)
    {
        var root = new GameObject("SEA Chan World");
        if (parent != null) root.transform.SetParent(parent, false);

        var camGo = new GameObject("Main Camera");
        camGo.transform.SetParent(root.transform, false);
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 7f;
        cam.backgroundColor = new Color(0.04f, 0.05f, 0.09f);
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<CameraFollow>();

        var audioGo = new GameObject("AudioManager");
        audioGo.transform.SetParent(root.transform, false);
        audioGo.AddComponent<AudioManager>();

        var uiGo = new GameObject("UI  (dibangun otomatis saat Play)");
        uiGo.transform.SetParent(root.transform, false);
        uiGo.AddComponent<UIManager>();

        var gmGo = new GameObject("GameManager");
        gmGo.transform.SetParent(root.transform, false);
        gmGo.AddComponent<GameManager>();

        var gp = new GameObject("Gameplay");
        gp.transform.SetParent(root.transform, false);

        var groundGo = new GameObject("Ground  (lantai tak berujung)");
        groundGo.transform.SetParent(gp.transform, false);
        groundGo.AddComponent<GroundLayer>();

        var propsGo = new GameObject("Props  (dekorasi)");
        propsGo.transform.SetParent(gp.transform, false);
        propsGo.AddComponent<PropsLayer>();

        var wallsGo = new GameObject("Walls  (dinding / obstacle)");
        wallsGo.transform.SetParent(gp.transform, false);
        wallsGo.AddComponent<WallLayer>();

        var player = new GameObject("SEA Chan");
        player.transform.SetParent(gp.transform, false);
        var sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Circle;
        // tanpa tint — supaya warna asli sprite SEA Chan terlihat natural
        sr.color = Color.white;
        sr.sortingOrder = 12;
        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        var col = player.AddComponent<CircleCollider2D>();
        col.radius = 0.32f;
        player.AddComponent<Health>();
        player.AddComponent<PlayerStats>();
        player.AddComponent<WeaponManager>();
        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerVisual>();

        var spGo = new GameObject("EnemySpawner  (robot & boss)");
        spGo.transform.SetParent(gp.transform, false);
        spGo.AddComponent<EnemySpawner>();

        return root;
    }

    public static void DestroyGameplay()
    {
        if (gameplay != null)
        {
            Object.Destroy(gameplay);
            gameplay = null;
        }
    }

    // =====================================================================
    // Dipakai oleh tool editor: membangun ISI DUNIA saja (tanpa UI/logika)
    // ke dalam sebuah parent, supaya bisa dilihat & diedit di Hierarchy.
    public static GameObject BuildPreviewInto(Transform parent)
    {
        var world = new GameObject("Gameplay");
        world.transform.SetParent(parent, false);

        var groundGo = new GameObject("Ground");
        groundGo.transform.SetParent(world.transform, false);
        var ground = groundGo.AddComponent<GroundLayer>();

        var propsGo = new GameObject("Props");
        propsGo.transform.SetParent(world.transform, false);
        var props = propsGo.AddComponent<PropsLayer>();

        var wallsGo = new GameObject("Walls");
        wallsGo.transform.SetParent(world.transform, false);
        var walls = wallsGo.AddComponent<WallLayer>();

        // karakter (visual saja, tanpa logika)
        var go = new GameObject("SEA Chan");
        go.transform.SetParent(world.transform, false);
        go.transform.position = new Vector3(0f, 0f, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Circle;
        // tanpa tint — supaya warna asli sprite SEA Chan terlihat natural
        sr.color = Color.white;
        sr.sortingOrder = 12;

        var ptex = Resources.Load<Texture2D>("SEAChan/seachan_front_0");
        if (ptex != null)
        {
            ptex.filterMode = FilterMode.Point;
            sr.sprite = Sprite.Create(ptex, new Rect(0, 0, ptex.width, ptex.height),
                new Vector2(0.5f, 0.06f), 300f);
        }

        ground.Init(go.transform, LoadFloorSprites());
        props.Init(go.transform, LoadPropSprites());
        walls.Init(go.transform, LoadWallSprites());

        props.PlaceOnce();
        walls.BuildOnce();

        return world;
    }

    // ---------------------------------------------------------------------
    static PlayerController BuildPlayer(Transform parent)
    {
        var go = new GameObject("SEA Chan");
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Circle;
        // tanpa tint — supaya warna asli sprite SEA Chan terlihat natural
        sr.color = Color.white;
        sr.sortingOrder = 12;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.32f;   // supaya player tertahan oleh dinding

        var health = go.AddComponent<Health>();
        health.Init(100f);

        var stats = go.AddComponent<PlayerStats>();
        stats.Init();

        var wm = go.AddComponent<WeaponManager>();
        var pc = go.AddComponent<PlayerController>();
        go.AddComponent<PlayerVisual>();     // sprite SEA Chan + animasi jalan
        wm.Add(WeaponType.Gun);              // senjata awal: pistol orbit (arah kursor)

        return pc;
    }

    static Sprite[] LoadWallSprites()
    {
        var list = new List<Sprite>();
        foreach (var name in new[] { "Environment/wall_block_0", "Environment/wall_block_1" })
        {
            var tex = Resources.Load<Texture2D>(name);
            if (tex == null) continue;
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            float ppu = tex.width / WallLayer.Block;
            list.Add(Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu));
        }
        return list.Count > 0 ? list.ToArray() : null;
    }

    // ---------------------------------------------------------------------
    static Sprite[] LoadFloorSprites()
    {
        var list = new List<Sprite>();
        foreach (var name in new[] { "Environment/floor_block_0", "Environment/floor_block_1" })
        {
            var tex = Resources.Load<Texture2D>(name);
            if (tex == null) continue;
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            float ppu = tex.width / GroundLayer.TileSize;
            list.Add(Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu));
        }
        return list.Count > 0 ? list.ToArray() : null;
    }

    static Sprite[] LoadPropSprites()
    {
        var list = new List<Sprite>();
        var texs = Resources.LoadAll<Texture2D>("Props");
        foreach (var tex in texs)
        {
            if (tex == null) continue;
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            // 64 px = 1 unit, supaya ukuran dekorasi konsisten dengan lantai & dinding
            list.Add(Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.06f), 64f));
        }
        return list.Count > 0 ? list.ToArray() : null;
    }
}

// Menjamin game tetap berjalan walau scene kosong.
public static class AutoBoot
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (GameBootstrap.Initialized) return;
        if (Object.FindAnyObjectByType<GameBootstrap>() != null) return;
        var go = new GameObject("GameBootstrap");
        go.AddComponent<GameBootstrap>();
    }
}
