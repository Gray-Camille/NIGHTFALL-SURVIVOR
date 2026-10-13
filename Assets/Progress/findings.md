# Findings & Decisions

## Requirements
- 2D 俯视角像素生存游戏
- 玩家 WASD 移动，自动攻击最近敌人
- 敌人波次递增从屏幕外刷出
- 击杀掉落 XP，升级随机属性加成
- 死亡后可 R 键重新开始
- 所有素材程序化生成，无外部依赖

## Technical Research

### 精灵生成
- Unity Texture2D 可逐像素构建 Sprite
- filterMode = Point 保持像素锐利
- 16×16 分辨率适合复古像素风格
- 调色板预定义 Color 常量，风格统一

### 场景启动
- `RuntimeInitializeOnLoadMethod(AfterSceneLoad)` 可自动初始化
- 但旧 SceneSetup.cs 的 ExecuteInEditMode 对象会持久化到场景
- DestroyImmediate 在 Start 中同步清理最可靠
- 协程中 Destroy + WaitForEndOfFrame 会留给旧对象 Update 窗口

### 实体管理
- Unity Prefab 系统在场景序列化/反序列化时可能丢失组件
- 直接 `new GameObject()` + `AddComponent<T>()` 保证组件齐全
- 避免使用 Tag 系统（需要 TagManager 预定义）
- 使用 Singleton 模式 + FindObjectsOfType 替代

### 敌人与成长
- 敌人类型差异通过血量、速度、伤害、XP、射程和行为分支表达，而不是只换外观
- Shooter 在保持距离后发射 `EnemyProjectile`，把敌人的“攻击”与碰撞伤害区分开
- 敌人难度缩放集中在 `GameManager.SpawnEnemy`，避免 `Enemy.Start` 对已配置数值再叠加一次
- 防御力使用“减去护甲但最低 1 点”的公式，提升生存能力同时避免绝对免伤
- 精英用“解锁波次 + 随机间隔”制造不确定性；Boss 用固定大间隔保证稀有性与节律
- 掉落按敌人类别分层：小怪掉少量金币，精英掉道具，Boss 掉高额奖励，避免掉落价值与难度错位
- `PickupDrop` 统一金币和道具的吸取逻辑，即时道具直接修改玩家属性并刷新 HUD
- 敌人防御应放在 `Enemy.TakeDamage`，与玩家攻击伤害分离，并保留最低 1 点伤害
- 波次成长用每波 +4% 的生命缩放、伤害开方缩放和每 3 波 +1 防御，压力稳定提升而不瞬间失控
- 程序化图案精灵比通用色块更能表达职业/体型；生成器缓存需要在新图案调试时清理
- 精英/Boss 用世界空间 Sprite 血条即可满足运行时反馈，不需要额外 UI Canvas
- 多样攻击通过“类型 + 特殊冷却 + 模式切换”实现，避免每个敌人都写独立更新循环
- 大地图优先使用一张大纹理绘制区块，而不是生成数千个 Tile GameObject

### 摄像机
- 正交摄像机 orthographicSize = 5 适合 16:9 屏幕
- 动态钳制公式: clamp = floorBound - orthoSize * aspect
- 玩家边界 ±19.5 接近地板边缘，摄像机自动停止不超界

### UI 系统
- Canvas ScreenSpaceOverlay + CanvasScaler
- 面板需 SetAsFirstSibling 避免遮盖文字
- `LegacyRuntime.ttf` 在 Unity 2022 可能为 null，回退 Arial 系统字体
- 锚点：LowerLeft → Vector2.zero，LowerRight → (1,0)，之前错误映射到 Vector2.up/(1,1)
- HUD 文本必须在创建后赋给 `UIManager` 字段；否则对象存在但 UI 数据不会刷新
- 本项目 HUD 使用 Legacy `UnityEngine.UI.Text`，验证时不要误用 `TMPro.TextMeshProUGUI`
- Unity 摄像机渲染截图通常不包含 Screen Space Overlay；验证 Overlay UI 需检查组件状态、RectTransform 或使用 UI 渲染流程

