# XiaoCaoKit(非完整项目)

某个子项目,无法直接运行,小部分代码可提取。

收集GamePlay中常用的工具函数，配置加载等等，对XiaocaoTool和技能编辑器的进一步整理


## 开发目标


* 1.XiaoCao框架专属
	* 1.1 XiaoCaoWindow 编辑器扩展 
	* 1.2 XiaoCao的Entity  (取Ecs的ec思路)
		* Entity.cs  
		* BehaviorEntity.cs 
		* Role.cs  
	* 1.3 XCSkillEditor 技能编辑器
 		* 保存数据 & 运行时 -> XCTask & XCTaskRunner 
		* 位移&动画&特效&音效
		* 伤害触发器 
		* 消息 
		* 特殊处理 
			* 自定义脚本轨道 IXCCommand 
			* 预设Tag (如自动索敌) [TODO]

* 2.基本框架/工具
	* YooAsset资源加载模块  ResMgr.cs
	* 对象池 AssetPool.cs

	* UniTask&计时器 XCTime.cs
	* 消息系统  GameEvent.cs (TEngine里的,很好用)
	* Log封装  Debuger.cs
	* 调试信息GUI DebugGUI.cs 

	* So配置加载  LoadSoConfig.cs 
	* 存档加载 SavaMgr.cs
	* 编辑器工具
		* 组件绑定扩展  XCComponentMenu.AutoBind()
  		* 监视器扩展
    		* 替换资源工具


* 3.1 GamePlayModule GamePlay模块
	* 3.1.1 相机控制  CameraMgr.cs
		* TopDown 
		* 第三人称 
		* 索敌视角 [TODO]
	* 3.1.2 luban表格配置  LubanTables.cs 
	(luban读取有性能优势,复杂的结构, 偏中高量级适用,轻量级不太顺手
	轻量级配置还是使用xx.ini配置 IniFile.cs)
	* 3.1.3 游戏启动流程 RunDemo.cs
		* 等待配置,资源加载完毕-> 
		* 加载玩家 
		* 场景进度 [TODO]

	* 游戏数据  GameData.cs
	* 游戏周期 
		* 加载, 开始, 运行, 完成, 退出  GameState 
		* 配合消息系统状态切换时,发送消息  GameMgr.cs
	
* 3.2 GamePlayModule.Role 角色
	* Role
		* 皮肤-角色技能解耦 
		皮肤Id->外表, mod支持 
		种族Id->技能 
		* 组件模式  (主要是代码分区)
  		* 时停处理 
    		* 属性修改器封装 [TODO]
		
	* 玩家 Player0.cs
		* 移动 
		* 平a&翻滚 
		* 死亡&受击 
		* 1个技能 
	* 敌人
		* AI

* 3.3 GamePlayModule 其他
 * 关卡 GamePlayModule
	* 场景切换
	* 门 : 进入后切换关卡


 * 常用UI GamePlayModule (低优先)
	* 血条ui 
 	* 设置面板 
	* 物体/NPC交互组件  [TODO]
	* 物品格子容器/详情 [TODO]
		* InventoryUI.cs 

* Test工具相关
			



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

示例项目就不建议开源了

## 吐槽

Entity的模式有点难度,需要做很多轮子配合, 但越到后期优势越大,目前轮子们已大功告成




