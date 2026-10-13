using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 程序化生成所有像素精灵——无需任何外部图片素材
/// </summary>
public static class PixelSpriteGenerator
{
    private const int PixelsPerUnit = 16;

    // ===== 颜色调色板 =====
    private static readonly Color Transparent = new Color(0, 0, 0, 0);
    private static readonly Color SkinLight = new Color(1f, 0.85f, 0.7f);
    private static readonly Color SkinShadow = new Color(0.85f, 0.65f, 0.5f);
    private static readonly Color HairGold = new Color(0.95f, 0.75f, 0.2f);
    private static readonly Color HairDark = new Color(0.7f, 0.5f, 0.1f);
    private static readonly Color EyeGreen = new Color(0.15f, 0.85f, 0.3f);
    private static readonly Color EyeDark = new Color(0.05f, 0.3f, 0.1f);
    private static readonly Color ArmorBlue = new Color(0.25f, 0.55f, 0.9f);
    private static readonly Color ArmorLight = new Color(0.45f, 0.75f, 1f);
    private static readonly Color CapeRed = new Color(0.85f, 0.2f, 0.15f);
    private static readonly Color BladeBlue = new Color(0.4f, 0.7f, 1f);
    private static readonly Color BladeWhite = new Color(0.8f, 0.95f, 1f);
    private static readonly Color GoldTrim = new Color(0.9f, 0.75f, 0.1f);
    private static readonly Color SlimeGreen = new Color(0.3f, 0.85f, 0.3f);
    private static readonly Color SlimeDark = new Color(0.1f, 0.5f, 0.1f);
    private static readonly Color SlimeHighlight = new Color(0.6f, 0.95f, 0.6f);
    private static readonly Color BatPurple = new Color(0.55f, 0.25f, 0.75f);
    private static readonly Color BatDark = new Color(0.3f, 0.1f, 0.45f);
    private static readonly Color BruteRed = new Color(0.8f, 0.25f, 0.2f);
    private static readonly Color BruteDark = new Color(0.45f, 0.1f, 0.1f);
    private static readonly Color ShooterGreen = new Color(0.25f, 0.75f, 0.55f);
    private static readonly Color ShooterDark = new Color(0.1f, 0.4f, 0.3f);
    private static readonly Color EnemyPurple = new Color(0.75f, 0.2f, 0.8f);
    private static readonly Color ProjectileYellow = new Color(1f, 0.9f, 0.2f);
    private static readonly Color ProjectileOrange = new Color(1f, 0.6f, 0.1f);
    private static readonly Color XPBlue = new Color(0.3f, 0.6f, 1f);
    private static readonly Color XPCyan = new Color(0.2f, 0.9f, 0.9f);
    private static readonly Color FloorDark = new Color(0.15f, 0.12f, 0.18f);
    private static readonly Color FloorLight = new Color(0.22f, 0.18f, 0.25f);
    private static readonly Color BloodRed = new Color(0.8f, 0.1f, 0.1f);
    private static readonly Color White = Color.white;

    private static Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

    /// <summary>
    /// 清除缓存（切换场景时调用）
    /// </summary>
    public static void ClearCache()
    {
        _spriteCache.Clear();
    }

