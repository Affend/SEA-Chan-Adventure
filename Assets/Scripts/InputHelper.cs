using UnityEngine;

// Pembungkus input supaya game tidak crash walau "Active Input Handling"
// diset ke mode baru. Semua baca tombol lewat class ini.
public static class InputHelper
{
    static bool _checked;
    static bool _legacyOk = true;
    static bool _axisOk = true;
    static readonly System.Collections.Generic.HashSet<string> _badButtons =
        new System.Collections.Generic.HashSet<string>();

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

    // Aman dipakai walau tombol/axis tidak ada di Input Manager (tidak error).
    public static bool GetButtonDownSafe(string name)
    {
        EnsureChecked();
        if (!_legacyOk || _badButtons.Contains(name)) return false;
        try { return Input.GetButtonDown(name); }
        catch (System.Exception) { _badButtons.Add(name); return false; }
    }

    // Gerak: keyboard (WASD / panah) + gamepad (stick kiri) sekaligus.
    public static Vector2 MoveAxis()
    {
        EnsureChecked();
        Vector2 m = Vector2.zero;

        if (GetKey(KeyCode.A) || GetKey(KeyCode.LeftArrow)) m.x -= 1f;
        if (GetKey(KeyCode.D) || GetKey(KeyCode.RightArrow)) m.x += 1f;
        if (GetKey(KeyCode.S) || GetKey(KeyCode.DownArrow)) m.y -= 1f;
        if (GetKey(KeyCode.W) || GetKey(KeyCode.UpArrow)) m.y += 1f;

        // Gamepad: pakai axis bawaan Unity (Horizontal/Vertical) kalau tersedia.
        if (_axisOk && m.sqrMagnitude < 0.01f)
        {
            try
            {
                float x = Input.GetAxisRaw("Horizontal");
                float y = Input.GetAxisRaw("Vertical");
                if (Mathf.Abs(x) > 0.18f || Mathf.Abs(y) > 0.18f) m = new Vector2(x, y);
            }
            catch (System.Exception)
            {
                _axisOk = false;
                Debug.LogWarning("[SEA Chan] Axis 'Horizontal/Vertical' tidak ditemukan di Input Manager — " +
                    "gamepad tidak aktif, keyboard tetap jalan normal.");
            }
        }

        if (m.sqrMagnitude > 1f) m.Normalize();
        return m;
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
