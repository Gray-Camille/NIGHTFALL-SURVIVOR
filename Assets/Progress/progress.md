# Progress Log

### Phase 19 收尾：清空存档 + 打包前检查
- **Status:** complete
- 需求：把 3 个存档槽全部清空，并把工程调整到可直接打包的状态（打包由用户自己执行）
- 发现：
  - 存档存在 **PlayerPrefs**（不是磁盘文件），落盘位置是注册表 `HKCU\Software\DefaultCompany\PixelSurvivor`，路径由 Player Settings 的 `companyName` / `productName` 决定
  - 全仓只有 `GamePersistence.cs` 使用 PlayerPrefs（`rg` 确认），清空范围封闭，不会漏
  - 清空前实测三槽：槽 1 / 2 只存在 `Exists=True`（金币 0、道具 0，早先那批 3 金币 / 1 道具并没有落盘）；注册表里当时就没有任何 `PS_*` 键
- 操作：在编辑器里调 `GamePersistence.ClearAll()`（内部对每槽 `ClearSlot` 后统一 `PlayerPrefs.Save()`），并 `SelectSlot(1)` 把静态槽位复位；**没有**用 `PlayerPrefs.DeleteAll()`（会连 Unity 的 `Screenmanager` 设置一起删）
- 验证：
  - 三槽 `SlotExists=False` / `GetSlotGold=0` / `GetSlotItemTotal=0`；`PS_S*_Exists` / `_BankGold` / `_Summary` / `_Item_*` 逐键 `PlayerPrefs.HasKey` 全为 False，`leftoverCount=0`
  - `reg query "HKCU\Software\DefaultCompany\PixelSurvivor" /s` 无任何 `PS_*` 残留（只剩 Unity 系统键）
  - 退出 Play Mode 后再 `ClearAll()` 复测一次（防停播时 `OnApplicationQuit -> Save` 把槽写回），结果仍全空
  - `read_console` 0 error
- 打包前检查（Build Settings）：`MainMenuScene`(buildIndex 0) -> `MainScene`(buildIndex 1)，两个场景均 `enabled: true`
- 备注：Player Settings 仍是 `DefaultCompany / PixelSurvivor`（version 1.0、960x540、FullScreenWindow、Mono2x）；公司名 / 产品名可由用户自行修改，但改 `productName` 会换掉存档所在的注册表键

### Phase 18 功能：局内返回主菜单（Esc 暂停菜单）
- **Status:** complete
- 症状：游戏中无法回主菜单 / 存档页，只有死亡后的结算面板才能返回；`R` / `M` 在局内也不生效
- 根因：`GameManager.Update()` 里 `if (!IsGameOver) return;` 挡在 `R` / `M` 之前，两个键只在死亡后生效；`Esc` 原本只做「关闭模态面板」，没有面板可关时什么都不做（而帮助文本一直写着「暂停 / 关闭面板 Esc」）
- 实现：
  - `UIManager` 新增 `pausePanel` 与 `IsPausePanelOpen`，并把暂停面板并入 `IsModalOpen` → 打开即 `Time.timeScale = 0`，敌人 / 波次 / 弹幕全部冻结（复用既有的暂停唯一真源 `ApplyPauseState`）
  - 新增 `TogglePauseMenu()` / `ClosePauseMenu()`；暂停时禁止叠加打开商店 / 背包（`ToggleShop` / `ToggleInventory` 加 `IsPausePanelOpen` 守卫）；`CloseTopModal()` 把暂停面板放在最外层，`HideAllMenus()` 也会收起它
  - `SceneBootstrap.BuildPausePanel()`：520×360 面板，标题「已暂停」+「按 Esc 回到游戏」+ 三个按钮（继续游戏 / 重新开始本局 / 返回主菜单（保存进度））；HUD 底部提示补上「Esc 菜单」
  - `GameManager.Update()`：`Esc` 先逐层关闭面板，局内没有面板可关时开关暂停菜单；`R` / `M` 改为局内随时可用（不必等死亡）
- 验证（Play Mode 实测，借用空的 3 号存档槽，测完 `ClearSlot(3)` 还原）：
  - 暂停：开面板后 `IsModalOpen=True`、`Time.timeScale=0`，1 只史莱姆在约 7 秒真实时间里坐标完全不变（(5.91,6.48)），波次停在 3
  - 布局：8 个子物体（2 个文本 + 3 个按钮 + 3 个按钮文字）全部落在面板矩形内
  - 「继续游戏」按钮 → 面板收起、`timeScale=1`；`Esc` 链路 `CloseTopModal()` → 关闭暂停面板且 `timeScale=1`；暂停时按 B / I 不会叠加打开面板
  - 「返回主菜单」按钮 → 场景切到 `MainMenuScene`、`timeScale=1`，本局进度已写入存档（第 3 波 / 击杀 12 / 金币 5）

### Phase 17 热修：提示框里的文字跑到框外（文本 helper 缺 pivot）
- **Status:** complete
- 症状：Phase 16 之后提示框本身已经贴在道具正下方，但框里的文字（道具名 + 效果描述）不在框内，整体偏到框的左上外侧
- 根因：`SceneBootstrap.T()` / `MenuUI.T()` 这两个文本 helper 只把 `anchorMin/anchorMax` 设成 `av`（按 TextAnchor 推出的角/边锚点），**没有设 pivot**，于是 pivot 停留在默认的 (0.5,0.5)。调用方是按「角锚点 + 相对偏移」书写的（如 UpperLeft + (14,-12) 表示「距父矩形左上角右 14、下 12」），实际却变成「矩形中心落在该偏移点」，整块文字因此向左偏半个框宽、向上偏半个框高。提示框文字框宽 300、面板宽 328，直接左移 150px，字全跑到框外左侧
- 修复：两个 `T()` helper 各补一句 `rt.pivot = av;`，让 pivot 与锚点成对出现。此前 `hpText` / `xpText` / `lvText` / 商店描述 / 游戏说明正文这 5 处已在 `T()` 返回后手动重设 pivot 绕过该坑，这些局部绕行不受影响
- 验证（Play Mode + `GetWorldCorners`/`WorldToScreenPoint` 数值测量）：
  - 提示框：6 个道具格子逐个悬停，标题与描述均在面板矩形内（`inPanel=True`）且 `onScreen=True`，水平居中、面板位于格子正下方
  - HUD 顺带修复：`killText` / `timeText` / `defenseText` / `goldText` / `buffText` / `hudHint` 此前**全部在屏幕外**（如 killText x -540..660，字形起点 -540 完全不可见），现已全部 `onScreen=True`
  - 菜单：游戏说明标题/正文、选择存档位标题均在面板内且 `onScreen=True`
  - 商店 / 背包 / 符文面板：全部文字均在面板矩形内

