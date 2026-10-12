using UnityEngine;

// Pembungkus input supaya game tidak crash walau "Active Input Handling"
// diset ke mode baru. Semua baca tombol lewat class ini.
public static class InputHelper
{
    static bool _checked;
    static bool _legacyOk = true;

    static void EnsureChecked()
    {
        if (_checked) return;
        _checked = true;
        try { Input.GetKey(KeyCode.Space); }
        catch (System.Exception)
        {
            _legacyOk = false;
            Debug.LogWarning("[SEA Chan] Legacy Input tidak aktif. " +
                "Buka Edit > Project Settings > Player > Active Input Handling = 'Input Manager (Old)' atau 'Both'.");
        }
    }

    public static bool GetKey(KeyCode key)
    {
        EnsureChecked();
        if (!_legacyOk) return false;
        return Input.GetKey(key);
    }

    public static bool GetKeyDown(KeyCode key)
    {
        EnsureChecked();
        if (!_legacyOk) return false;
        return Input.GetKeyDown(key);
    }

    public static Vector2 MousePosition()
    {
        EnsureChecked();
        if (!_legacyOk) return Vector2.zero;
        return Input.mousePosition;
    }

    public static bool GetMouseButtonDown(int button)
    {
        EnsureChecked();
        if (!_legacyOk) return false;
        return Input.GetMouseButtonDown(button);
    }

    public static bool GetMouseButton(int button)
    {
        EnsureChecked();
        if (!_legacyOk) return false;
        return Input.GetMouseButton(button);
    }
}
