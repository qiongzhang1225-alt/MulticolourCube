# 彩色方块 · Multicolour Cube

> 一款基于 Unity 2022 的 2D 平台跳跃游戏。玩家操控一个可四面旋转的彩色方块，利用每个面的特殊能力穿越关卡，收集星星与金币，解锁更多章节，并在终章 Boss 战中通过颜色解谜击败巨型三角形 Boss。

##  参赛试玩 Demo 下载

Windows 平台可运行 Demo（解压即玩，无需安装）：

[点击下载完整 Demo](https://github.com/qiongzhang1225-alt/MulticolourCube/releases/download/v1.0.0/multicolour-cube.zip)

---

## 游戏特色

- **面能力系统** — 每个面可搭载不同能力（粘墙、跳跃增强、子弹防御、传送锚点）
- **丰富平台机制** — 移动、消失、变色、按钮、撞碎、可重置等多种平台
- **存档点 & 完整状态重置** — 死亡后恢复至最近存档点，所有可重置对象同步还原
- **星星 & 金币收集** — 每关最多 3 颗星，成绩跨会话持久保存
- **章节消耗解锁** — 累积足够可用星星后，主动消耗以解锁下一章节
- **关卡选择 UI** — 每关显示三星槽位与最佳成绩，右上角全局资源 HUD
- **Boss 终章战（Level 11）** — 双区颜色解谜 + 多阶段 Boss 攻击模式
- **沉浸式 UX** — 主菜单方块特效 + 气泡背景、设置面板、跨场景 BGM 持续播放

---

## 操作说明

> 仅使用 **键盘 + 鼠标**，无需手柄。所有操作可在游戏内学习关卡（Level 0）中熟悉。

### 基础操作

| 操作 | 按键 | 说明 |
|------|------|------|
| 左右移动 | <kbd>A</kbd> / <kbd>D</kbd> 或 <kbd>←</kbd> / <kbd>→</kbd> | 控制方块水平移动 |
| 上下输入 | <kbd>W</kbd> / <kbd>S</kbd> 或 <kbd>↑</kbd> / <kbd>↓</kbd> | 在粘墙状态下用于上下攀爬 |
| 跳跃 | <kbd>Space</kbd> | 站在地面、天花板或墙面（粘墙）时可跳；空中无法二段跳 |
| **翻滚换面** | **鼠标左键** | 朝鼠标方向翻滚 90°，**让另一个面接触地面 → 切换当前激活的能力**（核心机制） |
| 传送锚点 | 鼠标右键 | 仅在「传送锚点」面激活时可用：首次按键设置锚点，再次按键瞬移回锚点 |
| 暂停 / 设置 | 屏幕左上角按钮 | 暂停游戏、调整音量与分辨率 |
| 关闭通关结算 | <kbd>Esc</kbd> | 在通关界面快速返回 |

### 四面能力切换

方块共有 **四个面**，每个面预先绑定一种能力。**翻滚（鼠标左键）** 是切换能力的唯一方式：哪一面接触地面/墙面，哪一面的能力就生效。

| 当前接触面 | 能力 | 触发与效果 |
|------------|------|------------|
| 粘墙面（StickyWall） | 接触墙壁自动激活 | 零重力贴墙 + <kbd>W</kbd>/<kbd>S</kbd> 上下攀爬 + <kbd>Space</kbd> 墙跳 |
| 跳跃增强面（JumpBoost） | 落在特殊平台 | 该面着地时跳跃力倍增 |
| 子弹防御面（BulletDefense） | 该面朝向子弹 | 自动免疫或反弹来袭子弹 |
| 传送锚点面（TeleportAnchor） | 鼠标右键 | 设置 / 召回锚点，瞬移规避陷阱 |
| 颜色感应面（ColorSensor） | 接触平台 | 把当前面颜色"涂"到变色平台，用于解谜 |

> **小贴士**：颜色感应配合 Boss 关的解谜机制 —— 翻滚到不同颜色面，再去踩对应颜色的变色平台，让 SuggestUI 的提示全部点亮即可掉落奖励箱。

### Boss 关额外说明

- **5 颗心血量** — Boss 关玩家自带 5 颗心，被击中只损失 1 颗并在重生点复活，**不会**触发死亡画面；5 颗心耗尽时才算死亡并重置关卡
- **彩球（ColorBall）** — 从天而降的小球颜色提示当前要解的是 **左侧** 还是 **右侧** 的颜色组合
- **奖励箱** — 解开任意一侧的颜色组合后，奖励箱从空中掉落；接触它会随机开出 **追踪导弹**（攻击 Boss）或 **爱心**（恢复 1 点血量）

---

## 关卡结构

| 章节 | 关卡 | 解锁条件 |
|------|------|----------|
| 第一章 | Level 1 – 5 | 默认开放 |
| 第二章 | Level 6 – 10 | 消耗 **12 颗**可用星星 |
| 终章   | Level 11（Boss） | 消耗解锁 / 通关第二章 |

- **Level 0** — 测试 / 教学关卡，始终可进入
- 章节内，当前关获得 **≥ 2 颗星**即可解锁下一关
- 可用星星 = 各关最佳星星总和 − 已消耗星星数

---

## 玩家能力

| 能力 | 触发方式 | 效果 |
|------|----------|------|
| 粘墙（StickyWall） | 对应面接触墙壁 | 零重力攀附 + 墙跳 |
| 跳跃增强（JumpBoost） | 对应面落在特殊平台 | 跳跃力倍增 |
| 子弹防御（BulletDefense） | 手动激活 | 免疫或反弹子弹 |
| 传送锚点（TeleportAnchor） | 右键设置 / 传送 | 瞬移回标记点 |
| 颜色感应（ColorSensor） | 接触平台 | 检测接触面颜色，驱动变色平台 |

---

## 平台 & 机关

- 移动平台（定点往返）
- 按钮触发平台
- 消失平台（站上后延迟消失）
- 下方撞碎平台 / 子弹破坏平台
- 变色平台（随玩家面颜色或携色球变化）
- 炮塔 + 子弹
- 死亡区域（即死）
- **携色球（BallColorCarrier）** — 让非玩家物体也能触发变色平台
- **死亡清理区（DestroyOnDeathZone）** — 玩家死亡时清空区域内残留物
- **周期重生器（PeriodicRespawner）** — 用于循环刷新道具/小球

---

## Boss 关卡（Level 11）

终章 Boss 战是颜色解谜与弹幕躲避的结合：

### Boss 攻击模式
- **追踪激光（BossLaser）** — 长时间预瞄 + 限速旋转，玩家可读条躲避
- **散射子弹（BossBullet）** — 多发扇形弹幕
- **空中投掷物（BossRainZone）** — 上空区域按权重随机掉落多种 prefab

### 玩家解谜攻击流程
1. **提示面板（BossSuggestUI）** — 8 格 SpriteRenderer（左 4 + 右 4），显示当前需要的目标颜色
2. **彩色球（ColorBall + BallColorCarrier）** — 从掉落区随机生成，球的颜色决定要解出哪一侧（左/右）
3. **变色平台 + 不可达机关** — 每侧 4 块平台，其中一块需用彩球去触发
4. **奖励箱（BossRewardBox）** — 当一侧 ColorConditionGroup 全部满足，从天而降，开出：
   - **追踪导弹（BossBomb）** — 立即锁定 Boss，命中造成 2 点伤害
   - **爱心（BossHeartPickup）** — 玩家恢复 1 点生命

### 玩家生命系统
- **5 颗心血量（BossPlayerHP）** — 仅 Boss 关启用
- 受击后默认软复活（直接回到 Spawn 点，不弹死亡面板）
- 5 颗心耗尽时才触发完整死亡流程并重新加载场景
- 顶部 HUD 实时显示剩余血量

---

## 数据持久化

所有关卡成绩通过 `PlayerPrefs` 跨会话保存：

| 键名 | 类型 | 说明 |
|------|------|------|
| `BestStars_{关卡名}` | int | 该关最佳星星数 |
| `BestCoins_{关卡名}` | int | 该关最佳金币数 |
| `Completed_{关卡名}` | int (0/1) | 是否通关 |
| `SpentStars` | int | 已消耗用于解锁的星星数 |
| `ChapterUnlocked_{索引}` | int (0/1) | 对应章节是否已解锁 |
| `BGMVolume` / `SFXVolume` | float | 设置面板音量保存 |

---

## 主菜单 & UI

- **MenuBackground / MenuBubbles / MenuCube** — 主菜单动态背景与方块特效
- **GameHUD** — 关卡内右上角 HUD（金币、星星、计时）
- **SettingsPanel** — 设置面板（音量、分辨率等）
- **DeathEffectUI** — Mario 风格死亡画面（独立 Canvas，可挂特效）
- **VictoryUI2** — 通关结算面板
- **BossHealthBar** — Boss 关顶部血条

---

## 音频系统

- **BGMManager** — 单例 + DontDestroyOnLoad，跨场景延续 BGM
- **SceneBGM** — 每个场景挂一个，告诉 BGMManager 切换到目标曲目
- **设置面板** — 音量实时联动 PlayerPrefs

---

## 项目结构

```
Assets/
├── Code/
│   ├── Players/          # 玩家控制器 & 面能力
│   ├── Map/              # 平台、机关、存档点、携色球
│   │   └── Re/           # 可重置对象系统
│   ├── Boss/             # Boss、激光、子弹、奖励箱、心、HP
│   ├── Canvas/           # UI（胜利、死亡、HUD、设置）
│   ├── Camera/           # 视差滚动
│   ├── Audio/            # BGMManager、SceneBGM
│   ├── Menu/             # 主菜单背景、气泡、方块特效
│   ├── LevelDataManager.cs   # 数据持久化工具
│   ├── ChapterConfig.cs      # 章节配置 ScriptableObject
│   ├── LevelButton.cs        # 关卡选择按钮组件
│   ├── LevelSelect.cs        # 关卡选择控制器
│   └── GameTimer.cs          # 计时器单例
├── Scenes/
│   ├── Main_menu.unity
│   ├── LevelSelect.unity
│   └── Level 0-11.unity
├── Prefabricate/         # 角色、平台、道具、Boss 预制体
│   ├── BossBullet / BossLaser / BossMissile / BossRewardBox
│   ├── ColorBall / PlayCube
│   ├── BGMManager / GameHUD / SettingsPanel
│   └── DeathOverlay / Vanish / moveable …
├── Settings/
│   └── ChapterConfig.asset   # 章节结构配置
├── Materials/            # 材质
├── Texture/ & Screenshots/   # 贴图与截图
├── smiley-sans-v2.0.1/   # 中文字体
└── bgm/                  # 背景音乐
```

---

## 技术栈

| 项目 | 版本 |
|------|------|
| Unity | 2022.3.62f1 (LTS) |
| 渲染管线 | Universal Render Pipeline (URP) 2D |
| 语言 | C# |
| 相机 | Cinemachine 2.10.4 |
| UI 文字 | TextMesh Pro 3.0.9 |
| 字体 | ZCOOL 快乐体 · 得意黑 · Smiley Sans |

---

## 如何运行

1. 克隆仓库
   ```bash
   git clone https://github.com/qiongzhang1225-alt/unity.git
   ```
2. 用 **Unity Hub** 打开项目（推荐版本 2022.3.x LTS）
3. 等待依赖包自动导入
4. 打开 `Assets/Scenes/Main_menu.unity`，点击 Play

---

## 分支说明

| 分支 | 说明 |
|------|------|
| `main` | 稳定版本 |
| `master` | 开发分支（含最新 Boss 关与各项系统） |