### Phase 16 热修：道具悬停提示框跑到屏幕外
- **Status:** complete
- 症状：鼠标悬停在背包道具格子上时，提示框（道具名称 + 效果描述）出现在屏幕外，完全看不到
- 根因：`UIManager.Update()` 每帧调用 `FollowMouse()`，用 `ScreenPointToLocalPointInRectangle` 取到的是「以父矩形中心为原点」的坐标，却直接写进 `tooltipPanel.anchoredPosition`，而该面板的 anchor/pivot 是左下角 (0,1)。坐标系混用把提示框推到屏幕左下外侧；更关键的是它每帧覆盖 `PlaceTooltip` 算好的位置，所以「贴格子正下方」的定位从未真正生效
- 修复：删除 `FollowMouse()` 方法及 `Update()` 里的调用；提示框位置的唯一来源改为 `PlaceTooltip`（取格子下沿中心、水平居中、`gap=8px`），新增「下方空间不足则翻转到格子上方」分支，最后统一 `Clamp` 在画布矩形内（`pad=8px`），保证任意分辨率都不出屏
- 验证：Play Mode 下遍历 6 个道具格子，提示框屏幕矩形全部 `onScreen=True`、`below=True`（如 HealthPotion 提示框 y 687..987 / 格子 y 1011..1305），中心与格子中心对齐；另将面板下移制造「下方放不下」场景，确认自动翻转到格子上方且 `onScreen=True`

### Phase 15 热修：从主菜单进不了游戏（场景是空的）
- **Status:** complete
- 症状：主菜单点「开始游戏 / 继续游戏」再点存档位后，游戏场景不加载（进去是空白场景）
- 根因：`SceneBootstrap` / `MainMenuBootstrap` 用的是 `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`，该回调**只在启动时触发一次**，`SceneManager.LoadScene` 之后不会重跑，所以切进 `MainScene` 时没有任何人创建 GameManager / 玩家 / UI
- 修复：两个 bootstrap 改挂 `SceneManager.sceneLoaded`（每次场景加载后都触发），保留 `AfterSceneLoad` 作兜底并用「场景根对象名」去重，避免事件与兜底同时命中时重复搭建
- 附带：`BeforeSceneLoad` 钩子里加 `MenuFlow.Clear()`，清掉上一次 Play 遗留的跨场景开局请求（关闭域重载时静态字段会残留）
- 实测：菜单 → 开始游戏 → 1 号存档 → `MainScene` 正常搭出 Camera / MapManager / UIManager / GameManager / Player / EnemySpawner；游戏内按 M → 主菜单重建成功；菜单 → 继续游戏 → 存档正常读取（currentSlot=1）
- 实测：4 个主菜单按钮的 GraphicRaycaster 命中均为 Button 本身，无遮挡物；点击有效
### Phase 15: 主菜单独立场景、悬停提示定位与存档隔离
- **Status:** complete
- Actions taken:
  - 新增 MenuFlow.cs：菜单↔游戏场景交接静态类（GameSceneName="MainScene"、MenuSceneName="MainMenuScene"、RequestStart(newGame, slot) / Consume(out, out) / Clear）
  - 新增独立场景 Assets/Scenes/MainMenuScene.unity，并置于 Build Settings 首位（MainMenuScene → MainScene）
  - 新增 MainMenuBootstrap.cs：RuntimeInitializeOnLoadMethod(AfterSceneLoad) 仅当活动场景为 MainMenuScene 时创建 MenuUI，游戏场景不再构建菜单
  - 新增 MenuUI.cs：程序化 Canvas(1280×720, match 0.5) + 主面板 / 存档面板 / 说明面板；标题改名《永夜幸存者》+ 副标题 N I G H T F A L L  S U R V I V O R S；4 按钮居中（开始游戏 / 继续游戏 / 游戏说明 / 退出游戏）；删除底部「3 个存档位…」说明行；存档面板标题「…：选择一个存档位」并删除「空存档会直接开始新游戏…」提示；说明面板返回按钮回到屏幕内；Esc 逐层返回
  - 新增 MenuBackground.cs：程序化动态背景（4×256 竖直夜空渐变 + 月亮柔光 + 远景塔楼剪影带暖色窗灯 + 96 颗正弦闪烁缓慢上浮星）
  - SceneBootstrap.cs：OnSceneLoaded 在主菜单场景直接 return；CanvasScaler 补 referenceResolution(1280,720) + MatchWidthOrHeight + match=0.5（修面板出屏根因）；删除旧 BuildMainMenu / BuildSlotPanel / BuildHelpPanel 三个方法
  - UIManager.cs：删除 in-scene 主菜单 / 存档 / 说明全部字段与方法；IsModalOpen 收敛为 商店 ‖ 符文 ‖ 背包；新增 ShowItemTooltip(kind, anchor) 重载 + PlaceTooltip：提示框水平对齐格子中心、垂直贴格子下沿 -8px，并 Clamp 在画布内
  - PlayerController.cs：AddGold / PurchaseHealthPotion / PurchaseAttackPotion 内补 GamePersistence.SetBankGold(currentGold)，金币即时写回当前槽位，实现「金币与道具不跨存档」
  - GameManager.cs：InitGame 末尾改为「有 pending → Consume 后 StartRun，否则 LoadScene(MainMenuScene)」；ReturnToMenu(saveRun) 改为先存档 → timeScale=1 → LoadScene(MainMenuScene)；新增 OnDestroy

