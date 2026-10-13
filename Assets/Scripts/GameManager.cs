using UnityEngine;

public enum GameState { Intro, Menu, Playing, Paused, GameOver, UpgradeSelect, Cutscene }

// Mengatur alur permainan: menu utama, mulai main, pause, naik level, game over.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; } = GameState.Intro;
    public bool IsRunning => State == GameState.Playing;

    public PlayerController Player { get; private set; }
    public EnemySpawner Spawner { get; private set; }

    public int Kills { get; private set; }
    public int BossKills { get; private set; }
    public float TimeAlive { get; private set; }
    public int Tier { get; private set; } = 1;

    // --- combo (bunuh beruntun) & ultimate ---
    public int Combo { get; private set; }
    float comboTimer;
    int bonusScore;
    public float Charge { get; private set; }
    public const float ChargeMax = 100f;

    // Ultimate TIDAK bisa ditumpuk: setelah dipakai ada jeda cooldown
    // dan selama cooldown meter tidak bisa terisi lagi.
    public float UltimateCooldown { get; private set; }
    public const float UltimateCooldownTime = 25f;
    public bool UltimateLocked => UltimateCooldown > 0f;
    public bool ChargeReady => Charge >= ChargeMax && !UltimateLocked;
    // Berapa bagian meter yang terisi (0..1) — dipakai HUD.
    public float ChargeRatio => UltimateLocked
        ? 1f - Mathf.Clamp01(UltimateCooldown / UltimateCooldownTime)
        : Mathf.Clamp01(Charge / ChargeMax);

    // Skor: musuh + bonus boss + waktu + bonus combo.
    public int Score => Kills * 10 + BossKills * 500 + Mathf.FloorToInt(TimeAlive) * 2 + bonusScore;

    void Awake()
    {
        // pengaman: hanya boleh ada satu GameManager
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        State = GameState.Intro;

        ScreenSetup.Load();            // terapkan mode layar tersimpan
        LeaderboardSource.LoadLocal();  // baca skor resmi dari file bawaan
    }

    void Start()
    {
        // coba ambil leaderboard resmi terbaru (kalau URL diisi) — gagal pun tidak masalah
        StartCoroutine(LeaderboardSource.RefreshRemote());
        // papan skor ONLINE: skor semua pemain dari semua device
        // (otomatis nonaktif kalau PublicKey di OnlineLeaderboard.cs masih kosong)
        StartCoroutine(OnlineLeaderboard.Fetch());
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics() { Instance = null; }

    // Dipanggil setelah layar opening selesai / dilewati.
    public void ShowMainMenu()
    {
        State = GameState.Menu;
        AudioManager.Track("bgm_menu");
        var ui = UIManager.Instance;
        if (ui != null)
        {
            ui.ShowIntro(false);
            ui.ShowHud(false);
            ui.ShowPause(false);
            ui.ShowGameOver(false);
            ui.ShowLeaderboard(false);
            ui.ShowMenu(true);
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (State == GameState.Playing)
        {
            TimeAlive += Time.deltaTime;

            // combo memudar kalau tidak ada kill
            if (Combo > 0)
            {
                comboTimer -= Time.deltaTime;
                if (comboTimer <= 0f) Combo = 0;
            }

            if (UltimateCooldown > 0f) UltimateCooldown -= Time.deltaTime;

            // ultimate: tekan tombol (bawaan Q) kalau meter penuh & tidak cooldown
            if (!Keybinds.Capturing && ChargeReady &&
                InputHelper.GetKeyDown(Keybinds.Get(GameAction.Ultimate))) Unleash();
        }

        // tombol bisa diubah di Pengaturan (bawaan: Esc / M)
        if (!Keybinds.Capturing && InputHelper.GetKeyDown(Keybinds.Get(GameAction.Mute)))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.ToggleMute();
        }

        if (!Keybinds.Capturing && InputHelper.GetKeyDown(Keybinds.Get(GameAction.Pause)) &&
            (State == GameState.Playing || State == GameState.Paused))
            TogglePause();
    }

    // Ledakan energi: melukai musuh di sekitar player (bukan penghapus layar).
    void Unleash()
    {
        Charge = 0f;
        UltimateCooldown = UltimateCooldownTime;   // tidak bisa dipakai dua kali beruntun

        float radius = 9f;
        float dmg = 50f + Tier * 8f;

        var list = Enemy.All;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var e = list[i];
            if (e == null) continue;
            if (Vector2.Distance(e.transform.position, Player.transform.position) <= radius)
                e.ApplyDamage(dmg);
        }

        AuraVisual.Spawn(Player.transform.position, radius, new Color(0.6f, 0.95f, 1f, 0.5f));
        Burst.Spawn(Player.transform.position, new Color(0.7f, 1f, 1f), 30, 12f, 0.3f, 0.7f);
        CameraFollow.DoShake(0.3f, 0.35f);
        AudioManager.Play("laser", 0.9f);
        AudioManager.Play("levelup", 0.8f);
        if (UIManager.Instance != null) UIManager.Instance.ShowBanner("ENERGY BURST!");
    }

    // ------------------------------------------------------------- MULAI
    public void StartGame()
    {
        // pengaman: jangan mulai dua kali dalam frame yang sama
        if (_starting) return;
        _starting = true;
        StartCoroutine(ClearStartingFlag());

        Unhook();
        Time.timeScale = 1f;

        Kills = 0; BossKills = 0; TimeAlive = 0f; Tier = 1;
        Combo = 0; comboTimer = 0f; bonusScore = 0; Charge = 0f; UltimateCooldown = 0f;
        Enemy.All.Clear();
        AudioManager.Track("bgm_game");

        var ui = UIManager.Instance;
        if (ui != null)
        {
            ui.ShowIntro(false);
            ui.ShowMenu(false);
            ui.ShowLeaderboard(false);
            ui.ShowGameOver(false);
            ui.ShowPause(false);
            ui.ShowHud(true);
        }

        var refs = GameBootstrap.CreateGameplay();
        Player = refs.player;
        Spawner = refs.spawner;

        if (Player != null)
        {
            Player.Health.Died += OnPlayerDied;
            Player.Stats.LeveledUp += OnLevelUp;
        }

        State = GameState.Playing;
    }

    void Unhook()
    {
        if (Player != null)
        {
            if (Player.Health != null) Player.Health.Died -= OnPlayerDied;
            if (Player.Stats != null) Player.Stats.LeveledUp -= OnLevelUp;
        }
        Player = null;
        Spawner = null;
    }

    bool _starting;
    System.Collections.IEnumerator ClearStartingFlag()
    {
        yield return null;
        _starting = false;
    }

    public void ReturnToMenu()
    {
        Unhook();
        GameBootstrap.DestroyGameplay();
        Enemy.All.Clear();

        Time.timeScale = 1f;
        State = GameState.Menu;
        AudioManager.Track("bgm_menu");

        var ui = UIManager.Instance;
        if (ui != null)
        {
            ui.ShowHud(false);
            ui.ShowPause(false);
            ui.ShowGameOver(false);
            ui.ShowLeaderboard(false);
            ui.ShowMenu(true);
        }
    }

    // ------------------------------------------------------------ SKOR
    public void AddKill()
    {
        Kills++;
        Combo++;
        comboTimer = 2f;
        bonusScore += 10 * Mathf.Clamp(Combo / 5 + 1, 1, 10);   // makin banyak combo makin besar
        if (UltimateCooldown <= 0f) Charge = Mathf.Min(ChargeMax, Charge + 1f);
    }

    void OnLevelUp(int level)
    {
        if (State != GameState.Playing || Player == null) return;

        AudioManager.Play("levelup");
        State = GameState.UpgradeSelect;
        Time.timeScale = 0f;

        // efek naik level: cincin + percikan di sekeliling pemain, lalu teks "LEVEL UP!"
        if (Player != null)
        {
            AuraVisual.Spawn(Player.transform.position, 3.2f, new Color(0.55f, 0.95f, 1f, 0.55f));
            Burst.Spawn(Player.transform.position, new Color(0.7f, 0.95f, 1f), 18, 7f, 0.22f, 0.6f);
        }
        if (UIManager.Instance != null) UIManager.Instance.ShowLevelUp(level);

        var options = UpgradeManager.Roll(Player, 3);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowUpgrades(options, () =>
            {
                Time.timeScale = 1f;
                State = GameState.Playing;
            });
        }
        else
        {
            Time.timeScale = 1f;
            State = GameState.Playing;
        }
    }

    void OnPlayerDied(Health h)
    {
        State = GameState.GameOver;
        Time.timeScale = 1f;

        if (Player != null)
        {
            var sr = Player.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
        }

        AudioManager.Play("gameover");
        AudioManager.Track("bgm_defeat", false);

        if (UIManager.Instance != null)
            UIManager.Instance.ShowGameOverStats(TimeAlive, Kills,
                Player != null && Player.Stats != null ? Player.Stats.Level : 1,
                BossKills, Score);
    }

    // Cutscene singkat saat boss khusus muncul (tingkat 3, 6, 9, ...).
    // Kamera diarahkan ke lokasi boss supaya terlihat dia muncul di mana.
    public void TriggerBossCutscene(Enemy boss)
    {
        if (State != GameState.Playing) return;
        if (boss == null || UIManager.Instance == null) return;

        State = GameState.Cutscene;
        UIManager.Instance.PlayBossCutscene(boss.DisplayName, boss.transform, () =>
        {
            if (State == GameState.Cutscene) State = GameState.Playing;
        });
    }

    public void OnBossDefeated()
    {
        Tier++;
        BossKills++;
        if (UltimateCooldown <= 0f) Charge = Mathf.Min(ChargeMax, Charge + 15f);
        AudioManager.Track("bgm_game");   // balik ke musik biasa setelah boss tumbang
        if (Spawner != null) Spawner.OnTierUp(Tier);
        if (UIManager.Instance != null)
            UIManager.Instance.ShowBanner("TIER " + Tier + " — robot makin kuat!");
    }

    // ------------------------------------------------------------- PAUSE
    public void TogglePause()
    {
        if (State == GameState.Playing)
        {
            State = GameState.Paused;
            Time.timeScale = 0f;
            if (UIManager.Instance != null) UIManager.Instance.ShowPause(true);
        }
        else if (State == GameState.Paused)
        {
            State = GameState.Playing;
            Time.timeScale = 1f;
            if (UIManager.Instance != null) UIManager.Instance.ShowPause(false);
        }
    }
}
