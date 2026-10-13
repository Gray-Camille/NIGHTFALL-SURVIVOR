using UnityEngine;

public enum ItemKind
{
    GoldCoin,
    HealthPotion,   // 血瓶（商店购买 / BOSS 掉落）：按 1 立刻回血
    AttackPotion,   // 攻击药剂（商店购买）：按 2 永久 +35% 攻击
    RagePotion,     // 狂暴药剂（掉落）：按 3 攻击 +50%，持续 15 秒
    HastePotion,    // 迅捷药剂（掉落）：按 4 移速 +40%，持续 12 秒
    IronShield,     // 铁壁护盾（掉落）：按 5 防御 +8，持续 20 秒
    MagnetCharm     // 磁力护符（掉落）：按 6 拾取范围 ×3，持续 12 秒
}

public class PickupDrop : MonoBehaviour
{
    public ItemKind kind = ItemKind.HealthPotion;
    public int amount = 1;
    public float collectSpeed = 6f;
    public float collectRange = 3f;

    private Rigidbody2D _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        var player = PlayerController.Instance;
        if (player == null || GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        Vector2 toPlayer = player.transform.position - transform.position;
        float range = collectRange * player.PickupRangeMultiplier;   // 磁力护符会放大拾取范围
        if (toPlayer.magnitude <= range)
        {
            _rb.velocity = Vector2.Lerp(_rb.velocity, toPlayer.normalized * collectSpeed, Time.deltaTime * 8f);
        }

        if (toPlayer.magnitude <= 0.8f)
        {
            Collect(player);
        }
    }

    void Collect(PlayerController player)
    {
        if (kind == ItemKind.GoldCoin)
        {
            player.AddGold(amount);
        }
        else
        {
            player.AddItem(kind);
            UIManager.Instance?.ShowAnnouncement(PickupDrop.GetItemName(kind), Color.white);
        }

        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player != null) Collect(player);
    }

    /// <summary>掉落提示文案统一由 ItemCatalog 提供，避免名称/按键在两处写得不一致。</summary>
    public static string GetItemName(ItemKind kind)
    {
        return ItemCatalog.PickupHint(kind);
    }
}