### Phase 14: 背包、悬停提示、3 存档位与主菜单
- **Status:** complete
- Actions taken:
  - 新增 ItemCatalog.cs：道具元数据集中表（AllItems / MaxStack / DisplayName / Description / PickupHint / HotkeyIndex / SpriteName / Tint）。背包格子顺序 = AllItems 顺序（血瓶/攻击药剂/狂暴/迅捷/铁壁/磁力），上限刻意各不相同（9/5/9/9/6/6）
  - 新增 InventorySlotUI.cs：挂在背包格子上，IPointerEnter → UIManager.ShowItemTooltip(kind)，IPointerExit → HideItemTooltip；格子只做通知，文案与布局归 UIManager
  - 背包界面（SceneBootstrap.BuildInventoryPanel）：标题「背包」+ 金币栏 inventoryGoldText（「金币：NG」）+ 6 个格子 CreateInventorySlot（图标 / 名称 / 「xN/上限」）。空槽压暗到 35% 亮度，满槽数量文字变橙 (1,0.55,0.35)
  - 道具提示框（BuildTooltip + UIManager.ShowItemTooltip）：标题「名称   N / 上限」（用道具主色 ItemCatalog.Tint），正文「效果说明 + 按 X 使用」；面板固定 328×100，标题/正文各占一行固定位置，杜绝与商品描述重叠
  - 悬停文案来自 ItemCatalog.Description：血瓶「立刻恢复 60 点生命」/ 攻击药剂「永久 +35% 攻击力」/ 狂暴「攻击力 +50%，持续 15 秒」/ 迅捷「移动速度 +40%，持续 12 秒」/ 铁壁「防御力 +8，持续 20 秒」/ 磁力「拾取范围 ×3，持续 12 秒」
  - GamePersistence 由「单份全局存档」重写为 3 存档位：SlotCount=3、CurrentSlot/SelectSlot、SlotExists、CreateOrOverwrite、ClearSlot、ClearAll、GetSlotGold、GetSlotItemTotal、GetSlotSummary/SetSlotSummary、Save(PlayerController)；键名统一加 "PS_<slot>_" 前缀
  - 旧档迁移 MigrateLegacySaveIfNeeded：首次运行时把旧版 PS_Gold / PS_Item_* 复制进 1 号槽并删除旧键，用 PS_LegacyMigrated 标记只做一次；由 GameManager.InitGame 调用
  - 主菜单（BuildMainMenu）：开始游戏 / 继续游戏 / 游戏说明 / 退出游戏；继续游戏按钮在无任何存档时 interactable=false
  - 存档选择面板（BuildSlotPanel + ShowSlotSelect）：标题随模式变化；3 个槽位按钮，空档显示「存档 N / 空存档 — 点击开始新游戏」（灰字），有档显示「存档 N   金币 NG   道具 N 件 / 进度：第 N 波 ｜ 击杀 N ｜ 用时 mm:ss / 点击将覆盖此存档并重新开始 或 点击继续这一局」（覆盖模式橙字）
  - GameManager 拆出 StartRun(bool newGame, int slot)：SelectSlot → newGame 时 CreateOrOverwrite → HideAllMenus → 清 kills/time → _inRun=true → timeScale=1 → 重置 HUD → SpawnPlayer + SpawnEnemySpawner；InitGame 末尾改为 ReturnToMenu(false)，只有 StartRun 才真正开局
  - ReturnToMenu(bool saveRun)：saveRun 时先 GamePersistence.Save，再 ClearRun 清掉 Enemy/XPOrb/PickupDrop/Projectile/EnemyProjectile/EnemySpawner/Player 并把 MapManager 复位到第 1 波，最后 ShowMainMenu
  - 暂停真源扩展：UIManager.IsModalOpen 由「商店 ‖ 符文」扩成「商店 ‖ 符文 ‖ 背包 ‖ 主菜单 ‖ 存档选择 ‖ 说明」，商店与背包互斥（开一个自动关另一个），CloseTopModal 供 Esc 逐层关闭
  - MapManager 加固：UpdateForWave 开头 if (_texture == null || _pixels == null) return;，避免清场/重开瞬间在地图纹理尚未重建时越界
- Files created/modified:
  - Assets/Scripts/ItemCatalog.cs (created)
  - Assets/Scripts/InventorySlotUI.cs (created)
  - Assets/Scripts/GamePersistence.cs (rewritten)
  - Assets/Scripts/GameManager.cs (modified)
  - Assets/Scripts/UIManager.cs (modified)
  - Assets/Scripts/SceneBootstrap.cs (modified)
  - Assets/Scripts/MapManager.cs (modified)
  - Assets/Scripts/PlayerController.cs (modified)
- Runtime validation:
  - 空档新建：gm.StartRun(true, 2) → 2 号槽 exists=True、0 金币、0 道具；玩家 100HP / Lv1 / 0G；符文面板弹出（ts=0）、_inRun=True
  - 覆盖已有档：1 号槽原为 352G / 10 件道具，gm.StartRun(true, 1) 后槽位与玩家金币/道具全部归零，符文面板弹出
  - 符文流程：ui.SelectRune(0) → pendingRuneChoices 归零、符文面板关闭、timeScale 回到 1
  - 实跑约 23 秒：wave=3、time=22.9、kills=25、场上 3 只 Slime、玩家 120/120 Lv2，控制台 0 报错
  - 死亡保存：先加 48G 与 2 个狂暴药剂，TakeDamage(99999) → 结算面板弹出；1 号槽存得 gold=67、items=3，摘要「进度：第 5 波 ｜ 击杀 51 ｜ 用时 00:49」
  - 继续游戏：gm.StartRun(false, 1) → CurrentSlot=1、gold=67、rage=2、lv=1、kills=0、符文面板弹出、ts=0（金币与道具继承，等级/击杀重置）
  - 背包：ui.ToggleInventory() → 背包面板 ON、IsModalOpen=True、timeScale=0（打开背包真正暂停）
  - 悬停提示：ui.ShowItemTooltip(ItemKind.RagePotion) → 标题「狂暴药剂   2 / 9」，正文「攻击力 +50%，持续 15 秒 / 按 3 使用」
  - 编译与运行均干净（控制台仅剩 1 条无害 WebSocket 警告）


