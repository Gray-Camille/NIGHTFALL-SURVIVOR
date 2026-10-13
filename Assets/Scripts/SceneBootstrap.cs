using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SceneBootstrap
{
    /// <summary>
    /// 订阅「场景加载完成」事件。注意 RuntimeInitializeOnLoadMethod 只在启动时触发一次，
    /// 之后再 LoadScene 不会重跑；若只靠它，从主菜单切进游戏时 MainScene 会是空的（没有
    /// GameManager / 玩家 / UI）。所以必须改挂 sceneLoaded，它在每次场景加载后都会触发。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void HookSceneLoaded()
    {
        // 关闭域重载时静态事件会跨 Play 残留，先减后加保证只订阅一次
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        // 同理清掉上一次 Play 遗留的「从菜单开局」请求，避免一按 Play 就直接进游戏
        MenuFlow.Clear();
    }

    /// <summary>兜底：确保启动时的第一个场景也被处理一次（此时场景已完成 Awake）。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void HookFirstScene()
    {
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 主菜单是独立场景，交给 MainMenuBootstrap，这里不搭任何战斗系统
        if (scene.name == MenuFlow.MenuSceneName) return;
        // 同一场景被处理两次（事件 + 兜底）时不要重复搭建
        foreach (var root in scene.GetRootGameObjects())
            if (root != null && root.name == "___BootstrapRunner") return;
        new GameObject("___BootstrapRunner").AddComponent<BootstrapRunner>();
    }
}

public class BootstrapRunner : MonoBehaviour
{
    void Start()
    {
        BuildAll();
        DestroyImmediate(gameObject);
    }

    void BuildAll()
    {
        // 关闭域重载后静态精灵缓存会残留已销毁的贴图，进入游戏时先清空
        PixelSpriteGenerator.ClearCache();

        var existingCamera = Camera.main;

        EnsureEventSystem();
        var camObj = existingCamera != null ? existingCamera.gameObject : new GameObject("MainCamera");
        camObj.tag = "MainCamera";
        var cam = existingCamera != null ? existingCamera : camObj.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 5;
        cam.backgroundColor = new Color(0.08f, 0.06f, 0.12f);
        camObj.transform.position = new Vector3(0, 0, -10);
        if (camObj.GetComponent<CameraFollow>() == null) camObj.AddComponent<CameraFollow>().floorBound = 40f;

        new GameObject("MapManager").AddComponent<MapManager>();

        BuildUI();
        new GameObject("GameManager").AddComponent<GameManager>().InitGame();
    }

