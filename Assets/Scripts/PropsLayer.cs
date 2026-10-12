using UnityEngine;

// Menyebar dekorasi (pohon, batu, api unggun, kaktus, dll) secara acak
// namun tetap konsisten: posisi & jenis ditentukan dari koordinat dunia,
// jadi prop tidak "melompat" saat player berjalan.
public class PropsLayer : MonoBehaviour
{
    const float Cell = 9f;
    const int Cols = 7;
    const int Rows = 6;
    const int SortingOrder = -50;

    Transform player;
    Sprite[] props;
    SpriteRenderer[] rends;

    public void Init(Transform player, Sprite[] props)
    {
        this.player = player;
        this.props = (props != null && props.Length > 0) ? props : null;

        int n = Cols * Rows;
        rends = new SpriteRenderer[n];
        for (int i = 0; i < n; i++)
        {
            var go = new GameObject("Prop");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = SortingOrder;
            sr.enabled = false;
            rends[i] = sr;
        }
    }

    static int Hash(int x, int y)
    {
        int h = x * 374761393 + y * 668265263;
        h = (h ^ (h >> 13)) * 1274126177;
        return h ^ (h >> 16);
    }

    void LateUpdate()
    {
        Place();
    }

    // Dipakai editor untuk menempatkan dekorasi sekali (pratinjau).
    public void PlaceOnce()
    {
        Place();
    }

    void Place()
    {
        if (player == null || rends == null) return;

        if (props == null)
        {
            for (int i = 0; i < rends.Length; i++) rends[i].enabled = false;
            return;
        }

        Vector2 p = player.position;
        int bx = Mathf.FloorToInt(p.x / Cell);
        int by = Mathf.FloorToInt(p.y / Cell);

        for (int i = 0; i < rends.Length; i++)
        {
            int cx = bx + (i % Cols) - Cols / 2;
            int cy = by + (i / Cols) - Rows / 2;

            int h = Hash(cx, cy);
            int roll = Mathf.Abs(h) % 100;

            if (roll >= 28) { rends[i].enabled = false; continue; }

            rends[i].enabled = true;
            rends[i].sprite = props[Mathf.Abs(h / 11) % props.Length];

            float jx = ((Mathf.Abs(h / 3) % 100) / 100f - 0.5f) * Cell * 0.7f;
            float jy = ((Mathf.Abs(h / 5) % 100) / 100f - 0.5f) * Cell * 0.7f;

            float px2 = (cx + 0.5f) * Cell + jx;
            float py2 = (cy + 0.5f) * Cell + jy;

            // jangan menaruh dekorasi di sekitar titik awal player
            if (px2 * px2 + py2 * py2 < 16f) { rends[i].enabled = false; continue; }

            rends[i].transform.position = new Vector3(px2, py2, 0f);
        }
    }
}
