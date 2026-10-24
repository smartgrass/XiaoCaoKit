# XiaoCaoKit (非完整)

非完整项目,无法直接导入

 Unity 游戏框架层：资源、事件、计时、实体、技能、配置、UI 与编辑器工具。

初衷是把 GamePlay 里反复出现的工具函数、配置加载、技能编辑流程收拢到一起，**让业务代码只写规则，表现和数值全部外置**。
  
代码在 `Assets/XiaoCaoKit`，`Runtime/Demo` 是跑在这套框架上的业务层，运行入口为 `Assets/_Scene/GameLauncher.unity`（项目外）。

> **路径约定**：文中未标注的路径都相对于 `Assets/XiaoCaoKit`（本子模块根目录）；
>   
> 标注「项目外」的路径属于宿主工程，不在本仓库内。

---

## 开发目标

### 1. 总目标

一套能直接带到下一款动作游戏里复用的框架层：换皮、换关卡、换技能都只改资产和配置，不动框架代码。

三条硬指标：

- **表现可外置**：位移、动画、特效、音效、伤害判定全部在编辑器里编排，不写死在代码里
- **数据可外置**：技能、关卡、数值、文案走 Luban 表 / SO / ini，改数值不重新编译
- **逻辑可拆**：实体 + 组件分段（移动 / 控制 / 动画 / 受击），单个角色逻辑不至于糊成一个几千行脚本

### 2. 设计原则

| 原则     | 具体做法                                                   |
| ------ | ------------------------------------------------------ |
| 数据驱动   | 技能走时间轴资产，关卡走 `LevelSetting` + 场景预制体，数值走 Luban          |
| 组合优于继承 | `Entity` / `BehaviorEntity` / `Role` 三层，能力靠组件挂载而不是层层派生 |
| 弱依赖    | 网络、打包、平台能力做成可摘除模块，核心玩法不依赖它们                            |
| 编辑期优先  | 能靠编辑器工具解决的（绑定、动画机、存档调试），不做成运行时约定                       |
| 轻量     | 面向个人/小团队，不引入重型框架，不为了架构而架构                              |

### 3. 分层目标与现状

| 层                                  | 职责                                                                    | 现状                |
| ---------------------------------- | --------------------------------------------------------------------- | ----------------- |
| 基础工具 `0Common` `1Tools` `Log`      | 单例、标记对象、数学/颜色/字符串/File 扩展、日志与调试 GUI                                   | 完成                |
| 资源 `ResMgr`                        | YooAsset 加载、引用计数、各类对象池、缓存池                                            | 完成                |
| 事件与时间 `3Event` `Timer`             | 全局消息系统、UniTask 计时、`XCTime` 统一时间缩放                                     | 完成                |
| 实体 `2Entity`                       | Entity / BehaviorEntity / Role / RoleControl / RoleMovement / RoleMgr | 完成（属性修改器未封装）      |
| 技能 `XCSkillEditor`                 | Flux 时间轴 → `XCTask` → `XCTaskRunner`                                  | 完成（预设 Tag 自动索敌待做） |
| 配置 `Luban` `Config`                | Luban 表、ini、SO 加载、本地存档                                                | 完成                |
| UI `Framework/UI` + `Demo/UI/Base` | 面板管理、Tab/子面板、滚动列表、Toast、弹窗、按钮组                                        | 完成                |
| 玩法模块 `GamePlayModoule`             | 相机、输入、关卡流程、战斗辅助（命中/顿帧/层标记）                                            | 基础可用（索敌视角、场景进度待做） |
| 编辑器工具 `Editor`                     | 技能编辑器、动画窗口、资源/构建工具、存档调试                                               | 完成                |

### 4. 明确不做的部分

- **联网与服务端**：单机优先，只在需要时以模块接入，可随时剥离（网络框架参考 Fantasy / MagicOnion）
- **重型依赖与平台适配层**：不为假设中的需求提前抽象
- **打包流程**：保持最小可用（`Editor/Tools/BuildTool.cs` 与项目外的 `Tools/Build.bat` 均为占位），不投入额外精力

---

## 已有能力

按目录对照，只列主干。

### 基础工具（`Runtime/Framework/0Common`、`1Tools`、`Log`）

- 单例基类：`Singleton` / `MonoSingleton` / `MonoSingletonPrefab`
- `MarkObject` + `MarkObjectMgr`：场景对象标记与快速取用
- 工具集：Animator / 颜色 / 数学 / 字符串 / File / Unity 扩展 / EzySlice 切割
- 日志：`Debuger` + `DebugGUI` 运行时调试信息

### 资源（`Runtime/Framework/ResMgr`）

- `ResMgr`：YooAsset 编辑器模拟与真机 bundle 统一入口
- 对象池族：`ObjectPool` / `AssetPool` / `CachePool` / `LoopPool` / `PoolMgr`
- `AssetsUsing` / `ShortKeyCache`：资源引用与句柄缓存