### Phase 13: 开局符文、击杀回血、限时道具与跨局存档
- **Status:** complete
- Actions taken:
  - 新增 GamePersistence.cs 静态存档层（PlayerPrefs）：BankGold / GetItemCount / SetItemCount / Save(player) / ClearAll；键名用 ItemKind 枚举名（PS_Item_ + ToString()），避免枚举顺序变动读错档
  - 开局赠送符文：PlayerController.Start 末尾 pendingRuneChoices = Mathf.Max(pendingRuneChoices, 1) 并立即 TryShowRuneChoice()，进游戏第一件事就是符文 3 选 1
  - 击杀回血：GameManager.OnEnemyDied 新增 HealPlayerPercent(percent, msg)——精英怪回复 25% 最大生命、Boss 回复 50%，并弹出公告
  - 升级成长上调：LevelUp 中 damageMultiplier +0.12→+0.18、moveSpeedPerLevel 0.12→0.16、升级回血 15%→20%；符文三选一保底奖励 0.3/0.2/15 → 0.35/0.25/20
  - 难度下调：GetDifficultyScale time/60*0.5→*0.35；GetWaveScale *0.04→*0.03；敌方护甲增长 /3→/4
  - Boss 节奏：EnemySpawner BossStartWave 10→15、BossWaveInterval 10→15（即第 15/30/45… 波）；GameManager.SpawnBoss 类型判定 wave/10→wave/15，bossScale 基准 -10→-15
  - 刷怪压力：每波敌人数 +2→+1；spawnInterval 衰减 0.92→0.95、下限 0.3→0.35
  - 限时道具系统（4 种）：PickupDrop.ItemKind 重构为 GoldCoin + HealthPotion + AttackPotion + 限时四件套（RagePotion 狂暴 / HastePotion 迅捷 / IronShield 铁壁 / MagnetCharm 磁力）；拾取进背包（AddItem）而非立即生效
  - PlayerController 道具实现：RageDuration 15s（攻击 ×1.5）、HasteDuration 12s（移速 ×1.4）、IronShieldDuration 20s（EffectiveArmor +8）、MagnetDuration 12s（拾取范围 ×3）；Update 内 TickItemTimers 递减，AddItem / GetItemCount / SetItemCount / UseItem / GetBuffSummary 完整闭环
  - 掉落表：小怪 45% 掉 2-4 金币、12% 掉 1 个随机道具；精英 6-11 金币 + 1 道具；Boss 15-24 金币 + 血瓶 + 2 道具
  - 跨局持久化：GameOver / OnApplicationQuit / OnApplicationPause(true) 调用 GamePersistence.Save；金币与全部背包道具写入 PlayerPrefs，RestartGame 不再清零金币，新一局 PlayerController 在 Start 中 LoadPersistentProgress() 读回
  - HUD 新增金色 buffText 显示限时道具倒计时（UpdateBuffUI，带缓存比较避免每帧写 Text）；商店 potionText 改为两行「背包」清单；GameOver 面板增加存档金币提示
  - 快捷键：1 血瓶 / 2 攻击药剂 / 3 狂暴 / 4 迅捷 / 5 铁壁 / 6 磁力
- Files created/modified:
  - Assets/Scripts/GamePersistence.cs (created)
  - Assets/Scripts/PickupDrop.cs (modified)
  - Assets/Scripts/PlayerController.cs (modified)
  - Assets/Scripts/GameManager.cs (modified)
  - Assets/Scripts/EnemySpawner.cs (modified)
  - Assets/Scripts/UIManager.cs (modified)
  - Assets/Scripts/SceneBootstrap.cs (modified)
  - Assets/Scripts/PixelSpriteGenerator.cs (modified)
  - Assets/Scripts/RuneSystem.cs (modified)
- Runtime validation:
  - Play Mode 干净回归：gm/ui/pc 均非空、控制台 0 error / 0 warning；开局 pendingRuneChoices=1、timeScale=0、runePanel 弹出 3 张卡（力量符文 / 环绕符文 / 疾风符文）
  - 选卡后 pendingRuneChoices 归零、timeScale 恢复 1、IsModalOpen=False（开局符文流程闭环，选完即进入游戏并撤掉开场遮罩）
  - 波次常量反射读取：EliteStartWave=6、BossStartWave=15、BossWaveInterval=15
  - 击杀回血：精英 20→45（+25%）、Boss 20→70（+50%）
  - 限时道具：rage timer=15 atkMul=1.5；haste moveMul=1.4；ironShield EffectiveArmor 13（base 5）；magnet pickRangeMul=3；HUD 实测渲染「狂暴 15.0s    迅捷 12.0s    铁壁 20.0s    磁力 10.7s」
  - 持久化：Save 后 bankGold=250 rage=4；RestartGame 后新 Player lv=1 hp=100/100 speed=5、金币非 0（读回存档）、rage=4 haste=2 hpPot=1 atkPot=3（金币与背包继承、等级/移速重置）
  - 商店两行背包文本无重叠、shopGold 与 HUD 金币数值一致；截图 Assets/Screenshots/shop_with_buffs_v12.png

