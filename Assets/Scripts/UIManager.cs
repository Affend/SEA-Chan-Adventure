using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Membangun & mengatur seluruh layar UI:
// Menu utama (opening), Pengaturan audio, HUD, Pause, Game Over, dan pilih Upgrade.
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // ---- referensi HUD ----
    GameObject hudRoot;
    Text timerText, killText, tierText, levelText;
    RectTransform hpFill, xpFill, chargeFill;
    Text comboText, hpText, ultLabel;
    Image lowHp;
    const float HpW = 420f, HpH = 34f, XpW = 420f, XpH = 18f;

    GameObject bossRoot;
    RectTransform bossFill;
    Text bossName;
    Image bossArrow;

    RectTransform canvasRect;
    RectTransform crosshair;
    readonly Text[] weaponRows = new Text[4];

    GameObject leaderboardRoot;
    InputField nameField;
    Button saveButton;
    Text boardText, boardHint, bigBoardText;
    int pendingScore, pendingKills, pendingBosses;
    float pendingTime;

    // opening / intro
    GameObject introRoot;
    Image introBg;
    Text introTitle, introSub, introLine, introHint;
    bool introSkipped;

    // banner notifikasi
    Text bannerText;
    float bannerTimer;
    Text menuBestText;
    RectTransform[] menuDots;
    Vector2[] menuDotDir;
    Image damageFlash;
    // cutscene boss
    GameObject cutsceneRoot;
    Image cutBg;
    Text cutLabel, cutName;

    // ---- layar ----
    GameObject menuRoot, settingsRoot, pauseRoot, gameOverRoot, upgradeRoot;
    Text goStats;
    Button[] upButtons;
    Text[] upTexts;
    List<UpgradeDef> current;
    Action pendingDone;

    // ---- pengaturan audio ----
    Slider masterSlider, musicSlider, sfxSlider;
    Text masterVal, musicVal, sfxVal;
    Action settingsReturn;

    public static UIManager Create(Transform parent)
    {
        // pengaman: hanya boleh ada satu UI
        if (Instance != null) return Instance;

        var go = new GameObject("UI");
        go.transform.SetParent(parent, false);
        var ui = go.AddComponent<UIManager>();
        ui.Build();
        return ui;
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics() { Instance = null; }

    // =====================================================================
    void Build()
    {
        UIFactory.EnsureEventSystem(transform);
        var canvas = UIFactory.CreateCanvas(transform, "Canvas", 100);
        Transform c = canvas.transform;
        canvasRect = (RectTransform)canvas.transform;

        BuildHud(c);
        BuildBossBar(c);
        BuildWeaponPanel(c);
        BuildCrosshair(c);
        BuildDamageFlash(c);
        BuildBossCutscene(c);
        BuildMenu(c);
        BuildSettings(c);
        BuildPause(c);
        BuildGameOver(c);
        BuildLeaderboard(c);
        BuildUpgrade(c);
        BuildIntro(c);

        ShowMenu(false);
        ShowHud(false);
        ShowSettings(false);
        ShowPause(false);
        ShowGameOver(false);
        ShowLeaderboard(false);
        ShowIntro(false);
    }

    // ---------------------------------------------------------------- HUD
    void BuildHud(Transform c)
    {
        hudRoot = new GameObject("HUD", typeof(RectTransform));
        hudRoot.transform.SetParent(c, false);
        var rt = (RectTransform)hudRoot.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        Transform p = hudRoot.transform;

        // panel kiri bawah
        UIFactory.Image("HudCard", p, new Color(0.05f, 0.07f, 0.11f, 0.72f),
            Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(24f, 24f), new Vector2(HpW + 40f, 202f));

        UIFactory.Label(p, "HP", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(44f, 196f),
            new Vector2(80f, 24f), 20, UIFactory.TextDim, TextAnchor.LowerLeft, true);
        UIFactory.Bar(p, "HpBar", new Vector2(44f, 168f), new Vector2(HpW, HpH),
            new Color(0.90f, 0.25f, 0.33f), out hpFill);

        UIFactory.Label(p, "EXP", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(44f, 144f),
            new Vector2(80f, 22f), 18, UIFactory.TextDim, TextAnchor.LowerLeft, true);
        UIFactory.Bar(p, "XpBar", new Vector2(44f, 120f), new Vector2(XpW, XpH),
            new Color(0.35f, 0.90f, 0.62f), out xpFill);

        ultLabel = UIFactory.Label(p, "ULTIMATE  (Q)", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(44f, 94f),
            new Vector2(300f, 22f), 18, UIFactory.TextDim, TextAnchor.LowerLeft, true);
        RectTransform chargeRt;
        UIFactory.Bar(p, "ChargeBar", new Vector2(44f, 70f), new Vector2(XpW, XpH),
            new Color(0.45f, 0.92f, 1f), out chargeRt);
        chargeFill = chargeRt;

        levelText = UIFactory.Label(p, "LV 1", Vector2.zero, Vector2.zero, Vector2.zero,
            new Vector2(300f, 34f), new Vector2(200f, 30f), 24, UIFactory.Accent2, TextAnchor.LowerLeft, true);

        // angka HP di ujung bar
        hpText = UIFactory.Label(p, "", Vector2.zero, Vector2.zero, Vector2.zero,
            new Vector2(300f, 168f), new Vector2(164f, 30f), 20,
            UIFactory.TextMain, TextAnchor.LowerRight);

        // indikator combo (bunuh beruntun)
        comboText = UIFactory.Label(p, "", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(30f, -276f), new Vector2(420f, 46f), 34,
            new Color(1f, 0.78f, 0.3f, 0f), TextAnchor.UpperLeft, true);

        // panel kanan atas (timer / musuh / tier)
        UIFactory.Image("StatsCard", p, new Color(0.05f, 0.07f, 0.11f, 0.72f),
            Vector2.one, Vector2.one, Vector2.one, new Vector2(-18f, -18f), new Vector2(300f, 164f));

        timerText = UIFactory.Label(p, "00:00", Vector2.one, Vector2.one, Vector2.one,
            new Vector2(-30f, -30f), new Vector2(260f, 46f), 40, UIFactory.TextMain, TextAnchor.UpperRight, true);
        killText = UIFactory.Label(p, "0 musuh", Vector2.one, Vector2.one, Vector2.one,
            new Vector2(-30f, -80f), new Vector2(260f, 30f), 22, UIFactory.TextDim, TextAnchor.UpperRight);
        tierText = UIFactory.Label(p, "TIER 1", Vector2.one, Vector2.one, Vector2.one,
            new Vector2(-30f, -116f), new Vector2(260f, 30f), 22, UIFactory.Accent, TextAnchor.UpperRight, true);

        hintTextPlaceholder(p);

        // banner notifikasi (mis. "BOSS MUNCUL!", "TIER 2")
        bannerText = UIFactory.Label(p, "", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(1200f, 56f), 40,
            new Color(1f, 0.85f, 0.35f, 0f), TextAnchor.MiddleCenter, true);
    }

    void hintTextPlaceholder(Transform p)
    {
        UIFactory.Label(p, "WASD / panah untuk bergerak  •  ESC untuk pause",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -26f), new Vector2(900f, 30f), 20, new Color(1f, 1f, 1f, 0.35f), TextAnchor.UpperCenter);
    }

    void BuildBossBar(Transform c)
    {
        bossRoot = new GameObject("BossBar", typeof(RectTransform));
        bossRoot.transform.SetParent(c, false);
        var rt = (RectTransform)bossRoot.transform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -96f);   // diturunkan agar tidak menutup apa pun
        rt.sizeDelta = new Vector2(900f, 96f);

        // nama boss di ATAS bar
        bossName = UIFactory.Label(bossRoot.transform, "BOSS",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -6f), new Vector2(900f, 38f), 28,
            new Color(1f, 0.58f, 0.52f), TextAnchor.UpperCenter, true);

        // bar HP di BAWAH nama (dibuat manual supaya posisinya pasti tidak tumpang tindih)
        var bgRt = UIFactory.Rect("BossHpBg", bossRoot.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 14f), new Vector2(900f, 26f));
        var bgImg = bgRt.gameObject.AddComponent<Image>();
        bgImg.color = new Color(0.04f, 0.05f, 0.08f, 0.92f);
        bgImg.raycastTarget = false;

        bossFill = UIFactory.Rect("BossHpFill", bgRt, Vector2.zero, Vector2.zero, Vector2.zero,
            new Vector2(3f, 3f), new Vector2(894f, 20f));
        var fillImg = bossFill.gameObject.AddComponent<Image>();
        fillImg.color = new Color(0.88f, 0.26f, 0.46f);
        fillImg.raycastTarget = false;

        bossRoot.SetActive(false);

        // penunjuk arah boss kalau dia berada di luar layar
        bossArrow = UIFactory.Image("BossArrow", c, new Color(1f, 0.36f, 0.32f, 0.95f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(58f, 58f));
        bossArrow.sprite = SpriteFactory.Arrow;
        bossArrow.gameObject.SetActive(false);
    }

    void UpdateBossArrow()
    {
        if (bossArrow == null || canvasRect == null) return;

        var gm = GameManager.Instance;
        var cam = Camera.main;
        if (gm == null || cam == null || !gm.IsRunning)
        {
            if (bossArrow.gameObject.activeSelf) bossArrow.gameObject.SetActive(false);
            return;
        }

        Enemy boss = null;
        var all = Enemy.All;
        for (int i = 0; i < all.Count; i++)
            if (all[i] != null && all[i].IsBoss) { boss = all[i]; break; }

        if (boss == null)
        {
            if (bossArrow.gameObject.activeSelf) bossArrow.gameObject.SetActive(false);
            return;
        }

        Vector3 vp = cam.WorldToViewportPoint(boss.transform.position);
        bool off = vp.x < 0.07f || vp.x > 0.93f || vp.y < 0.09f || vp.y > 0.91f;
        if (!off)
        {
            if (bossArrow.gameObject.activeSelf) bossArrow.gameObject.SetActive(false);
            return;
        }

        Vector2 d = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
        if (d.sqrMagnitude < 0.0001f) d = Vector2.right;
        d.Normalize();

        float limX = canvasRect.rect.width * 0.5f - 90f;
        float limY = canvasRect.rect.height * 0.5f - 90f;
        float sx = Mathf.Abs(d.x) > 0.001f ? limX / Mathf.Abs(d.x) : float.MaxValue;
        float sy = Mathf.Abs(d.y) > 0.001f ? limY / Mathf.Abs(d.y) : float.MaxValue;
        float s = Mathf.Min(sx, sy);

        bossArrow.rectTransform.anchoredPosition = d * s;
        bossArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        if (!bossArrow.gameObject.activeSelf) bossArrow.gameObject.SetActive(true);
    }

    void BuildWeaponPanel(Transform c)
    {
        // panel di KIRI ATAS
        var cardRt = UIFactory.Rect("WeaponPanel", c, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(340f, 230f));
        var bg = cardRt.gameObject.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.07f, 0.11f, 0.75f);
        bg.raycastTarget = false;

        UIFactory.Label(cardRt, "SENJATA", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(16f, -10f), new Vector2(310f, 30f), 22,
            UIFactory.Accent2, TextAnchor.UpperLeft, true);

        for (int i = 0; i < weaponRows.Length; i++)
        {
            weaponRows[i] = UIFactory.Label(cardRt, "", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(16f, -46f - i * 34f), new Vector2(310f, 30f), 20,
                UIFactory.TextMain, TextAnchor.UpperLeft);
        }
    }

    void BuildDamageFlash(Transform c)
    {
        damageFlash = UIFactory.FullPanel(c, "DamageFlash", new Color(0.85f, 0.06f, 0.09f, 0f));
        damageFlash.raycastTarget = false;

        // peringatan HP rendah (berdenyut di tepi layar)
        lowHp = UIFactory.FullPanel(c, "LowHpWarn", new Color(0.80f, 0.05f, 0.08f, 0f));
        lowHp.raycastTarget = false;
    }

    // Dipanggil saat player kena damage: seluruh layar berkilat merah sebentar.
    public void DamageFlash()
    {
        if (damageFlash == null) return;
        var col = damageFlash.color;
        col.a = 0.24f;
        damageFlash.color = col;
    }

    // -------------------------------------------------- CUTSCENE BOSS
    void BuildBossCutscene(Transform c)
    {
        cutBg = UIFactory.FullPanel(c, "BossCutscene", new Color(0f, 0f, 0f, 0f));
        cutBg.raycastTarget = false;
        cutsceneRoot = cutBg.gameObject;

        cutLabel = UIFactory.Label(cutsceneRoot.transform, "!!  WARNING  !!", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 96f),
            new Vector2(1400f, 50f), 36, new Color(1f, 0.35f, 0.32f, 0f), TextAnchor.MiddleCenter, true);

        cutName = UIFactory.Label(cutsceneRoot.transform, "", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f),
            new Vector2(1700f, 130f), 88, new Color(1f, 0.92f, 0.88f, 0f), TextAnchor.MiddleCenter, true);
        UIFactory.AddOutline(cutName, 3f);

        cutsceneRoot.SetActive(false);
    }

    void SetCutText(float labelA, float nameA)
    {
        if (cutLabel != null) { var c = cutLabel.color; c.a = labelA; cutLabel.color = c; }
        if (cutName != null) { var c = cutName.color; c.a = nameA; cutName.color = c; }
    }

    public void PlayBossCutscene(string bossName, Transform boss, Action onDone)
    {
        if (cutsceneRoot == null)
        {
            if (onDone != null) onDone();
            return;
        }

        cutName.text = bossName.ToUpperInvariant();
        cutName.rectTransform.localScale = Vector3.one;
        SetCutText(0f, 0f);
        cutBg.color = new Color(0f, 0f, 0f, 0f);
        cutsceneRoot.SetActive(true);

        StartCoroutine(CutsceneRoutine(boss, onDone));
    }

    IEnumerator CutsceneRoutine(Transform boss, Action onDone)
    {
        var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;

        // 1) kamera bergerak ke LOKASI BOSS (menandakan dia muncul di mana)
        if (cam != null && boss != null) cam.SetFocus(boss.position, 1.18f);

        float t = 0f;
        while (t < 0.8f)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / 0.8f);
            cutBg.color = new Color(0f, 0f, 0f, a * 0.45f);
            SetCutText(a, 0f);
            yield return null;
        }

        // tanda ledakan kecil di posisi boss supaya mata langsung tertuju ke sana
        if (boss != null)
        {
            AuraVisual.Spawn(boss.position, 3.4f, new Color(1f, 0.3f, 0.3f, 0.35f));
            Burst.Spawn(boss.position, new Color(1f, 0.6f, 0.5f), 16, 6.5f, 0.22f, 0.6f);
        }

        // 2) nama boss muncul membesar
        t = 0f;
        while (t < 0.45f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.45f));
            SetCutText(1f, k);
            cutName.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.4f, 1f, k);
            yield return null;
        }

        // 3) tahan sebentar
        t = 0f;
        while (t < 1.4f)
        {
            t += Time.unscaledDeltaTime;
            SetCutText(1f, 1f);
            yield return null;
        }

        // 4) kamera kembali ke player + tampilan memudar
        if (cam != null) cam.ClearFocus();

        t = 0f;
        while (t < 0.7f)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(t / 0.7f);
            SetCutText(k, k);
            cutBg.color = new Color(0f, 0f, 0f, 0.45f * k);
            yield return null;
        }

        cutBg.color = new Color(0f, 0f, 0f, 0f);
        cutsceneRoot.SetActive(false);
        if (onDone != null) onDone();
    }

    void BuildCrosshair(Transform c)
    {
        crosshair = UIFactory.Rect("Crosshair", c, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44f, 44f));

        var col = new Color(0.45f, 1f, 0.85f, 0.9f);
        UIFactory.Image("H", crosshair, col, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 3f));
        UIFactory.Image("V", crosshair, col, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(3f, 30f));
        UIFactory.Image("Dot", crosshair, new Color(1f, 1f, 1f, 0.95f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, 6f));
    }

    // ------------------------------------------------------------ OPENING
    void BuildIntro(Transform c)
    {
        var bg = UIFactory.FullPanel(c, "IntroRoot", Color.black);
        introRoot = bg.gameObject;
        introBg = bg;

        // ukuran & warna disamakan dengan judul di menu supaya transisinya menyatu
        introTitle = UIFactory.Label(introRoot.transform, "SEA CHAN", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f),
            new Vector2(820f, 90f), 84, new Color(0.93f, 0.95f, 0.98f, 0f), TextAnchor.MiddleCenter, true);
        introSub = UIFactory.Label(introRoot.transform, "ADVENTURE", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -46f),
            new Vector2(820f, 70f), 48, new Color(0.20f, 0.78f, 0.72f, 0f), TextAnchor.MiddleCenter, true);
        introLine = UIFactory.Label(introRoot.transform, "", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f),
            new Vector2(1600f, 120f), 36, new Color(0.9f, 0.94f, 1f, 0f), TextAnchor.MiddleCenter);
        introHint = UIFactory.Label(introRoot.transform, "", new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 64f),
            new Vector2(1500f, 40f), 22, new Color(1f, 1f, 1f, 0f), TextAnchor.MiddleCenter);

        introRoot.SetActive(false);
    }

    public void ShowIntro(bool on)
    {
        if (introRoot != null) introRoot.SetActive(on);
    }

    public void PlayIntro()
    {
        if (introRoot == null)
        {
            if (GameManager.Instance != null) GameManager.Instance.ShowMainMenu();
            return;
        }
        introRoot.SetActive(true);
        introSkipped = false;
        SetIntroAlpha(1f);
        SetIntroText("", 0f, 0f, 0f, 0f);
        StartCoroutine(IntroRoutine());
    }

    void SetIntroAlpha(float a)
    {
        if (introBg != null) { var c = introBg.color; c.a = a; introBg.color = c; }
    }

    void SetIntroText(string line, float titleA, float subA, float lineA, float hintA)
    {
        if (introLine != null)
        {
            introLine.text = line;
            var c = introLine.color; c.a = lineA; introLine.color = c;
        }
        if (introTitle != null) { var c = introTitle.color; c.a = titleA; introTitle.color = c; }
        if (introSub != null) { var c = introSub.color; c.a = subA; introSub.color = c; }
        if (introHint != null) { var c = introHint.color; c.a = hintA; introHint.color = c; }
    }

    void SetIntroPos(Vector2 titlePos, Vector2 subPos)
    {
        if (introTitle != null) introTitle.rectTransform.anchoredPosition = titlePos;
        if (introSub != null) introSub.rectTransform.anchoredPosition = subPos;
    }

    void SetIntroScale(float s)
    {
        if (introTitle != null) introTitle.rectTransform.localScale = Vector3.one * s;
        if (introSub != null) introSub.rectTransform.localScale = Vector3.one * s;
    }

    IEnumerator IntroRoutine()
    {
        if (introHint != null) introHint.text = "Klik / tekan tombol apa saja untuk melewati";

        yield return new WaitForSecondsRealtime(0.6f);

        string[] lines =
        {
            "Tahun 2187. Kota telah dikuasai oleh mesin.",
            "Satu-satunya harapan: seorang gadis bernama SEA Chan.",
            "Bertahanlah, hancurkan semua robot, dan kalahkan bosnya."
        };

        foreach (var ln in lines)
        {
            if (introSkipped) break;

            float t = 0f;
            while (t < 0.7f && !introSkipped)
            {
                t += Time.unscaledDeltaTime;
                SetIntroText(ln, 0f, 0f, Mathf.Clamp01(t / 0.7f), 0.7f);
                yield return null;
            }
            float hold = 0f;
            while (hold < 1.5f && !introSkipped)
            {
                hold += Time.unscaledDeltaTime;
                SetIntroText(ln, 0f, 0f, 1f, 0.7f);
                yield return null;
            }
            float o = 0f;
            while (o < 0.5f && !introSkipped)
            {
                o += Time.unscaledDeltaTime;
                SetIntroText(ln, 0f, 0f, 1f - Mathf.Clamp01(o / 0.5f), 0.7f);
                yield return null;
            }
        }

        if (!introSkipped)
        {
            // logo muncul dari tengah...
            SetIntroPos(new Vector2(0f, 20f), new Vector2(0f, -46f));
            SetIntroScale(0.86f);
            SetIntroText("", 0f, 0f, 0f, 0f);

            float a = 0f;
            while (a < 0.7f)
            {
                a += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(a / 0.7f));
                SetIntroText("", k, k, 0f, 0f);
                SetIntroScale(Mathf.Lerp(0.86f, 1f, k));
                yield return null;
            }
            SetIntroText("", 1f, 1f, 0f, 0f);
            yield return new WaitForSecondsRealtime(0.8f);

            // ...lalu naik ke posisi judul di menu utama (kartu di kiri)
            float m = 0f;
            while (m < 1.0f)
            {
                m += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(m / 1.0f));
                SetIntroPos(Vector2.Lerp(new Vector2(0f, 20f), new Vector2(-330f, 290f), k),
                            Vector2.Lerp(new Vector2(0f, -46f), new Vector2(-330f, 220f), k));
                yield return null;
            }

            // tampilkan menu di belakang, lalu buka tirai hitam (judul sudah seposisi)
            if (menuRoot != null) menuRoot.SetActive(true);
            yield return null;

            float fo = 0f;
            while (fo < 0.9f)
            {
                fo += Time.unscaledDeltaTime;
                SetIntroAlpha(1f - Mathf.Clamp01(fo / 0.9f));
                yield return null;
            }
        }

        SetIntroAlpha(0f);
        introRoot.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.ShowMainMenu();
    }

    bool IntroActive => introRoot != null && introRoot.activeSelf;

    public void ShowBanner(string msg)
    {
        if (bannerText == null) return;
        bannerText.text = msg;
        var c = bannerText.color; c.a = 1f; bannerText.color = c;
        bannerTimer = 2.4f;
    }

    // -------------------------------------------------------------- MENU
    void BuildMenu(Transform c)
    {
        var bg = UIFactory.FullPanel(c, "MenuRoot", new Color(0.02f, 0.04f, 0.07f, 1f));
        menuRoot = bg.gameObject;

        // latar gradasi
        var grad = UIFactory.Image("Gradient", menuRoot.transform, Color.white,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        grad.sprite = UIFactory.GradientSprite(new Color(0.02f, 0.04f, 0.07f), new Color(0.06f, 0.13f, 0.22f));
        grad.type = Image.Type.Simple;

        // titik-titik bercahaya yang bergerak pelan
        menuDots = new RectTransform[14];
        menuDotDir = new Vector2[menuDots.Length];
        for (int i = 0; i < menuDots.Length; i++)
        {
            var col = new Color(0.45f, 0.95f, 1f, UnityEngine.Random.Range(0.08f, 0.22f));
            var d = UIFactory.Image("Dot" + i, menuRoot.transform, col,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(UnityEngine.Random.Range(-900f, 900f), UnityEngine.Random.Range(-500f, 500f)),
                Vector2.one * UnityEngine.Random.Range(6f, 20f));
            d.sprite = UIFactory.CircleSprite();
            menuDots[i] = d.rectTransform;
            menuDotDir[i] = new Vector2(UnityEngine.Random.Range(-14f, 14f), UnityEngine.Random.Range(6f, 22f));
        }

        // artwork utama (karakter SEA Chan) di sisi kanan
        var artTex = Resources.Load<Texture2D>("UI/menu_art");
        if (artTex != null)
        {
            artTex.filterMode = FilterMode.Bilinear;
            var artSprite = Sprite.Create(artTex, new Rect(0, 0, artTex.width, artTex.height),
                new Vector2(0.5f, 0.5f), 100f);
            var art = UIFactory.Image("MenuArt", menuRoot.transform, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(380f, -30f), new Vector2(1200f, 935f));
            art.sprite = artSprite;
            art.preserveAspect = true;
        }

        // lapisan gelap dari kiri supaya tulisan tetap terbaca
        var shade = UIFactory.Image("Shade", menuRoot.transform, Color.white,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        shade.sprite = UIFactory.GradientSpriteH(new Color(0.01f, 0.02f, 0.04f, 0.96f),
                                                 new Color(0.01f, 0.02f, 0.04f, 0.10f));
        shade.type = Image.Type.Simple;

        var card = UIFactory.Card(menuRoot.transform, "MenuCard", new Vector2(-330f, 10f), new Vector2(860f, 800f),
            new Color(0.20f, 0.78f, 0.72f, 1f));

        UIFactory.Label(card, "SEA CHAN", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 280f), new Vector2(820f, 90f), 84,
            UIFactory.TextMain, TextAnchor.MiddleCenter, true);
        UIFactory.Label(card, "ADVENTURE", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(820f, 70f), 48,
            UIFactory.Accent, TextAnchor.MiddleCenter, true);

        // garis tepi pada judul supaya lebih tegas (gaya arcade)
        var menuTexts = card.GetComponentsInChildren<Text>();
        for (int i = 0; i < menuTexts.Length; i++)
            if (menuTexts[i].text == "SEA CHAN" || menuTexts[i].text == "ADVENTURE")
                UIFactory.AddOutline(menuTexts[i], 2.8f);
        UIFactory.Label(card, "Showcase  •  Game Dev Study Group SEA GEN 11",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 160f), new Vector2(820f, 36f), 24, UIFactory.TextDim, TextAnchor.MiddleCenter);

        menuBestText = UIFactory.Label(card, "", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 124f), new Vector2(820f, 30f), 24,
            UIFactory.Accent, TextAnchor.MiddleCenter, true);

        UIFactory.TextButton(card, "MULAI BERMAIN", new Vector2(0f, 66f), new Vector2(460f, 78f), 32,
            () => GameManager.Instance.StartGame());
        UIFactory.TextButton(card, "PAPAN SKOR", new Vector2(0f, -26f), new Vector2(460f, 64f), 26,
            () => { ShowMenu(false); ShowLeaderboard(true); });
        UIFactory.TextButton(card, "PENGATURAN", new Vector2(0f, -110f), new Vector2(460f, 64f), 26,
            () => { ShowMenu(false); settingsReturn = () => { ShowSettings(false); ShowMenu(true); }; ShowSettings(true); });
        UIFactory.TextButton(card, "KELUAR", new Vector2(0f, -194f), new Vector2(460f, 64f), 26,
            () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
                Application.Quit();
            });

        UIFactory.Label(menuRoot.transform, "Dibuat oleh Mentor Divisi Game Dev  •  Study Group SEA GEN 11",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 78f), new Vector2(1700f, 34f), 22, UIFactory.Accent2, TextAnchor.MiddleCenter, true);

        UIFactory.Label(menuRoot.transform, "Cara main: kumpulkan XP, hindari musuh, pilih upgrade, kalahkan Boss.",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 40f), new Vector2(1400f, 40f), 24, UIFactory.TextDim, TextAnchor.MiddleCenter);
    }

    // --------------------------------------------------------- SETTINGS
    void BuildSettings(Transform c)
    {
        var bg = UIFactory.FullPanel(c, "SettingsRoot", new Color(0.02f, 0.03f, 0.06f, 0.92f));
        settingsRoot = bg.gameObject;

        var card = UIFactory.Card(settingsRoot.transform, "Card", Vector2.zero, new Vector2(820f, 540f),
            new Color(0.20f, 0.78f, 0.72f, 1f));

        UIFactory.Label(card, "PENGATURAN AUDIO", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(760f, 56f), 40,
            UIFactory.TextMain, TextAnchor.MiddleCenter, true);

        masterVal = VolumeRow(card, "Master Volume", 110f, new Color(0.90f, 0.90f, 0.95f), out masterSlider);
        musicVal = VolumeRow(card, "Musik", 20f, new Color(0.45f, 0.75f, 1f), out musicSlider);
        sfxVal = VolumeRow(card, "Efek Suara", -70f, new Color(1f, 0.78f, 0.35f), out sfxSlider);

        if (AudioManager.Instance != null)
        {
            masterSlider.value = AudioManager.Instance.Master;
            musicSlider.value = AudioManager.Instance.MusicVol;
            sfxSlider.value = AudioManager.Instance.SfxVol;
            RefreshVolumeLabels();
        }

        masterSlider.onValueChanged.AddListener(v => { AudioManager.Instance.SetMaster(v); RefreshVolumeLabels(); });
        musicSlider.onValueChanged.AddListener(v => { AudioManager.Instance.SetMusic(v); RefreshVolumeLabels(); });
        sfxSlider.onValueChanged.AddListener(v => { AudioManager.Instance.SetSfx(v); RefreshVolumeLabels(); });

        UIFactory.TextButton(card, "KEMBALI", new Vector2(0f, -190f), new Vector2(320f, 68f), 28,
            () => { var r = settingsReturn; ShowSettings(false); r?.Invoke(); });
    }

    Text VolumeRow(Transform card, string label, float y, Color color, out Slider slider)
    {
        UIFactory.Label(card, label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(-200f, y + 34f), new Vector2(360f, 30f), 24,
            UIFactory.TextDim, TextAnchor.LowerLeft);
        var val = UIFactory.Label(card, "100%", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(320f, y + 34f), new Vector2(120f, 30f), 24,
            UIFactory.TextMain, TextAnchor.LowerRight, true);
        slider = UIFactory.VolumeSlider(card, label + "Slider", new Vector2(0f, y), new Vector2(600f, 30f), color);
        return val;
    }

    void RefreshVolumeLabels()
    {
        if (masterVal == null || AudioManager.Instance == null) return;
        masterVal.text = Mathf.RoundToInt(AudioManager.Instance.Master * 100f) + "%";
        musicVal.text = Mathf.RoundToInt(AudioManager.Instance.MusicVol * 100f) + "%";
        sfxVal.text = Mathf.RoundToInt(AudioManager.Instance.SfxVol * 100f) + "%";
    }

    // ------------------------------------------------------------ PAUSE
    void BuildPause(Transform c)
    {
        var bg = UIFactory.FullPanel(c, "PauseRoot", new Color(0.02f, 0.03f, 0.06f, 0.85f));
        pauseRoot = bg.gameObject;

        var card = UIFactory.Card(pauseRoot.transform, "Card", Vector2.zero, new Vector2(700f, 480f),
            new Color(0.20f, 0.78f, 0.72f, 1f));

        UIFactory.Label(card, "PAUSE", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(640f, 70f), 56,
            UIFactory.TextMain, TextAnchor.MiddleCenter, true);
        UIFactory.Label(card, "Tekan ESC untuk lanjut", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(640f, 30f), 22,
            UIFactory.TextDim, TextAnchor.MiddleCenter);

        UIFactory.TextButton(card, "LANJUT", new Vector2(0f, 10f), new Vector2(420f, 68f), 28,
            () => GameManager.Instance.TogglePause());
        UIFactory.TextButton(card, "PENGATURAN", new Vector2(0f, -76f), new Vector2(420f, 60f), 24,
            () => { ShowPause(false); settingsReturn = () => { ShowSettings(false); ShowPause(true); }; ShowSettings(true); });
        UIFactory.TextButton(card, "MENU UTAMA", new Vector2(0f, -150f), new Vector2(420f, 60f), 24,
            () => GameManager.Instance.ReturnToMenu());

        pauseRoot.SetActive(false);
    }

    // -------------------------------------------------------- GAME OVER
    void BuildGameOver(Transform c)
    {
        var bg = UIFactory.FullPanel(c, "GameOverRoot", new Color(0.08f, 0.02f, 0.04f, 0.93f));
        gameOverRoot = bg.gameObject;

        var card = UIFactory.Card(gameOverRoot.transform, "Card", Vector2.zero, new Vector2(980f, 820f),
            new Color(0.90f, 0.30f, 0.35f, 1f));

        UIFactory.Label(card, "GAME OVER", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(900f, 76f), 58,
            new Color(1f, 0.45f, 0.45f), TextAnchor.MiddleCenter, true);

        goStats = UIFactory.Label(card, "", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 232f), new Vector2(900f, 150f), 28,
            UIFactory.TextMain, TextAnchor.MiddleCenter);

        UIFactory.Label(card, "Masukkan namamu untuk papan skor:", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 138f),
            new Vector2(760f, 30f), 22, UIFactory.TextDim, TextAnchor.MiddleCenter);

        nameField = UIFactory.NameField(card, "NameField", new Vector2(0f, 86f), new Vector2(460f, 58f), "Nama kamu");
        saveButton = UIFactory.TextButton(card, "SIMPAN SKOR", new Vector2(0f, 16f), new Vector2(360f, 56f), 24,
            () => SaveScore());

        boardHint = UIFactory.Label(card, "", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -26f), new Vector2(900f, 28f), 22,
            UIFactory.Accent, TextAnchor.MiddleCenter, true);

        boardText = UIFactory.Label(card, "", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(900f, 210f), 24,
            UIFactory.TextMain, TextAnchor.UpperCenter);

        UIFactory.TextButton(card, "MAIN LAGI", new Vector2(-220f, -320f), new Vector2(420f, 66f), 28,
            () => GameManager.Instance.StartGame());
        UIFactory.TextButton(card, "MENU UTAMA", new Vector2(220f, -320f), new Vector2(420f, 66f), 28,
            () => GameManager.Instance.ReturnToMenu());

        gameOverRoot.SetActive(false);
    }

    // ------------------------------------------------------ PAPAN SKOR
    void BuildLeaderboard(Transform c)
    {
        var bg = UIFactory.FullPanel(c, "LeaderboardRoot", new Color(0.02f, 0.04f, 0.07f, 0.95f));
        leaderboardRoot = bg.gameObject;

        var card = UIFactory.Card(leaderboardRoot.transform, "Card", Vector2.zero, new Vector2(1080f, 720f),
            new Color(1f, 0.78f, 0.28f, 1f));

        UIFactory.Label(card, "PAPAN SKOR", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 290f), new Vector2(900f, 70f), 52,
            UIFactory.Accent2, TextAnchor.MiddleCenter, true);
        UIFactory.Label(card, "Skor = musuh ×10  +  boss ×500  +  detik bertahan ×2",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 236f), new Vector2(940f, 30f), 22, UIFactory.TextDim, TextAnchor.MiddleCenter);

        bigBoardText = UIFactory.Label(card, "", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(980f, 380f), 26,
            UIFactory.TextMain, TextAnchor.UpperCenter);

        UIFactory.TextButton(card, "KEMBALI", new Vector2(0f, -280f), new Vector2(360f, 66f), 28,
            () => { ShowLeaderboard(false); ShowMenu(true); });

        leaderboardRoot.SetActive(false);
    }

    // --------------------------------------------------------- UPGRADE
    void BuildUpgrade(Transform c)
    {
        var bg = UIFactory.FullPanel(c, "UpgradeRoot", new Color(0.02f, 0.04f, 0.07f, 0.9f));
        upgradeRoot = bg.gameObject;

        var card = UIFactory.Card(upgradeRoot.transform, "Card", Vector2.zero, new Vector2(1000f, 660f),
            new Color(0.20f, 0.78f, 0.72f, 1f));

        UIFactory.Label(card, "NAIK LEVEL!", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(900f, 60f), 48,
            UIFactory.Accent2, TextAnchor.MiddleCenter, true);
        UIFactory.Label(card, "Pilih 1 upgrade", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(900f, 34f), 24,
            UIFactory.TextDim, TextAnchor.MiddleCenter);

        upButtons = new Button[3];
        upTexts = new Text[3];
        float[] ys = { 100f, -20f, -140f };
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            var b = UIFactory.TextButton(card, "", new Vector2(0f, ys[i]), new Vector2(820f, 104f), 26,
                () => Choose(idx));
            upButtons[i] = b;
            upTexts[i] = b.GetComponentInChildren<Text>();
        }

        upgradeRoot.SetActive(false);
    }

    // =====================================================================
    public void ShowMenu(bool on) { if (menuRoot != null) menuRoot.SetActive(on); }
    public void ShowHud(bool on) { if (hudRoot != null) hudRoot.SetActive(on); }
    public void ShowSettings(bool on) { if (settingsRoot != null) settingsRoot.SetActive(on); }
    public void ShowPause(bool on) { if (pauseRoot != null) pauseRoot.SetActive(on); }
    public void ShowGameOver(bool on) { if (gameOverRoot != null) gameOverRoot.SetActive(on); }

    public void ShowLeaderboard(bool on)
    {
        if (leaderboardRoot == null) return;
        leaderboardRoot.SetActive(on);
        if (on) RefreshBoards();
    }

    public void ShowGameOverStats(float time, int kills, int level, int bosses, int score)
    {
        ShowGameOver(true);

        pendingScore = score;
        pendingKills = kills;
        pendingBosses = bosses;
        pendingTime = time;

        if (goStats != null)
        {
            int m = Mathf.FloorToInt(time / 60f);
            int s = Mathf.FloorToInt(time % 60f);
            goStats.text = "SKOR  " + score +
                           "\nWaktu " + string.Format("{0:00}:{1:00}", m, s) +
                           "   •   Level " + level +
                           "\nMusuh " + kills + "   •   Boss " + bosses;
        }

        if (boardHint != null) boardHint.text = "";
        if (saveButton != null) saveButton.gameObject.SetActive(true);
        if (nameField != null) { nameField.text = ""; nameField.gameObject.SetActive(true); }

        RefreshBoards();
    }

    void SaveScore()
    {
        string nm = nameField != null ? nameField.text : "";
        int rank = Leaderboard.Add(nm, pendingScore, pendingKills, pendingBosses, pendingTime);

        if (boardHint != null)
            boardHint.text = rank == 0
                ? "Rekor baru! Kamu peringkat #1"
                : (rank > 0 ? "Tersimpan — peringkat #" + (rank + 1) : "Skor belum masuk 10 besar");

        if (saveButton != null) saveButton.gameObject.SetActive(false);
        if (nameField != null) nameField.gameObject.SetActive(false);

        RefreshBoards();
    }

    void RefreshBoards()
    {
        string text = Leaderboard.Format(10);
        if (boardText != null) boardText.text = "— 10 BESAR —\n" + text;
        if (bigBoardText != null) bigBoardText.text = text;
    }

    public void ShowUpgrades(List<UpgradeDef> ups, Action done)
    {
        current = ups;
        pendingDone = done;

        for (int i = 0; i < upButtons.Length; i++)
        {
            bool has = ups != null && i < ups.Count;
            upButtons[i].gameObject.SetActive(has);
            if (!has) continue;
            upTexts[i].text = string.IsNullOrEmpty(ups[i].Desc)
                ? ups[i].Title
                : ups[i].Title + "\n<size=20><color=#9fb0c6>" + ups[i].Desc + "</color></size>";
        }
        if (upgradeRoot != null) upgradeRoot.SetActive(true);
    }

    void Choose(int i)
    {
        if (current != null && i < current.Count && current[i].Apply != null) current[i].Apply();
        if (upgradeRoot != null) upgradeRoot.SetActive(false);
        var d = pendingDone;
        pendingDone = null;
        if (d != null) d();
    }

    void Update()
    {
        var gm = GameManager.Instance;
        bool playing = gm != null && gm.IsRunning;

        // lewati opening dengan tombol / klik apa saja
        if (IntroActive && !introSkipped)
        {
            if (InputHelper.GetKeyDown(KeyCode.Space) || InputHelper.GetKeyDown(KeyCode.Escape) ||
                InputHelper.GetKeyDown(KeyCode.Return) || InputHelper.GetMouseButtonDown(0))
                introSkipped = true;
        }

        // crosshair mengikuti kursor, hanya saat bermain
        if (crosshair != null)
        {
            crosshair.gameObject.SetActive(playing);
            if (playing && canvasRect != null)
            {
                Vector2 local;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect, InputHelper.MousePosition(), null, out local))
                    crosshair.anchoredPosition = local;
            }
        }
        Cursor.visible = !playing;

        // kilat merah memudar (indikator kena damage)
        if (damageFlash != null && damageFlash.color.a > 0f)
        {
            var fc = damageFlash.color;
            fc.a = Mathf.Max(0f, fc.a - Time.unscaledDeltaTime * 1.1f);
            damageFlash.color = fc;
        }

        if (gm == null || hudRoot == null || !hudRoot.activeSelf)
        {
            if (menuRoot != null && menuRoot.activeSelf)
            {
                if (menuBestText != null) menuBestText.text = "Rekor tertinggi: " + Leaderboard.Best();
                DriftMenuDots();
            }
            return;
        }
        if (gm.Player == null) return;

        var player = gm.Player;
        if (player.Health != null)
        {
            UIFactory.SetBarFill(hpFill, player.Health.Max <= 0f ? 0f : player.Health.Current / player.Health.Max, HpW, HpH);
            if (hpText != null)
                hpText.text = Mathf.CeilToInt(player.Health.Current) + " / " + Mathf.CeilToInt(player.Health.Max);
        }
        if (player.Stats != null)
        {
            UIFactory.SetBarFill(xpFill, player.Stats.XPToNext <= 0f ? 0f : player.Stats.XP / player.Stats.XPToNext, XpW, XpH);
            if (levelText != null) levelText.text = "LV " + player.Stats.Level;
        }

        // meter ultimate (juga menampilkan cooldown)
        if (chargeFill != null)
        {
            UIFactory.SetBarFill(chargeFill, gm.ChargeRatio, XpW, XpH);
            var ci = chargeFill.GetComponent<Image>();
            if (ci != null)
            {
                if (gm.UltimateLocked) ci.color = new Color(0.42f, 0.44f, 0.52f);       // abu = cooldown
                else if (gm.ChargeReady) ci.color = new Color(1f, 0.95f, 0.5f);          // emas = siap
                else ci.color = new Color(0.45f, 0.92f, 1f);                             // cyan = mengisi
            }

            if (ultLabel != null)
            {
                if (gm.UltimateLocked)
                {
                    ultLabel.text = "ULTIMATE  COOLDOWN  " + Mathf.CeilToInt(gm.UltimateCooldown) + "s";
                    ultLabel.color = new Color(0.60f, 0.63f, 0.72f);
                }
                else if (gm.ChargeReady)
                {
                    ultLabel.text = "ULTIMATE  (Q)  —  SIAP!";
                    ultLabel.color = new Color(1f, 0.95f, 0.5f);
                }
                else
                {
                    ultLabel.text = "ULTIMATE  (Q)";
                    ultLabel.color = UIFactory.TextDim;
                }
            }
        }

        // indikator combo
        if (comboText != null)
        {
            if (gm.Combo >= 3)
            {
                comboText.text = "COMBO  x" + gm.Combo;
                var cc = comboText.color;
                cc.a = Mathf.Min(1f, 0.55f + gm.Combo * 0.03f);
                comboText.color = cc;
            }
            else
            {
                var cc = comboText.color;
                cc.a = 0f;
                comboText.color = cc;
            }
        }

        // peringatan HP rendah (berdenyut)
        if (lowHp != null && player.Health != null)
        {
            float ratio = player.Health.Max <= 0f ? 1f : player.Health.Current / player.Health.Max;
            float target = ratio < 0.3f ? 0.08f + 0.09f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.5f)) : 0f;
            var lc = lowHp.color;
            lc.a = Mathf.MoveTowards(lc.a, target, 0.6f * Time.unscaledDeltaTime);
            lowHp.color = lc;
        }

        int min = Mathf.FloorToInt(gm.TimeAlive / 60f);
        int sec = Mathf.FloorToInt(gm.TimeAlive % 60f);
        if (timerText != null) timerText.text = string.Format("{0:00}:{1:00}", min, sec);
        if (killText != null) killText.text = gm.Kills + " musuh";
        if (tierText != null) tierText.text = "TIER " + gm.Tier;

        UpdateWeaponPanel();
        UpdateBossBar();
        UpdateBossArrow();

        if (bannerTimer > 0f && bannerText != null)
        {
            bannerTimer -= Time.unscaledDeltaTime;
            var c = bannerText.color;
            c.a = Mathf.Clamp01(bannerTimer / 0.7f);
            bannerText.color = c;
            if (bannerTimer <= 0f) bannerText.text = "";
        }
    }

    void DriftMenuDots()
    {
        if (menuDots == null || menuRoot == null || !menuRoot.activeSelf) return;
        float dt = Time.unscaledDeltaTime;
        for (int i = 0; i < menuDots.Length; i++)
        {
            if (menuDots[i] == null) continue;
            Vector2 p = menuDots[i].anchoredPosition + menuDotDir[i] * dt;
            if (p.y > 560f) p.y = -560f;
            if (p.x > 980f) p.x = -980f;
            else if (p.x < -980f) p.x = 980f;
            menuDots[i].anchoredPosition = p;
        }
    }

    void UpdateWeaponPanel()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.Player == null || gm.Player.Weapons == null) return;

        var list = gm.Player.Weapons.weapons;
        for (int i = 0; i < weaponRows.Length; i++)
        {
            if (weaponRows[i] == null) continue;
            if (i < list.Count && list[i] != null)
                weaponRows[i].text = "• " + list[i].DisplayName + "   " + Pips(list[i].Level, list[i].MaxLevel);
            else
                weaponRows[i].text = "";
        }
    }

    // Level senjata ditampilkan sebagai titik (●●●○○)
    static string Pips(int level, int max)
    {
        if (max <= 0) max = 6;
        var sb = new System.Text.StringBuilder(max);
        for (int i = 0; i < max; i++) sb.Append(i < level ? '●' : '○');
        return sb.ToString();
    }

    void UpdateBossBar()
    {
        if (bossRoot == null) return;

        Enemy boss = null;
        var all = Enemy.All;
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i] != null && all[i].IsBoss) { boss = all[i]; break; }
        }

        if (boss == null) { if (bossRoot.activeSelf) bossRoot.SetActive(false); return; }

        if (!bossRoot.activeSelf) bossRoot.SetActive(true);
        if (bossName != null) bossName.text = boss.DisplayName;
        if (bossFill != null && boss.Health != null)
            UIFactory.SetBarFill(bossFill, boss.Health.Max <= 0f ? 0f : boss.Health.Current / boss.Health.Max, 900f, 26f);
    }
}
