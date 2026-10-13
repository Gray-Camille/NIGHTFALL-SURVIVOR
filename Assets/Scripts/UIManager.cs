using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    void OnDestroy() { if (Instance == this) Instance = null; }

    [Header("HUD")]
    public GameObject gameStartPanel, gameOverPanel, pausePanel;
    public Text killText, waveText, timeText, hpText, xpText, lvText, levelUpText, gameOverKillText, gameOverTimeText;
    public Text defenseText;
    public Text goldText;
    public Text announcementText;
    public Text buffText;
    public Text gameOverGoldText;
    public Image healthFill, xpFill;
    public float healthFillMaxW = 300f, xpFillMaxW = 300f;

    [Header("Shop")]
    public GameObject shopPanel;
    public Text shopGoldText;
    public Text potionText;
    public Button healthPotionButton;
    public Button attackPotionButton;

    [Header("Rune")]
    public GameObject runePanel;
    public Button[] runeButtons = new Button[3];
    public Text[] runeTexts = new Text[3];

    [Header("Inventory")]
    public GameObject inventoryPanel;
    public Text inventoryGoldText;
    public Text inventoryHintText;
    public Image[] inventoryIcons = new Image[6];
    public Text[] inventoryCountTexts = new Text[6];

    [Header("Tooltip")]
    public GameObject tooltipPanel;
    public Text tooltipTitleText;
    public Text tooltipBodyText;

    // ===== 模态面板：所有需要暂停游戏的界面都必须登记在这里 =====
    public bool IsShopOpen => shopPanel != null && shopPanel.activeSelf;
    public bool IsRunePanelOpen => runePanel != null && runePanel.activeSelf;
    public bool IsInventoryOpen => inventoryPanel != null && inventoryPanel.activeSelf;
    public bool IsPausePanelOpen => pausePanel != null && pausePanel.activeSelf;
    public bool IsModalOpen => IsShopOpen || IsRunePanelOpen || IsInventoryOpen || IsPausePanelOpen;

    private RectTransform _hpFillRt, _xpFillRt;
    private float _levelUpTimer;
    private float _announcementTimer;
    private readonly List<RuneDefinition> _currentRuneOptions = new List<RuneDefinition>();
    private string _buffSummary = string.Empty;

    void Awake() { Instance = this; }

    void Start()
    {
        if (healthFill) _hpFillRt = healthFill.GetComponent<RectTransform>();
        if (xpFill) _xpFillRt = xpFill.GetComponent<RectTransform>();
    }

    void Update()
    {
        if (_levelUpTimer > 0) { _levelUpTimer -= Time.deltaTime; if (_levelUpTimer <= 0 && levelUpText) levelUpText.gameObject.SetActive(false); }
        if (_announcementTimer > 0) { _announcementTimer -= Time.deltaTime; if (_announcementTimer <= 0 && announcementText) announcementText.gameObject.SetActive(false); }
        var gm = GameManager.Instance;
        if (gm != null && !gm.IsGameOver && timeText)
            timeText.text = string.Format("Time: {0:D2}:{1:D2}", (int)(gm.GetGameTime() / 60), (int)(gm.GetGameTime() % 60));
    }

    /// <summary>暂停的唯一真源：只要有模态面板打开就 timeScale = 0，关闭后恢复。</summary>
    public void ApplyPauseState()
    {
        Time.timeScale = IsModalOpen ? 0f : 1f;
    }

    // ===== HUD =====
    public void ShowGameStart() { if (gameStartPanel) gameStartPanel.SetActive(true); if (gameOverPanel) gameOverPanel.SetActive(false); }
    public void HideGameStart() { if (gameStartPanel) gameStartPanel.SetActive(false); }
    public void UpdateKillUI(int k) { if (killText) killText.text = "Kills: " + k; }
    public void UpdateWaveUI(int w) { if (waveText) waveText.text = "Wave " + w; }

    public void UpdateDefenseUI(int armor) { if (defenseText) defenseText.text = "DEF: " + armor; }
    public void UpdateGoldUI(int gold) { if (goldText) goldText.text = "Gold: " + gold; }

    /// <summary>HUD 上的限时道具剩余时间；内容不变时不重复写 Text。</summary>
    public void UpdateBuffUI(PlayerController player)
    {
        if (buffText == null) return;
        string summary = player != null ? player.GetBuffSummary() : string.Empty;
        if (summary == _buffSummary) return;
        _buffSummary = summary;
        buffText.text = summary;
    }

    public void UpdateInventoryUI(PlayerController player)
    {
        if (player == null) return;
        RefreshInventoryText(player);
        if (IsInventoryOpen) RefreshInventoryUI();
    }

    private void RefreshInventoryText(PlayerController player)
    {
        if (potionText == null) return;
        potionText.text = BuildInventorySummary(player);
    }

    /// <summary>一行式背包摘要，商店与背包面板共用，保证同一份数字不会两处写法不一致。</summary>
    private static string BuildInventorySummary(PlayerController player)
    {
        var sb = new System.Text.StringBuilder("背包  ");
        foreach (var kind in ItemCatalog.AllItems)
            sb.AppendFormat("{0} x{1}/{2}   ", ItemCatalog.DisplayName(kind), player.GetItemCount(kind), ItemCatalog.MaxStack(kind));
        return sb.ToString().TrimEnd();
    }

    // ===== 背包 =====
    public void ToggleInventory()
    {
        if (!inventoryPanel || IsRunePanelOpen || IsPausePanelOpen) return;
        if (IsShopOpen) shopPanel.SetActive(false);          // 同时只允许一个功能性面板
        bool willOpen = !IsInventoryOpen;
        inventoryPanel.SetActive(willOpen);
        if (willOpen) RefreshInventoryUI();
        else HideItemTooltip();
        ApplyPauseState();
    }

    public void CloseInventory()
    {
        if (!IsInventoryOpen) return;
        inventoryPanel.SetActive(false);
        HideItemTooltip();
        ApplyPauseState();
    }

    // ===== 暂停菜单（局内按 Esc） =====
    public void TogglePauseMenu()
    {
        if (!pausePanel || IsRunePanelOpen) return;
        if (IsPausePanelOpen) { ClosePauseMenu(); return; }
        if (IsShopOpen) CloseShop();
        if (IsInventoryOpen) CloseInventory();
        pausePanel.SetActive(true);
        ApplyPauseState();
    }

    public void ClosePauseMenu()
    {
        if (!IsPausePanelOpen) return;
        pausePanel.SetActive(false);
        ApplyPauseState();
    }

    /// <summary>刷新金币与 6 个格子的图标 / 数量 / 名称底色。</summary>
    public void RefreshInventoryUI()
    {
        if (!inventoryPanel || !inventoryPanel.activeSelf) return;
        var player = PlayerController.Instance;
        if (player == null) return;

        if (inventoryGoldText) inventoryGoldText.text = "金币：" + player.currentGold + " G";

        for (int i = 0; i < ItemCatalog.AllItems.Length; i++)
        {
            var kind = ItemCatalog.AllItems[i];
            int count = player.GetItemCount(kind);
            int max = ItemCatalog.MaxStack(kind);

            if (i < inventoryIcons.Length && inventoryIcons[i] != null)
            {
                inventoryIcons[i].sprite = PixelSpriteGenerator.GetSprite(ItemCatalog.SpriteName(kind));
                // 空槽位压暗，有货时恢复正常亮度
                float dim = count > 0 ? 1f : 0.35f;
                var tint = ItemCatalog.Tint(kind);
                inventoryIcons[i].color = new Color(tint.r * dim + (1f - dim) * 0.2f, tint.g * dim + (1f - dim) * 0.2f, tint.b * dim + (1f - dim) * 0.2f, 1f);
            }
            if (i < inventoryCountTexts.Length && inventoryCountTexts[i] != null)
            {
                bool full = count >= max;
                inventoryCountTexts[i].text = "x" + count + "/" + max;
                inventoryCountTexts[i].color = full ? new Color(1f, 0.55f, 0.35f) : Color.white;
            }
        }

        if (inventoryHintText)
            inventoryHintText.text = "鼠标悬停查看效果；按 1~6 使用道具，按 I 关闭";
    }

    public void ShowItemTooltip(ItemKind kind) { ShowItemTooltip(kind, null); }

    /// <summary>显示道具效果提示框；anchor 不为空时紧贴该格子（道具）的正下方。</summary>
    public void ShowItemTooltip(ItemKind kind, RectTransform anchor)
    {
        if (tooltipPanel == null || tooltipTitleText == null || tooltipBodyText == null) return;
        int max = ItemCatalog.MaxStack(kind);
        int count = PlayerController.Instance != null ? PlayerController.Instance.GetItemCount(kind) : 0;
        tooltipTitleText.text = ItemCatalog.DisplayName(kind) + "   " + count + " / " + max;
        tooltipTitleText.color = ItemCatalog.Tint(kind);
        tooltipBodyText.text = ItemCatalog.Description(kind) + "\n按 " + ItemCatalog.HotkeyIndex(kind) + " 使用";

        var titleRt = tooltipTitleText.rectTransform;
        var bodyRt = tooltipBodyText.rectTransform;
        titleRt.sizeDelta = new Vector2(300, 26);
        bodyRt.sizeDelta = new Vector2(300, 46);
        titleRt.anchoredPosition = new Vector2(14, -12);
        bodyRt.anchoredPosition = new Vector2(14, -42);

        var rt = tooltipPanel.transform as RectTransform;
        rt.sizeDelta = new Vector2(328, 100);
        tooltipPanel.SetActive(true);
        PlaceTooltip(rt, anchor);
    }

    /// <summary>
    /// 把提示框摆到格子的正下方（水平居中对齐）；下方空间不够时改摆到格子上方，
    /// 最后统一 Clamp 在画布内，保证任何分辨率下都不会跑出屏幕。
    /// 注意：位置只能由这里决定，不要再在 Update 里跟随鼠标，否则每帧都会被覆盖。
    /// </summary>
    private void PlaceTooltip(RectTransform tooltip, RectTransform anchor)
    {
        var canvas = tooltip.parent as RectTransform;
        if (canvas == null) return;

        float width = tooltip.sizeDelta.x;
        float height = tooltip.sizeDelta.y;
        const float gap = 8f;   // 与格子之间留的缝
        const float pad = 8f;   // 与画布边缘的最小间距
        float left;
        float top;              // 提示框「上沿」距画布底边的高度（pivot 是左上）

        if (anchor != null)
        {
            var corners = new Vector3[4];                 // 0=左下 1=左上 2=右上 3=右下
            anchor.GetWorldCorners(corners);
            Vector3 bottomCenter = (corners[0] + corners[3]) * 0.5f;
            Vector3 topCenter = (corners[1] + corners[2]) * 0.5f;
            Vector3 low = canvas.InverseTransformPoint(bottomCenter);    // 格子下沿（画布局部坐标）
            Vector3 high = canvas.InverseTransformPoint(topCenter);      // 格子上沿

            left = low.x - canvas.rect.xMin - width * 0.5f;   // 与格子中心水平对齐

            float below = low.y - canvas.rect.yMin - gap;     // 贴在格子下沿时，提示框的上沿位置
            if (below - height >= pad)                        // 下方放得下 → 正下方
                top = below;
            else                                              // 放不下 → 翻到格子上方，露出格子
                top = high.y - canvas.rect.yMin + gap + height;
        }
        else
        {
            left = 24f;
            top = height + 24f;
        }

        left = Mathf.Clamp(left, pad, Mathf.Max(pad, canvas.rect.width - width - pad));
        top = Mathf.Clamp(top, height + pad, Mathf.Max(height + pad, canvas.rect.height - pad));

        tooltip.anchorMin = Vector2.zero;
        tooltip.anchorMax = Vector2.zero;
        tooltip.pivot = new Vector2(0f, 1f);
        tooltip.anchoredPosition = new Vector2(left, top);
    }

    public void HideItemTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    // ===== 商店 =====
    public void ToggleShop()
    {
        if (!shopPanel || IsRunePanelOpen || IsPausePanelOpen) return;
        if (IsInventoryOpen) { inventoryPanel.SetActive(false); HideItemTooltip(); }
        bool willOpen = !IsShopOpen;
        shopPanel.SetActive(willOpen);
        if (willOpen) UpdateShopUI();
        ApplyPauseState();
    }

    public void CloseShop()
    {
        if (!shopPanel || !shopPanel.activeSelf) return;
        shopPanel.SetActive(false);
        ApplyPauseState();
    }

    public void UpdateShopUI()
    {
        var player = PlayerController.Instance;
        if (player == null || !shopPanel || !shopPanel.activeSelf) return;
        if (shopGoldText) shopGoldText.text = "Gold: " + player.currentGold;
        RefreshInventoryText(player);
        if (healthPotionButton) healthPotionButton.interactable = player.currentGold >= 12;
        if (attackPotionButton) attackPotionButton.interactable = player.currentGold >= 30;
    }

    public void PurchaseHealthPotion() => PlayerController.Instance?.PurchaseHealthPotion();
    public void PurchaseAttackPotion() => PlayerController.Instance?.PurchaseAttackPotion();

    /// <summary>关闭最上层的模态面板，返回是否真的关掉了东西（Esc 用）。</summary>
    public bool CloseTopModal()
    {
        if (IsPausePanelOpen) { ClosePauseMenu(); return true; }
        if (IsInventoryOpen) { CloseInventory(); return true; }
        if (IsShopOpen) { CloseShop(); return true; }
        if (IsRunePanelOpen) { HideRuneChoices(); return true; }
        return false;
    }

    /// <summary>开始新一局前收起玩家可能开着的界面（主菜单在独立场景里，不归这里管）。</summary>
    public void HideAllMenus()
    {
        if (inventoryPanel) inventoryPanel.SetActive(false);
        if (shopPanel) shopPanel.SetActive(false);
        HideItemTooltip();
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        ApplyPauseState();
    }

    // ===== 通用提示 =====
    public void ShowAnnouncement(string message, Color color)
    {
        if (!announcementText) return;
        announcementText.text = message;
        announcementText.color = color;
        announcementText.gameObject.SetActive(true);
        _announcementTimer = 2f;
    }

    public void UpdateHealthUI(int cur, int max)
    {
        float pct = (float)cur / max;
        if (_hpFillRt) _hpFillRt.sizeDelta = new Vector2(healthFillMaxW * pct, _hpFillRt.sizeDelta.y);
        if (healthFill) healthFill.color = Color.Lerp(Color.red, Color.green, pct);
        if (hpText) hpText.text = string.Format("HP: {0}/{1}", cur, max);
    }

    public void UpdateXPUI(int cur, int need, int lv)
    {
        float pct = (float)cur / need;
        if (_xpFillRt) _xpFillRt.sizeDelta = new Vector2(xpFillMaxW * pct, _xpFillRt.sizeDelta.y);
        if (xpFill) xpFill.color = Color.Lerp(new Color(0.2f, 0.4f, 0.9f), new Color(0.2f, 0.9f, 1f), pct);
        if (xpText) xpText.text = string.Format("XP: {0}/{1}", cur, need);
        if (lvText) lvText.text = "Lv." + lv;
    }

    public void ShowLevelUp(int lv)
    {
        if (levelUpText) { levelUpText.text = "LEVEL UP! Lv." + lv; levelUpText.gameObject.SetActive(true); _levelUpTimer = 2f; }
    }

    public void ShowGameOver(int k, float t)
    {
        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (gameOverKillText) gameOverKillText.text = "Kills: " + k;
        if (gameOverTimeText) gameOverTimeText.text = string.Format("Survived: {0:D2}:{1:D2}", (int)(t / 60), (int)(t % 60));
        if (gameOverGoldText)
            gameOverGoldText.text = "存档 " + GamePersistence.CurrentSlot + " 已保存：" + GamePersistence.BankGold + "G ｜ 道具 " +
                (PlayerController.Instance != null ? CountItems(PlayerController.Instance) : 0) + " 件，下一局继续使用";
    }

    private static int CountItems(PlayerController player)
    {
        int total = 0;
        foreach (var kind in ItemCatalog.AllItems) total += player.GetItemCount(kind);
        return total;
    }

    // ===== 符文选择 =====
    public void TryShowRuneChoice()
    {
        var player = PlayerController.Instance;
        if (player == null || runePanel == null) return;
        if (player.pendingRuneChoices <= 0) return;
        if (IsShopOpen || IsInventoryOpen) return;

        _currentRuneOptions.Clear();
        _currentRuneOptions.AddRange(RuneSystem.RollOptions(player, runeButtons.Length));

        for (int i = 0; i < runeButtons.Length; i++)
        {
            if (runeButtons[i] == null) continue;
            bool hasOption = i < _currentRuneOptions.Count;
            runeButtons[i].gameObject.SetActive(hasOption);
            if (!hasOption) continue;
            var rune = _currentRuneOptions[i];
            if (runeTexts[i] != null)
            {
                runeTexts[i].text = rune.name + RuneLabelSuffix(player, rune) + "\n" + rune.description;
                runeTexts[i].color = rune.color;
            }
            if (runeButtons[i].image != null)
                runeButtons[i].image.color = new Color(0.16f, 0.16f, 0.26f, 0.98f);
        }

        if (_currentRuneOptions.Count > 0)
        {
            runePanel.SetActive(true);
            ApplyPauseState();
        }
    }

    public void SelectRune(int index)
    {
        var player = PlayerController.Instance;
        if (player == null) return;
        if (index < 0 || index >= _currentRuneOptions.Count) return;

        var rune = _currentRuneOptions[index];
        RuneSystem.Apply(player, rune.id);
        player.runesTaken++;
        player.pendingRuneChoices = Mathf.Max(0, player.pendingRuneChoices - 1);
        ShowAnnouncement("获得符文：" + rune.name, rune.color);

        HideRuneChoices();
        if (player.pendingRuneChoices > 0) TryShowRuneChoice();
    }

    /// <summary>一次性符文标注“仅一次”；可重复获取的符文显示本次将叠加到的等级。</summary>
    private static string RuneLabelSuffix(PlayerController player, RuneDefinition rune)
    {
        if (rune == null || player == null) return string.Empty;
        if (RuneSystem.IsOneTimeOnly(rune.id)) return "（仅一次）";
        int stack = player.GetRuneStacks(rune.id);
        return stack > 0 ? " Lv." + (stack + 1) : string.Empty;
    }

    public void HideRuneChoices()
    {
        if (runePanel != null) runePanel.SetActive(false);
        _currentRuneOptions.Clear();
        ApplyPauseState();
    }

    public void ResetRuneChoices()
    {
        if (runePanel != null) runePanel.SetActive(false);
        _currentRuneOptions.Clear();
    }
}