    /// <summary>
    /// 获取或创建精灵
    /// </summary>
    public static Sprite GetSprite(string name)
    {
        if (_spriteCache.TryGetValue(name, out var cached))
            return cached;

        Sprite sprite = null;
        switch (name)
        {
            case "hero": sprite = CreateHeroSprite(); break;
            case "slime": sprite = CreateSlimeSprite(); break;
            case "bat": sprite = CreateBatSprite(); break;
            case "brute": sprite = CreateBruteSprite(); break;
            case "shooter": sprite = CreateShooterSprite(); break;
            case "skeleton": sprite = CreateSkeletonSprite(); break;
            case "crawler": sprite = CreateCrawlerSprite(); break;
            case "elite_knight": sprite = CreateEliteKnightSprite(); break;
            case "elite_warlock": sprite = CreateEliteWarlockSprite(); break;
            case "stone_golem": sprite = CreateStoneGolemSprite(); break;
            case "shadow_assassin": sprite = CreateShadowAssassinSprite(); break;
            case "slime_king": sprite = CreateSlimeKingSprite(); break;
            case "demon_overlord": sprite = CreateDemonOverlordSprite(); break;
            case "projectile": sprite = CreateProjectileSprite(); break;
            case "enemy_projectile": sprite = CreateEnemyProjectileSprite(); break;
            case "xp_orb": sprite = CreateXPSprite(); break;
            case "gold_coin": sprite = CreateCoinSprite(); break;
            case "health_potion": sprite = CreateItemSprite(new Color(0.85f, 0.15f, 0.2f), Color.white); break;
            case "power_crystal": sprite = CreateItemSprite(new Color(0.95f, 0.8f, 0.1f), Color.white); break;
            case "armor_plate": sprite = CreateItemSprite(new Color(0.35f, 0.55f, 0.9f), Color.white); break;
            case "swift_charm": sprite = CreateItemSprite(new Color(0.2f, 0.85f, 0.5f), Color.white); break;
            case "attack_potion": sprite = CreateItemSprite(new Color(0.9f, 0.55f, 0.15f), Color.white); break;
            case "rage_potion": sprite = CreateItemSprite(new Color(0.95f, 0.3f, 0.15f), Color.white); break;
            case "haste_potion": sprite = CreateItemSprite(new Color(0.25f, 0.9f, 0.85f), Color.white); break;
            case "iron_shield": sprite = CreateItemSprite(new Color(0.65f, 0.7f, 0.8f), Color.white); break;
            case "magnet_charm": sprite = CreateItemSprite(new Color(0.75f, 0.4f, 0.95f), Color.white); break;
            case "orbit_blade": sprite = CreateOrbitBladeSprite(); break;
            case "aura_ring": sprite = CreateAuraRingSprite(); break;
            case "hp_bar": sprite = CreateHealthBarSprite(); break;
            case "floor": sprite = CreateFloorTile(); break;
            default: sprite = CreateFallbackSprite(); break;
        }

        _spriteCache[name] = sprite;
        return sprite;
    }

    public static Sprite GetEnemySprite(Enemy.EnemyType type)
    {
        string name = type.ToString().ToLowerInvariant();
        switch (type)
        {
            case Enemy.EnemyType.EliteKnight: name = "elite_knight"; break;
            case Enemy.EnemyType.EliteWarlock: name = "elite_warlock"; break;
            case Enemy.EnemyType.StoneGolem: name = "stone_golem"; break;
            case Enemy.EnemyType.ShadowAssassin: name = "shadow_assassin"; break;
            case Enemy.EnemyType.SlimeKing: name = "slime_king"; break;
            case Enemy.EnemyType.DemonOverlord: name = "demon_overlord"; break;
        }
        return GetSprite(name);
    }

