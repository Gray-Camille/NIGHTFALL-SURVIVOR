# Task Plan: Pixel Survivor 像素地牢生存

## Goal
在 Unity 2022.3.57f1c2 中构建一款无需外部素材的 2D 像素生存小游戏（Vampire Survivors 风格），所有精灵图由代码程序化生成。

## Current Phase
Phase 19 — 清空存档 + 打包前检查（已完成）

## Phases

### Phase 1: 项目初始化
- [x] 创建 Unity 项目目录结构
- [x] 配置 Packages/manifest.json（2D Feature、UGUI 等）
- [x] 配置 ProjectSettings（ProjectVersion、EditorBuildSettings）
- **Status:** complete

### Phase 2: 程序化精灵生成
- [x] 实现 PixelSpriteGenerator.cs — 逐像素绘制所有游戏素材
- [x] 勇者（16×16 金发蓝甲红披风）
- [x] 史莱姆敌人（16×16 绿色水滴形）
- [x] 魔法弹（8×8 橙黄菱形）
- [x] XP 宝珠（8×8 蓝青钻石形）
- [x] 地板瓦片（16×16 棋盘格暗紫底）
- **Status:** complete

### Phase 3: 核心游戏逻辑
- [x] PlayerController — WASD 移动、自动攻击、升级系统
- [x] Enemy — AI 追踪、碰撞伤害
- [x] EnemySpawner — 波次递增刷怪
- [x] Projectile — 跟踪弹幕
- [x] XPOrb — 经验吸取
- [x] GameManager — 游戏主控、死亡/重启
- [x] UIManager — 血量条、经验条、等级、击杀、波次
- **Status:** complete

### Phase 4: 场景启动 & 相机
- [x] SceneBootstrap — RuntimeInitializeOnLoadMethod 启动
- [x] BootstrapRunner — DestroyImmediate 清理旧对象
- [x] CameraFollow — 平滑跟随 + 动态边界钳制
- **Status:** complete

### Phase 5: Bug 修复 & 迭代
- [x] Tag 系统不兼容 → 改用组件查找
- [x] SceneSetup.cs 编辑模式旧对象持久化 → DestroyImmediate 清理
- [x] DontDestroyOnLoad 旧 GameManager 抢占 Instance → Aggressive Awake
- [x] 协程清理导致旧对象 Update 仍执行 → 直接 Start 内 DestroyImmediate
- [x] 摄像机显示地板外 → 动态 CameraFollow 钳制
- [x] Prefab 组件缺失 → 改为直接 new GameObject + AddComponent
- [x] 人物方向固定 → 按水平输入 flipX
- [x] 经验条重启不清零 → 多处 UI 重置
- [x] UI 文本（Kills/HP/Wave/Time/XP/Lv）不显示 — 已修复：HUD 文本创建后绑定到 UIManager，Legacy Text 运行时可正常更新
- **Status:** complete

### Phase 6: 敌人多样性与角色成长
- [x] 新增敌人类型：Slime、Bat、Brute、Shooter
- [x] 按波次概率解锁：Wave 2 出现 Bat，Wave 3 出现 Brute，Wave 4 出现 Shooter
- [x] Shooter 保持距离并发射敌方弹幕
- [x] Brute 高血量高伤害慢速推进
- [x] Bat 低血量高速度快速骚扰
- [x] 每次升级固定提升攻击倍率与防御力
- [x] 受伤结算按防御力减伤，最低保留 1 点伤害
- [x] 新增 DEF HUD 并随升级/重启刷新
- [x] HUD 血量/经验具体数值与等级显示
- [x] Unity 编译与 Play Mode 运行验证
- **Status:** complete

### Phase 7: 精英/Boss 与掉落系统
- [x] 扩充小怪：Slime、Bat、Brute、Shooter、Skeleton、Crawler
- [x] 新增精英：EliteKnight、EliteWarlock、StoneGolem、ShadowAssassin
- [x] Wave 6 启用精英，首次出现为 3 或 5 波后，之后继续随机间隔 3 或 5 波
- [x] 新增 Boss：SlimeKing、DemonOverlord
- [x] Wave 10 启用 Boss，每 10 波出现一次
- [x] 小怪按概率掉落金币，金币数量随波次提升
- [x] 精英高概率掉落道具，Boss 固定掉落更多金币和道具
- [x] 新增即时道具：生命药剂、力量水晶、护甲碎片、迅捷护符
- [x] 新增 Gold HUD 和事件提示文本
- [x] Unity 编译、Play Mode 精英/Boss 生成与掉落验证
- **Status:** complete

