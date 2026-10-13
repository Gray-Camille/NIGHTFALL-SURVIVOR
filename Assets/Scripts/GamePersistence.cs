using UnityEngine;

/// <summary>
/// 跨局持久化：3 个存档槽，每槽独立保存金币与道具背包。
/// 死亡 / 退出游戏 / 返回主菜单时写入，开始新游戏时覆盖对应槽位。
/// 局内成长（等级、符文、限时效果）不保存，重开后重新计算。
/// </summary>
public static class GamePersistence
{
    public const int SlotCount = 3;

    private const string GoldFormat = "PS_S{0}_BankGold";
    private const string ItemFormat = "PS_S{0}_Item_";
    private const string ExistsFormat = "PS_S{0}_Exists";
    private const string SummaryFormat = "PS_S{0}_Summary";

    // 旧版本只有一份全局存档，这里只在首次运行时迁移一次
    private const string LegacyGoldKey = "PS_BankGold";
    private const string LegacyItemPrefix = "PS_Item_";
    private const string LegacyDoneKey = "PS_LegacyMigrated";

    private static int _currentSlot = 1;

    /// <summary>当前正在游玩的存档槽（1..3）。</summary>
    public static int CurrentSlot { get { return _currentSlot; } }

    public static void SelectSlot(int slot)
    {
        _currentSlot = Mathf.Clamp(slot, 1, SlotCount);
    }

    private static string GoldKey(int slot) { return string.Format(GoldFormat, slot); }
    private static string ItemKey(int slot, ItemKind kind) { return string.Format(ItemFormat, slot) + kind; }
    private static string ExistsKey(int slot) { return string.Format(ExistsFormat, slot); }
    private static string SummaryKey(int slot) { return string.Format(SummaryFormat, slot); }

    /// <summary>该槽位是否有存档。空槽 = 开始新游戏，非空槽 = 可继续或被覆盖。</summary>
    public static bool SlotExists(int slot)
    {
        return PlayerPrefs.GetInt(ExistsKey(Mathf.Clamp(slot, 1, SlotCount)), 0) == 1;
    }

    /// <summary>空存档则新建，已有存档则直接覆盖，然后切到该槽位。</summary>
    public static void CreateOrOverwrite(int slot)
    {
        slot = Mathf.Clamp(slot, 1, SlotCount);
        ClearSlot(slot);
        PlayerPrefs.SetInt(ExistsKey(slot), 1);
        PlayerPrefs.Save();
        SelectSlot(slot);
    }

    public static void ClearSlot(int slot)
    {
        slot = Mathf.Clamp(slot, 1, SlotCount);
        PlayerPrefs.DeleteKey(GoldKey(slot));
        foreach (var kind in ItemCatalog.AllItems) PlayerPrefs.DeleteKey(ItemKey(slot, kind));
        PlayerPrefs.DeleteKey(ExistsKey(slot));
        PlayerPrefs.DeleteKey(SummaryKey(slot));
        PlayerPrefs.Save();
    }

    /// <summary>清空全部存档（调试用）。</summary>
    public static void ClearAll()
    {
        for (int slot = 1; slot <= SlotCount; slot++) ClearSlot(slot);
        PlayerPrefs.Save();
    }

    // ===== 当前槽位的读写（PlayerController / 背包 UI 直接用这几个）=====

    /// <summary>当前槽位保存下来的金币。</summary>
    public static int BankGold { get { return PlayerPrefs.GetInt(GoldKey(_currentSlot), 0); } }
    public static void SetBankGold(int value) { PlayerPrefs.SetInt(GoldKey(_currentSlot), Mathf.Max(0, value)); }

    // 用枚举名做键：以后新增/调整枚举顺序也不会读错旧存档
    public static int GetItemCount(ItemKind kind) { return PlayerPrefs.GetInt(ItemKey(_currentSlot, kind), 0); }
    public static void SetItemCount(ItemKind kind, int count) { PlayerPrefs.SetInt(ItemKey(_currentSlot, kind), Mathf.Max(0, count)); }

    // ===== 任意槽位的只读查询（存档选择界面用）=====

    public static int GetSlotGold(int slot)
    {
        return PlayerPrefs.GetInt(GoldKey(Mathf.Clamp(slot, 1, SlotCount)), 0);
    }

    public static int GetSlotItemTotal(int slot)
    {
        slot = Mathf.Clamp(slot, 1, SlotCount);
        int total = 0;
        foreach (var kind in ItemCatalog.AllItems) total += PlayerPrefs.GetInt(ItemKey(slot, kind), 0);
        return total;
    }

    /// <summary>把当前局内的金币与背包写回当前存档槽。</summary>
    public static void Save(PlayerController player)
    {
        if (player == null) return;
        PlayerPrefs.SetInt(ExistsKey(_currentSlot), 1);
        SetBankGold(player.currentGold);
        foreach (var kind in ItemCatalog.AllItems) SetItemCount(kind, player.GetItemCount(kind));
        // 顺手记一行进度摘要，让「继续游戏」界面能区分各个存档
        var gm = GameManager.Instance;
        if (gm != null) SetSlotSummary(_currentSlot, gm.GetWave(), gm.GetKillCount(), gm.GetGameTime());
        PlayerPrefs.Save();
    }

    /// <summary>「继续游戏」界面上显示的一行进度摘要。</summary>
    public static string GetSlotSummary(int slot)
    {
        slot = Mathf.Clamp(slot, 1, SlotCount);
        return PlayerPrefs.GetString(SummaryKey(slot), "进度：第 1 波 ｜ 击杀 0 ｜ 用时 00:00");
    }

    public static void SetSlotSummary(int slot, int wave, int kills, float time)
    {
        slot = Mathf.Clamp(slot, 1, SlotCount);
        PlayerPrefs.SetString(SummaryKey(slot), string.Format("进度：第 {0} 波 ｜ 击杀 {1} ｜ 用时 {2:D2}:{3:D2}", wave, kills, (int)(time / 60f), (int)(time % 60f)));
    }

    /// <summary>旧版本的单份全局存档第一次运行时搬进 1 号槽，避免玩家资产凭空消失。</summary>
    public static void MigrateLegacySaveIfNeeded()
    {
        if (PlayerPrefs.GetInt(LegacyDoneKey, 0) == 1) return;

        if (!PlayerPrefs.HasKey(LegacyGoldKey))
        {
            PlayerPrefs.SetInt(LegacyDoneKey, 1);
            PlayerPrefs.Save();
            return;
        }

        PlayerPrefs.SetInt(GoldKey(1), PlayerPrefs.GetInt(LegacyGoldKey, 0));
        PlayerPrefs.DeleteKey(LegacyGoldKey);
        foreach (var kind in ItemCatalog.AllItems)
        {
            string legacy = LegacyItemPrefix + kind;
            if (PlayerPrefs.HasKey(legacy))
            {
                PlayerPrefs.SetInt(ItemKey(1, kind), PlayerPrefs.GetInt(legacy, 0));
                PlayerPrefs.DeleteKey(legacy);
            }
        }
        PlayerPrefs.SetInt(ExistsKey(1), 1);
        PlayerPrefs.SetInt(LegacyDoneKey, 1);
        PlayerPrefs.Save();
    }
}
