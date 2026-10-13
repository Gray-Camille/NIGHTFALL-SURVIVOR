using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    private const int MapHalfSize = 40;
    private const int PixelsPerBlock = 2;

    private static readonly string[] BiomeNames = { "草原", "雪原", "熔岩", "虚空" };
    private static readonly Color[][] Biomes =
    {
        new[] { new Color(0.18f, 0.45f, 0.22f), new Color(0.14f, 0.36f, 0.18f), new Color(0.35f, 0.65f, 0.30f) },
        new[] { new Color(0.55f, 0.65f, 0.78f), new Color(0.44f, 0.53f, 0.68f), new Color(0.80f, 0.88f, 0.95f) },
        new[] { new Color(0.32f, 0.10f, 0.08f), new Color(0.22f, 0.06f, 0.05f), new Color(0.68f, 0.18f, 0.08f) },
        new[] { new Color(0.10f, 0.08f, 0.18f), new Color(0.07f, 0.05f, 0.12f), new Color(0.25f, 0.18f, 0.40f) }
    };

    private Texture2D _texture;
    private Color32[] _pixels;
    private int _biomeIndex = -1;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        int size = MapHalfSize * 2 * PixelsPerBlock;
        _texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        _pixels = new Color32[size * size];
        var sprite = Sprite.Create(_texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 2f);
        var renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = -10;
        UpdateForWave(1);
    }

    public void UpdateForWave(int wave)
    {
        // Start() 之前纹理还没建好；此时调用直接跳过，Start 里会再调一次完成首绘
        if (_texture == null || _pixels == null) return;

        int nextIndex = Mathf.Clamp(((wave - 1) / 5) % Biomes.Length, 0, Biomes.Length - 1);
        if (nextIndex == _biomeIndex) return;

        _biomeIndex = nextIndex;
        DrawBiome(_biomeIndex);
        UIManager.Instance?.ShowAnnouncement("进入新区域：" + BiomeNames[_biomeIndex], Color.white);
    }

    private void DrawBiome(int biomeIndex)
    {
        var colors = Biomes[biomeIndex];
        int blocks = MapHalfSize * 2;
        int texSize = blocks * PixelsPerBlock;

        // 先算好每个区块的颜色，再一次性写入像素数组，避免逐像素 SetPixel 的巨大开销
        var blockColors = new Color32[blocks * blocks];
        for (int blockY = 0; blockY < blocks; blockY++)
        for (int blockX = 0; blockX < blocks; blockX++)
        {
            float noise = Mathf.PerlinNoise(blockX * 0.18f, blockY * 0.18f);
            Color color = (blockX + blockY) % 2 == 0 ? colors[0] : colors[1];
            if (noise > 0.72f) color = colors[2];
            blockColors[blockY * blocks + blockX] = color;
        }

        for (int blockY = 0; blockY < blocks; blockY++)
        {
            int startY = blockY * PixelsPerBlock;
            for (int blockX = 0; blockX < blocks; blockX++)
            {
                Color32 color = blockColors[blockY * blocks + blockX];
                int startX = blockX * PixelsPerBlock;
                for (int y = 0; y < PixelsPerBlock; y++)
                {
                    int rowStart = (startY + y) * texSize + startX;
                    for (int x = 0; x < PixelsPerBlock; x++) _pixels[rowStart + x] = color;
                }
            }
        }

        _texture.SetPixels32(_pixels);
        _texture.Apply(false, false);
    }
}