## Technical Decisions
| Decision | Rationale |
|----------|-----------|
| 直接 new GameObject 生成实体 | 避免 Prefab 序列化/组件丢失 |
| DestroyImmediate 同步清理 | 杜绝旧对象帧间 Update 报错 |
| 动态摄像机钳制 | 精确控制不显示地板边界外 |
| 程序化精灵生成 | 无外部素材依赖 |
| orthographicSize = 5 | 适中的视野和像素表现 |
| 按波次概率解锁新敌人 | 保持基础压力的同时逐步引入更强敌人 |
| 固定升级攻击/防御 + 随机额外强化 | 保证角色成长方向可见，同时保留随机变化 |

## Visual/Browser Findings
- 调试红色大字 "CAN YOU SEE THIS?" 成功渲染 → Canvas 系统正常工作
- HUD 数值文本已修复：创建后绑定 `UIManager` 字段，Play Mode 可读到 `HP: 当前/最大`、`XP: 当前/所需`、`Lv.N`、`DEF`
- Gold HUD 和事件提示使用 Legacy Text；精英/Boss 出现时通过 `UIManager.ShowAnnouncement` 提供可读反馈
- 玩家移动、敌人追踪、弹幕、经验吸取核心逻辑已运行正常

## Resources
- Unity 2022.3.57f1c2 API 文档
- Texture2D.SetPixels / Sprite.Create 文档
- RuntimeInitializeOnLoadMethod 生命周期
- CameraFollow 动态钳制计算


### Text Pivot 与重叠
- Text 的 RectTransform pivot 默认 (0.5, 0.5)；当两个 Text 锚点相同且位置接近时，pivot 会影响视觉重叠方向，需显式设置 pivot 为 (0, 0.5) 靠左对齐

### 精灵命名映射
- C# 枚举 ToString() 保留 PascalCase，而精灵缓存键使用 snake_case（如 elite_knight）；必须用显式映射而非 ToString().ToLower()，否则命中 fallback 色块
- PixelSpriteGenerator.GetEnemySprite(Enemy.EnemyType) 是唯一入口，Enemy.cs 和 GameManager.cs 均已统一调用

### 暂停机制
- Time.timeScale = 0 仅冻结使用 Time.deltaTime 的物理移动；需额外 bool 门控阻止攻击发射、冲刺计时器等逻辑
- GameManager.InitGame 必须重置 timeScale = 1 并调用 CloseShop()，防止死亡重启后残留暂停

### 启动性能
- RuntimeInitializeOnLoadMethod 中的 DestroyImmediate 全场景循环和 Texture2D 大尺寸分配是启动卡顿主因；删除循环并将地图纹理分辨率减半后首次点击无卡顿

*Update this file after every 2 view/browser/search operations*


