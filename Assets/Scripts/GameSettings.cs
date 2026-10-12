using System.Collections.Generic;
using UnityEngine;

// =====================================================================
// Pengaturan tombol (bisa di-remap) & mode layar.
// Disimpan di PlayerPrefs supaya tetap tersimpan setelah game ditutup.
// =====================================================================

public enum GameAction { Dash, Ultimate, Pause, Mute }

public static class Keybinds
{
    // true saat pemain sedang menekan tombol baru di menu pengaturan
    // (supaya tombol yang ditekan tidak ikut menjalankan aksi game).
    public static bool Capturing { get; set; }

    const string Prefix = "sea_key_";

    static readonly Dictionary<GameAction, KeyCode> Defaults = new Dictionary<GameAction, KeyCode>
    {
        { GameAction.Dash, KeyCode.LeftShift },
        { GameAction.Ultimate, KeyCode.Q },
        { GameAction.Pause, KeyCode.Escape },
        { GameAction.Mute, KeyCode.M }
    };

    static readonly Dictionary<GameAction, KeyCode> Current = new Dictionary<GameAction, KeyCode>();
    static bool loaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { loaded = false; Current.Clear(); }

    public static void Load()
    {
        if (loaded) return;
        loaded = true;
        Current.Clear();
        foreach (var kv in Defaults)
            Current[kv.Key] = (KeyCode)PlayerPrefs.GetInt(Prefix + kv.Key, (int)kv.Value);
    }

    public static KeyCode Get(GameAction a)
    {
        Load();
        KeyCode k;
        return Current.TryGetValue(a, out k) ? k : Defaults[a];
    }

    public static void Set(GameAction a, KeyCode k)
    {
        Load();
        Current[a] = k;
        PlayerPrefs.SetInt(Prefix + a, (int)k);
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        Load();
        foreach (var kv in Defaults)
        {
            Current[kv.Key] = kv.Value;
            PlayerPrefs.SetInt(Prefix + kv.Key, (int)kv.Value);
        }
        PlayerPrefs.Save();
    }

    public static string Name(GameAction a)
    {
        if (a == GameAction.Dash) return "Dash";
        if (a == GameAction.Ultimate) return "Ultimate";
        if (a == GameAction.Pause) return "Pause";
        return "Hening (Mute)";
    }

    public static string Label(GameAction a) { return Pretty(Get(a)); }

    public static string Pretty(KeyCode k)
    {
        switch (k)
        {
            case KeyCode.LeftShift: return "SHIFT KIRI";
            case KeyCode.RightShift: return "SHIFT KANAN";
            case KeyCode.LeftControl: return "CTRL KIRI";
            case KeyCode.RightControl: return "CTRL KANAN";
            case KeyCode.LeftAlt: return "ALT KIRI";
            case KeyCode.RightAlt: return "ALT KANAN";
            case KeyCode.Escape: return "ESC";
            case KeyCode.Space: return "SPASI";
            case KeyCode.Return: return "ENTER";
            case KeyCode.UpArrow: return "PANAH ATAS";
            case KeyCode.DownArrow: return "PANAH BAWAH";
            case KeyCode.LeftArrow: return "PANAH KIRI";
            case KeyCode.RightArrow: return "PANAH KANAN";
            case KeyCode.Tab: return "TAB";
            case KeyCode.Backspace: return "BACKSPACE";
            case KeyCode.Mouse0: return "KLIK KIRI";
            case KeyCode.Mouse1: return "KLIK KANAN";
        }
        return k.ToString().ToUpper();
    }

    // Menangkap tombol yang baru saja ditekan (untuk mode "tekan tombol baru").
    public static bool TryCapture(out KeyCode key)
    {
        key = KeyCode.None;
        try
        {
            if (!Input.anyKeyDown) return false;

            foreach (KeyCode k in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (k == KeyCode.None) continue;
                if (k <= KeyCode.Mouse6 && k >= KeyCode.Mouse0) continue;   // klik mouse diabaikan
                if (Input.GetKeyDown(k)) { key = k; return true; }
            }
        }
        catch { }
        return false;
    }
}

// Ukuran UI — berguna kalau dipresentasikan di layar besar / proyektor.
public static class UiScale
{
    static readonly float[] Steps = { 1.0f, 1.15f, 1.3f, 1.5f };
    const string Key = "sea_ui_scale";
    static int index;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { index = 0; }

    public static float Factor => Steps[Mathf.Clamp(index, 0, Steps.Length - 1)];

    public static void Load() { index = Mathf.Clamp(PlayerPrefs.GetInt(Key, 0), 0, Steps.Length - 1); }

    public static void Cycle()
    {
        index = (index + 1) % Steps.Length;
        PlayerPrefs.SetInt(Key, index);
        PlayerPrefs.Save();
    }

    public static string Label() { return Mathf.RoundToInt(Factor * 100f) + "%"; }
}

// Mode tampilan: layar penuh atau resolusi jendela tertentu.
public static class ScreenSetup
{
    static readonly Vector2Int[] Modes =
    {
        new Vector2Int(0, 0),          // layar penuh
        new Vector2Int(1280, 720),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080)
    };

    const string Key = "sea_screen_mode";
    static int index;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { index = 0; }

    public static void Load()
    {
        index = Mathf.Clamp(PlayerPrefs.GetInt(Key, 0), 0, Modes.Length - 1);
        Apply();
    }

    public static void Cycle()
    {
        index = (index + 1) % Modes.Length;
        PlayerPrefs.SetInt(Key, index);
        PlayerPrefs.Save();
        Apply();
    }

    public static void Apply()
    {
        var m = Modes[index];
        try
        {
            if (m.x <= 0)
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height,
                    FullScreenMode.FullScreenWindow);
            else
                Screen.SetResolution(m.x, m.y, FullScreenMode.Windowed);
        }
        catch { }
    }

    public static string Label()
    {
        var m = Modes[index];
        return m.x <= 0 ? "LAYAR PENUH" : (m.x + " x " + m.y);
    }
}
