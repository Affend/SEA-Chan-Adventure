using System.Collections.Generic;
using UnityEngine;

// Dinding batu sebagai penghalang (obstacle) untuk player.
// Dinding dibuat per "chunk" di sekitar player dan dihapus saat sudah jauh,
// jadi map tetap terasa tak berujung tanpa membebani performa.
// Tata letak tiap chunk bersifat tetap (deterministik), jadi dinding di tempat
// yang sama akan selalu sama.
public class WallLayer : MonoBehaviour
{
    public const float ChunkSize = 30f;
    const int Radius = 2;          // chunk aktif di sekitar player (5x5)
    public const float Block = 1f;   // ukuran satu blok dinding (unit)
    const float SafeRadius = 8f;   // tidak ada dinding di sekitar titik awal player
    const int SortingOrder = 4;

    Transform player;
    Sprite[] sprites;
    readonly Dictionary<long, GameObject> chunks = new Dictionary<long, GameObject>();

    public void Init(Transform player, Sprite[] sprites)
    {
        this.player = player;
        this.sprites = (sprites != null && sprites.Length > 0) ? sprites : null;
    }

    static long Key(int x, int y) => ((long)x << 32) ^ (uint)y;

    void LateUpdate()
    {
        Refresh();
    }

    // Dipakai editor untuk membangun dinding sekali (pratinjau).
    public void BuildOnce()
    {
        Refresh();
    }

    void Refresh()
    {
        if (player == null || sprites == null) return;

        int pcx = Mathf.FloorToInt(player.position.x / ChunkSize);
        int pcy = Mathf.FloorToInt(player.position.y / ChunkSize);

        for (int dx = -Radius; dx <= Radius; dx++)
        {
            for (int dy = -Radius; dy <= Radius; dy++)
            {
                long k = Key(pcx + dx, pcy + dy);
                if (!chunks.ContainsKey(k))
                    chunks[k] = BuildChunk(pcx + dx, pcy + dy);
            }
        }

        List<long> remove = null;
        foreach (var kv in chunks)
        {
            long k = kv.Key;
            int cx = (int)(k >> 32);
            int cy = unchecked((int)(k & 0xffffffff));
            if (Mathf.Abs(cx - pcx) > Radius + 1 || Mathf.Abs(cy - pcy) > Radius + 1)
            {
                if (remove == null) remove = new List<long>();
                remove.Add(k);
            }
        }

        if (remove != null)
        {
            for (int i = 0; i < remove.Count; i++)
            {
                if (chunks[remove[i]] != null) Destroy(chunks[remove[i]]);
                chunks.Remove(remove[i]);
            }
        }
    }

    GameObject BuildChunk(int cx, int cy)
    {
        var root = new GameObject("Walls_" + cx + "_" + cy);
        root.transform.SetParent(transform, false);

        // hash sederhana supaya tata letak tetap untuk chunk yang sama
        int h = cx * 73856093 ^ cy * 19349663;
        var rng = new System.Random(h);

        float ox = cx * ChunkSize;
        float oy = cy * ChunkSize;

        int structures = 1 + rng.Next(2);
        for (int s = 0; s < structures; s++)
        {
            int type = rng.Next(4);
            float sx = ox + 6f + (float)rng.NextDouble() * (ChunkSize - 12f);
            float sy = oy + 6f + (float)rng.NextDouble() * (ChunkSize - 12f);

            if (type == 0) Line(root.transform, rng, sx, sy);
            else if (type == 1) Room(root.transform, rng, sx, sy);
            else if (type == 2) Corner(root.transform, rng, sx, sy);
            else Pillar(root.transform, rng, sx, sy);
        }

        return root;
    }

    void Line(Transform parent, System.Random rng, float sx, float sy)
    {
        bool horizontal = rng.Next(2) == 0;
        int len = 5 + rng.Next(8);
        for (int i = 0; i < len; i++)
            AddBlock(parent, horizontal ? sx + i * Block : sx, horizontal ? sy : sy + i * Block);
    }

    void Room(Transform parent, System.Random rng, float sx, float sy)
    {
        int w = 6 + rng.Next(5);
        int h = 6 + rng.Next(4);
        int gapSide = rng.Next(4);
        int gapAt = 1 + rng.Next(Mathf.Max(1, (gapSide < 2 ? w : h) - 2));

        for (int i = 0; i < w; i++)
        {
            if (!(gapSide == 0 && i == gapAt)) AddBlock(parent, sx + i * Block, sy);
            if (!(gapSide == 1 && i == gapAt)) AddBlock(parent, sx + i * Block, sy + (h - 1) * Block);
        }
        for (int j = 1; j < h - 1; j++)
        {
            if (!(gapSide == 2 && j == gapAt)) AddBlock(parent, sx, sy + j * Block);
            if (!(gapSide == 3 && j == gapAt)) AddBlock(parent, sx + (w - 1) * Block, sy + j * Block);
        }
    }

    void Corner(Transform parent, System.Random rng, float sx, float sy)
    {
        int len = 5 + rng.Next(5);
        bool flipX = rng.Next(2) == 0;
        bool flipY = rng.Next(2) == 0;
        for (int i = 0; i < len; i++)
        {
            AddBlock(parent, sx + (flipX ? -i : i) * Block, sy);
            AddBlock(parent, sx, sy + (flipY ? -i : i) * Block);
        }
    }

    void Pillar(Transform parent, System.Random rng, float sx, float sy)
    {
        int w = 3 + rng.Next(2);
        int h = 3 + rng.Next(2);
        for (int i = 0; i < w; i++)
            for (int j = 0; j < h; j++)
                AddBlock(parent, sx + i * Block, sy + j * Block);
    }

    void AddBlock(Transform parent, float x, float y)
    {
        // jangan menghalangi titik awal player
        if (x * x + y * y < SafeRadius * SafeRadius) return;

        var go = new GameObject("Wall");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprites[Mathf.Abs(Mathf.RoundToInt((x * 3f + y * 7f) / Block)) % sprites.Length];
        sr.sortingOrder = SortingOrder;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(Block * 0.98f, Block * 0.98f);
    }
}