### 事件与时间（`Runtime/Framework/3Event`、`Timer`）

- `GameEvent`：字符串 key + 强类型参数的消息总线，支持延迟/条件发送
- `EventDispatcher` / `EventMgr`：内部事件分发与监听生命周期管理
- `XCTime`：统一 `deltaTime / timeScale`，时停、子弹时间、顿帧都走同一套时间源
- `Timer`：UniTask 驱动的延时/循环计时器

### 实体（`Runtime/Framework/2Entity`）

- `Entity` / `BehaviorEntity`：取 ECS 的「数据与行为分离」思路，但不是完整 ECS
- `Role`：角色主体，皮肤（外观）与种族（技能）解耦，支持运行时换装换武器
- `RoleControl`：技能释放、状态切换、时停处理、动画速度
- `RoleMovement`：位移、重力、地面检测、掉出地图保护
- `HealthBehavior`：生命与受击、死亡事件
- `RoleMgr` / `EnemyMaker` / `IdMgr`：角色注册、敌人生成、运行时 id

### 技能编辑器（`Runtime/Modules/XCSkillEditor` + `Editor/ModuleEditor/FluxEditor`）

- 基于 Flux 的时间轴，自定义轨道覆盖：
  - 位移（`XCMoveEvent` / `XCLineEvent`）
  - 动画（`XCAnimEvent`，帧事件驱动）
  - 特效与表现（`XCShowEvent`）
  - 音效（`XCAudioEvent`）
  - 伤害触发（`XCTriggerEvent`）
  - 消息（`XCMsgEvent`）
  - 命令（`XCCommondEvent`）
- 自定义命令轨道 `IXCCommand`：移动到目标、输入接续、输入等待、普攻派生等
- 保存为 `XCTask` 数据，运行时 `XCTaskRunner` 逐帧推进
- 动画机状态由编辑器重建（`FluxStateMachine` 是衍生数据，不反向同步）
- 怪物行为复用同一套序列，不单写 AI 动画逻辑

### 配置与存档（`Runtime/Modules/Luban`、`Config`）

- `LubanTables`：表数据读取 + 默认值容错，产物目录为 `ExtraRes/GameConfig/Luban`（项目外）
- `IniFile`：轻量配置（本地化、音效表等），改完直接生效
- SO 配置加载：技能、纹章、奖励池、AI 命令等
- `SaveMgr`：`persistentDataPath/Data/*.data` 二进制存档（项目外的运行时目录），配 `PlayerSaveDataWindow` 调试

### UI（`Runtime/Framework/UI`、`Demo/UI/Base`）

- `UIMgr` + `UIPrefabs`：面板加载、层级、显隐与返回栈
- `SubPanel` / `TabPanel` / `TabView`：页签与子面板基类
- 格子与列表：Unity 原生 `ScrollRect` + 项目格子脚本（`ItemCell` / `BuffItemCell` 等）
- Toast、Dialog、ButtonGroup、按钮事件封装
- 本地化：`Localizer` + 中/英 ini（`ExtraRes/GameConfig/Localize`，项目外），key 集中在 `LocalizeKey`

### 玩法模块（`Runtime/Modules/GamePlayModoule`）

- 相机：`CameraMgr`（俯视 / 第三人称切换）、`CamerAimer`、`CamEffectMgr`（震屏）、`ObstacleFadeController`（遮挡淡出）
- 时间与打击：`TimeStopMgr`（时停）、`HitStop`（顿帧）、`HitHelper`（伤害盒判定）
- 输入：New Input System，`MobileInput` 提供摇杆、触屏、按键字段
- 关卡流程：`GameMgr` 状态机 + `SceneLoader` + `Loading` 场景（项目外 `_Scene/Loading.unity`），`LevelControl` 管敌人生成与起终点
- 战斗辅助：`PullRole`（吸附）、`ExecuteGroup`（延时/条件执行组）、层与 Tag 常量生成

### 编辑器工具（`Editor`）

- `XiaoCaoWindow`：工具窗口基类与通用绘制
- 动画：参数增删、曲线调试、速度烘焙、呼吸姿态、批量修改
- 资源：引用查找、资源替换、Sprite 导入后处理、Shader 变体收集
- 构建：`BuildTool` / `YooAssetBuildHelper` / `CIBuildHelper`
- 存档调试：`PlayerSaveDataWindow`，可直接改等级、金币、技能、支援角色养成

---

## 使用方式

新增代码按分层放，不要往 `Demo` 里堆通用逻辑：

| 内容                      | 位置                   |
| ----------------------- | -------------------- |
| 通用工具 / 与业务无关的能力         | `Runtime/Framework/` |
| 可复用的玩法系统（相机、输入、战斗辅助）    | `Runtime/Modules/`   |
| 具体游戏的业务（角色、道具、关卡、UI 面板） | `Runtime/Demo/`      |
| 编辑器窗口与工具                | `Editor/`            |