### Phase 11 关键发现
- **UGUI 必须存在 EventSystem**：项目早期全靠键盘输入，场景里从未创建 EventSystem，导致商店 Button 收不到任何点击（`EventSystem.current == null`）。修复方式：SceneBootstrap.EnsureEventSystem() 在构建场景时检查并补建 EventSystem + StandaloneInputModule。
- **程序化精灵的字符串键必须严格匹配 case 表**：case 表用下划线键（health_potion / gold_coin），而 `kind.ToString().ToLowerInvariant()` 产出无下划线（healthpotion），永远落入 fallback 洋红块。枚举→精灵键必须走显式映射方法（GetItemSpriteName / GetEnemySprite），不要依赖 ToString 约定。
- **点 Play 卡顿的主因是编辑器域重载**：实测 BuildAll 冷启动仅约 30ms（25 个精灵 29.4ms + 160×160 地图纹理 2.9ms），说明肉眼可见的卡顿来自进入 Play Mode 时的 domain reload，而非游戏初始化。通过 UnityEditor.EditorSettings.enterPlayModeOptionsEnabled + DisableDomainReload（只关域重载、保留场景重载）解决。
- **关域重载后静态缓存必须在构建时清空**：没有域重载意味着静态字段跨 Play 保留，旧 Texture2D/Sprite 已被销毁而引用仍在，故 BuildAll 开头调用 PixelSpriteGenerator.ClearCache() 强制重建。
- **暂停要有一个唯一真源**：UIManager.IsModalOpen（商店 || 符文面板）作为唯一判定，Time.timeScale 0/1，并在 Enemy / PlayerController / RuneAttacks 三处设置逻辑门，避免只暂停部分系统。
- **符文节奏参数**：PlayerController.levelsPerRune = 3，即第 3/6/9… 级各给一次 3 选 1；复活符文在玩家已有复活次数时从候选池移除（随机不重复抽取）。
- **本仓库文本文件为 LF 换行**：用 PowerShell 逐行改文件时按 "`n" 切分，不要用 "`r`n"，否则会把整份文件当成一行。

### Phase 12 关键发现
- **“只能选一次”必须同时作用于抽取与显示**：一次性符文（不屈/无敌）不仅要在 RollOptions 阶段从候选池剔除（否则会出现选了却没效果的选项），还要在 UI 卡片标注「（仅一次）」，否则玩家会误以为可以叠加。统一由 RuneSystem.IsOneTimeOnly(id) 判定，避免两处逻辑漂移。
- **“第二次起叠加伤害”的判定要用获取前的层数**：Apply 中必须先取 already = GetRuneStacks(id) 再 AddRuneStack(id)，用 already≥1 表示这是第二次及以后；顺序写反会让第一次获取就被误判为叠加。
- **长无敌与受击无敌必须共用同一个计时器**：GrantInvincibility 只延长不缩短（if (duration > _invincibleTimer)），否则 6s 周期的 1.5s 无敌会被 0.3s 的受击无敌覆盖而形同虚设；视觉上用时长阈值（>0.4s）区分“符文无敌（呼吸脉冲、保持可见）”与“受击无敌（闪烁）”。
- **升级移速成长靠“重建 PlayerController”实现重开重置**：+0.12 直接写进实例字段，重开时旧实例被 Destroy 并新建，天然回到初始速度，无需额外记录基础值；上限判定放在自增之后（if (level <= moveSpeedBonusMaxLevel)）。

