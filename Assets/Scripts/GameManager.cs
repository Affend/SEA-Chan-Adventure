using UnityEngine;

public enum GameState { Intro, Menu, Playing, Paused, GameOver, UpgradeSelect }

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

    // Skor: musuh + bonus besar untuk Boss + waktu bertahan.
    public int Score => Kills * 10 + BossKills * 500 + Mathf.FloorToInt(TimeAlive) * 2;

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
        if (State == GameState.Playing) TimeAlive += Time.deltaTime;

        if (InputHelper.GetKeyDown(KeyCode.Escape) &&
            (State == GameState.Playing || State == GameState.Paused))
            TogglePause();
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
    public void AddKill() { Kills++; }

    void OnLevelUp(int level)
    {
        if (State != GameState.Playing || Player == null) return;

        AudioManager.Play("levelup");
        State = GameState.UpgradeSelect;
        Time.timeScale = 0f;

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

    public void OnBossDefeated()
    {
        Tier++;
        BossKills++;
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
