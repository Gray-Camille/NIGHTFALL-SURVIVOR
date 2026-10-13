using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    [HideInInspector] public bool IsGameOver;
    private int kills;
    private float time;
    private bool _ready;
    private bool _inRun;

    void Awake() { if (Instance != null && Instance != this) Destroy(Instance.gameObject); Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    public void InitGame()
    {
        if (_ready) return;
        _ready = true;
        IsGameOver = false;
        kills = 0;
        time = 0;
        // 老版本的全局存档搬进 1 号槽，避免玩家资产凭空消失
        GamePersistence.MigrateLegacySaveIfNeeded();
        UIManager.Instance?.CloseShop();
        UIManager.Instance?.ResetRuneChoices();
        UIManager.Instance?.UpdateBuffUI(null);
        // 主菜单是独立场景：从菜单点进来的按选择开局，直接打开本场景则送回菜单
        if (MenuFlow.HasPending)
        {
            bool newGame;
            int slot;
            MenuFlow.Consume(out newGame, out slot);
            StartRun(newGame, slot);
        }
        else
        {
            SceneManager.LoadScene(MenuFlow.MenuSceneName);
        }
    }

    /// <summary>开始一局：newGame = true 时覆盖对应存档槽，否则读取该槽存档继续。</summary>
    public void StartRun(bool newGame, int slot)
    {
        if (!_ready) return;

        GamePersistence.SelectSlot(slot);
        if (newGame) GamePersistence.CreateOrOverwrite(slot);

        UIManager.Instance?.HideAllMenus();
        UIManager.Instance?.ResetRuneChoices();
        IsGameOver = false;
        kills = 0;
        time = 0;
        _inRun = true;
        Time.timeScale = 1f;
        UIManager.Instance?.UpdateKillUI(0);
        UIManager.Instance?.UpdateWaveUI(1);
        UIManager.Instance?.UpdateDefenseUI(0);
        UIManager.Instance?.UpdateBuffUI(null);
        SpawnPlayer();
        SpawnEnemySpawner();
    }

    /// <summary>回到主菜单：saveRun = true 时先把金币与背包写回存档，然后切到主菜单场景。</summary>
    public void ReturnToMenu(bool saveRun)
    {
        if (saveRun && _inRun) GamePersistence.Save(PlayerController.Instance);
        ClearRun();
        IsGameOver = false;
        _inRun = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(MenuFlow.MenuSceneName);
    }

    /// <summary>清掉一局里的全部实体，回到「还没开始」的干净状态。</summary>
    private void ClearRun()
    {
        foreach (var e in FindObjectsOfType<Enemy>()) Destroy(e.gameObject);
        foreach (var xp in FindObjectsOfType<XPOrb>()) Destroy(xp.gameObject);
        foreach (var pickup in FindObjectsOfType<PickupDrop>()) Destroy(pickup.gameObject);
        foreach (var p in FindObjectsOfType<Projectile>()) Destroy(p.gameObject);
        foreach (var p in FindObjectsOfType<EnemyProjectile>()) Destroy(p.gameObject);
        var sp = FindObjectOfType<EnemySpawner>();
        if (sp) Destroy(sp.gameObject);
        var pl = PlayerController.Instance;
        if (pl) Destroy(pl.gameObject);
        MapManager.Instance?.UpdateForWave(1);
    }

    void Update()
    {
        if (!_ready) return;
        if (_inRun && !IsGameOver) time += Time.deltaTime;

        // Esc：先逐层关闭模态面板（暂停 / 背包 / 商店 / 符文）；局内没有面板可关时开关暂停菜单
        if (Input.GetKeyDown(KeyCode.Escape) && UIManager.Instance != null)
        {
            if (UIManager.Instance.CloseTopModal()) return;
            if (_inRun && !IsGameOver) { UIManager.Instance.TogglePauseMenu(); return; }
        }

        if (!_inRun) return;
        // 局内随时可以重开本局 / 返回主菜单，不必等到死亡
        if (Input.GetKeyDown(KeyCode.R)) RestartGame();
        if (Input.GetKeyDown(KeyCode.M)) ReturnToMenu(true);
    }

    void SpawnPlayer()
    {
        var go = new GameObject("Player");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PixelSpriteGenerator.GetSprite("hero");
        sr.sortingOrder = 10;
        go.transform.localScale = Vector3.one * 2f;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0; rb.drag = 8f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.35f;
        var pc = go.AddComponent<PlayerController>();
        go.AddComponent<RuneAttacks>();
    }

    public void SpawnEnemy(int wave = 1)
    {
        Enemy.EnemyType type = Enemy.EnemyType.Slime;
        if (wave >= 3 && Random.value < 0.25f) type = Enemy.EnemyType.Brute;
        if (wave >= 4 && Random.value < 0.2f) type = Enemy.EnemyType.Shooter;
        if (wave >= 4 && type != Enemy.EnemyType.Shooter && Random.value < 0.15f) type = Enemy.EnemyType.Skeleton;
        if (wave >= 5 && Random.value < 0.2f) type = Enemy.EnemyType.Crawler;
        if (wave >= 2 && type == Enemy.EnemyType.Slime && Random.value < 0.35f) type = Enemy.EnemyType.Bat;

        var e = CreateEnemy(type, Enemy.EnemyCategory.Normal);
        if (type == Enemy.EnemyType.Slime) { e.maxHealth = 30; e.moveSpeed = 2.5f; e.contactDamage = 10; e.xpReward = 5; }
        else if (type == Enemy.EnemyType.Bat) { e.maxHealth = 18; e.moveSpeed = 4.2f; e.contactDamage = 6; e.xpReward = 7; }
        else if (type == Enemy.EnemyType.Brute) { e.maxHealth = 80; e.moveSpeed = 1.4f; e.contactDamage = 18; e.xpReward = 15; }
        else if (type == Enemy.EnemyType.Shooter) { e.maxHealth = 34; e.moveSpeed = 2.2f; e.contactDamage = 8; e.xpReward = 12; e.attackCooldown = 1.6f; }
        else if (type == Enemy.EnemyType.Skeleton) { e.maxHealth = 45; e.moveSpeed = 3f; e.contactDamage = 12; e.xpReward = 9; }
        else { e.maxHealth = 55; e.moveSpeed = 2.8f; e.contactDamage = 14; e.xpReward = 11; }

        float s = GetDifficultyScale() * GetWaveScale(wave);
        ApplyDifficulty(e, s, wave);
        SpawnAtRandomEdge(e.gameObject);
    }

    public void SpawnElite(int wave)
    {
        var types = new[] { Enemy.EnemyType.EliteKnight, Enemy.EnemyType.EliteWarlock, Enemy.EnemyType.StoneGolem, Enemy.EnemyType.ShadowAssassin };
        var type = types[Random.Range(0, types.Length)];
        var e = CreateEnemy(type, Enemy.EnemyCategory.Elite);
        switch (type)
        {
            case Enemy.EnemyType.EliteKnight:
                e.maxHealth = 240; e.moveSpeed = 2.4f; e.contactDamage = 24; e.xpReward = 55; e.attackCooldown = 1.2f;
                break;
            case Enemy.EnemyType.EliteWarlock:
                e.maxHealth = 190; e.moveSpeed = 2.1f; e.contactDamage = 16; e.xpReward = 60; e.attackCooldown = 1.4f;
                break;
            case Enemy.EnemyType.StoneGolem:
                e.maxHealth = 360; e.moveSpeed = 1.1f; e.contactDamage = 30; e.xpReward = 65;
                break;
            default:
                e.maxHealth = 170; e.moveSpeed = 5.2f; e.contactDamage = 20; e.xpReward = 50;
                break;
        }

        float eliteScale = 1f + Mathf.Max(0, wave - 6) * 0.04f;
        ApplyDifficulty(e, GetDifficultyScale() * eliteScale, wave);
        SpawnAtRandomEdge(e.gameObject);
        UIManager.Instance?.ShowAnnouncement("精英怪出现！", new Color(0.9f, 0.7f, 0.15f));
    }

    public void SpawnBoss(int wave)
    {
        var type = (wave / 15) % 2 == 0 ? Enemy.EnemyType.SlimeKing : Enemy.EnemyType.DemonOverlord;
        var e = CreateEnemy(type, Enemy.EnemyCategory.Boss);
        if (type == Enemy.EnemyType.SlimeKing)
        {
            e.maxHealth = 900; e.moveSpeed = 1.6f; e.contactDamage = 35; e.xpReward = 180; e.attackCooldown = 1.8f;
        }
        else
        {
            e.maxHealth = 1100; e.moveSpeed = 1.8f; e.contactDamage = 28; e.xpReward = 220; e.attackCooldown = 1.5f;
        }

        float bossScale = 1f + Mathf.Max(0, wave - 15) * 0.05f;
        ApplyDifficulty(e, GetDifficultyScale() * bossScale, wave);
        SpawnAtRandomEdge(e.gameObject);
        UIManager.Instance?.ShowAnnouncement("BOSS 出现！", Color.red);
    }

    private Enemy CreateEnemy(Enemy.EnemyType type, Enemy.EnemyCategory category)
    {
        var go = new GameObject(type.ToString());
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PixelSpriteGenerator.GetEnemySprite(type);
        sr.sortingOrder = 5;
        go.transform.localScale = Vector3.one * 2f;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0; rb.drag = 6f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = type == Enemy.EnemyType.Brute || type == Enemy.EnemyType.StoneGolem ? 0.5f : 0.35f;
        var e = go.AddComponent<Enemy>();
        e.type = type;
        e.category = category;
        e.armor = category == Enemy.EnemyCategory.Boss ? 12 : category == Enemy.EnemyCategory.Elite ? 5 : 0;
        return e;
    }

    private void ApplyDifficulty(Enemy enemy, float scale, int wave)
    {
        enemy.maxHealth = Mathf.RoundToInt(enemy.maxHealth * scale);
        enemy.currentHealth = enemy.maxHealth;
        enemy.moveSpeed *= 1f + (scale - 1f) * 0.15f;
        enemy.contactDamage = Mathf.Max(1, Mathf.RoundToInt(enemy.contactDamage * Mathf.Sqrt(scale)));
        enemy.armor += Mathf.FloorToInt(Mathf.Max(0, wave - 1) / 4f);
    }

    private float GetWaveScale(int wave)
    {
        return 1f + Mathf.Max(0, wave - 1) * 0.03f;
    }

    private void SpawnAtRandomEdge(GameObject enemy)
    {
        Vector2 playerPos = PlayerController.Instance != null ?
            (Vector2)PlayerController.Instance.transform.position : Vector2.zero;
        float angle = Random.Range(0f, Mathf.PI * 2f);
        enemy.transform.position = playerPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 12f;
    }

    public void SpawnProjectile(Vector3 pos, Transform target, int damage)
    {
        var go = new GameObject("Projectile");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PixelSpriteGenerator.GetSprite("projectile");
        sr.sortingOrder = 8;
        go.transform.localScale = Vector3.one * 1.5f;
        go.transform.position = pos;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0; rb.drag = 0;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.15f; col.isTrigger = true;
        var p = go.AddComponent<Projectile>();
        p.SetTarget(target);
        p.damage = damage;
        p.speed = 12f;
        Destroy(go, 3f);
    }

    public void SpawnProjectileDirection(Vector3 pos, Vector2 direction, int damage)
    {
        var go = new GameObject("Projectile");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PixelSpriteGenerator.GetSprite("projectile");
        sr.sortingOrder = 8;
        go.transform.localScale = Vector3.one * 1.5f;
        go.transform.position = pos;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0; rb.drag = 0;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.15f; col.isTrigger = true;
        var p = go.AddComponent<Projectile>();
        p.SetDirection(direction);
        p.damage = damage;
        p.speed = 12f;
        Destroy(go, 3f);
    }

    public void SpawnEnemyProjectile(Vector3 pos, Vector3 target, int damage)
    {
        var go = new GameObject("EnemyProjectile");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PixelSpriteGenerator.GetSprite("enemy_projectile");
        sr.sortingOrder = 8;
        go.transform.localScale = Vector3.one * 1.3f;
        go.transform.position = pos;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.15f; col.isTrigger = true;
        var p = go.AddComponent<EnemyProjectile>();
        p.damage = damage;
        p.Launch(target - pos);
        Destroy(go, 5f);
    }

    public void SpawnRadialEnemyProjectile(Vector3 pos, Vector2 direction, int damage, float speed)
    {
        var go = new GameObject("EnemyProjectile");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PixelSpriteGenerator.GetSprite("enemy_projectile");
        sr.sortingOrder = 8;
        go.transform.localScale = Vector3.one * 1.3f;
        go.transform.position = pos;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.15f; col.isTrigger = true;
        var p = go.AddComponent<EnemyProjectile>();
        p.damage = damage;
        p.speed = speed;
        p.Launch(direction);
        Destroy(go, 5f);
    }

    public void SpawnXP(Vector3 pos, int amount)
    {
        var go = new GameObject("XPOrb");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PixelSpriteGenerator.GetSprite("xp_orb");
        sr.sortingOrder = 7;
        go.transform.localScale = Vector3.one * 1.5f;
        go.transform.position = pos;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0; rb.drag = 2f;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.2f; col.isTrigger = true;
        var xp = go.AddComponent<XPOrb>();
        xp.xpAmount = amount;
        Destroy(go, 15f);
    }

    public void OnEnemyDied(Enemy enemy)
    {
        SpawnXP(enemy.transform.position, enemy.xpReward);
        if (enemy.category == Enemy.EnemyCategory.Normal)
        {
            // 小怪：掉金币 + 小概率掉限时道具
            if (Random.value < 0.45f) SpawnGold(enemy.transform.position, Random.Range(2, 4) + GetWave() / 8);
            if (Random.value < 0.12f) SpawnItem(enemy.transform.position, GetRandomItemKind());
        }
        else if (enemy.category == Enemy.EnemyCategory.Elite)
        {
            SpawnGold(enemy.transform.position, Random.Range(6, 11) + GetWave() / 5);
            SpawnItem(enemy.transform.position, GetRandomItemKind());
            HealPlayerPercent(0.25f, "击杀精英怪：恢复 25% 生命");
        }
        else
        {
            SpawnGold(enemy.transform.position, Random.Range(15, 24) + GetWave() / 3);
            SpawnItem(enemy.transform.position, ItemKind.HealthPotion);
            SpawnItem(enemy.transform.position, GetRandomItemKind());
            SpawnItem(enemy.transform.position, GetRandomItemKind());
            HealPlayerPercent(0.5f, "击杀 BOSS：恢复 50% 生命");
        }

        AddKill();
    }

    /// <summary>击杀精英怪 / BOSS 恢复一定百分比的最大生命。</summary>
    private void HealPlayerPercent(float percent, string message)
    {
        var player = PlayerController.Instance;
        if (player == null) return;
        player.Heal(Mathf.RoundToInt(player.maxHealth * percent));
        UIManager.Instance?.ShowAnnouncement(message, new Color(0.4f, 1f, 0.5f));
    }

    private ItemKind GetRandomItemKind()
    {
        var kinds = new[] { ItemKind.RagePotion, ItemKind.HastePotion, ItemKind.IronShield, ItemKind.MagnetCharm, ItemKind.HealthPotion };
        return kinds[Random.Range(0, kinds.Length)];
    }

    public void SpawnGold(Vector3 pos, int amount)
    {
        amount = Mathf.Max(1, amount);
        var go = new GameObject("GoldCoin");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PixelSpriteGenerator.GetSprite("gold_coin");
        sr.sortingOrder = 7;
        go.transform.localScale = Vector3.one * 1.3f;
        go.transform.position = pos;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0; rb.drag = 2f;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.2f; col.isTrigger = true;
        var pickup = go.AddComponent<PickupDrop>();
        pickup.kind = ItemKind.GoldCoin;
        pickup.amount = amount;
        Destroy(go, 20f);
    }

    public void SpawnItem(Vector3 pos, ItemKind kind)
    {
        var go = new GameObject(kind.ToString());
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PixelSpriteGenerator.GetSprite(GetItemSpriteName(kind));
        sr.sortingOrder = 7;
        go.transform.localScale = Vector3.one * 1.8f;
        go.transform.position = pos;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0; rb.drag = 2f;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.28f; col.isTrigger = true;
        var pickup = go.AddComponent<PickupDrop>();
        pickup.kind = kind;
        Destroy(go, 20f);
    }

    private static string GetItemSpriteName(ItemKind kind)
    {
        switch (kind)
        {
            case ItemKind.HealthPotion: return "health_potion";
            case ItemKind.AttackPotion: return "attack_potion";
            case ItemKind.RagePotion: return "rage_potion";
            case ItemKind.HastePotion: return "haste_potion";
            case ItemKind.IronShield: return "iron_shield";
            case ItemKind.MagnetCharm: return "magnet_charm";
            default: return "gold_coin";
        }
    }

    void SpawnEnemySpawner()
    {
        var go = new GameObject("EnemySpawner");
        go.AddComponent<EnemySpawner>();
    }

    public void AddKill() { kills++; UIManager.Instance?.UpdateKillUI(kills); }

    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        // 死亡时把金币与背包写回存档，下一局继续使用
        GamePersistence.Save(PlayerController.Instance);
        UIManager.Instance?.ShowGameOver(kills, time);
    }

    void OnApplicationQuit()
    {
        if (_inRun) GamePersistence.Save(PlayerController.Instance);
    }

    void OnApplicationPause(bool paused)
    {
        if (paused && _inRun) GamePersistence.Save(PlayerController.Instance);
    }

    public void RestartGame()
    {
        if (!_inRun) return;
        IsGameOver = false; kills = 0; time = 0;
        Time.timeScale = 1f;
        UIManager.Instance?.HideAllMenus();
        UIManager.Instance?.ResetRuneChoices();
        UIManager.Instance?.UpdateXPUI(0, 20, 1);
        UIManager.Instance?.UpdateHealthUI(100, 100);
        UIManager.Instance?.UpdateKillUI(0);
        UIManager.Instance?.UpdateWaveUI(1);
        UIManager.Instance?.UpdateDefenseUI(0);
        UIManager.Instance?.UpdateBuffUI(null);
        ClearRun();
        SpawnPlayer(); SpawnEnemySpawner();
    }

    public float GetDifficultyScale() => 1f + (time / 60f) * 0.35f;
    public float GetGameTime() => time;
    public int GetKillCount() => kills;
    public int GetWave() { var spawner = FindObjectOfType<EnemySpawner>(); return spawner != null ? spawner.GetWave() : 1; }
}