### Phase 12: 无敌符文、符文无限叠加与升级移速成长
- **Status:** complete
- Actions taken:
  - RuneSystem 新增 RuneId.Invincible「无敌符文」：首次获取立即获得 2.5s 无敌（InvincibleFirstDuration），此后每 6s（InvinciblePulseInterval）自动再获得 1.5s（InvinciblePulseDuration）无敌
  - 无敌符文为一次性符文：新增 RuneSystem.IsOneTimeOnly(id) 统一标记 Revive/Invincible；已拥有时从 RollOptions 候选池移除，UIManager 卡片名后缀显示「（仅一次）」
  - 符文无限叠加：除一次性符文外所有符文均可反复获取；新增 RuneSystem.IsAttackType(id)（散射/环绕/雷霆/灼烧光环）标记攻击类型符文，第二次起每次再获取额外提升该攻击类型伤害（散射 +12%/层，环绕·雷霆·灼烧光环 +20%/层）
  - PlayerController 新增 _runeStacks(Dictionary<RuneId,int>) 与 GetRuneStacks/AddRuneStack；Apply 中先取 already=GetRuneStacks(id) 判定是否第二次及以后，再 AddRuneStack
  - 无敌表现：PlayerController.UpdateInvincible() 统一处理——长无敌(>0.4s) 保持可见并以青蓝呼吸脉冲提示，受击后的短无敌沿用闪烁；GrantInvincibility(d) 只延长不缩短；ResetInvinciblePulseTimer() 在首次获取时把脉冲计时对齐到 6s
  - 升级移速成长：新增 moveSpeedPerLevel=0.12f、moveSpeedBonusMaxLevel=20；LevelUp 中 level≤20 时 moveSpeed += 0.12（局内永久），重开时 PlayerController 被重建故自动回到初始值
  - UIManager 新增 RuneLabelSuffix：一次性符文显示「（仅一次）」，可叠加符文显示「 Lv.N」；卡片文本 = name + 后缀 + 描述
  - RuneAttacks 三条攻击（环绕/灼烧光环/连锁闪电）与主角弹幕伤害分别乘以 (1 + 对应 DamageBonus)
  - SceneBootstrap 符文面板副标题更新为「升级奖励：随机 3 选 1，可重复获取的符文会继续叠加（选择时游戏暂停）」
- Files created/modified:
  - Assets/Scripts/RuneSystem.cs (modified)
  - Assets/Scripts/PlayerController.cs (modified)
  - Assets/Scripts/RuneAttacks.cs (modified)
  - Assets/Scripts/UIManager.cs (modified)
  - Assets/Scripts/SceneBootstrap.cs (modified)
- Runtime validation:
  - Play Mode 干净回归：gm/ui/pc 均非空、控制台 0 error；RuneId 枚举共 13 项
  - 无敌符文：Apply 后 invincibleRuneTaken=True、IsInvincible=True；TakeDamage(9999) 血量不变且未 GameOver；把脉冲间隔临时改为 0.5s 实测可再次进入无敌
  - 一次性排除：注入 invincibleRuneTaken 后 300 次 RollOptions(3) 中无敌符文出现 0 次
  - 叠加：环绕 2 次 → orbitCount 0→2、orbitDamageBonus 0→0.2、GetRuneStacks=2；散射 2 次 → scatterDamageBonus=0.12
  - 移速成长：连续升级至 level 28，moveSpeed 5.24→7.28（level 4~20 共 17 级 × 0.12 = 2.04；21 级起不再提升，上限生效）
  - 卡片文案：实测面板渲染「无敌符文（仅一次）」「不屈符文（仅一次）」「环绕符文 Lv.3」

### Phase 11: 商店购买闭环、启动卡顿根因与符文系统
- **Status:** complete
- Actions taken:
  - 修复道具精灵 fallback：GameManager 新增 GetItemSpriteName(ItemKind) 显式下划线映射（HealthPotion→health_potion 等），此前用 kind.ToString().ToLowerInvariant() 恒不命中 case 表，所有掉落物都渲染成洋红 fallback 色块
  - 商店"商品无法购买"根因：项目缺少 EventSystem，UGUI Button 收不到点击；SceneBootstrap.EnsureEventSystem() 在启动时自动补建 EventSystem + StandaloneInputModule
  - 商店排版：面板放大，两条描述 Text 的 pivot 改为 (0,0.5) 并右移，商品按钮与描述文字水平分离，消除使用效果与购买金额重叠
  - 商店购买链路打通：UIManager.PurchaseHealthPotion/PurchaseAttackPotion → PlayerController，实测射线点击血瓶 12G、攻击药剂 30G 均成功扣费入库
  - 点 Play 卡顿根因：编辑器进入 Play Mode 的域重载（domain reload），非游戏初始化开销（实测冷启动仅约 30ms）；已开启 Enter Play Mode Options 并关闭域重载（保留场景重载），写入 ProjectSettings/EditorSettings.asset
  - 域重载兼容加固：SceneBootstrap.BuildAll 开头调用 PixelSpriteGenerator.ClearCache()，避免静态精灵缓存跨 Play 残留已销毁贴图
  - 新增符文系统 RuneSystem.cs：每 3 级（levelsPerRune）弹出 3 选 1 符文面板（海克斯风格），12 种效果池、随机不重复、复活符文在已有复活时不再出现
  - 符文 12 种：力量(攻+25%)/鹰眼(范围+2)/疾风(移速+15%)/坚壁(防御+3)/生命(上限+30并回血)/散射(+1弹幕)/环绕(+1刀刃)/雷霆(+1连锁闪电)/灼烧光环/生机(每秒+1.5HP)/智慧(每秒+1XP)/不屈(1次复活)
  - UIManager 统一模态门 IsModalOpen（= 商店 || 符文面板）→ Time.timeScale 0/1；Enemy/PlayerController/RuneAttacks 三处逻辑门，开商店或符文面板时角色与怪物全部停止移动与攻击
- Files created/modified:
  - Assets/Scripts/RuneSystem.cs (created)
  - Assets/Scripts/RuneAttacks.cs (created)
  - Assets/Scripts/GameManager.cs (modified)
  - Assets/Scripts/SceneBootstrap.cs (modified)
  - Assets/Scripts/UIManager.cs (modified)
  - Assets/Scripts/PlayerController.cs (modified)
  - Assets/Scripts/PixelSpriteGenerator.cs (modified)
  - ProjectSettings/EditorSettings.asset (modified)
- Runtime validation:
  - Play Mode 回归：gm/ui/pc/EventSystem 均非空，timeScale=1，控制台 0 error
  - 符文面板：注入经验后 3 选 1 面板弹出、timeScale=0 暂停；选择后属性正确生效（力量 damageMultiplier +0.25），面板继续处理剩余选项
  - 商店：射线点击血瓶 200→188G（库存 0→1）、攻击药剂 188→158G；使用血瓶 HP 20→80、攻击药剂 damageMultiplier +0.35
  - 关闭域重载后二次进入 Play Mode 正常，无残留报错