### Phase 8: 敌人形象与成长修正
- [x] 新增 Skeleton、Crawler、四种精英和两种 Boss 的独立像素轮廓
- [x] 骑士/刺客/术士/石魔/史莱姆王/魔王不再使用通用色块
- [x] 为 Enemy 增加 armor 防御属性，玩家攻击先减去防御且至少造成 1 点伤害
- [x] Wave 1 起小怪生命约每波 +4%，攻击随缩放提升，防御约每 3 波 +1
- [x] 精英/Boss 也随当前波次继续成长
- [x] 角色升级时恢复最大生命 15%
- [x] Unity 编译、精灵图案透明度、Wave 10 属性和升级回血验证
- **Status:** complete

### Phase 9: 战斗反馈、区域与商店
- [x] 启动时清理精灵缓存，确保精英/Boss 专属图案立即生效
- [x] 精英和 Boss 头顶显示跟随血条，受伤后按比例刷新
- [x] Shooter/Skeleton/Crawler 远程攻击，Bat/Slime 冲刺，Brute 发散弹幕
- [x] EliteKnight/ShadowAssassin 冲刺，EliteWarlock 扇形/发散弹，StoneGolem 发散弹幕
- [x] SlimeKing 冲刺/发散弹切换，DemonOverlord 扇形/发散弹切换
- [x] 地图扩展到 80x80 单位，玩家和相机边界同步扩大
- [x] 每 5 波切换一次区域：草原、雪原、熔岩、虚空
- [x] 新增商店，B 键开关
- [x] 商店可用金币购买血瓶和攻击药剂
- [x] 按 1 使用血瓶恢复 60 HP，按 2 使用攻击药剂永久提升 35% 攻击
- [x] Unity 编译与 Play Mode 验证
- **Status:** complete
### Phase 10: 商店修复、性能优化与命名映射
- [x] 商店 UI 重排，描述文字 pivot 修复消除重叠
- [x] 开商店 Time.timeScale = 0，角色和怪物完全暂停
- [x] 关商店/重启 timeScale = 1，状态正确恢复
- [x] SceneBootstrap 删除 DestroyImmediate 循环，BuildAll 复用 Camera，地图纹理减半，启动卡顿消除
- [x] GetEnemySprite 枚举到精灵键映射，精英/Boss 显示专属 16×16 图案
- [x] Unity 编译通过，Play Mode 实测暂停和 UI 均正常
- **Status:** complete

### Phase 11: 商店购买闭环、启动卡顿根因与符文系统
- [x] 修复道具精灵 fallback（GetItemSpriteName 显式下划线映射）
- [x] SceneBootstrap.EnsureEventSystem() 自动补建 EventSystem，商店按钮可点击购买
- [x] 商店描述文字与商品按钮水平分离，消除重叠
- [x] 关闭编辑器域重载（Enter Play Mode Options）消除点 Play 卡顿
- [x] 新增 RuneSystem + RuneAttacks：每 3 级 3 选 1 符文，12 种效果
- [x] 统一模态门 IsModalOpen，开商店/符文面板时全场暂停
- **Status:** complete

### Phase 12: 无敌符文、符文无限叠加与升级移速成长
- [x] 新增无敌符文：首次 2.5s 无敌，此后每 6s 获得 1.5s 无敌，仅可获取一次
- [x] 一次性符文统一标记（不屈/无敌），已拥有则从候选池移除并在卡片上标注
- [x] 符文可无限叠加：攻击类型符文第二次起额外提升该类型伤害（散射 +12%，环绕/雷霆/灼烧 +20%）
- [x] 升级提升小额移动速度（+0.12/级），20 级封顶，重开重置
- [x] 符文卡片显示「（仅一次）」或「 Lv.N」后缀
- **Status:** complete

### Phase 13: 开局符文、击杀回血、限时道具与跨局存档
- [x] 开局即赠送一次符文 3 选 1（Start 中 pendingRuneChoices 保底 1）
- [x] 击杀精英怪回血 25%、Boss 回血 50%（HealPlayerPercent）
- [x] 升级成长上调：攻击 +0.18/级、移速 +0.16/级、升级回血 20%
- [x] 难度下调：时间系数 0.5→0.35、波次系数 0.04→0.03、敌方护甲 /3→/4、每波 +2→+1
- [x] Boss 每 15 波一次（起始 15、间隔 15），精英怪随机 3 或 5 波
- [x] 限时道具 4 种（狂暴/迅捷/铁壁/磁力）+ 背包 + 按键 3~6 使用 + HUD 倒计时
- [x] 金币与道具跨局持久化（GamePersistence / PlayerPrefs），重开继承、等级与移速重置
- **Status:** complete

