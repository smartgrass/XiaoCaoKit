# XiaoCaoKit

## 子项目

框架是做项目顺手做的, 方面下个项目快速启动开发

为了项目和框架拆分,特地拆除子项目

需要补全很多引用,切换到 Act_Example(挖坑当demo相对完整时)


(挖坑:建群&demo&教程细化)


## 开发目标

1.XiaoCao框架专属
---1.1 XiaoCaoWindow编辑器扩展 √
---1.2 XiaoCao的Entity框架  √
	借鉴Ecs的id-容器思路,管理核心对象
	BehaviorEntity代替MonoBehavior
---1.3 XCSkillEditor 技能编辑器
	位移&动画 √
	伤害触发器 ------- TODO
	事件Process & 预设Tag 

2.基本框架/工具
--5.0.YooAsset资源加载模块 √
--5.1.对象池 √
	定时器自动回收 √
--5.2UniTask&计时器 √
--5.3消息系统 √
--5.4Log封装 √

3.GamePlayModule GamePlay模块
---3.1 相机控制 √
	TopDown √
	第三人称 √
	索敌 ------- TODO
---3.2 存档&配置读写 √
---3.3 luban表格配置 ------- TODO
---3.4 游戏启动流程
	等待配置,资源加载完毕-> √
	加载玩家->√
	场景进度 ------- TODO
---3.3 游戏周期&数据√
	加载, 开始, 运行, 完成, 退出 
	配合消息系统状态切换时,发送消息

--- 玩家
	移动 ------- TODO
	平a&翻滚 ------- TODO
	死亡&受击 ------- TODO
	1个技能 ------- TODO
--- 敌人 ------- TODO

---3.4 常用UI(不重要延后)
	血条ui ------- TODO
	物体/NPC交互组件 ------- TODO
	物品格子容器/详情 ------- TODO

### 暂时做的部分

打包 x (对个人开发似乎不是很必要, 挖个坑就好)

网络服务 x (对单机开发是负担, 但会做为模块接入, 可随时移除)



需要安装

Cinemachine ->PackageManager

NewtonJson  ->PackageManager

DoTween		>PackageManager

UniTask		>PackageManager

yooAsset	官网