配置表修改后生成：在项目外的 `Tools/` 目录执行 `gen_code_data.bat`，代码进 `Runtime/Modules/Luban/LubanGenCode`，
  
数据进项目外的 `ExtraRes/GameConfig/Luban`。

### 依赖的项目外目录

这些内容不在本子模块内，但框架的运行与构建离不开它们：

| 目录（项目外）                         | 用途                                         |
| ------------------------------- | ------------------------------------------ |
| `Assets/_Scene/`                | 游戏场景：GameLauncher / Home / Level / Loading |
| `Assets/_Res/`                  | 游戏资源根目录（YooAsset 收集路径）                     |
| `Tools/Datas/`                  | Luban 表源 xlsx（关卡、技能、道具、任务等）                |
| `Tools/`                        | 表生成脚本、打包脚本、贴图与模型处理工具                       |
| `ExtraRes/GameConfig/Luban/`    | 表数据产物                                      |
| `ExtraRes/GameConfig/Localize/` | 中 / 英文案 ini                                |

---

## 依赖的库

除 Flux 被内嵌改造过外，框架没有把第三方库改成私有实现。下面按引入方式分成三组。

### UPM 包（Package Manager）

| 库 | 包名 / 仓库 | 版本 | 用途 |
| --- | --- | --- | --- |
| YooAsset | `com.tuyoogame.yooasset` | 2.2.4-preview | 资源加载与打包（`ResMgr`） |
| UniTask | `com.cysharp.unitask`（Cysharp/UniTask） | git 源，未锁 tag | 异步、计时器、TMP 扩展 |
| Cinemachine | `com.unity.cinemachine` | 2.10.7 | 相机控制与镜头 |
| TextMeshPro | `com.unity.ugui`（Unity 6 起合并） | 2.0.0 | 文本渲染 |
| SharpZipLib | `com.unity.sharp-zip-lib` | 1.4.1 | `ZipHelper` 压缩解压 |
| UniVRM | `com.vrmc.gltf` / `com.vrmc.univrm` / `com.vrmc.vrm` | 0.128.3 | VRM 模型与姿态调试 |
| Newtonsoft.Json | `com.unity.nuget.newtonsoft-json` | 3.2.2 | 配置、日志的 JSON 序列化 |

### 内置依赖（随本仓库提交，不走 UPM）

| 库 | 来源 | 版本 | 用途 | 路径 |
| --- | --- | --- | --- | --- |
| NaughtyAttributes | dbrizov/NaughtyAttributes | 2.0.7 | Inspector 增强特性 | `Plugin/NaughtyAttributes` |
| Odin Serializer | Sirenix Odin Serializer | 1.1.1.0 | 存档二进制序列化（`SaveMgr`） | `Plugin/Sirenix` |
| Rotary Heart Physics | PhysicsDebugExtension（`PhysicsExtension` 程序集） | 未标注 | 物理调试扩展 | `Plugin/Rotary Heart` |
| GG.Extensions | Greener-Games/Unity_Extensions | 未标注 | Unity 类型扩展（AspectRatio / Color / Await 等） | `Runtime/Framework/1Tools/Unity_Extensions` |
| Flux | noio/Flux 的改造版 | 未标注 | 技能时间轴（`XCFlux`，动画与序列部分有改动） | `Runtime/Modules/XCSkillEditor/Runtime/XCFlux` |

### 项目外依赖

只剩一个：DOTween（Demigiant，Asset Store 版，版本未标注），放在项目外的 `Assets/Plugins/Demigiant`，
用于补间动画；其 UI 模块已关闭（`DOTweenModuleUI` 顶部 `#if false`），UI 动画走项目自己的扩展。

几点说明：

- 版本以项目外的 `Packages/manifest.json` 为准；内置插件看各自目录里的 `package.json` / `Version.txt`，查不到的就写「未标注」。
- 事件系统 `GameEvent` / `EventMgr` 的源码取自 TEngine，直接内嵌并保留 `TEngine` 命名空间，不算包依赖。
- URP 是宿主工程的渲染管线，框架代码不引用 `UnityEngine.Rendering.Universal`。

---

## 待办

- 属性修改器封装：目前加/减属性散在 `RoleControl` 与纹章效果里，缺统一入口
- 索敌视角：相机只做了俯视与第三人称
- 技能预设 Tag：自动索敌、锁定等预设行为，省掉每个技能重复配置
- 场景进度：`RunDemo` 启动流程中的加载进度还没接到 Loading 表现

## 其他

虽不做网络部分，但有网络框架引路

[Fantasy](https://github.com/qq362946/Fantasy)

[Magiconion](https://github.com/Cysharp/MagicOnion)

## 关于开源项目的看法

未来网络上开源的项目会越来越多，对个人开发者十分友好，

开源规模从小到大, 脚本->工具->框架->示例项目

示例项目得剔除项目内容比较麻烦暂时做不了

## 吐槽

Entity 的模式有点难度,但拆出数据和逻辑很干净
