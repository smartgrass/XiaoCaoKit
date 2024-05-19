# XiaoCaoKit

工具集子项目,按需提取吧

此框架是做项目顺手做的, 方便下个项目快速启动开发，特地拆出子项目。

无法直接使用，需要补全很多库引用。

推荐直接提取一部分功能使用。

此项目将收集GamePlay中会经常用到的工具函数，以及常用的配置加载等等，是对XiaocaoTool和技能编辑器的进一步整理。

主要目标是GamePlay部分，对于热更代码，服务器等不侧重。

## 开发目标


* 1.XiaoCao框架专属
	* 1.1 XiaoCaoWindow 编辑器扩展 √ XiaoCaoWindow.cs
	* 1.2 XiaoCao的Entity (借鉴Ecs的id-容器思路,管理核心动态对象) √
		* Entity.cs
		* BehaviorEntity.cs
	* 1.3 XCSkillEditor 技能编辑器
		* 位移&动画 √
		* 伤害触发器 √
		* 消息 √
		* 特殊处理 
			* 自定义脚本轨道 IXCCommand √ 
			* 预设Tag (如自动索敌)

* 2.基本框架/工具
	* YooAsset资源加载模块 √ ResMgr.cs
	* 对象池 √ AssetPool.cs

	* UniTask&计时器 √ XCTime.cs
	* 消息系统 √  GameEvent.cs
	* Log封装 √ Debuger.cs
	* 调试信息GUI √ DebugGUI.cs 

	* 配置加载 √ LoadSoConfig.cs (自动new)
	* 存档加载 √ SavaMgr.cs
	* 编辑器工具
		* 组件绑定扩展  XCComponentMenu.AutoBind()

* 3.GamePlayModule GamePlay模块
	* 3.1 相机控制 √ CameraMgr.cs
		* TopDown √
		* 第三人称 √
		* 索敌视角 ------- TODO
	* luban表格配置 √ LubanTables.cs
	* 游戏启动流程 RunDemo.cs
		* 等待配置,资源加载完毕-> √
		* 加载玩家-> √
		* 场景进度 ------- TODO

	* 游戏数据 √ GameData.cs
	* 游戏周期 √
		* 加载, 开始, 运行, 完成, 退出  GameState
		* 配合消息系统状态切换时,发送消息  GameMgr.cs
	

 * 玩家 Player0.cs
	* 移动 √
	* 平a&翻滚 ------- TODO
	* 死亡&受击 ------- TODO
	* 1个技能 ------- TODO
 * 敌人 ------- TODO

 * 关卡 GamePlayModule
	* 场景切换
	* 门 : 进入后切换关卡


 * 常用UI GamePlayModule (低优先)
	* 血条ui ------- TODO
	* 物体/NPC交互组件 ------- TODO
	* 物品格子容器/详情 ------- TODO 
		* InventoryUI.cs




### 暂时做的部分

打包 x (对个人开发似乎不是很必要, 挖个坑就好)

网络服务 x (对单机开发是负担, 但会做为模块接入, 可随时移除)



需要安装

Cinemachine -> PackageManager

NewtonJson  -> PackageManager

DoTween		-> PackageManager

UniTask		-> PackageManager

yooAsset -> PackageManager



## 其他

虽不做网络部分，但有网络框架引路

[Fantasy](https://github.com/qq362946/Fantasy)

[Magiconion](https://github.com/Cysharp/MagicOnion)


## 关于开源项目的看法

未来网络上开源的项目会越来越多，对个人开发者十分友好，

开源规模从小到大, 脚本->工具->框架->示例项目

示例项目就不建议开源了, 带来的后果是技术贬值, 开源的作者干活累了自己。

Act_Example属于示例项目阶段，不会开源了, 摆烂了。
