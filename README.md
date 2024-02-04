# XiaoCaoKit

恭喜你发现了正在开发中一个子项目

此框架是做项目顺手做的, 方便下个项目快速启动开发，特地拆出子项目。

无法直接使用，需要补全很多库引用。

推荐直接提取一部分功能使用。

此项目将收集GamePlay中会经常用到的工具函数，以及常用的配置加载等等，是对XiaocaoTool和技能编辑器的进一步整理。

主要目标是GamePlay部分，对于热更代码，服务器等不侧重。

## 开发目标


* 1.XiaoCao框架专属
	* 1.1 XiaoCaoWindow编辑器扩展 √
	* 1.2 XiaoCao的Entity框架  √ (借鉴Ecs的id-容器思路,管理核心对象)
	* BehaviorEntity代替MonoBehavior
	* 1.3 XCSkillEditor 技能编辑器
		* 位移&动画 √
		* 伤害触发器 ------- TODO
		* 事件Process & 预设Tag 

* 2.基本框架/工具
	* 2.1.YooAsset资源加载模块 √
	* 2.0.对象池 √
		* 定时器自动回收 √
		
	* UniTask&计时器 √
	* 消息系统 √
	* Log封装 √

* 3.GamePlayModule GamePlay模块
	* 3.1 相机控制 √
		* TopDown √
		* 第三人称 √
		* 索敌视角 ------- TODO
	* 3.2 存档&配置读写 √
	* luban表格配置 √
	* 游戏启动流程
		* 等待配置,资源加载完毕-> √
		* 加载玩家->√
		* 场景进度 ------- TODO
	* 3.3 游戏周期&数据 √
		* 加载, 开始, 运行, 完成, 退出 
		* 配合消息系统状态切换时,发送消息

 * 玩家
	* 移动 √
	* 平a&翻滚 ------- TODO
	* 死亡&受击 ------- TODO
	* 1个技能 ------- TODO
 * 敌人 ------- TODO

 * 3.4 常用UI(不重要延后)
	* 血条ui ------- TODO
	* 物体/NPC交互组件 ------- TODO
	* 物品格子容器/详情 ------- TODO

### 暂时做的部分

打包 x (对个人开发似乎不是很必要, 挖个坑就好)

网络服务 x (对单机开发是负担, 但会做为模块接入, 可随时移除)



需要安装

Cinemachine -> PackageManager

NewtonJson  -> PackageManager

DoTween		-> PackageManager

UniTask		-> PackageManager

yooAsset -> PackageManager





## 关于开源项目的看法

未来网络上开源的项目会越来越多，对个人开发者十分友好，

开源规模从小到大, 脚本->工具->框架->示例项目

示例项目就不建议开源了, 带来的后果是技术贬值, 开源的作者干活累了自己。

Act_Example属于示例项目阶段，可能不会直接开源了。