    void EnsureEventSystem()
    {
        // UI Button 必须依赖 EventSystem 才能接收点击事件
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    void BuildUI()
    {
        var uiObj = new GameObject("UIManager");
        var ui = uiObj.AddComponent<UIManager>();
        var cgo = new GameObject("Canvas"); cgo.transform.SetParent(uiObj.transform);
        cgo.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        // 1280x720 + 0.5 混合匹配：非 16:9 的分辨率下面板也不会跑出屏幕
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        cgo.AddComponent<GraphicRaycaster>();
        Transform ct = cgo.transform;
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Font.CreateDynamicFontFromOSFont("Arial", 14);

        // Panels created as empty placeholders (no visual until needed)
        ui.gameStartPanel = new GameObject("GameStartPanel");
        ui.gameStartPanel.transform.SetParent(ct, false);
        var gsRt = ui.gameStartPanel.AddComponent<RectTransform>();
        gsRt.anchorMin = Vector2.zero; gsRt.anchorMax = Vector2.one; gsRt.sizeDelta = Vector2.zero;
        var gsImg = ui.gameStartPanel.AddComponent<Image>();
        gsImg.color = new Color(0, 0, 0, 0.7f);
        gsImg.raycastTarget = false;
        ui.gameStartPanel.transform.SetAsFirstSibling();
        T(ui.gameStartPanel.transform, "PIXEL SURVIVOR", 48, TextAnchor.MiddleCenter, f, 0, 0).color = Color.white;
        T(ui.gameStartPanel.transform, "Press WASD to move!", 24, TextAnchor.MiddleCenter, f, 0, -60).color = Color.gray;
        // Hide the panel at start so texts are visible
        ui.gameStartPanel.SetActive(false);

        ui.gameOverPanel = new GameObject("GameOverPanel");
        ui.gameOverPanel.transform.SetParent(ct, false);
        var goRt = ui.gameOverPanel.AddComponent<RectTransform>();
        goRt.anchorMin = Vector2.zero; goRt.anchorMax = Vector2.one; goRt.sizeDelta = Vector2.zero;
        var goImg = ui.gameOverPanel.AddComponent<Image>();
        goImg.color = new Color(0, 0, 0, 0.8f);
        goImg.raycastTarget = false;
        ui.gameOverPanel.transform.SetSiblingIndex(1);
        ui.gameOverPanel.SetActive(false);
        T(ui.gameOverPanel.transform, "GAME OVER", 52, TextAnchor.MiddleCenter, f, 0, 0).color = Color.red;
        ui.gameOverKillText = T(ui.gameOverPanel.transform, "Kills: 0", 28, TextAnchor.MiddleCenter, f, 0, -50);
        ui.gameOverTimeText = T(ui.gameOverPanel.transform, "Survived: 00:00", 24, TextAnchor.MiddleCenter, f, 0, -85);
        ui.gameOverGoldText = T(ui.gameOverPanel.transform, "", 22, TextAnchor.MiddleCenter, f, 0, -125);
        ui.gameOverGoldText.color = new Color(0.95f, 0.8f, 0.25f);
        T(ui.gameOverPanel.transform, "按 R 重新开始 ｜ 按 M 返回主菜单", 22, TextAnchor.MiddleCenter, f, 0, -168).color = new Color(0.7f, 0.7f, 0.3f);
        var restartButton = CreateShopButton(ui.gameOverPanel.transform, f, "重新开始", new Vector2(-115, -222), 200, 50,
            () => GameManager.Instance?.RestartGame());
        restartButton.image.color = new Color(0.24f, 0.5f, 0.3f);
        var backToMenuButton = CreateShopButton(ui.gameOverPanel.transform, f, "返回主菜单", new Vector2(115, -222), 200, 50,
            () => GameManager.Instance?.ReturnToMenu(true));
        backToMenuButton.image.color = new Color(0.3f, 0.34f, 0.55f);

        // ---- UI TEXT ELEMENTS (renders on top) ----
        ui.killText = T(ct, "Kills: 0", 24, TextAnchor.UpperLeft, f, 20, -20);
        ui.waveText = T(ct, "Wave 1", 28, TextAnchor.UpperCenter, f, 0, -20);
        ui.timeText = T(ct, "Time: 00:00", 20, TextAnchor.UpperRight, f, -20, -20);

        ui.defenseText = T(ct, "DEF: 0", 20, TextAnchor.UpperLeft, f, 20, -76);
        ui.goldText = T(ct, "Gold: 0", 20, TextAnchor.UpperLeft, f, 20, -108);
        ui.defenseText.color = new Color(0.7f, 0.8f, 1f);

        ui.buffText = T(ct, "", 18, TextAnchor.UpperLeft, f, 20, -140);
        ui.buffText.color = new Color(1f, 0.85f, 0.5f);

        var hudHint = T(ct, "WASD 移动 ｜ B 商店 ｜ I 背包 ｜ 1~6 使用道具 ｜ Esc 菜单", 15, TextAnchor.LowerRight, f, -20, 16);
        hudHint.rectTransform.sizeDelta = new Vector2(560, 40);
        hudHint.color = new Color(0.55f, 0.58f, 0.68f);

        var hpBg = new GameObject("HPBarBg"); hpBg.transform.SetParent(ct, false);
        var hpBgRt = hpBg.AddComponent<RectTransform>();
        hpBgRt.anchorMin = hpBgRt.anchorMax = new Vector2(0, 1);
        hpBgRt.pivot = new Vector2(0, 1); hpBgRt.sizeDelta = new Vector2(300, 12);
        hpBgRt.anchoredPosition = new Vector2(20, -70);
        hpBg.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f);

        var hpFill = new GameObject("HPBarFill"); hpFill.transform.SetParent(hpBg.transform, false);
        var hpFillRt = hpFill.AddComponent<RectTransform>();
        hpFillRt.anchorMin = new Vector2(0, 0); hpFillRt.anchorMax = new Vector2(0, 1);
        hpFillRt.pivot = new Vector2(0, 0.5f); hpFillRt.sizeDelta = new Vector2(300, 0);
        hpFillRt.anchoredPosition = Vector2.zero;
        ui.healthFill = hpFill.AddComponent<Image>();
        ui.healthFill.color = new Color(0.3f, 1f, 0.3f);
        ui.healthFillMaxW = 300f;

        var xpBg = new GameObject("XPBarBg"); xpBg.transform.SetParent(ct, false);
        var xpBgRt = xpBg.AddComponent<RectTransform>();
        xpBgRt.anchorMin = xpBgRt.anchorMax = new Vector2(0, 0);
        xpBgRt.pivot = new Vector2(0, 0); xpBgRt.sizeDelta = new Vector2(300, 10);
        xpBgRt.anchoredPosition = new Vector2(20, 38);
        xpBg.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f);

