using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主菜单的动态背景：夜色竖直渐变 + 闪烁星空 + 远景地牢剪影 + 一轮冷月。
/// 全部用代码生成贴图，不依赖任何外部素材。
/// </summary>
public class MenuBackground : MonoBehaviour
{
    private const float HalfWidth = 640f;
    private const float HalfHeight = 360f;

    private RectTransform[] _stars;
    private Image[] _starImages;
    private float[] _starPhase;
    private float[] _starSpeed;
    private float[] _starAlpha;
    private float[] _starRise;
    private float _time;

    public static void Build(Transform canvas)
    {
        var go = new GameObject("MenuBackground");
        go.transform.SetParent(canvas, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        go.AddComponent<MenuBackground>().Setup(rt);
    }

    void Setup(RectTransform root)
    {
        BuildGradient(root);
        BuildMoon(root);
        BuildSkyline(root);
        BuildStars(root);
    }

    void BuildGradient(RectTransform root)
    {
        var go = new GameObject("Gradient");
        go.transform.SetParent(root, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        var image = go.AddComponent<Image>();
        image.sprite = GradientSprite();
        image.color = Color.white;
        image.preserveAspect = false;
        image.raycastTarget = false;
    }

    void BuildMoon(RectTransform root)
    {
        var glow = new GameObject("MoonGlow");
        glow.transform.SetParent(root, false);
        var grt = glow.AddComponent<RectTransform>();
        grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0.5f);
        grt.pivot = new Vector2(0.5f, 0.5f);
        grt.sizeDelta = new Vector2(420, 420);
        grt.anchoredPosition = new Vector2(360, 190);
        var gimg = glow.AddComponent<Image>();
        gimg.sprite = SoftDotSprite(64);
        gimg.color = new Color(0.62f, 0.6f, 0.95f, 0.30f);
        gimg.raycastTarget = false;

        var core = new GameObject("MoonCore");
        core.transform.SetParent(root, false);
        var crt = core.AddComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(96, 96);
        crt.anchoredPosition = new Vector2(360, 190);
        var cimg = core.AddComponent<Image>();
        cimg.sprite = SoftDotSprite(48);
        cimg.color = new Color(0.88f, 0.86f, 1f, 0.55f);
        cimg.raycastTarget = false;
    }

    void BuildSkyline(RectTransform root)
    {
        var container = new GameObject("Skyline");
        container.transform.SetParent(root, false);
        var crt = container.AddComponent<RectTransform>();
        crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one; crt.sizeDelta = Vector2.zero;

        var color = new Color(0.035f, 0.028f, 0.07f, 0.96f);
        float x = -720f;
        int index = 0;
        while (x < 720f)
        {
            float width = Random.Range(46f, 120f);
            float height = Random.Range(70f, 210f);
            // 偶尔留出一段空地，让剪影错落一些
            if (index % 5 == 3) height *= 0.45f;
            var block = new GameObject("Block");
            block.transform.SetParent(container.transform, false);
            var rt = block.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(x + width * 0.5f, -26f);
            var image = block.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            // 塔尖上的小窗灯火
            var window = new GameObject("Window");
            window.transform.SetParent(block.transform, false);
            var wrt = window.AddComponent<RectTransform>();
            wrt.anchorMin = wrt.anchorMax = new Vector2(0.5f, 1f);
            wrt.pivot = new Vector2(0.5f, 1f);
            wrt.sizeDelta = new Vector2(6, 8);
            wrt.anchoredPosition = new Vector2(0, -14f);
            var wimg = window.AddComponent<Image>();
            wimg.color = new Color(1f, 0.78f, 0.38f, Random.Range(0.25f, 0.75f));
            wimg.raycastTarget = false;

            x += width + Random.Range(4f, 22f);
            index++;
        }
    }

    void BuildStars(RectTransform root)
    {
        var container = new GameObject("Stars");
        container.transform.SetParent(root, false);
        var crt = container.AddComponent<RectTransform>();
        crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one; crt.sizeDelta = Vector2.zero;

        int count = 96;
        _stars = new RectTransform[count];
        _starImages = new Image[count];
        _starPhase = new float[count];
        _starSpeed = new float[count];
        _starAlpha = new float[count];
        _starRise = new float[count];

        var bigSprite = SoftDotSprite(32);
        for (int i = 0; i < count; i++)
        {
            var starObject = new GameObject("Star");
            starObject.transform.SetParent(container.transform, false);
            var rt = starObject.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            float size = Random.Range(5f, 17f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(Random.Range(-HalfWidth, HalfWidth), Random.Range(-HalfHeight, HalfHeight));
            var image = starObject.AddComponent<Image>();
            image.sprite = bigSprite;
            image.raycastTarget = false;
            float warmth = Random.value;
            image.color = new Color(0.85f + warmth * 0.15f, 0.86f + warmth * 0.1f, 1f, 0.6f);

            _stars[i] = rt;
            _starImages[i] = image;
            _starPhase[i] = Random.Range(0f, Mathf.PI * 2f);
            _starSpeed[i] = Random.Range(0.6f, 2.2f);
            _starAlpha[i] = Random.Range(0.25f, 0.95f);
            _starRise[i] = Random.Range(2f, 9f);
        }
    }

    void Update()
    {
        if (_stars == null) return;
        _time += Time.unscaledDeltaTime;
        for (int i = 0; i < _stars.Length; i++)
        {
            var rt = _stars[i];
            if (rt == null) continue;
            float pulse = 0.55f + 0.45f * Mathf.Sin(_time * _starSpeed[i] + _starPhase[i]);
            var image = _starImages[i];
            if (image != null)
            {
                var c = image.color;
                c.a = _starAlpha[i] * pulse;
                image.color = c;
            }
            var pos = rt.anchoredPosition;
            pos.y += _starRise[i] * Time.unscaledDeltaTime;
            if (pos.y > HalfHeight + 12f)
            {
                pos.y = -HalfHeight - 12f;
                pos.x = Random.Range(-HalfWidth, HalfWidth);
            }
            rt.anchoredPosition = pos;
        }
    }

    // ===== 程序化贴图 =====
    static Sprite GradientSprite()
    {
        const int height = 256;
        var texture = new Texture2D(4, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[4 * height];
        for (int y = 0; y < height; y++)
        {
            float t = (float)y / (height - 1);       // 0 = 底部，1 = 顶部
            Color color;
            if (t > 0.62f)
                color = Color.Lerp(new Color(0.24f, 0.12f, 0.30f), new Color(0.03f, 0.03f, 0.09f), Mathf.InverseLerp(0.62f, 1f, t));
            else if (t > 0.34f)
                color = Color.Lerp(new Color(0.07f, 0.05f, 0.15f), new Color(0.24f, 0.12f, 0.30f), Mathf.InverseLerp(0.34f, 0.62f, t));
            else
                color = Color.Lerp(new Color(0.04f, 0.03f, 0.08f), new Color(0.07f, 0.05f, 0.15f), Mathf.InverseLerp(0f, 0.34f, t));
            var c32 = (Color32)color;
            for (int x = 0; x < 4; x++) pixels[y * 4 + x] = c32;
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return Sprite.Create(texture, new Rect(0, 0, 4, height), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite _softDot;

    static Sprite SoftDotSprite(int size)
    {
        if (_softDot != null) return _softDot;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[size * size];
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) - radius;
                float dy = (y + 0.5f) - radius;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                float a = Mathf.Clamp01(1f - d);
                a = a * a;                        // 二次衰减，边缘更柔
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        _softDot = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _softDot;
    }
}
