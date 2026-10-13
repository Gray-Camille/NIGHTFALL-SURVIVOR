using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }
    [Header("Movement")] public float moveSpeed = 5f;
    [Header("Attack")] public float attackRange = 8f;
    public float attackCooldown = 0.4f;
    [Header("Health")] public int maxHealth = 100;
    public int currentHealth;
    [Header("XP")] public int currentXP = 0;
    public int xpToNextLevel = 20;
    public int level = 1;

    private float _attackTimer;
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Enemy _nearestEnemy;
    private float _invincibleTimer;
    private const float InvincibleDuration = 0.3f;
    private const float FloorBound = 39.5f;
    public float damageMultiplier = 1f;
    public float attackSpeedMultiplier = 1f;
    [Header("Defense")] public int armor;
    [Header("Economy")] public int currentGold;
    public int healthPotionCount;
    public int attackPotionCount;
    public int ragePotionCount;
    public int hastePotionCount;
    public int ironShieldCount;
    public int magnetCharmCount;

    [Header("Rune Stats")]
    public int multishotCount;
    public int orbitCount;
    public int chainLevel;
    public int auraLevel;
    public float regenPerSecond;
    public float xpPerSecond;
    public int revivesLeft;

    [Header("Rune Progress")]
    public int runesTaken;
    public int pendingRuneChoices;
    public int levelsPerRune = 3;
    private int _nextRuneLevel = 3;
    private float _regenTimer;
    private float _xpFlowTimer;

    [Header("Level Growth")]
    public float moveSpeedPerLevel = 0.16f;   // 每次升级提升的移动速度
    public int moveSpeedBonusMaxLevel = 20;   // 超过该等级后，升级不再提升移动速度

    [Header("Rune: Invincibility")]
    public bool invincibleRuneTaken;
    public float invincibleFirstDuration = RuneSystem.InvincibleFirstDuration;
    public float invinciblePulseInterval = RuneSystem.InvinciblePulseInterval;
    public float invinciblePulseDuration = RuneSystem.InvinciblePulseDuration;
    private float _invinciblePulseTimer;

    [Header("Rune: Attack Type Damage Scaling")]
    public float scatterDamageBonus;
    public float orbitDamageBonus;
    public float chainDamageBonus;
    public float auraDamageBonus;
    private readonly Dictionary<RuneId, int> _runeStacks = new Dictionary<RuneId, int>();

    // ===== 限时道具（掉落获得，可跨局保存，效果有持续时间）=====
    public const float RageDuration = 15f;
    public const float HasteDuration = 12f;
    public const float IronShieldDuration = 20f;
    public const float MagnetDuration = 12f;
    public const float RageAttackBonus = 0.5f;
    public const float HasteMoveBonus = 0.4f;
    public const int IronShieldArmorBonus = 8;
    public const float MagnetRangeMultiplier = 3f;

    [Header("Timed Item Buffs")]
    public float rageTimer;
    public float hasteTimer;
    public float ironShieldTimer;
    public float magnetTimer;
    private int _lastDisplayedArmor;

    /// <summary>狂暴药剂生效时的攻击力倍率。</summary>
    public float ItemAttackMultiplier { get { return rageTimer > 0f ? 1f + RageAttackBonus : 1f; } }
    /// <summary>迅捷药剂生效时的移动速度倍率。</summary>
    public float ItemMoveMultiplier { get { return hasteTimer > 0f ? 1f + HasteMoveBonus : 1f; } }
    /// <summary>磁力护符生效时的拾取范围倍率。</summary>
    public float PickupRangeMultiplier { get { return magnetTimer > 0f ? MagnetRangeMultiplier : 1f; } }
    /// <summary>含铁壁护盾加成的实际防御力。</summary>
    public int EffectiveArmor { get { return armor + (ironShieldTimer > 0f ? IronShieldArmorBonus : 0); } }

    void Awake()
    {
        Instance = this;
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
    }
    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        _sr.sprite = PixelSpriteGenerator.GetSprite("hero");
        _sr.sortingOrder = 10;
        transform.localScale = Vector3.one * 2f;
        LoadPersistentProgress();
        UIManager.Instance?.UpdateHealthUI(currentHealth, maxHealth);
        UIManager.Instance?.UpdateDefenseUI(EffectiveArmor);
        UIManager.Instance?.UpdateXPUI(currentXP, xpToNextLevel, level);
        UIManager.Instance?.UpdateGoldUI(currentGold);
        UIManager.Instance?.UpdateShopUI();
        UIManager.Instance?.UpdateBuffUI(this);

        // 开局赠送一次符文 3 选 1
        pendingRuneChoices = Mathf.Max(pendingRuneChoices, 1);
        UIManager.Instance?.TryShowRuneChoice();
    }

    /// <summary>读取上一局保存下来的金币与道具背包（等级/符文/限时效果等局内成长不保存）。</summary>
    private void LoadPersistentProgress()
    {
        currentGold = GamePersistence.BankGold;
        healthPotionCount = GamePersistence.GetItemCount(ItemKind.HealthPotion);
        attackPotionCount = GamePersistence.GetItemCount(ItemKind.AttackPotion);
        ragePotionCount = GamePersistence.GetItemCount(ItemKind.RagePotion);
        hastePotionCount = GamePersistence.GetItemCount(ItemKind.HastePotion);
        ironShieldCount = GamePersistence.GetItemCount(ItemKind.IronShield);
        magnetCharmCount = GamePersistence.GetItemCount(ItemKind.MagnetCharm);
        _lastDisplayedArmor = armor;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) { _rb.velocity = Vector2.zero; return; }

        // 打开任意模态面板（商店 / 背包 / 符文）时，角色与怪物都会停止移动与攻击
        if (Input.GetKeyDown(KeyCode.B)) UIManager.Instance?.ToggleShop();
        if (Input.GetKeyDown(KeyCode.I)) UIManager.Instance?.ToggleInventory();

        // 使用道具的按键在暂停时也生效：打开背包就能直接嗑药，并立刻看到数量减少
        if (Input.GetKeyDown(KeyCode.Alpha1)) UseHealthPotion();
        if (Input.GetKeyDown(KeyCode.Alpha2)) UseAttackPotion();
        if (Input.GetKeyDown(KeyCode.Alpha3)) UseItem(ItemKind.RagePotion);
        if (Input.GetKeyDown(KeyCode.Alpha4)) UseItem(ItemKind.HastePotion);
        if (Input.GetKeyDown(KeyCode.Alpha5)) UseItem(ItemKind.IronShield);
        if (Input.GetKeyDown(KeyCode.Alpha6)) UseItem(ItemKind.MagnetCharm);

        if (UIManager.Instance != null && UIManager.Instance.IsModalOpen)
        {
            _rb.velocity = Vector2.zero;
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        _rb.velocity = new Vector2(h, v).normalized * moveSpeed * ItemMoveMultiplier;
        // Face movement direction
        if (h < 0) _sr.flipX = true;
        else if (h > 0) _sr.flipX = false;
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, -FloorBound, FloorBound);
        p.y = Mathf.Clamp(p.y, -FloorBound, FloorBound);
        transform.position = p;
        FindEnemy();
        AutoAttack();
        TickRuneStats();
        TickItemTimers();
        UpdateInvincible();
    }

    void UpdateInvincible()
    {
        if (_invincibleTimer <= 0f)
        {
            _invincibleTimer = 0f;
            _sr.color = Color.white;
            _sr.enabled = true;
            return;
        }

        _invincibleTimer -= Time.deltaTime;
        if (_invincibleTimer > 0.4f)
        {
            // 无敌符文给的是长无敌：保持可见，用青蓝色呼吸脉冲提示
            _sr.enabled = true;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 18f);
            _sr.color = Color.Lerp(Color.white, new Color(0.55f, 0.95f, 1f), pulse);
        }
        else
        {
            // 受击后的短无敌：沿用闪烁提示
            _sr.color = Color.white;
            _sr.enabled = Mathf.FloorToInt(_invincibleTimer * 20) % 2 == 0;
        }
    }

    public bool IsInvincible { get { return _invincibleTimer > 0f; } }

    /// <summary>只延长无敌时间，不会因为连续触发而缩短剩余时长。</summary>
    public void GrantInvincibility(float duration)
    {
        if (duration > _invincibleTimer) _invincibleTimer = duration;
    }

    public void ResetInvinciblePulseTimer()
    {
        _invinciblePulseTimer = invinciblePulseInterval;
    }

    public int GetRuneStacks(RuneId id)
    {
        int value;
        return _runeStacks.TryGetValue(id, out value) ? value : 0;
    }

    public void AddRuneStack(RuneId id)
    {
        _runeStacks[id] = GetRuneStacks(id) + 1;
    }

    void TickRuneStats()
    {
        if (regenPerSecond > 0f)
        {
            _regenTimer += Time.deltaTime;
            if (_regenTimer >= 0.5f)
            {
                _regenTimer -= 0.5f;
                Heal(Mathf.Max(1, Mathf.RoundToInt(regenPerSecond * 0.5f)));
            }
        }
        if (xpPerSecond > 0f)
        {
            _xpFlowTimer += Time.deltaTime;
            if (_xpFlowTimer >= 1f)
            {
                _xpFlowTimer -= 1f;
                AddXP(Mathf.Max(1, Mathf.RoundToInt(xpPerSecond)));
            }
        }
        if (invincibleRuneTaken)
        {
            // 无敌符文：每隔 invinciblePulseInterval 秒获得一段 invinciblePulseDuration 秒无敌
            _invinciblePulseTimer -= Time.deltaTime;
            if (_invinciblePulseTimer <= 0f)
            {
                _invinciblePulseTimer = invinciblePulseInterval;
                GrantInvincibility(invinciblePulseDuration);
            }
        }
    }

    void FindEnemy()
    {
        _nearestEnemy = null; float nd = float.MaxValue;
        foreach (var e in FindObjectsOfType<Enemy>())
        {
            float d = Vector2.Distance(transform.position, e.transform.position);
            if (d < nd && d <= attackRange) { nd = d; _nearestEnemy = e; }
        }
    }

    void AutoAttack()
    {
        _attackTimer -= Time.deltaTime;
        if (_attackTimer <= 0 && _nearestEnemy != null)
        {
            _attackTimer = attackCooldown / attackSpeedMultiplier;
            int damage = Mathf.RoundToInt(10 * damageMultiplier * (1f + scatterDamageBonus) * ItemAttackMultiplier);
            Vector2 baseDir = ((Vector2)_nearestEnemy.transform.position - (Vector2)transform.position).normalized;
            GameManager.Instance.SpawnProjectile(transform.position, _nearestEnemy.transform, damage);
            for (int i = 1; i <= multishotCount; i++)
            {
                float spread = 12f * Mathf.Ceil(i / 2f) * (i % 2 == 1 ? 1f : -1f);
                Vector2 dir = Rotate(baseDir, spread);
                GameManager.Instance.SpawnProjectileDirection(transform.position, dir, damage);
            }
        }
    }

    private static Vector2 Rotate(Vector2 direction, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians); float sin = Mathf.Sin(radians);
        return new Vector2(direction.x * cos - direction.y * sin, direction.x * sin + direction.y * cos);
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UIManager.Instance?.UpdateHealthUI(currentHealth, maxHealth);
    }

    public void TakeDamage(int dmg)
    {
        if (_invincibleTimer > 0) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        int reducedDamage = Mathf.Max(1, dmg - EffectiveArmor);
        currentHealth -= reducedDamage; _invincibleTimer = InvincibleDuration;
        if (currentHealth <= 0)
        {
            if (revivesLeft > 0)
            {
                revivesLeft--;
                currentHealth = maxHealth;
                _invincibleTimer = 2f;
                UIManager.Instance?.ShowAnnouncement("不屈符文触发：原地复活！", new Color(1f, 0.85f, 0.3f));
            }
            else
            {
                currentHealth = 0;
                GameManager.Instance?.GameOver();
            }
        }
        else
        {
            StartCoroutine(Shake());
        }
        UIManager.Instance?.UpdateHealthUI(currentHealth, maxHealth);
        UIManager.Instance?.UpdateXPUI(currentXP, xpToNextLevel, level);
    }

    System.Collections.IEnumerator Shake()
    {
        Vector3 o = transform.localPosition;
        for (float t = 0; t < 0.15f; t += Time.deltaTime) { transform.localPosition = o + (Vector3)(Random.insideUnitCircle * 0.15f); yield return null; }
        transform.localPosition = o;
    }

    public void AddXP(int amt)
    {
        currentXP += amt;
        UIManager.Instance?.UpdateXPUI(currentXP, xpToNextLevel, level);
        while (currentXP >= xpToNextLevel) { currentXP -= xpToNextLevel; LevelUp(); }
    }

    void LevelUp()
    {
        level++; xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * 1.3f);
        damageMultiplier += 0.18f;
        armor += 1;
        // 升级提升少量移动速度，局内永久；到达等级上限后不再提升
        // （重开时 PlayerController 会被重建，moveSpeed 自动回到初始值）
        if (level <= moveSpeedBonusMaxLevel) moveSpeed += moveSpeedPerLevel;
        int healedHealth = Mathf.RoundToInt(maxHealth * 0.2f);
        currentHealth = Mathf.Min(currentHealth + healedHealth, maxHealth);
        int c = Random.Range(0, 3);
        if (c == 0) damageMultiplier += 0.35f;
        else if (c == 1) attackSpeedMultiplier += 0.25f;
        else { maxHealth += 20; currentHealth = Mathf.Min(currentHealth + 20, maxHealth); }
        UIManager.Instance?.UpdateHealthUI(currentHealth, maxHealth);
        UIManager.Instance?.UpdateDefenseUI(EffectiveArmor);
        UIManager.Instance?.ShowLevelUp(level);
        UIManager.Instance?.UpdateXPUI(currentXP, xpToNextLevel, level);

        while (level >= _nextRuneLevel)
        {
            _nextRuneLevel += Mathf.Max(1, levelsPerRune);
            pendingRuneChoices++;
        }
        if (pendingRuneChoices > 0) UIManager.Instance?.TryShowRuneChoice();
    }

    public void AddGold(int amount)
    {
        currentGold += amount;
        // 金币和道具一样立刻写回当前存档槽，保证「本存档内的金币」始终等于存档里的数值
        GamePersistence.SetBankGold(currentGold);
        UIManager.Instance?.UpdateGoldUI(currentGold);
    }

    /// <summary>拾取到的道具不会立即生效，而是进入背包，由玩家按键使用（背包跨局保存）。</summary>
    public void AddItem(ItemKind kind)
    {
        if (kind == ItemKind.GoldCoin) return;

        // 每种道具有各自的背包上限，满了就不再收，避免数量突破格子容量
        int max = ItemCatalog.MaxStack(kind);
        int current = GetItemCount(kind);
        if (current >= max)
        {
            UIManager.Instance?.ShowAnnouncement(ItemCatalog.DisplayName(kind) + " 已达上限 " + max, new Color(0.95f, 0.6f, 0.25f));
            return;
        }

        SetItemCount(kind, current + 1);
        GamePersistence.SetItemCount(kind, GetItemCount(kind));
        UIManager.Instance?.UpdateShopUI();
        UIManager.Instance?.UpdateInventoryUI(this);
    }

    public int GetItemCount(ItemKind kind)
    {
        switch (kind)
        {
            case ItemKind.HealthPotion: return healthPotionCount;
            case ItemKind.AttackPotion: return attackPotionCount;
            case ItemKind.RagePotion: return ragePotionCount;
            case ItemKind.HastePotion: return hastePotionCount;
            case ItemKind.IronShield: return ironShieldCount;
            case ItemKind.MagnetCharm: return magnetCharmCount;
            default: return 0;
        }
    }

    public void SetItemCount(ItemKind kind, int value)
    {
        value = Mathf.Max(0, value);
        switch (kind)
        {
            case ItemKind.HealthPotion: healthPotionCount = value; break;
            case ItemKind.AttackPotion: attackPotionCount = value; break;
            case ItemKind.RagePotion: ragePotionCount = value; break;
            case ItemKind.HastePotion: hastePotionCount = value; break;
            case ItemKind.IronShield: ironShieldCount = value; break;
            case ItemKind.MagnetCharm: magnetCharmCount = value; break;
        }
    }

    /// <summary>使用限时道具：效果只在本次局内持续一段时间，剩余道具数量会保存到下一局。</summary>
    public void UseItem(ItemKind kind)
    {
        if (GetItemCount(kind) <= 0) return;
        string label;
        Color color;
        switch (kind)
        {
            case ItemKind.RagePotion:
                rageTimer = RageDuration; label = "狂暴药剂：攻击 +50%（15 秒）"; color = new Color(1f, 0.45f, 0.2f); break;
            case ItemKind.HastePotion:
                hasteTimer = HasteDuration; label = "迅捷药剂：移速 +40%（12 秒）"; color = new Color(0.3f, 1f, 0.9f); break;
            case ItemKind.IronShield:
                ironShieldTimer = IronShieldDuration; label = "铁壁护盾：防御 +8（20 秒）"; color = new Color(0.7f, 0.8f, 1f); break;
            case ItemKind.MagnetCharm:
                magnetTimer = MagnetDuration; label = "磁力护符：拾取范围 ×3（12 秒）"; color = new Color(0.8f, 0.5f, 1f); break;
            default:
                return;
        }
        SetItemCount(kind, GetItemCount(kind) - 1);
        GamePersistence.SetItemCount(kind, GetItemCount(kind));
        UIManager.Instance?.UpdateShopUI();
        UIManager.Instance?.UpdateInventoryUI(this);
        UIManager.Instance?.ShowAnnouncement(label, color);
    }

    /// <summary>当前生效的限时效果摘要，用于 HUD 显示剩余时间。</summary>
    public string GetBuffSummary()
    {
        var sb = new System.Text.StringBuilder();
        if (rageTimer > 0f) sb.AppendFormat("狂暴 {0:F1}s    ", rageTimer);
        if (hasteTimer > 0f) sb.AppendFormat("迅捷 {0:F1}s    ", hasteTimer);
        if (ironShieldTimer > 0f) sb.AppendFormat("铁壁 {0:F1}s    ", ironShieldTimer);
        if (magnetTimer > 0f) sb.AppendFormat("磁力 {0:F1}s    ", magnetTimer);
        return sb.ToString();
    }

    void TickItemTimers()
    {
        if (rageTimer > 0f) rageTimer = Mathf.Max(0f, rageTimer - Time.deltaTime);
        if (hasteTimer > 0f) hasteTimer = Mathf.Max(0f, hasteTimer - Time.deltaTime);
        if (ironShieldTimer > 0f) ironShieldTimer = Mathf.Max(0f, ironShieldTimer - Time.deltaTime);
        if (magnetTimer > 0f) magnetTimer = Mathf.Max(0f, magnetTimer - Time.deltaTime);
        UIManager.Instance?.UpdateBuffUI(this);
        if (_lastDisplayedArmor != EffectiveArmor)
        {
            _lastDisplayedArmor = EffectiveArmor;
            UIManager.Instance?.UpdateDefenseUI(EffectiveArmor);
        }
    }

    public bool PurchaseHealthPotion()
    {
        const int cost = 12;
        if (currentGold < cost) return false;
        currentGold -= cost;
        healthPotionCount++;
        GamePersistence.SetBankGold(currentGold);
        GamePersistence.SetItemCount(ItemKind.HealthPotion, healthPotionCount);
        UIManager.Instance?.UpdateGoldUI(currentGold);
        UIManager.Instance?.UpdateShopUI();
        UIManager.Instance?.ShowAnnouncement("购买血瓶成功", Color.green);
        return true;
    }

    public bool PurchaseAttackPotion()
    {
        const int cost = 30;
        if (currentGold < cost) return false;
        currentGold -= cost;
        attackPotionCount++;
        GamePersistence.SetBankGold(currentGold);
        GamePersistence.SetItemCount(ItemKind.AttackPotion, attackPotionCount);
        UIManager.Instance?.UpdateGoldUI(currentGold);
        UIManager.Instance?.UpdateShopUI();
        UIManager.Instance?.ShowAnnouncement("购买攻击药剂成功", new Color(0.9f, 0.7f, 0.2f));
        return true;
    }

    public void UseHealthPotion()
    {
        if (healthPotionCount <= 0 || currentHealth >= maxHealth) return;
        healthPotionCount--;
        GamePersistence.SetItemCount(ItemKind.HealthPotion, healthPotionCount);
        Heal(60);
        UIManager.Instance?.UpdateShopUI();
        UIManager.Instance?.UpdateInventoryUI(this);
        UIManager.Instance?.ShowAnnouncement("使用血瓶", Color.green);
    }

    public void UseAttackPotion()
    {
        if (attackPotionCount <= 0) return;
        attackPotionCount--;
        GamePersistence.SetItemCount(ItemKind.AttackPotion, attackPotionCount);
        damageMultiplier += 0.35f;
        UIManager.Instance?.UpdateShopUI();
        UIManager.Instance?.ShowAnnouncement("使用攻击药剂", new Color(0.9f, 0.7f, 0.2f));
    }
}