### Phase 13 关键发现
- **存档键不要用枚举的整数序号**：道具背包用 `PlayerPrefs` 存，键名走 `"PS_Item_" + kind.ToString()`（枚举名）而不是 `(int)kind`。本轮 ItemKind 正好被重构过（删掉 PowerCrystal/ArmorPlate/SwiftCharm、新增 4 个限时道具），若用序号存旧档会把"狂暴药剂"读成"装甲板"，名字键天然免疫顺序变动。
- **"拾取 ≠ 立即生效"**：本轮把掉落道具改成进背包（`AddItem`），使用时才由 `UseItem` 施加限时增益。这让道具能跨局携带（存档只需要存"数量"），也让玩家可在 Boss 波前主动嗑药，比 Vampire Survivors 的自动生效更适合做存档。
- **限时增益统一走"乘区 + 计时器"而不是直接改基础值**：`ItemAttackMultiplier / ItemMoveMultiplier / PickupRangeMultiplier` 是计算属性（`timer>0 ? 加成 : 1`），`EffectiveArmor = armor + (ironShieldTimer>0 ? 8 : 0)`。直接改 `armor/moveSpeed` 字段会在 buff 到期时无法还原，而且会和"升级永久加成"互相污染。
- **重开继承 vs 重置要分层**：PlayerController 实例字段 = 局内状态（等级、移速、HP，随实例销毁而重置）；`GamePersistence`（PlayerPrefs）= 跨局资产（金币、背包道具）。`RestartGame` 里删掉 `UpdateGoldUI(0)`，并让新实例在 `Start` 调 `LoadPersistentProgress()`，即天然得到"继承金币道具、重置等级移速"。
- **模态暂停要覆盖输入层而不只是时间层**：`Time.timeScale=0` 只停 `Time.deltaTime` 驱动的移动与攻击；`Input.GetKeyDown` 在暂停时**照常触发**。因此 `ToggleShop` 需要显式 `if (IsRunePanelOpen) return;` 防止在符文面板上再叠一层商店，否则关闭商店会误把 timeScale 拉回 1 而符文面板仍开着。
- **编辑器内探查字段名要先 grep**：本项目金币字段是 `PlayerController.currentGold`（不在 GameManager），防御是 `armor`（初始 0，升级 +1）而非 `baseArmor`。用 `execute_code` 探查前先 `rg` 确认，否则会得到 "does not contain a definition" 的编译错误，且 CodeDom 的报错会级联污染后续所有 `StringBuilder.Append` 行，看起来像多个错误实际上只有一个。
### Phase 14 关键发现
- **背包容量必须由一张元数据表统一供给**：MaxStack / DisplayName / Description / HotkeyIndex / SpriteName / Tint 全部收进 ItemCatalog 静态表，SceneBootstrap 建 UI、UIManager 刷新格子与提示框、PlayerController 判定上限三处共用同一份数据，避免「格子里写 9、实际能囤 12」这类漂移。新增道具只要往 AllItems 里加一项、在 switch 里补一条，三处 UI 自动跟上。
- **悬停提示不该长在格子自己的逻辑里**：格子挂 InventorySlotUI（IPointerEnter/Exit）只负责「通知」，文案拼装、面板尺寸与定位、显隐全部交给 UIManager.ShowItemTooltip(kind)。这样同一份提示框可以复用给未来的商店/结算界面，格子脚本保持在 20 行以内。
- **存档槽位用「键前缀」实现，而不是多份 PlayerPrefs 分区**：所有键都带 "PS_<slot>_" 前缀（GoldKey/ItemKey/SummaryKey/ExistsKey），SelectSlot 只切一个静态 _currentSlot，读写路径完全不变，Save/Load 不需要知道槽位存在。旧档迁移只是把无前缀的旧键复制到 1 号槽再删除旧键，并用 PS_LegacyMigrated 保证只做一次。
- **「空档=新游戏 / 有档=覆盖」必须在进入游戏前判定**：SlotPanel 按钮回调里，开始游戏模式下 SlotExists 为真时直接 CreateOrOverwrite，即「覆盖存档开新档」；继续游戏模式下空档按钮 interactable=false，从源头杜绝读出全 0 的假存档。
- **主菜单要求「开局不刷怪」**：GameManager.InitGame 末尾改为 ReturnToMenu(false)，即构建完场景就停在主菜单，只有 StartRun 才 SpawnPlayer + SpawnEnemySpawner。副作用是暂停判定必须把主菜单/存档面板也算作模态，否则主菜单背后怪物照常活动。
- **模态互斥要做成双向**：ToggleInventory 里若商店正开就关商店，ToggleShop 里若背包正开就关背包，两者又都不能盖在符文面板上。统一由 IsModalOpen 汇总成暂停真源，Esc 走 CloseTopModal 逐层关闭，才不会出现「关了背包 timeScale 却被别人拉回 1」的竞态。
### Phase 15 关键发现
- **主菜单必须是独立场景，而不是 in-scene 面板**：菜单与游戏在同一场景时，暂停判定要把「主菜单/存档/说明」也算模态，否则菜单背后怪物照样活动（Phase 14 的补丁方向）。独立场景后，`SceneBootstrap.OnSceneLoaded` 只要在主菜单场景直接 return 即可，游戏侧代码完全不必知道菜单长什么样；菜单侧由 `MainMenuBootstrap`（`RuntimeInitializeOnLoadMethod(AfterSceneLoad)`）在场景就绪后创建 UI，天然做到「谁的场景谁负责」。
- **跨场景交接用静态类而不是 `DontDestroyOnLoad`**：`MenuFlow.RequestStart(newGame, slot)` 把「新档 or 继续 + 槽位号」写进静态字段，`GameManager.InitGame` 用 `Consume` 取走并 `Clear`，避免把菜单状态下沉到 GameManager。域重载关闭时静态字段会跨 Play 残留，所以 `Consume` 必须顺便清空，否则上一局的「开始游戏」请求会污染下一次运行。
- **面板出屏的根因是 CanvasScaler 缺 referenceResolution**：只设 `ScaleWithScreenSize` 而没给 `referenceResolution` 时，Scaler 用默认 800×600 计算缩放，`matchWidthOrHeight` 也没打开，于是「按 1920 宽设计的面板」被放大到超出视口。补上 `referenceResolution=(1280,720)` + `MatchWidthOrHeight` + `match=0.5` 后，说明面板返回按钮与「继续游戏：选择一个存档位」标题都自动回到屏幕内——不需要逐个挪坐标。
- **悬停提示框要锚定到「格子」而不是鼠标**：`PlaceTooltip` 用 `GetWorldCorners` 取格子的下沿中心，再 `canvas.InverseTransformPoint` 转回画布局部坐标，水平与格子中心对齐、垂直贴下沿 -8px，最后 Clamp 在画布矩形内。跟着鼠标走会在格子边缘与屏幕底部来回跳，锚定格子后位置稳定且永不遮挡格子本身。
- **「不跨存档」要靠写入时机而不是读取时机**：金币原本只在 `GamePersistence.Save`（局末/回菜单）落盘，中途切换槽位或崩溃就会把 A 档金币带进 B 档。改为 `AddGold` / 购买时立即 `SetBankGold(currentGold)` 写回当前槽位，写入与「当前槽位」始终同一时刻绑定，隔离才真正成立。

