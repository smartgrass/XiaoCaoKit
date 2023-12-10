# XiaoCaoKit

## 开发目标

1.XiaoCaoWindow编辑器扩展 √

2.XiaoCao的Entity框架  √
	借鉴Ecs的E的思路,作为主要实体类,取代MonoBehavior
	抽象

3.Log封装 √

4.YooAsset资源加载模块 --

5.游戏运行周期管理 
	启动界面进度展示->等待配置,资源加载完毕->进入界面
5.1关卡周期
	加载, 开始, 运行, 完成, 退出

6.对象池 (考虑通用性) , 自动回收(t)

7.UniTask&计时器 --

8.消息系统

9.GamePlayModule 


//YooBoot
UniEvent



### 暂时做的部分

打包 x (对个人开发似乎不是很必要, 挖个坑就好)

网络服务 x (对单机开发是负担, 但会做为模块接入, 可随时移除)