## Session: 2026-10-13
### Phase 10: 商店修复、性能优化与命名映射
- **Status:** complete
- Actions taken:
  - 商店 UI 重排为 560×340 面板，商品按钮和描述文字重新定位；两条描述 Text 的 pivot 改为 (0, 0.5)，消除使用效果与购买金额文字重叠
  - UIManager.ToggleShop/CloseShop 设置 Time.timeScale = 0/1 实现暂停；Enemy 和 PlayerController 增加双逻辑门确保暂停时不移动不攻击
  - GameManager.InitGame 重置 timeScale = 1 并调用 CloseShop()，防止重启后残留暂停状态
  - SceneBootstrap 删除全场景 DestroyImmediate 循环和 ClearCache() 调用，BuildAll 复用已有 Camera.main，MapManager.PixelsPerBlock 4→2（地图纹理 320→160），消除开始游戏卡顿
  - 新增 PixelSpriteGenerator.GetEnemySprite(Enemy.EnemyType) 枚举到精灵键映射（EliteKnight→elite_knight 等），修复因 ToString() 小写无下划线导致精英/Boss 命中 fallback 色块的问题
  - Enemy.cs 和 GameManager.cs 改用 GetEnemySprite 统一获取敌人精灵
- Files created/modified:
  - Assets/Scripts/SceneBootstrap.cs (modified)
  - Assets/Scripts/UIManager.cs (modified)
  - Assets/Scripts/GameManager.cs (modified)
  - Assets/Scripts/Enemy.cs (modified)
  - Assets/Scripts/PixelSpriteGenerator.cs (modified)
  - Assets/Scripts/MapManager.cs (modified)
  - Assets/Scripts/PlayerController.cs (modified)
- Runtime validation:
  - Unity 编译通过无错误
  - Play Mode 实测：开商店 timeScale = 0，面板 560×340，描述文字无重叠
  - 截图 Assets/Screenshots/shop_layout_fixed.png 已目检确认无重叠
  - 6 个专属精灵（EliteKnight/EliteWarlock/StoneGolem/ShadowAssassin/SlimeKing/DemonOverlord）均为 16×16 非方块
  - 退出 Play Mode 确认编辑器状态恢复正常

### Phase 9: 战斗反馈、区域与商店
- **Status:** complete
- Actions taken:
  - `BootstrapRunner.BuildAll` 启动时调用 `PixelSpriteGenerator.ClearCache()`，避免旧程序化精灵缓存覆盖新图案
  - `Enemy` 增加精英/Boss 头顶血条和多样攻击状态机
  - 新增冲刺、扇形弹、发散弹、远程单体、摇摆移动等行为
  - 新增 `MapManager`，用单张大纹理替代大量地板 Sprite，提高大地图性能
  - 地图半径扩到 40 单位，每 5 波在草原/雪原/熔岩/虚空之间切换
  - `PlayerController` 增加血瓶/攻击药剂库存与使用逻辑
  - `UIManager` 增加商店面板、金币显示、库存显示和购买按钮
- Files created/modified:
  - Assets/Scripts/Enemy.cs (modified)
  - Assets/Scripts/EnemySpawner.cs (modified)
  - Assets/Scripts/GameManager.cs (modified)
  - Assets/Scripts/MapManager.cs (created)
  - Assets/Scripts/PixelSpriteGenerator.cs (modified)
  - Assets/Scripts/PlayerController.cs (modified)
  - Assets/Scripts/UIManager.cs (modified)
  - Assets/Scripts/SceneBootstrap.cs (modified)
- Runtime validation:
  - 精英/Boss 生成后 HealthBarRoot 与 HealthBarFill 均存在，受伤后血条比例正确刷新
  - Wave 11 区域更新成功；新图案精灵透明像素数量均非全量
  - 商店面板可开关，金币 100 可购买血瓶和攻击药剂，购买后剩余 58 金币
  - 血瓶恢复 HP，攻击药剂使伤害倍率增加 0.35
  - 已触发 Bat/Slime/EliteKnight/ShadowAssassin 特殊攻击，无异常
  - Unity 编译无错误，Play Mode 验证后控制台无错误/警告

### Phase 8: 敌人形象与成长修正
- **Status:** complete
- Actions taken:
  - 用独立图案精灵替换新增敌人原先的通用椭圆/色块形象
  - 新增 Skeleton、Crawler、EliteKnight、EliteWarlock、StoneGolem、ShadowAssassin、SlimeKing、DemonOverlord 专属轮廓
  - `Enemy` 新增 `armor`；`TakeDamage` 使用 `Mathf.Max(1, damage - armor)`
  - `GameManager` 增加 `GetWaveScale`；波次提升生命、攻击和防御，精英/Boss 同样受波次成长影响
  - `PlayerController.LevelUp` 升级时恢复 `maxHealth * 15%`
- Files created/modified:
  - Assets/Scripts/Enemy.cs (modified)
  - Assets/Scripts/GameManager.cs (modified)
  - Assets/Scripts/PlayerController.cs (modified)
  - Assets/Scripts/PixelSpriteGenerator.cs (modified)
  - Assets/Progress/Screenshots/enemy_sprites_validation_large.png (generated)
- Runtime validation:
  - 清理精灵缓存后，8 个新图案的透明像素数量均非全量，说明不再是无轮廓色块
  - Wave 10 Crawler 属性为 HP 75 / Damage 16 / Armor 3，波次成长生效
  - Wave 10 DemonOverlord 属性为 HP 1100 / Damage 28 / Armor 15，Boss 波次成长生效
  - 玩家将生命设为 10 后触发升级，可恢复到满血/较高血量，百分比回血生效
  - Unity 编译无错误，Play Mode 退出后控制台无错误/警告