### Phase 15 热修关键发现
- **`RuntimeInitializeOnLoadMethod` 不能当「每次进场景都跑」的钩子用**：它只在游戏启动时执行一次（`AfterSceneLoad` 是「首个场景加载完成后」那一次）。用它做场景装配时，单场景项目不会有问题，一旦拆成「主菜单场景 + 游戏场景」就会暴露：第二次 `LoadScene` 没人接手，场景是空的（现象是「点存档没反应 / 游戏场景像没加载」）。正确做法是在 `BeforeSceneLoad` 里 `SceneManager.sceneLoaded += ...`，该事件每次场景加载后都会触发（包含第一个场景）。
- **事件订阅 + 一次性回调要一起用并去重**：保险起见同时保留 `AfterSceneLoad` 兜底（万一某个 Unity 版本/加载模式下首个场景不发 `sceneLoaded`），但两条路径会为同一场景各跑一次，所以处理器必须幂等——这里用「新场景的根对象里是否已经有 `___BootstrapRunner` / `MenuUI`」判断，比静态 bool 可靠（关闭域重载时静态 bool 会跨 Play 残留，反而会永久挡住后续装配）。
- **拆场景后「谁来搭游戏」必须由场景名决定**：两个 bootstrap 各自只看自己关心的场景名（`MenuFlow.MenuSceneName` / 否则为游戏场景），互不干涉。这样 `MainMenuScene` 里永远不会出现 GameManager，`MainScene` 里也不会出现菜单。
- **静态跨场景状态要在 Play 开始时清一次**：关闭域重载时 `MenuFlow.HasPending` 会跨 Play 残留，上一局如果在「已点存档、还没被 Consume」的瞬间退出，下一次按 Play 就会直接跳进游戏。在 `BeforeSceneLoad` 里 `MenuFlow.Clear()` 一行解决。
- **验证要覆盖「真点击」而不只是直接调方法**：直接调用 `OnSlotButtonClicked` 只能证明逻辑对不对，证明不了玩家点得到。用 `GraphicRaycaster.Raycast` + `RectTransformUtility.WorldToScreenPoint` 在按钮中心各投一次，确认 `results[0].gameObject` 就是按钮本身（不是被某个全屏遮罩吃掉），才算「按钮真的能点」。

