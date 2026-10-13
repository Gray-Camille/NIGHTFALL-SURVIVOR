using UnityEngine;

/// <summary>
/// 道具的集中元数据表：显示名、背包上限、图标精灵键、效果说明、快捷键。
/// 背包 UI / 商店 / 掉落提示都从这里取文案，避免同一份描述在多处硬编码后漂移。
/// </summary>
public static class ItemCatalog
{
    /// <summary>背包里会占格子的道具（不含金币）。顺序即背包格子顺序。</summary>
    public static readonly ItemKind[] AllItems =
    {
        ItemKind.HealthPotion,
        ItemKind.AttackPotion,
        ItemKind.RagePotion,
        ItemKind.HastePotion,
        ItemKind.IronShield,
        ItemKind.MagnetCharm
    };

    /// <summary>每种道具的背包上限，刻意各不相同：消耗慢/便宜的能囤更多。</summary>
    public static int MaxStack(ItemKind kind)
    {
        switch (kind)
        {
            case ItemKind.HealthPotion: return 9;
            case ItemKind.AttackPotion: return 5;
            case ItemKind.RagePotion: return 9;
            case ItemKind.HastePotion: return 9;
            case ItemKind.IronShield: return 6;
            case ItemKind.MagnetCharm: return 6;
            default: return 0;
        }
    }

    public static string DisplayName(ItemKind kind)
    {
        switch (kind)
        {
            case ItemKind.GoldCoin: return "金币";
            case ItemKind.HealthPotion: return "血瓶";
            case ItemKind.AttackPotion: return "攻击药剂";
            case ItemKind.RagePotion: return "狂暴药剂";
            case ItemKind.HastePotion: return "迅捷药剂";
            case ItemKind.IronShield: return "铁壁护盾";
            case ItemKind.MagnetCharm: return "磁力护符";
            default: return "道具";
        }
    }

    /// <summary>鼠标悬停在背包格子上时显示的效果说明。</summary>
    public static string Description(ItemKind kind)
    {
        switch (kind)
        {
            case ItemKind.HealthPotion: return "立刻恢复 60 点生命";
            case ItemKind.AttackPotion: return "永久 +35% 攻击力（本局有效，死亡后重置）";
            case ItemKind.RagePotion: return "攻击力 +50%，持续 15 秒";
            case ItemKind.HastePotion: return "移动速度 +40%，持续 12 秒";
            case ItemKind.IronShield: return "防御力 +8（直接减免受到的伤害），持续 20 秒";
            case ItemKind.MagnetCharm: return "拾取范围 ×3，持续 12 秒";
            default: return string.Empty;
        }
    }

    /// <summary>掉落提示 / 公告上的一行式文案。</summary>
    public static string PickupHint(ItemKind kind)
    {
        int key = HotkeyIndex(kind);
        if (key <= 0) return "获得" + DisplayName(kind);
        return "获得" + DisplayName(kind) + "（按 " + key + " 使用）";
    }

    /// <summary>使用快捷键；0 表示该道具没有快捷键（金币）。</summary>
    public static int HotkeyIndex(ItemKind kind)
    {
        switch (kind)
        {
            case ItemKind.HealthPotion: return 1;
            case ItemKind.AttackPotion: return 2;
            case ItemKind.RagePotion: return 3;
            case ItemKind.HastePotion: return 4;
            case ItemKind.IronShield: return 5;
            case ItemKind.MagnetCharm: return 6;
            default: return 0;
        }
    }

    public static string SpriteName(ItemKind kind)
    {
        switch (kind)
        {
            case ItemKind.GoldCoin: return "gold_coin";
            case ItemKind.HealthPotion: return "health_potion";
            case ItemKind.AttackPotion: return "attack_potion";
            case ItemKind.RagePotion: return "rage_potion";
            case ItemKind.HastePotion: return "haste_potion";
            case ItemKind.IronShield: return "iron_shield";
            case ItemKind.MagnetCharm: return "magnet_charm";
            default: return "gold_coin";
        }
    }

    /// <summary>背包格子边框 / 提示标题的配色，让每种道具一眼可辨。</summary>
    public static Color Tint(ItemKind kind)
    {
        switch (kind)
        {
            case ItemKind.HealthPotion: return new Color(0.85f, 0.20f, 0.25f);
            case ItemKind.AttackPotion: return new Color(0.90f, 0.58f, 0.18f);
            case ItemKind.RagePotion: return new Color(0.95f, 0.35f, 0.18f);
            case ItemKind.HastePotion: return new Color(0.28f, 0.90f, 0.85f);
            case ItemKind.IronShield: return new Color(0.68f, 0.74f, 0.85f);
            case ItemKind.MagnetCharm: return new Color(0.78f, 0.45f, 0.95f);
            default: return new Color(0.95f, 0.80f, 0.25f);
        }
    }
}