### Phase 7: 精英/Boss 与掉落系统
- **Status:** complete
- Actions taken:
  - `Enemy` 增加 `EnemyCategory`：Normal、Elite、Boss，并扩充小怪与精英/Boss 类型
  - 新增 Skeleton、Crawler、EliteKnight、EliteWarlock、StoneGolem、ShadowAssassin、SlimeKing、DemonOverlord
  - `EnemySpawner` 使用调度状态：Wave 6 启用精英，随机 3 或 5 波后出现；Wave 10 启用 Boss，每 10 波固定出现
  - `GameManager` 集中生成精英/Boss、应用难度缩放、按类别结算掉落
  - 小怪有概率掉金币；精英掉更多金币并高概率掉道具；Boss 掉大量金币和道具
  - 新增 `PickupDrop`、金币精灵、四种即时道具精灵
  - `PlayerController` 增加金币持有和道具即时效果；HUD 增加 Gold 与事件提示
- Files created/modified:
  - Assets/Scripts/Enemy.cs (modified)
  - Assets/Scripts/EnemySpawner.cs (modified)
  - Assets/Scripts/GameManager.cs (modified)
  - Assets/Scripts/PickupDrop.cs (created)
  - Assets/Scripts/PixelSpriteGenerator.cs (modified)
  - Assets/Scripts/PlayerController.cs (modified)
  - Assets/Scripts/UIManager.cs (modified)
  - Assets/Scripts/SceneBootstrap.cs (modified)
- Runtime validation:
  - Play Mode 强制生成 3 个 Elite 和 1 个 Boss，均成功激活
  - StoneGolem 显示 `Elite|hp=363`，DemonOverlord 显示 `Boss|hp=1110`，难度缩放生效
  - 金币/道具生成数量为 2，Gold HUD 初始化正常，BOSS 提示激活
  - 道具效果验证：金币 25→30，伤害倍率 +0.3，防御 +2，攻速 +0.2，生命药剂恢复生效
  - Unity 编译无脚本错误，Play Mode 退出后控制台无错误/警告

### Phase 6: HUD 数值显示
- **Status:** complete
- Actions taken:
  - 确认 `UIManager` 使用 Legacy `UnityEngine.UI.Text`，并为 HP、XP、等级、DEF 文本提供运行时刷新接口
  - 在 `SceneBootstrap` 创建 HUD 文本并绑定到 `UIManager.hpText`、`xpText`、`lvText`、`defenseText`
  - HP 显示格式为 `HP: 当前/最大`，XP 显示格式为 `XP: 当前/所需`，等级显示在 XP 条旁边，格式为 `Lv.N`
- Files created/modified:
  - Assets/Scripts/SceneBootstrap.cs (modified)
  - Assets/Scripts/UIManager.cs (verified)
- Runtime validation:
  - UIManager 字段非空，四个文本 `activeInHierarchy=true`
  - `HP: 0/175`、`XP: 6/211`、`Lv.10`、`DEF: 9` 均能读取到运行时数值
  - HP、XP、Lv、DEF 文本 RectTransform 世界坐标有效，位置分布在血条、经验条和经验条右侧
- Notes:
  - 摄像机截图无法捕获 Screen Space Overlay UI，最终以 Play Mode 组件状态作为验证依据

### Phase 6: 敌人多样性与角色成长
- **Status:** complete
- Actions taken:
  - 为 `Enemy` 增加 `EnemyType`：Slime、Bat、Brute、Shooter
  - `EnemySpawner` 将当前波次传给 `GameManager`
  - `GameManager` 按波次选择敌人类型，并集中应用难度缩放，避免 `Enemy.Start` 二次缩放
  - Shooter 保持 7 单位距离，进入 11 单位射程后周期性发射敌方弹幕
  - 新增 `EnemyProjectile`，碰撞玩家后按伤害结算并销毁
  - 新增 Bat/Brute/Shooter/敌方弹幕的程序化像素精灵
  - `PlayerController` 每级固定提升攻击倍率 `+0.12` 与防御力 `+1`，同时保留一次随机额外强化
  - 受伤公式改为 `Mathf.Max(1, damage - armor)`
  - HUD 新增 `DEF` 文本，升级和重启后同步刷新
- Files created/modified:
  - Assets/Scripts/Enemy.cs (modified)
  - Assets/Scripts/EnemySpawner.cs (modified)
  - Assets/Scripts/GameManager.cs (modified)
  - Assets/Scripts/PlayerController.cs (modified)
  - Assets/Scripts/PixelSpriteGenerator.cs (modified)
  - Assets/Scripts/UIManager.cs (modified)
  - Assets/Scripts/SceneBootstrap.cs (modified)
  - Assets/Progress/task_plan.md (modified)
  - Assets/Progress/progress.md (modified)
  - Assets/Progress/findings.md (modified)

### Phase 6 Validation
| Test | Method | Expected | Actual | Status |
|------|--------|----------|--------|--------|
| Unity 编译 | force refresh + compile | 无脚本错误 | 无错误 | Pass |
| 敌人类型 | Play Mode 强制 Wave 2/3/4 生成 | 出现新类型 | Slime/Bat/Brute/Shooter 均可出现 | Pass |
| 远程攻击 | Play Mode 观察 Shooter | 敌人发射弹幕并伤害玩家 | EnemyProjectile 出现 | Pass |
| 防御减伤 | 临时设置 armor=4 后受到 10 点伤害 | 扣血 6 点 | HP 100 → 94 | Pass |
| DEF HUD | 升级或运行时刷新 | 显示防御力 | `DEF: 4` | Pass |
| 截图 | Play Mode Game View | 记录验证画面 | `Assets/Progress/Screenshots/enemy_growth_validation.png` | Pass |

*Update after completing each phase or encountering errors*

## Session: 2026-06-13

### Phase 1: 项目初始化
- **Status:** complete
- Actions taken:
  - 创建 PixelSurvivor 目录结构
  - 编写 Packages/manifest.json（包含 2D Feature、UGUI 等依赖）
  - 创建 ProjectSettings/ProjectVersion.txt、EditorBuildSettings.asset
