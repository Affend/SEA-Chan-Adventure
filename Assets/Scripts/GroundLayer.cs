using UnityEngine;

// Lantai batu yang seolah tak berujung.
// Grid tile "dikunci" ke koordinat dunia; tiap tile memilih sprite berdasarkan
// koordinat dunianya, jadi pola tetap konsisten walau player berjalan jauh.
public class GroundLayer : MonoBehaviour
{
    public const float TileSize = 2f;
    const int Cols = 24;
    const int Rows = 16;
    const int SortingOrder = -100;

    Transform player;
    SpriteRenderer[] rends;
    Sprite[] variants;
    int[] lastKey;

    public void Init(Transform player, Sprite[] variants)
    {
        this.player = player;
        this.variants = variants;

        int n = Cols * Rows;
        rends = new SpriteRenderer[n];
        lastKey = new int[n];

        for (int i = 0; i < n; i++)
        {
            int cx = i % Cols;
            int cy = i / Cols;

            var go = new GameObject("Floor");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3((cx - Cols / 2) * TileSize, (cy - Rows / 2) * TileSize, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = SortingOrder;
            sr.color = Color.white;

            if (variants != null && variants.Length > 0)
            {
                sr.sprite = variants[0];
                go.transform.localScale = Vector3.one;
            }
            else
            {
                sr.sprite = SpriteFactory.Square;
                sr.color = new Color(0.30f, 0.32f, 0.38f);
                go.transform.localScale = new Vector3(TileSize, TileSize, 1f);
            }

            rends[i] = sr;
            lastKey[i] = int.MinValue;
        }
    }

    void LateUpdate()
    {
        if (player == null || rends == null) return;

        Vector2 p = player.position;
        float ax = Mathf.Floor(p.x / TileSize) * TileSize;
        float ay = Mathf.Floor(p.y / TileSize) * TileSize;
        Vector3 anchor = new Vector3(ax, ay, 0f);

        if ((transform.position - anchor).sqrMagnitude > 0.0001f)
        {
            transform.position = anchor;
            for (int i = 0; i < lastKey.Length; i++) lastKey[i] = int.MinValue;
        }

        if (variants == null || variants.Length == 0) return;

        for (int i = 0; i < rends.Length; i++)
        {
            Vector3 wp = rends[i].transform.position;
            int wx = Mathf.RoundToInt(wp.x / TileSize);
            int wy = Mathf.RoundToInt(wp.y / TileSize);
            int key = wx * 73856093 ^ wy * 19349663;
            if (key == lastKey[i]) continue;
            lastKey[i] = key;

            int idx = 0;
            if (variants.Length > 1 && Mathf.Abs(key) % 19 == 0) idx = 1;
            rends[i].sprite = variants[Mathf.Clamp(idx, 0, variants.Length - 1)];
        }
    }
}
