using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 主菜单（独立场景）的界面：标题 + 屏幕正中的按钮 + 存档选择 + 游戏说明。
/// 与游戏内的 UIManager 完全分离——菜单场景里不存在任何战斗系统、玩家与 HUD。
/// </summary>
public class MenuUI : MonoBehaviour
{
    public static MenuUI Instance;

    private Font _font;
    private GameObject _mainPanel, _slotPanel, _helpPanel;
    private Text _slotTitle;
    private readonly Button[] _slotButtons = new Button[3];
    private readonly Text[] _slotLabels = new Text[3];
    private Button _continueButton;
    private bool _slotForNewGame;

    void Awake()
    {
        Instance = this;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        EnsureCamera();
        EnsureEventSystem();
        Build();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        // Esc 逐层退回：说明 / 存档选择 -> 主菜单
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (_helpPanel != null && _helpPanel.activeSelf) HideHelp();
        else if (_slotPanel != null && _slotPanel.activeSelf) ShowMainMenu();
    }

    void EnsureCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var camObject = new GameObject("MainCamera");
            camObject.tag = "MainCamera";
            camObject.transform.position = new Vector3(0, 0, -10);
            cam = camObject.AddComponent<Camera>();
        }
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.05f);
    }

    static void EnsureEventSystem()
    {
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    void Build()
    {
        var canvasObject = new GameObject("MenuCanvas");
        canvasObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        Transform ct = canvasObject.transform;

        MenuBackground.Build(ct);      // 最底层：夜色渐变 + 星空 + 远景剪影
        BuildMainPanel(ct);
        BuildSlotPanel(ct);
        BuildHelpPanel(ct);
        ShowMainMenu();
    }

    // ===== 主菜单 =====
    void BuildMainPanel(Transform ct)
    {
        _mainPanel = new GameObject("MainMenuPanel");
        _mainPanel.transform.SetParent(ct, false);
        var rt = _mainPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        var img = _mainPanel.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.18f);   // 只轻微压暗，露出背景
        img.raycastTarget = true;

        var title = T(_mainPanel.transform, "永夜幸存者", 66, TextAnchor.MiddleCenter, 0, 150);
        title.color = new Color(0.96f, 0.95f, 1f);
        T(_mainPanel.transform, "N I G H T F A L L   S U R V I V O R S", 18, TextAnchor.MiddleCenter, 0, 92)
            .color = new Color(0.56f, 0.62f, 0.85f);
        Bar(_mainPanel.transform, new Color(0.5f, 0.55f, 0.8f, 0.35f), new Vector2(420, 2), new Vector2(0, 62));

        var startButton = MakeButton(_mainPanel.transform, "开 始 游 戏", new Vector2(0, 44), new Color(0.22f, 0.5f, 0.32f));
        startButton.onClick.AddListener(() => ShowSlotSelect(true));
        _continueButton = MakeButton(_mainPanel.transform, "继 续 游 戏", new Vector2(0, -24), new Color(0.24f, 0.4f, 0.62f));
        _continueButton.onClick.AddListener(() => ShowSlotSelect(false));
        var helpButton = MakeButton(_mainPanel.transform, "游 戏 说 明", new Vector2(0, -92), new Color(0.45f, 0.4f, 0.22f));
        helpButton.onClick.AddListener(ShowHelp);
        var quitButton = MakeButton(_mainPanel.transform, "退 出 游 戏", new Vector2(0, -160), new Color(0.5f, 0.26f, 0.26f));
        quitButton.onClick.AddListener(QuitGame);

        _mainPanel.SetActive(false);
    }

    // ===== 存档选择 =====
    void BuildSlotPanel(Transform ct)
    {
        _slotPanel = Panel(ct, new Color(0.05f, 0.05f, 0.1f, 0.97f), new Vector2(760, 450));
        _slotTitle = T(_slotPanel.transform, "选择存档位", 30, TextAnchor.UpperCenter, 0, -26);
        _slotTitle.rectTransform.sizeDelta = new Vector2(720, 42);

        for (int i = 0; i < _slotButtons.Length; i++)
        {
            int slot = i + 1;
            var button = MakeButton(_slotPanel.transform, "", new Vector2(0, 88 - i * 92), new Color(0.16f, 0.17f, 0.25f), 660, 88);
            button.onClick.AddListener(() => OnSlotButtonClicked(slot));
            var label = T(button.transform, "", 18, TextAnchor.MiddleCenter, 0, 0);
            label.rectTransform.sizeDelta = new Vector2(630, 82);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            _slotButtons[i] = button;
            _slotLabels[i] = label;
        }

        var backButton = MakeButton(_slotPanel.transform, "返 回", new Vector2(0, -168), new Color(0.3f, 0.3f, 0.38f), 200, 46);
        backButton.onClick.AddListener(ShowMainMenu);
        _slotPanel.SetActive(false);
    }

    // ===== 游戏说明 =====
    void BuildHelpPanel(Transform ct)
    {
        _helpPanel = Panel(ct, new Color(0.05f, 0.05f, 0.1f, 0.98f), new Vector2(840, 560));
        var title = T(_helpPanel.transform, "游戏说明", 32, TextAnchor.UpperCenter, 0, -26);
        title.rectTransform.sizeDelta = new Vector2(800, 44);

        string help =
            "移动 WASD        攻击 自动攻击最近的敌人        暂停 / 关闭面板 Esc\n" +
            "商店 B        背包 I        使用道具 1 ~ 6        重开本局 R        返回主菜单 M\n" +
            "\n" +
            "· 击杀敌人获得经验，升级后弹出符文 3 选 1；开局也会先送一次符文\n" +
            "· 符文可以反复获取并叠加，只有「不屈」「无敌」只能选一次\n" +
            "· 精英怪在第 6 波之后随机每 3 或 5 波出现一次；BOSS 每 15 波出现一次\n" +
            "· 击杀精英怪回复 25% 生命，击杀 BOSS 回复 50% 生命\n" +
            "· 小怪掉落金币与限时道具，精英怪与 BOSS 掉落更多\n" +
            "· 金币与背包道具只保存在当前存档里，换存档互不影响；等级与移速每局重置\n" +
            "· 背包每种道具有不同上限，把鼠标移到格子上可以查看效果\n" +
            "· 商店用金币购买血瓶与攻击药剂：血瓶按 1 回血，攻击药剂按 2 永久加攻击";

        var body = T(_helpPanel.transform, help, 18, TextAnchor.UpperLeft, 0, -84);
        var bodyRt = body.rectTransform;
        bodyRt.anchorMin = bodyRt.anchorMax = new Vector2(0.5f, 1f);
        bodyRt.pivot = new Vector2(0.5f, 1f);
        bodyRt.sizeDelta = new Vector2(760, 380);
        bodyRt.anchoredPosition = new Vector2(0, -84);
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        body.verticalOverflow = VerticalWrapMode.Overflow;
        body.lineSpacing = 1.25f;
        body.color = new Color(0.88f, 0.9f, 0.96f);

        var backButton = MakeButton(_helpPanel.transform, "返 回", new Vector2(0, -236), new Color(0.3f, 0.3f, 0.38f), 200, 46);
        backButton.onClick.AddListener(HideHelp);
        _helpPanel.SetActive(false);
    }

    // ===== 界面切换 =====
    public void ShowMainMenu()
    {
        if (_slotPanel != null) _slotPanel.SetActive(false);
        if (_helpPanel != null) _helpPanel.SetActive(false);
        if (_mainPanel != null) _mainPanel.SetActive(true);
        if (_continueButton != null) _continueButton.interactable = HasAnySave();
    }

    public static bool HasAnySave()
    {
        for (int slot = 1; slot <= GamePersistence.SlotCount; slot++)
            if (GamePersistence.SlotExists(slot)) return true;
        return false;
    }

    /// <summary>打开存档选择：forNewGame = true 时是「开始游戏」，false 时是「继续游戏」。</summary>
    public void ShowSlotSelect(bool forNewGame)
    {
        _slotForNewGame = forNewGame;
        if (_mainPanel != null) _mainPanel.SetActive(false);
        if (_helpPanel != null) _helpPanel.SetActive(false);
        if (_slotTitle != null) _slotTitle.text = forNewGame ? "开始游戏：选择一个存档位" : "继续游戏：选择一个存档位";
        RefreshSlotButtons();
        if (_slotPanel != null) _slotPanel.SetActive(true);
    }

    void RefreshSlotButtons()
    {
        for (int i = 0; i < _slotButtons.Length; i++)
        {
            int slot = i + 1;
            bool exists = GamePersistence.SlotExists(slot);
            if (_slotButtons[i] != null) _slotButtons[i].interactable = _slotForNewGame || exists;
            if (_slotLabels[i] == null) continue;

            if (!exists)
            {
                _slotLabels[i].text = "存档 " + slot + "\n空存档 — 点击开始新游戏";
                _slotLabels[i].color = new Color(0.62f, 0.64f, 0.72f);
            }
            else
            {
                string head = "存档 " + slot + "   金币 " + GamePersistence.GetSlotGold(slot) + " G   道具 " + GamePersistence.GetSlotItemTotal(slot) + " 件";
                string body = GamePersistence.GetSlotSummary(slot);
                string tail = _slotForNewGame ? "\n点击将覆盖此存档并重新开始" : "\n点击继续这一局";
                _slotLabels[i].text = head + "\n" + body + tail;
                _slotLabels[i].color = _slotForNewGame ? new Color(1f, 0.82f, 0.55f) : Color.white;
            }
        }
    }

    /// <summary>存档槽按钮回调：登记开局请求后切到游戏场景。</summary>
    public void OnSlotButtonClicked(int slot)
    {
        if (!_slotForNewGame && !GamePersistence.SlotExists(slot)) return;
        MenuFlow.RequestStart(_slotForNewGame, slot);
        SceneManager.LoadScene(MenuFlow.GameSceneName);
    }

    public void ShowHelp()
    {
        if (_mainPanel != null) _mainPanel.SetActive(false);
        if (_slotPanel != null) _slotPanel.SetActive(false);
        if (_helpPanel != null) _helpPanel.SetActive(true);
    }

    public void HideHelp() { if (_helpPanel != null) _helpPanel.SetActive(false); ShowMainMenu(); }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ===== 小工具 =====
    Text T(Transform parent, string text, int size, TextAnchor anchor, float x, float y)
    {
        var go = new GameObject("T");
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<Text>();
        txt.text = text; txt.fontSize = size; txt.alignment = anchor;
        txt.color = Color.white; txt.font = _font; txt.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        Vector2 av = anchor == TextAnchor.UpperLeft ? Vector2.up :
                    anchor == TextAnchor.UpperRight ? Vector2.one :
                    anchor == TextAnchor.LowerLeft ? Vector2.zero :
                    anchor == TextAnchor.LowerRight ? new Vector2(1, 0) :
                    anchor == TextAnchor.MiddleLeft ? new Vector2(0, 0.5f) :
                    anchor == TextAnchor.MiddleRight ? new Vector2(1, 0.5f) :
                    anchor == TextAnchor.UpperCenter ? new Vector2(0.5f, 1) :
                    anchor == TextAnchor.LowerCenter ? new Vector2(0.5f, 0) : new Vector2(0.5f, 0.5f);
        rt.anchorMin = av; rt.anchorMax = av; rt.pivot = av;
        rt.sizeDelta = new Vector2(400, 40); rt.anchoredPosition = new Vector2(x, y);
        return txt;
    }

    Image Bar(Transform parent, Color color, Vector2 size, Vector2 position)
    {
        var go = new GameObject("Bar");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size; rt.anchoredPosition = position;
        var image = go.AddComponent<Image>();
        image.color = color; image.raycastTarget = false;
        return image;
    }

    GameObject Panel(Transform parent, Color color, Vector2 size)
    {
        var go = new GameObject("Panel");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size; rt.anchoredPosition = Vector2.zero;
        var image = go.AddComponent<Image>();
        image.color = color; image.raycastTarget = true;
        return go;
    }

    Button MakeButton(Transform parent, string label, Vector2 position, Color color, float width = 320f, float height = 62f)
    {
        var go = new GameObject("Button");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = position;
        var image = go.AddComponent<Image>();
        image.color = Color.white;            // 真实颜色放在 ColorBlock 里，点击反馈才正确
        image.raycastTarget = true;
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = new Color(Mathf.Min(1f, color.r + 0.16f), Mathf.Min(1f, color.g + 0.16f), Mathf.Min(1f, color.b + 0.16f));
        colors.pressedColor = new Color(color.r * 0.78f, color.g * 0.78f, color.b * 0.78f);
        colors.selectedColor = color;
        colors.disabledColor = new Color(0.24f, 0.24f, 0.28f, 0.7f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        var text = T(go.transform, label, 24, TextAnchor.MiddleCenter, 0, 0);
        text.rectTransform.sizeDelta = new Vector2(width - 20f, height - 8f);
        return button;
    }
}