    // ===== 像素勇者 (16x16) =====
    private static Sprite CreateHeroSprite()
    {
        int w = 16, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = Transparent;

        void Set(int x, int y, Color c) { if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = c; }

        // 头发 (金发刺猬头)
        for (int y = 10; y <= 15; y++)
        for (int x = 4; x <= 11; x++)
        {
            if (y == 15 && (x >= 5 && x <= 10)) Set(x, y, HairGold);
            else if (y == 14 && (x >= 4 && x <= 11)) Set(x, y, HairGold);
            else if (y == 13 && (x >= 4 && x <= 11)) Set(x, y, HairGold);
            else if (y == 12 && (x >= 5 && x <= 10)) Set(x, y, HairGold);
            else if (y == 11 && (x >= 5 && x <= 10)) Set(x, y, HairDark);
            else if (y == 10 && (x >= 6 && x <= 9)) Set(x, y, HairDark);
        }
        // 呆毛
        Set(8, 16, HairGold);

        // 脸部 (皮肤)
        for (int y = 7; y <= 11; y++)
        for (int x = 6; x <= 9; x++)
        {
            if (px[y * w + x] == Transparent)
                Set(x, y, SkinLight);
        }
        // 耳朵
        Set(5, 9, SkinLight); Set(10, 9, SkinLight);

        // 眼睛
        Set(7, 10, EyeDark); Set(8, 10, EyeDark);
        Set(7, 9, EyeGreen); Set(8, 9, EyeGreen);
        Set(7, 8, EyeGreen); Set(8, 8, EyeGreen);

        // 嘴（微笑）
        Set(7, 7, new Color(0.5f, 0.2f, 0.2f)); Set(8, 7, new Color(0.5f, 0.2f, 0.2f));

        // 身体/铠甲
        for (int y = 3; y <= 6; y++)
        for (int x = 5; x <= 10; x++)
        {
            if (y >= 4 && y <= 5 && x >= 6 && x <= 9)
                Set(x, y, ArmorBlue);
            else if (y == 3 || y == 6)
                Set(x, y, ArmorLight);
        }
        // 胸甲亮点
        Set(7, 5, ArmorLight); Set(8, 5, ArmorLight);

        // 披风 (红)
        Set(3, 5, CapeRed); Set(3, 4, CapeRed);
        Set(12, 5, CapeRed); Set(12, 4, CapeRed);

        // 腿
        for (int y = 0; y <= 2; y++)
        for (int x = 6; x <= 9; x++)
            Set(x, y, new Color(0.3f, 0.25f, 0.35f));

        // 鞋子
        Set(6, 0, new Color(0.4f, 0.2f, 0.1f)); Set(7, 0, new Color(0.4f, 0.2f, 0.1f));
        Set(8, 0, new Color(0.4f, 0.2f, 0.1f)); Set(9, 0, new Color(0.4f, 0.2f, 0.1f));

        // 剑 (右手)
        Set(11, 6, BladeBlue); Set(11, 7, BladeBlue); Set(11, 8, BladeBlue);
        Set(11, 9, BladeBlue); Set(11, 10, BladeBlue); Set(11, 11, BladeWhite);
        Set(11, 12, BladeWhite); Set(11, 13, BladeWhite);
        Set(10, 6, GoldTrim); Set(12, 6, GoldTrim);
        Set(10, 7, GoldTrim); Set(12, 7, GoldTrim);

        // 盾 (左手)
        Set(4, 5, GoldTrim); Set(4, 6, GoldTrim); Set(4, 7, GoldTrim);
        Set(3, 5, GoldTrim); Set(3, 6, new Color(0.8f, 0.7f, 0.3f)); Set(3, 7, GoldTrim);
        Set(2, 6, GoldTrim);

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    // ===== 像素史莱姆 (16x16) =====
    private static Sprite CreateSlimeSprite()
    {
        int w = 16, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = Transparent;

        void Set(int x, int y, Color c) { if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = c; }

        // 史莱姆身体（椭圆水滴形）
        // 底部宽
        for (int y = 0; y <= 7; y++)
        for (int x = 3; x <= 12; x++)
            Set(x, y, SlimeGreen);

        // 中部
        for (int y = 8; y <= 11; y++)
        for (int x = 4; x <= 11; x++)
            Set(x, y, SlimeGreen);

        // 上部（变窄）
        for (int y = 12; y <= 13; y++)
        for (int x = 5; x <= 10; x++)
            Set(x, y, SlimeGreen);

        // 顶部
        for (int y = 14; y <= 15; y++)
        for (int x = 6; x <= 9; x++)
            Set(x, y, SlimeGreen);

        // 阴影
        for (int y = 0; y <= 2; y++)
        for (int x = 4; x <= 5; x++)
            Set(x, y, SlimeDark);

        // 高光
        Set(7, 12, SlimeHighlight); Set(8, 12, SlimeHighlight);
        Set(9, 11, SlimeHighlight);

        // 眼睛（凶恶）
        Set(6, 13, White); Set(7, 13, EyeDark);
        Set(9, 13, White); Set(10, 13, EyeDark);

        // 嘴
        Set(7, 10, SlimeDark); Set(8, 10, SlimeDark); Set(9, 10, SlimeDark);

        // 底部影子
        for (int x = 4; x <= 11; x++)
            Set(x, 0, new Color(0.05f, 0.2f, 0.05f, 0.5f));

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    // ===== 蝙蝠 (16x16) =====
    private static Sprite CreateBatSprite()
    {
        int w = 16, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = Transparent;

        void Set(int x, int y, Color c) { if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = c; }

        for (int x = 6; x <= 9; x++)
        for (int y = 5; y <= 10; y++)
            Set(x, y, BatPurple);
        for (int x = 2; x <= 5; x++)
        for (int y = 7 - Mathf.RoundToInt(Mathf.Abs(x - 5) * 0.5f); y <= 9; y++)
            Set(x, y, BatDark);
        for (int x = 10; x <= 13; x++)
        for (int y = 7 - Mathf.RoundToInt(Mathf.Abs(x - 10) * 0.5f); y <= 9; y++)
            Set(x, y, BatDark);
        Set(5, 12, BatDark); Set(10, 12, BatDark);
        Set(7, 9, Color.white); Set(8, 9, Color.white);
        Set(7, 8, Color.black); Set(8, 8, Color.black);
        Set(7, 6, BatDark); Set(8, 6, BatDark);

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    // ===== 重装怪 (16x16) =====
    private static Sprite CreateBruteSprite()
    {
        int w = 16, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = Transparent;

        void Set(int x, int y, Color c) { if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = c; }

        for (int x = 4; x <= 11; x++)
        for (int y = 2; y <= 12; y++)
            Set(x, y, BruteRed);
        for (int x = 5; x <= 10; x++) { Set(x, 12, BruteDark); Set(x, 8, BruteDark); }
        for (int y = 3; y <= 7; y++) { Set(5, y, BruteDark); Set(10, y, BruteDark); }
        for (int x = 2; x <= 4; x++) Set(x, 9, BruteDark);
        for (int x = 11; x <= 13; x++) Set(x, 9, BruteDark);
        Set(6, 10, Color.white); Set(9, 10, Color.white);
        Set(6, 9, Color.black); Set(9, 9, Color.black);
        for (int x = 6; x <= 9; x++) Set(x, 4, Color.black);

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    // ===== 远程射手 (16x16) =====
    private static Sprite CreateShooterSprite()
    {
        int w = 16, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = Transparent;

        void Set(int x, int y, Color c) { if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = c; }

        for (int x = 5; x <= 10; x++)
        for (int y = 4; y <= 12; y++)
            Set(x, y, ShooterGreen);
        for (int x = 6; x <= 9; x++) Set(x, 12, ShooterDark);
        for (int y = 5; y <= 11; y++) Set(5, y, ShooterDark);
        for (int x = 10; x <= 14; x++) Set(x, 8, ShooterDark);
        Set(7, 10, Color.white); Set(9, 10, Color.white);
        Set(7, 9, Color.black); Set(9, 9, Color.black);
        Set(8, 6, ShooterDark);

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    // ===== 敌方弹幕 (8x8) =====
    private static Sprite CreateEnemyProjectileSprite()
    {
        int w = 8, h = 8;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = Transparent;

        void Set(int x, int y, Color c) { if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = c; }

        for (int y = 2; y <= 5; y++)
        for (int x = 2; x <= 5; x++)
            Set(x, y, EnemyPurple);
        Set(3, 4, Color.white); Set(4, 4, Color.white);
        Set(3, 3, EnemyPurple); Set(4, 3, EnemyPurple);

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    // ===== 子弹/弹幕 (8x8) =====
    private static Sprite CreateProjectileSprite()
    {
        int w = 8, h = 8;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = Transparent;

        void Set(int x, int y, Color c) { if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = c; }

        // 魔法弹（菱形）
        Set(3, 4, ProjectileYellow); Set(4, 4, ProjectileOrange);
        Set(3, 3, ProjectileYellow); Set(4, 3, ProjectileOrange);
        Set(2, 4, ProjectileOrange); Set(5, 4, ProjectileYellow);
        Set(3, 5, ProjectileOrange); Set(4, 5, ProjectileYellow);
        Set(2, 3, ProjectileOrange); Set(5, 3, ProjectileYellow);
        Set(2, 5, ProjectileOrange); Set(5, 5, ProjectileYellow);
        Set(4, 2, ProjectileYellow); Set(4, 6, ProjectileYellow);
        // 中心亮点
        Set(3, 4, White); Set(4, 4, White);

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    // ===== XP宝珠 (8x8) =====
    private static Sprite CreateXPSprite()
    {
        int w = 8, h = 8;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = Transparent;

        void Set(int x, int y, Color c) { if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = c; }

        // 钻石形 XP 宝石
        Set(3, 2, XPCyan); Set(4, 2, XPCyan);
        Set(2, 3, XPCyan); Set(3, 3, XPBlue); Set(4, 3, XPBlue); Set(5, 3, XPCyan);
        Set(1, 4, XPCyan); Set(2, 4, XPBlue); Set(3, 4, XPBlue); Set(4, 4, XPBlue); Set(5, 4, XPBlue); Set(6, 4, XPCyan);
        Set(2, 5, XPBlue); Set(3, 5, XPBlue); Set(4, 5, XPBlue); Set(5, 5, XPBlue);
        Set(3, 6, XPBlue); Set(4, 6, XPBlue);
        // 高光
        Set(3, 3, XPCyan);

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    // ===== 地板瓦片 (16x16) =====
    private static Sprite CreateFloorTile()
    {
        int w = 16, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] px = new Color[w * h];

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            var r = (x + y) % 2 == 0 ? FloorDark : FloorLight;
            // 添加一些噪点
            float noise = Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 0.03f;
            px[y * w + x] = new Color(r.r + noise, r.g + noise, r.b + noise, 1f);
        }

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    // ===== 后备方块 =====
    private static Sprite CreatePatternSprite(string[] rows, Dictionary<char, Color> palette)
    {
        int w = 16, h = rows.Length;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (x < rows[y].Length && palette.TryGetValue(rows[y][x], out Color color))
                px[y * w + x] = color;
        }

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    private static Sprite CreateSkeletonSprite()
    {
        var palette = new Dictionary<char, Color>
        {
            ['W'] = new Color(0.88f, 0.88f, 0.82f),
            ['B'] = new Color(0.08f, 0.08f, 0.08f)
        };
        return CreatePatternSprite(new[]
        {
            "................",
            "................",
            "................",
            "......WWWW......",
            ".....WWWWWW.....",
            ".....WBWBBW.....",
            "......WWWW......",
            "......WWWW......",
            "....W.WWWW.W....",
            "....WWWWWWWW....",
            "....W.WWWW.W....",
            ".....WWWWWW.....",
            "......WW.WW.....",
            "......WW.WW.....",
            "......W...W.....",
            "................"
        }, palette);
    }

    private static Sprite CreateCrawlerSprite()
    {
        var palette = new Dictionary<char, Color>
        {
            ['G'] = new Color(0.55f, 0.22f, 0.12f),
            ['B'] = new Color(0.9f, 0.8f, 0.2f),
            ['L'] = new Color(0.25f, 0.1f, 0.05f)
        };
        return CreatePatternSprite(new[]
        {
            "................",
            "..L..........L..",
            "...L.GGGGGG.L...",
            "....LGGGGGGL....",
            ".....GBGBBG.....",
            "....GGGGGGGG....",
            "...GGGGGGGGGG...",
            "..L..GGGGGG..L..",
            ".L....GGGG....L.",
            "L......GG......L",
            "................",
            "................",
            "................",
            "................",
            "................",
            "................"
        }, palette);
    }

    private static Sprite CreateEliteKnightSprite()
    {
        var palette = new Dictionary<char, Color>
        {
            ['A'] = new Color(0.28f, 0.45f, 0.85f),
            ['B'] = new Color(0.05f, 0.08f, 0.15f),
            ['S'] = new Color(0.55f, 0.6f, 0.7f),
            ['W'] = new Color(0.85f, 0.9f, 1f),
            ['G'] = new Color(0.7f, 0.55f, 0.2f)
        };
        return CreatePatternSprite(new[]
        {
            "......AAA.......",
            ".....AAAAAA.....",
            ".....ABABAA.....",
            ".....AAAAAA.....",
            "S....AAAAAA..W..",
            "SS..AAAAAAAA.W..",
            "SS..AAAAAAAA.WW.",
            "SS..AAAAAAA.WW..",
            "S....AAAAAA.WW..",
            ".....AAAAAA.WW..",
            "......AAAA.WW...",
            ".....BB..BB.....",
            ".....BB..BB.....",
            ".....GG..GG.....",
            "................",
            "................"
        }, palette);
    }

    private static Sprite CreateEliteWarlockSprite()
    {
        var palette = new Dictionary<char, Color>
        {
            ['P'] = new Color(0.52f, 0.16f, 0.78f),
            ['B'] = new Color(0.9f, 0.8f, 0.2f),
            ['Y'] = new Color(0.35f, 0.9f, 0.9f)
        };
        return CreatePatternSprite(new[]
        {
            "..........YY....",
            ".........YYY....",
            "......PPPPP.....",
            ".....PPPPPPP....",
            ".....PBPBBP.....",
            ".....PPPPPPP....",
            "......PPPPP.....",
            ".....PPPPPPP....",
            "....PPPPPPPPP...",
            "....PPPPPPPPP...",
            "...PPPPPPPPPPP..",
            "...PPPPPPPPPPP..",
            "....PPPPPPPPP...",
            ".....PPPPPPP....",
            "......PPPPP.....",
            "................"
        }, palette);
    }

    private static Sprite CreateStoneGolemSprite()
    {
        var palette = new Dictionary<char, Color>
        {
            ['R'] = new Color(0.45f, 0.45f, 0.5f),
            ['D'] = new Color(0.25f, 0.25f, 0.28f),
            ['C'] = new Color(0.35f, 0.9f, 0.9f)
        };
        return CreatePatternSprite(new[]
        {
            "................",
            "...RRRRRRRRRR...",
            "..RRRRRRRRRRRR..",
            "..RRCCRRRRCCRR..",
            "..RRRRRRRRRRRR..",
            "RRRRRRRCCCRRRRRR",
            "RRRRRRCCCCCRRRRR",
            "RRRRRRRCCCRRRRRR",
            "..RRRRRRRRRRRR..",
            "..RRDRRRRRRDRR..",
            "..RRDRRRRRRDRR..",
            "...RRRRRRRRRR...",
            "...DDD....DDD...",
            "...DDD....DDD...",
            "................",
            "................"
        }, palette);
    }

    private static Sprite CreateShadowAssassinSprite()
    {
        var palette = new Dictionary<char, Color>
        {
            ['S'] = new Color(0.12f, 0.12f, 0.2f),
            ['P'] = new Color(0.75f, 0.25f, 0.85f),
            ['W'] = new Color(0.85f, 0.9f, 1f)
        };
        return CreatePatternSprite(new[]
        {
            "................",
            "......SSSS......",
            ".....SSSSSS.....",
            ".....SPPPPS.....",
            ".....SSSSSS.....",
            "W....SSSSSS...W.",
            "WW...SSSSSS..WW.",
            ".W...SSSSSS..W..",
            ".....SSSSSS.....",
            "......SSSS......",
            "......SSSS......",
            ".....SS..SS.....",
            ".....SS..SS.....",
            ".....WW..WW.....",
            "................",
            "................"
        }, palette);
    }

    private static Sprite CreateSlimeKingSprite()
    {
        var palette = new Dictionary<char, Color>
        {
            ['G'] = SlimeGreen,
            ['D'] = SlimeDark,
            ['H'] = SlimeHighlight,
            ['C'] = GoldTrim
        };
        return CreatePatternSprite(new[]
        {
            "......C.C.C.....",
            ".....CCCCCCC....",
            "................",
            ".....GGGGGG.....",
            "....GGGGGGGG....",
            "...GGGGGGGGGG...",
            "..GGGGGGGGGGGG..",
            "..GGBBGGGGBBGG..",
            "..GGBBGGGGBBGG..",
            "..GGGGDDDDGGGG..",
            ".HGGGGGGGGGGGGG.",
            ".HHGGGGGGGGGGGG.",
            "..GGGGGGGGGGGG..",
            "...GGGGGGGGGG...",
            "....DDDDDDDD....",
            "................"
        }, palette);
    }

    private static Sprite CreateDemonOverlordSprite()
    {
        var palette = new Dictionary<char, Color>
        {
            ['R'] = new Color(0.72f, 0.12f, 0.14f),
            ['D'] = new Color(0.18f, 0.05f, 0.08f),
            ['Y'] = new Color(0.95f, 0.8f, 0.15f),
            ['W'] = new Color(0.8f, 0.8f, 0.9f)
        };
        return CreatePatternSprite(new[]
        {
            "D...........D...",
            ".D.........D....",
            "..RRRRRRRRRR..W.",
            "..RRRRRRRRRR.WW.",
            "..RYRRYYRRYR.WW.",
            "...RRRRRRRR..W..",
            ".D..RRRRRR....D.",
            ".DD.RRRRRR..DD..",
            ".DDDRRRRRRDDD...",
            "..DDRRRRRRDD....",
            "....RRRRRR......",
            "....RR..RR......",
            "....RR..RR......",
            "...DDD..DDD.....",
            "................",
            "................"
        }, palette);
    }

    private static Sprite CreateCreatureSprite(Color primary, Color secondary, bool isBoss)
    {
        int w = 16, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] px = new Color[w * h];

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            Vector2 center = new Vector2(8f, 8f);
            Vector2 radius = isBoss ? new Vector2(7f, 7.5f) : new Vector2(6f, 6.5f);
            Vector2 offset = new Vector2(x - center.x, y - center.y);
            float ellipse = offset.x * offset.x / (radius.x * radius.x) + offset.y * offset.y / (radius.y * radius.y);
            if (ellipse <= 1f)
            {
                px[y * w + x] = y > 10 && Mathf.Abs(offset.x) < 3f ? secondary : primary;
            }
            else if (ellipse <= 1.15f)
            {
                px[y * w + x] = primary * 0.55f;
            }
        }

        void Set(int x, int y, Color c) { if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = c; }
        Set(6, 12, Color.white); Set(7, 12, Color.black);
        Set(9, 12, Color.white); Set(10, 12, Color.black);
        Set(7, 8, secondary); Set(8, 8, secondary);

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    private static Sprite CreateCoinSprite()
    {
        int w = 16, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(8, 8));
            if (distance <= 6.5f) px[y * w + x] = GoldTrim;
            if (distance <= 4.5f) px[y * w + x] = new Color(1f, 0.9f, 0.35f);
            if (Mathf.Abs(x - 8) < 2 && Mathf.Abs(y - 8) < 3) px[y * w + x] = new Color(0.85f, 0.65f, 0.1f);
        }

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    private static Sprite CreateItemSprite(Color primary, Color trim)
    {
        int w = 16, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int diamond = Mathf.Abs(x - 8) + Mathf.Abs(y - 8);
            if (diamond <= 6) px[y * w + x] = primary;
            if (diamond <= 3 && y > 8) px[y * w + x] = Color.Lerp(primary, Color.white, 0.35f);
            if (diamond == 6) px[y * w + x] = trim;
        }

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    // ===== 后备方块 =====
    private static Sprite CreateFallbackSprite()
    {
        int w = 16, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = Color.magenta;
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    private static Sprite CreateHealthBarSprite()
    {
        int w = 16, h = 4;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    private static Sprite CreateOrbitBladeSprite()
    {
        int w = 16, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int d = Mathf.Abs(x - 8) + Mathf.Abs(y - 8);
            if (d <= 2) px[y * w + x] = Color.white;
            else if (d <= 5) px[y * w + x] = new Color(0.55f, 0.8f, 1f);
            else if (d == 6) px[y * w + x] = new Color(0.3f, 0.5f, 0.9f, 0.8f);
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    private static Sprite CreateAuraRingSprite()
    {
        int w = 16, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
            if (d <= 7.6f && d >= 6.2f) px[y * w + x] = Color.white;
            else if (d < 6.2f) px[y * w + x] = new Color(1f, 1f, 1f, 0.10f);
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }
}