        var xpFill = new GameObject("XPBarFill"); xpFill.transform.SetParent(xpBg.transform, false);
        var xpFillRt = xpFill.AddComponent<RectTransform>();
        xpFillRt.anchorMin = new Vector2(0, 0); xpFillRt.anchorMax = new Vector2(0, 1);
        xpFillRt.pivot = new Vector2(0, 0.5f); xpFillRt.sizeDelta = new Vector2(0, 0);
        xpFillRt.anchoredPosition = Vector2.zero;
        ui.xpFill = xpFill.AddComponent<Image>();
        ui.xpFill.color = new Color(0.2f, 0.5f, 1f);
        ui.xpFillMaxW = 300f;

        ui.hpText = T(ct, "HP: 100/100", 16, TextAnchor.UpperLeft, f, 330, -70);
        var hpTextRt = ui.hpText.rectTransform;
        hpTextRt.anchorMin = hpTextRt.anchorMax = new Vector2(0, 1);
        hpTextRt.pivot = new Vector2(0, 0.5f);
        hpTextRt.sizeDelta = new Vector2(150, 20);
        hpTextRt.anchoredPosition = new Vector2(330, -70);
        ui.hpText.color = new Color(0.8f, 1f, 0.8f);

        ui.xpText = T(ct, "XP: 0/20", 14, TextAnchor.LowerLeft, f, 330, 38);
        var xpTextRt = ui.xpText.rectTransform;
        xpTextRt.anchorMin = xpTextRt.anchorMax = new Vector2(0, 0);
        xpTextRt.pivot = new Vector2(0, 0.5f);
        xpTextRt.sizeDelta = new Vector2(150, 20);
        xpTextRt.anchoredPosition = new Vector2(330, 38);
        ui.xpText.color = new Color(0.8f, 0.9f, 1f);

        ui.lvText = T(ct, "Lv.1", 16, TextAnchor.LowerLeft, f, 490, 38);
        var lvTextRt = ui.lvText.rectTransform;
        lvTextRt.anchorMin = lvTextRt.anchorMax = new Vector2(0, 0);
        lvTextRt.pivot = new Vector2(0, 0.5f);
        lvTextRt.sizeDelta = new Vector2(100, 20);
        lvTextRt.anchoredPosition = new Vector2(490, 38);
        ui.lvText.color = Color.yellow;

        ui.levelUpText = T(ct, "", 36, TextAnchor.MiddleCenter, f, 0, 0);
        ui.levelUpText.color = Color.yellow; ui.levelUpText.gameObject.SetActive(false);