### Phase 14: 背包、悬停提示、3 存档位与主菜单
- [x] 新增 ItemCatalog.cs 道具元数据表（名称 / 描述 / 拾取提示 / 热键 / 精灵名 / 主色 / 背包上限）
- [x] 道具上限各不相同：血瓶 9 / 攻击药剂 5 / 狂暴 9 / 迅捷 9 / 铁壁 6 / 磁力 6
- [x] 新增 InventorySlotUI.cs：格子悬停弹出效果说明，移出收起
- [x] 背包界面（按 I 开关）：金币栏 + 6 个道具格子（图标 / xN 上限 / 空槽压暗 / 满槽变橙）
- [x] 道具悬停提示框：标题「名称  N / 上限」+ 正文「效果说明 + 按 X 使用」
- [x] GamePersistence 重写为 3 存档位（SelectSlot / SlotExists / CreateOrOverwrite / ClearSlot / GetSlotGold / GetSlotItemTotal / GetSlotSummary）
- [x] 旧全局存档自动迁移进 1 号槽（MigrateLegacySaveIfNeeded）
- [x] 主菜单：开始游戏 / 继续游戏（无档置灰）/ 游戏说明 / 退出游戏
- [x] 存档选择面板：3 个槽位按钮，空档显示「空存档」，有档显示「金币 / 道具数 / 波次 / 击杀 / 用时」
- [x] GameManager 重构：StartRun(newGame, slot) / ReturnToMenu(saveRun) / ClearRun()
- [x] 暂停真源扩展为全模态：商店 / 符文 / 背包 / 主菜单 / 存档选择 / 说明
- **Status:** complete

### Phase 15: 主菜单独立场景、悬停提示定位与存档隔离
- [x] 新增 MenuFlow.cs 场景交接 + MainMenuScene.unity 独立菜单场景（Build Settings 首位）
- [x] 新增 MainMenuBootstrap.cs（AfterSceneLoad，仅主菜单场景生效）
- [x] 新增 MenuUI.cs：标题《永夜幸存者》+ 副标题 + 4 个居中按钮 + 存档/说明面板，删除多余说明文字与底部提示行
- [x] 新增 MenuBackground.cs：程序化动态夜空背景（渐变 + 月亮 + 塔楼剪影 + 闪烁星）
- [x] SceneBootstrap CanvasScaler 补 1280×720 + match 0.5，修复面板/按钮出屏
- [x] 删除 SceneBootstrap / UIManager 中旧的 in-scene 主菜单、存档选择、说明面板代码
- [x] 道具悬停提示框改为紧贴格子正下方（ShowItemTooltip(kind, anchor) + PlaceTooltip，带画布内 Clamp）
- [x] 金币与道具按存档隔离：金币变更即时写回当前槽位（GamePersistence.SetBankGold）
- [x] GameManager 用 LoadScene(MainMenuScene) 替换 in-scene 主菜单，InitGame 依 MenuFlow.HasPending 决定进游戏或回菜单
- **Status:** complete

## Key Questions
1. 为何 Canvas 中部分 Text 不渲染而调试 Text 正常？→ 已确认 Canvas 系统可用
2. 文本的 RectTransform 定位是否正确？→ 锚点 bug 已修复
3. 面板遮罩是否覆盖文字？→ 已改为 inactive 默认
4. 新敌人如何保持玩法差异？→ Bat 靠速度压制，Brute 靠血量/伤害压迫，Shooter 靠远程消耗
5. 防御力如何生效？→ `TakeDamage` 使用 `Mathf.Max(1, damage - armor)`，保证玩家不会被完全免伤

## Decisions Made
| Decision | Rationale |
|----------|-----------|
| 不使用 Prefab 系统 | 解决序列化对象组件缺失问题 |
| 运行时动态生成所有实体 | new GameObject + AddComponent 保证组件齐全 |
| DestroyImmediate 同步清理 | 避免旧对象 Update 在帧间执行 |
| 摄像机动态钳制 | 基于 orthoSize * aspect 精确计算，不扩大地板 |
| 程序化生成像素精灵 | 无需任何外部图片素材 |

## Errors Encountered
| Error | Attempt | Resolution |
|-------|---------|------------|
| Tag `Player`/`Enemy` 不存在 | ~5 | 全面改用 FindObjectsOfType + Singleton |
| SceneSetup.cs 编辑模式对象持久化 | ~8 | BootstrapRunner 在 Start 中 DestroyImmediate 清理 |
| DontDestroyOnLoad GM 抢占 Instance | ~3 | Awake 中直接替换旧 Instance |
| 协程 yield 期间旧对象 Update 报错 | ~2 | 放弃协程，Start 内同步 DestroyImmediate |
| Prefab 实例组件缺失 | ~2 | 放弃 Prefab 系统，直接 GameObject 创建 |
| 摄像机超出地板边界 | ~3 | 动态计算 camera clamp |
| UI 文字不显示 | ~4 | 面板遮盖+锚点错误+字体回退，部分修复 |
| 人物方向不变 | 1 | 添加 flipX |
| 经验条重启不清零 | 1 | 多处 UI 重置 |

## Notes
- 项目路径: `E:\Unity CSharp Workplace\demo\PixelSurvivor`
- Unity 版本: 2022.3.57f1c2 (D:\Unity\editor\2022.3.57f1c2)
- 零外部素材依赖，所有精灵程序化生成