### Phase 16 热修关键发现
- **同一个属性只能有一处写入者**：`PlaceTooltip` 已经按「格子锚点」把位置算对了，但 `Update()` 里还有个 `FollowMouse()` 每帧往同一个 `anchoredPosition` 写值——正确结果被每帧覆盖，于是「代码看起来是对的、效果却永远错」。排查这类「位置/尺寸总是差一点」的问题，第一步就是搜同一属性（`anchoredPosition` / `sizeDelta` / `localScale`）还有没有第二处写入者，而不是反复微调坐标系换算。
- **RectTransform 的坐标有两套，绝不能混用**：`ScreenPointToLocalPointInRectangle` 返回的是「以父矩形 pivot 为原点」的局部坐标，而 `anchoredPosition` 是「以 anchor 为原点、按 pivot 偏移」的坐标。两者只在父矩形 pivot 为 (0,0) 且 anchor 为 (0,0)、面板 pivot 为 (0,0) 时才数值相等。提示框面板的 anchor/pivot 是左下角 (0,1)，所以鼠标坐标写进去必然整体偏移一个「半宽 + 半高」的量级，表现为「跑到屏幕左下外侧」。
- **要「绝不出屏」就把翻转和 Clamp 都做上**：只做 Clamp 会把提示框压在屏幕底边、盖住格子本身；只做翻转在极端缩放/超长描述下仍可能溢出。先按「下方放得下就放下方，否则翻到上方」选边，再用 `Mathf.Clamp` 收进画布矩形，两个都用，任何分辨率/任意描述长度都不会出问题。第 3 条 Phase 15 的「锚定格子」结论只解决了「跟鼠标乱跳」，本轮才补上「纵向越界」这一半。

### Phase 17 热修关键发现
- **锚点与 pivot 必须成对设置**：`anchorMin/anchorMax` 决定「矩形上的哪一点钉在父矩形的哪个位置」，`pivot` 决定「矩形自身的哪一点算作它的坐标原点」，两者只在都是 (0.5,0.5) 时才恰好一致。凡是按「角锚点 + 偏移」书写的布局（UpperLeft + (14,-12) 想表达的是「距左上角右 14、下 12」），pivot 就必须是同一个角，否则整块矩形偏移半个宽/半个高——框越宽、字号越大，偏得越离谱。
- **helper 的缺陷会被逐个绕过，但不会自己消失**：这个 bug 存在很久了，`hpText` / `xpText` / `lvText` / 商店描述 / 说明正文这 5 处各自在 `T()` 返回后又手动重设了一次 `pivot`，所以「看起来是好的」；剩下没绕过的 6 个 HUD 文本 + 提示框文本就一直错着。排查这类「只有部分界面错位」的问题，先数「有多少调用点在 helper 之外又改了同一个属性」，通常就能反推出 helper 本身缺了什么。
- **改公共 helper 前要枚举全部调用点，改完要全量复测**：修 `T()` 会同时移动所有角/边锚点的文本，所以先用 `rg "TextAnchor\.\w+"` 列出全部调用点，确认「已手动设过 pivot 的位置不受影响、其余位置移动后正好落到作者本意」，再动手；验证时逐个界面测量，不能抽查一个就算过。
- **验证 UI 布局不必启动一局游戏**：`GameManager.InitGame()` 在没有 `MenuFlow` 待处理请求时会直接 `LoadScene(MainMenuScene)`（源码里有明确注释），所以「打开 MainScene 直接按 Play」是进不了游戏的，会立刻被送回菜单。为了不覆盖玩家存档（新开一局会写 `ExistsKey/GoldKey/SummaryKey`），可用反射只调 `BootstrapRunner.BuildUI()` 跳过 `BuildAll()` 里的 GameManager，就能在任意场景里量到真实的 UI 布局。注意用完要立刻 `DestroyImmediate` 掉临时 runner——否则它的 `Start()` 会跑 `BuildAll()` 建出 GameManager，随即被 `LoadScene` 送回主菜单，刚建好的 UI 一起被销毁（现象是下一次调用 `FindObjectOfType<UIManager>` 返回 null）。