- Files created/modified:
  - Packages/manifest.json (created)
  - ProjectSettings/ProjectVersion.txt (created)
  - ProjectSettings/EditorBuildSettings.asset (created)
  - ProjectSettings/ProjectSettings.asset (created)

### Phase 2: 程序化精灵生成
- **Status:** complete
- Actions taken:
  - 实现 PixelSpriteGenerator.cs
  - 逐一创建 5 种精灵：hero、slime、projectile、xp_orb、floor
  - 使用 Texture2D 逐像素填充，filterMode = Point
- Files created/modified:
  - Assets/Scripts/PixelSpriteGenerator.cs (created)

### Phase 3: 核心游戏逻辑
- **Status:** complete
- Actions taken:
  - 实现 PlayerController（移动/攻击/升级/受伤闪白）
  - 实现 Enemy（追踪/碰撞伤害/死亡掉落）
  - 实现 EnemySpawner（波次递增/边缘生成）
  - 实现 Projectile（跟踪弹幕/触发销毁）
  - 实现 XPOrb（吸取/闪烁效果）
  - 实现 GameManager（主控/重启/难度递增）
  - 实现 UIManager（血量/经验/击杀/波次/等级）
- Files created/modified:
  - Assets/Scripts/PlayerController.cs (created,多次重写)
  - Assets/Scripts/Enemy.cs (created,多次重写)
  - Assets/Scripts/EnemySpawner.cs (created,多次重写)
  - Assets/Scripts/Projectile.cs (created,多次重写)
  - Assets/Scripts/XPOrb.cs (created,多次重写)
  - Assets/Scripts/GameManager.cs (created,多次重写)
  - Assets/Scripts/UIManager.cs (created,多次重写)

### Phase 4: 场景启动 & 相机
- **Status:** complete
- Actions taken:
  - 实现 SceneBootstrap（RuntimeInitializeOnLoadMethod 入口）
  - 实现 BootstrapRunner（同步 DestroyImmediate + BuildAll）
  - 实现 CameraFollow（平滑跟随 + 动态钳制）
  - 分离 CameraFollow 为独立文件
- Files created/modified:
  - Assets/Scripts/SceneBootstrap.cs (created,多次重写)
  - Assets/Scripts/CameraFollow.cs (created)

### Phase 5: Bug 修复迭代
- **Status:** in_progress
- Actions taken:
  - 修复 1: Tag 系统 → 组件查找 + Singleton（影响所有脚本）
  - 修复 2: SceneSetup.cs 废弃（清空文件内容）
  - 修复 3: DontDestroyOnLoad GM 抢占 Instance（改 Awake）
  - 修复 4: 协程→同步清理（Start 内 DestroyImmediate）
  - 修复 5: 相机出界（动态钳制）
  - 修复 6: Prefab→直接 GameObject（GameManager 全部重写）
  - 修复 7: 人物方向（flipX）
  - 修复 8: 经验条重启不清零（多处 UI 重置）
  - 修复 9: UI 文字不显示（面板遮盖、锚点错误、字体回退——部分解决）
  - 修复 10: 面板遮盖文字（SetAsFirstSibling + inactive 默认）
- Files created/modified:
  - 所有脚本文件均经历多次重写

## Error Log
| Timestamp | Error | Attempt | Resolution |
|-----------|-------|---------|------------|
| 14:08 | Tag `Player`/`Enemy` 不存在 | 1-5 | 改用 FindObjectsOfType + Singleton |
| 14:10 | SceneSetup 旧对象持久化 | 1-8 | DestroyImmediate in BootstrapRunner |
| 14:15 | GM DontDestroyOnLoad 存活 | 1-3 | Awake 替换旧 Instance |
| 14:20 | 协程 yield 旧 Update 报错 | 1-2 | 同步 Start DestroyImmediate |
| 14:25 | Prefab 组件缺失 | 1-2 | 直接 new GameObject |
| 14:30 | 相机超出地板 | 1-3 | 动态相机钳制 |
| 14:35 | UI 文字不显示(面板遮盖) | 1-4 | SetAsFirstSibling + inactive |
| 14:40 | CS0841 局部变量未声明 | 1 | 调试代码移到正确位置 |
| 14:45 | 面板替换失败 | 1 | 全文重写 |
| 14:50 | 人物面朝固定 | 1 | 加 flipX |
| 14:55 | 经验条重启不清零 | 1 | 多处 UI 重置 |

## Test Results
| Test | Input | Expected | Actual | Status |
|------|-------|----------|--------|--------|
| Canvas 渲染 | 红色调试 Text | 可见 | 可见 ✓ | Pass |
| 玩家移动 | WASD | 角色移动 | 正常移动 ✓ | Pass |
| 方向翻转 | A/D | 面朝移动方向 | 正确翻转 ✓ | Pass |
| 边界限制 | 走到 ±19.5 | 无法超出 | 正常限制 ✓ | Pass |
| 相机跟随 | 移动角色 | 平滑跟踪 | 正常跟踪 ✓ | Pass |
| 敌人刷出 | 等待 2s | 史莱姆出现 | 正常出现 ✓ | Pass |
| 敌人追踪 | 敌人生成 | 向玩家移动 | 正常追踪 ✓ | Pass |
| 自动攻击 | 敌人在范围内 | 发射弹幕 | 正常发射 ✓ | Pass |
| 敌人死亡 | 弹幕命中 | 掉落 XP | 正常掉落 ✓ | Pass |
| UI 文字 | 游戏运行 | 显示完整 HUD | Kills/HP/Wave 等不显示 ✗ | Fail |
| R 键重启 | 死亡后按 R | 游戏重置 | 正常重置 ✓ | Pass |
| 经验条重置 | 死亡后按 R | 经验条清空 | 正常清空 ✓ | Pass |

## 5-Question Reboot Check
| Question | Answer |
|----------|--------|
| Where am I? | Phase 5 — Bug 修复 |
| Where am I going? | 修复 UI 文字显示，最终交付 |
| What is the goal? | 完成 2D 像素生存游戏 |
| What have I learned? | 见 findings.md |
| What have I done? | 核心玩法完成，UI 显示问题待修 |

*Update after completing each phase or encountering errors*