        ui.announcementText = T(ct, "", 28, TextAnchor.UpperCenter, f, 0, -80);
        ui.announcementText.color = new Color(0.9f, 0.8f, 0.2f);
        ui.announcementText.gameObject.SetActive(false);
        BuildShopPanel(ui, ct, f);
        BuildRunePanel(ui, ct, f);
        BuildInventoryPanel(ui, ct, f);
        BuildPausePanel(ui, ct, f);
        // 提示框必须最后创建：UGUI 的渲染顺序等于层级顺序，这样它才盖在背包格子之上
        BuildTooltip(ui, ct, f);
        // 主菜单 / 存档选择 / 游戏说明已搬到独立的 MainMenuScene（见 MenuUI）
    }

    Text T(Transform p, string t, int s, TextAnchor a, Font f, float x, float y)
    {
        var go = new GameObject("T"); go.transform.SetParent(p, false);
        var txt = go.AddComponent<Text>(); txt.text = t; txt.fontSize = s;
        txt.alignment = a; txt.color = Color.white; txt.font = f;
        txt.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        Vector2 av = a == TextAnchor.UpperLeft ? Vector2.up :
                    a == TextAnchor.UpperRight ? Vector2.one :
                    a == TextAnchor.LowerLeft ? Vector2.zero :
                    a == TextAnchor.LowerRight ? new Vector2(1, 0) :
                    a == TextAnchor.MiddleLeft ? new Vector2(0, 0.5f) :
                    a == TextAnchor.MiddleRight ? new Vector2(1, 0.5f) :
                    a == TextAnchor.UpperCenter ? new Vector2(0.5f, 1) :
                    a == TextAnchor.LowerCenter ? new Vector2(0.5f, 0) : new Vector2(0.5f, 0.5f);
        rt.anchorMin = av; rt.anchorMax = av; rt.pivot = av;
        rt.sizeDelta = new Vector2(400, 40); rt.anchoredPosition = new Vector2(x, y);
        return txt;
    }

    GameObject P(Transform p, Color c)
    {
        var go = new GameObject("Panel"); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        go.AddComponent<Image>().color = c;
        go.GetComponent<Image>().raycastTarget = false;
        return go;
    }

    void BuildShopPanel(UIManager ui, Transform ct, Font f)
    {
        ui.shopPanel = new GameObject("ShopPanel");
        ui.shopPanel.transform.SetParent(ct, false);
        var rt = ui.shopPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(700, 430);
        rt.anchoredPosition = Vector2.zero;
        var panelImage = ui.shopPanel.AddComponent<Image>();
        panelImage.color = new Color(0.07f, 0.07f, 0.11f, 0.97f);
        panelImage.raycastTarget = true;
        ui.shopPanel.SetActive(false);

        T(ui.shopPanel.transform, "SHOP  (B 关闭)", 28, TextAnchor.UpperCenter, f, 0, -20).color = Color.white;
        ui.shopGoldText = T(ui.shopPanel.transform, "Gold: 0", 22, TextAnchor.UpperCenter, f, 0, -62);
        ui.shopGoldText.color = new Color(0.95f, 0.8f, 0.25f);

        ui.healthPotionButton = CreateShopButton(ui.shopPanel.transform, f, "血瓶  12G", new Vector2(-175, 65), 250, 74, ui.PurchaseHealthPotion);
        ui.healthPotionButton.image.color = new Color(0.25f, 0.55f, 0.25f);
        CreateShopDescription(ui.shopPanel.transform, f, "按 1 使用：立刻恢复 60 点生命", 65f, Color.green);

        ui.attackPotionButton = CreateShopButton(ui.shopPanel.transform, f, "攻击药剂  30G", new Vector2(-175, -45), 250, 74, ui.PurchaseAttackPotion);
        ui.attackPotionButton.image.color = new Color(0.55f, 0.4f, 0.15f);
        CreateShopDescription(ui.shopPanel.transform, f, "按 2 使用：永久 +35% 攻击力", -45f, new Color(0.95f, 0.8f, 0.35f));

        ui.potionText = T(ui.shopPanel.transform, "背包  血瓶 x0   攻击药剂 x0", 18, TextAnchor.LowerCenter, f, 0, 78);
        ui.potionText.color = new Color(0.85f, 0.85f, 0.9f);
        ui.potionText.rectTransform.sizeDelta = new Vector2(620, 60);
        T(ui.shopPanel.transform, "商店：血瓶按 1、攻击药剂按 2；掉落道具：狂暴 3 / 迅捷 4 / 铁壁 5 / 磁力 6", 15, TextAnchor.LowerCenter, f, 0, 26).color = new Color(0.6f, 0.6f, 0.66f);
    }

    Text CreateShopDescription(Transform parent, Font font, string label, float y, Color color)
    {
        var text = T(parent, label, 17, TextAnchor.MiddleLeft, font, 330f, y);
        var rt = text.rectTransform;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(340, 40);
        text.color = color;
        return text;
    }

    /// <summary>局内暂停菜单：Esc 打开，可以继续、重开或返回主菜单（返回前会保存本局进度）。</summary>
    void BuildPausePanel(UIManager ui, Transform ct, Font f)
    {
        ui.pausePanel = new GameObject("PausePanel");
        ui.pausePanel.transform.SetParent(ct, false);
        var rt = ui.pausePanel.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(520, 360);
        rt.anchoredPosition = Vector2.zero;
        var panelImage = ui.pausePanel.AddComponent<Image>();
        panelImage.color = new Color(0.06f, 0.06f, 0.1f, 0.96f);
        panelImage.raycastTarget = true;

        T(ui.pausePanel.transform, "已暂停", 34, TextAnchor.UpperCenter, f, 0, -24).color = Color.white;
        T(ui.pausePanel.transform, "按 Esc 回到游戏", 17, TextAnchor.UpperCenter, f, 0, -74).color = new Color(0.7f, 0.72f, 0.8f);

        var resumeButton = CreateShopButton(ui.pausePanel.transform, f, "继续游戏", new Vector2(0, 32), 300, 56,
            ui.ClosePauseMenu);
        resumeButton.image.color = new Color(0.24f, 0.5f, 0.3f);
        var restartButton = CreateShopButton(ui.pausePanel.transform, f, "重新开始本局", new Vector2(0, -32), 300, 56,
            () => GameManager.Instance?.RestartGame());
        restartButton.image.color = new Color(0.5f, 0.42f, 0.2f);
        var menuButton = CreateShopButton(ui.pausePanel.transform, f, "返回主菜单（保存进度）", new Vector2(0, -96), 300, 56,
            () => GameManager.Instance?.ReturnToMenu(true));
        menuButton.image.color = new Color(0.3f, 0.34f, 0.55f);

        ui.pausePanel.SetActive(false);
    }

    void BuildRunePanel(UIManager ui, Transform ct, Font f)
    {
        ui.runePanel = new GameObject("RunePanel");
        ui.runePanel.transform.SetParent(ct, false);
        var rt = ui.runePanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(780, 390);
        rt.anchoredPosition = Vector2.zero;
        var panelImage = ui.runePanel.AddComponent<Image>();
        panelImage.color = new Color(0.06f, 0.05f, 0.12f, 0.97f);
        panelImage.raycastTarget = true;

        T(ui.runePanel.transform, "选择符文", 30, TextAnchor.UpperCenter, f, 0, -22).color = Color.white;
        T(ui.runePanel.transform, "升级奖励：随机 3 选 1，可重复获取的符文会继续叠加（选择时游戏暂停）", 18, TextAnchor.UpperCenter, f, 0, -64).color = new Color(0.7f, 0.7f, 0.8f);

        float[] xs = { -245f, 0f, 245f };
        for (int i = 0; i < 3; i++)
            ui.runeButtons[i] = CreateRuneButton(ui, ui.runePanel.transform, f, new Vector2(xs[i], -42f), i);

        ui.runePanel.SetActive(false);
    }

    Button CreateRuneButton(UIManager ui, Transform parent, Font font, Vector2 position, int index)
    {
        var buttonObject = new GameObject("RuneButton" + index);
        buttonObject.transform.SetParent(parent, false);
        var rt = buttonObject.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(220, 220);
        rt.anchoredPosition = position;
        var button = buttonObject.AddComponent<Button>();
        var image = buttonObject.AddComponent<Image>();
        image.raycastTarget = true;
        image.color = new Color(0.16f, 0.16f, 0.26f, 0.98f);
        button.targetGraphic = image;
        int captured = index;
        button.onClick.AddListener(() => UIManager.Instance?.SelectRune(captured));
        var label = T(buttonObject.transform, "", 21, TextAnchor.MiddleCenter, font, 0, 0);
        var labelRt = label.rectTransform;
        labelRt.sizeDelta = new Vector2(200, 195);
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        ui.runeTexts[index] = label;
        return button;
    }

    void BuildInventoryPanel(UIManager ui, Transform ct, Font f)
    {
        ui.inventoryPanel = new GameObject("InventoryPanel");
        ui.inventoryPanel.transform.SetParent(ct, false);
        var rt = ui.inventoryPanel.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(770, 430);
        rt.anchoredPosition = Vector2.zero;
        var image = ui.inventoryPanel.AddComponent<Image>();
        image.color = new Color(0.06f, 0.06f, 0.10f, 0.97f);
        image.raycastTarget = true;

        T(ui.inventoryPanel.transform, "背包  (I 关闭)", 28, TextAnchor.UpperCenter, f, 0, -20).color = Color.white;
        ui.inventoryGoldText = T(ui.inventoryPanel.transform, "金币：0 G", 24, TextAnchor.UpperCenter, f, 0, -60);
        ui.inventoryGoldText.color = new Color(0.95f, 0.8f, 0.25f);

        float[] xs = { -275f, -165f, -55f, 55f, 165f, 275f };
        for (int i = 0; i < ItemCatalog.AllItems.Length && i < xs.Length; i++)
            CreateInventorySlot(ui, ui.inventoryPanel.transform, f, new Vector2(xs[i], 26f), i);

        ui.inventoryHintText = T(ui.inventoryPanel.transform, "", 17, TextAnchor.LowerCenter, f, 0, 42);
        ui.inventoryHintText.color = new Color(0.65f, 0.68f, 0.78f);

        ui.inventoryPanel.SetActive(false);
    }

    void CreateInventorySlot(UIManager ui, Transform parent, Font font, Vector2 position, int index)
    {
        var kind = ItemCatalog.AllItems[index];

        var slotObject = new GameObject("Slot" + index);
        slotObject.transform.SetParent(parent, false);
        var rt = slotObject.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(98, 98);
        rt.anchoredPosition = position;
        var frame = slotObject.AddComponent<Image>();
        frame.color = new Color(0.15f, 0.15f, 0.22f, 1f);
        frame.raycastTarget = true;                       // 悬停靠它接收指针事件
        slotObject.AddComponent<InventorySlotUI>().kind = kind;

        var iconObject = new GameObject("Icon");
        iconObject.transform.SetParent(slotObject.transform, false);
        var iconRt = iconObject.AddComponent<RectTransform>();
        iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.sizeDelta = new Vector2(54, 54);
        iconRt.anchoredPosition = new Vector2(0, 12);
        var icon = iconObject.AddComponent<Image>();
        icon.sprite = PixelSpriteGenerator.GetSprite(ItemCatalog.SpriteName(kind));
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        ui.inventoryIcons[index] = icon;

        var count = T(slotObject.transform, "x0", 17, TextAnchor.LowerCenter, font, 0, 8);
        count.rectTransform.sizeDelta = new Vector2(92, 22);
        count.raycastTarget = false;
        ui.inventoryCountTexts[index] = count;

        var name = T(slotObject.transform, ItemCatalog.DisplayName(kind), 16, TextAnchor.LowerCenter, font, 0, 104);
        name.rectTransform.sizeDelta = new Vector2(112, 22);
        name.color = ItemCatalog.Tint(kind);
    }

    void BuildTooltip(UIManager ui, Transform ct, Font f)
    {
        ui.tooltipPanel = new GameObject("ItemTooltip");
        ui.tooltipPanel.transform.SetParent(ct, false);
        var rt = ui.tooltipPanel.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(328, 100);
        rt.anchoredPosition = Vector2.zero;
        var image = ui.tooltipPanel.AddComponent<Image>();
        image.color = new Color(0.04f, 0.04f, 0.08f, 0.98f);
        image.raycastTarget = false;                      // 不能吃掉格子上的指针事件

        ui.tooltipTitleText = T(ui.tooltipPanel.transform, "", 20, TextAnchor.UpperLeft, f, 14, -12);
        ui.tooltipTitleText.rectTransform.sizeDelta = new Vector2(300, 26);
        ui.tooltipBodyText = T(ui.tooltipPanel.transform, "", 16, TextAnchor.UpperLeft, f, 14, -42);
        ui.tooltipBodyText.rectTransform.sizeDelta = new Vector2(300, 46);
        ui.tooltipBodyText.color = new Color(0.85f, 0.88f, 0.95f);

        ui.tooltipPanel.SetActive(false);
    }

    Button CreateShopButton(Transform parent, Font font, string label, Vector2 position, int width, int height, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject("ShopButton");
        buttonObject.transform.SetParent(parent, false);
        var rt = buttonObject.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = position;
        var button = buttonObject.AddComponent<Button>();
        var image = buttonObject.AddComponent<Image>();
        image.raycastTarget = true;
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        var text = T(buttonObject.transform, label, 20, TextAnchor.MiddleCenter, font, 0, 0);
        text.color = Color.white;
        text.raycastTarget = false;
        return button;
    }
}