### Phase 18 功能关键发现
- **「按键不生效」往往是被它前面的一行 return 挡住的**：`R`（重开）和 `M`（回主菜单）确实写在 `Update()` 里，但前面有 `if (!IsGameOver) return;`，于是这两个键只在死亡结算界面生效，而帮助文本一直宣称局内可用。排查「某个快捷键没用」时，先找同一方法里排在它之前的分支与 return，再怀疑 `Input` 配置。
- **暂停要复用「唯一真源」，不要各处散写 `Time.timeScale`**：`UIManager.ApplyPauseState()` 是全项目唯一决定 `timeScale` 的地方，判据就是 `IsModalOpen`。新面板只要挂进 `IsModalOpen`，`Enemy` / `PlayerController` / `RuneAttacks` 三处的 `IsModalOpen` 判定会同时生效，不用逐个实体改。这也解释了「暂停后怪物还在动」这类问题的最佳修法是补进 `IsModalOpen`，而不是给每个实体加 `if (paused) return`。
- **新的模态面板必须同时接入三处**：`IsModalOpen`（暂停）、`CloseTopModal()`（Esc 逐层关闭）、`HideAllMenus()`（开局 / 重开时收起）。漏掉任何一处都会留下破窗——要么 Esc 关不掉，要么 `RestartGame` 之后面板还挂在屏幕上。
- **验证「暂停」要看实体坐标，不能只看 `timeScale` 数值**：`timeScale=0` 只证明赋值成功，实体是否真停住得看坐标。取一只敌人的 `transform.position`，隔约 7 秒（跨两次工具调用、约 1800 帧）再取一次，坐标完全一致才算过——顺带也确认了波次计时确实冻住。
- **测试存档相关功能先在空槽里做**：先用 `GamePersistence.SlotExists` / `GetSlotGold` 读出三个槽的占用情况，挑一个空槽（本轮是 3 号）跑完整端到端流程，测完 `ClearSlot(3)` 还原，并复核三个槽的数值都回到测试前。这样既拿到真实链路（真的切了场景、真的写了存档）的证据，又完全没动玩家已有存档。
### Phase 19 关键发现
- **PlayerPrefs 只在 `Save()` 那一刻落盘**：`SetInt` / `DeleteKey` 只改内存字典，所以「运行态读得到、注册表里没有」是正常现象。清存档必须走一次真正在 Unity 里执行并 `Save()` 的调用（本轮用现成的 `GamePersistence.ClearAll()`）；单纯删注册表项既不完整（内存里的值还在），也不必要（任何一次 `Save()` 都会把内存整份写回）。
- **不要用 `PlayerPrefs.DeleteAll()` 清存档**：它会连 Unity 自己的 `Screenmanager *`（分辨率 / 全屏 / 窗口位置 / 选中显示器）一起删掉，玩家的显示设置被重置。项目里已有按槽删除的 `ClearSlot` / `ClearAll`，范围精确，优先用它。
- **停播也可能回写存档**：`GameManager.OnApplicationQuit` 会 `Save(PlayerController)`，所以「清空 -> 退出 Play Mode -> 再复核」这个顺序有必要；停播后确认一遍三槽仍为空，才算真的干净。
- **改 `productName` 等于换存档位置**：存档键位于 `HKCU\Software\<companyName>\<productName>`，打包前若把 `PixelSurvivor` 改成别的名字，测试期积累的存档不会再被读到（本轮已清空，所以现在改是零成本）。

