# Shader Variants Collector

一个Unity编辑器插件，用于管理和收集Shader变体到ShaderVariantCollection中。

## 功能特性

### 主要功能
- **外部插件形式**: 可以作为独立插件发布到Unity Asset Store
- **可视化界面**: 提供直观的UI界面进行操作
- **TreeView显示**: 使用TreeView显示ShaderVariantCollection中的Shader和变体
- **变体收集**: 自动收集项目中未包含在ShaderVariantCollection中的变体
- **进度显示**: 收集过程中显示进度条
- **新变体管理**: 新收集的变体可以单独确认或忽略
- **批量操作**: 支持"确认全部"和"忽略全部"操作

### 手动添加功能
- **添加Shader**: 手动添加新的Shader到集合中
- **添加变体**: 为现有Shader添加新的关键字组合变体
- **关键字选择**: 提供关键字选择界面

### 引用查找功能
- **Shader索引**: 点击Shader可以定位到Shader文件
- **变体引用**: 显示变体被哪些Material和Prefab引用

### 删除功能
- **删除Shader**: 删除整个Shader及其所有变体
- **删除变体**: 删除特定的变体（关键字组合）

## 安装方法

### 方法1：作为Unity包安装
1. 将插件文件夹复制到Unity项目的`Assets/`目录下
2. 重启Unity编辑器
3. 在菜单栏中选择 `Tools > Shader Variants Collector` 打开插件窗口

### 方法2：通过Package Manager安装
1. 在Package Manager中点击"+"按钮
2. 选择"Add package from disk"
3. 选择插件的package.json文件
4. 点击"Add"安装

## 使用方法

### 基本操作
1. **选择ShaderVariantCollection**: 在插件窗口中选择要管理的ShaderVariantCollection资源
2. **查看现有变体**: TreeView会显示当前集合中的所有Shader和变体
3. **收集新变体**: 点击"Collect Variants"按钮开始收集项目中的新变体
4. **管理新变体**: 新变体会以绿色背景显示，可以单独确认或忽略

### 手动添加
1. **添加Shader**: 点击TreeView底部的"Add"按钮
2. **添加变体**: 点击每个Shader下的"Add"按钮
3. **选择关键字**: 在对话框中勾选需要的关键字组合

### 删除操作
1. **删除Shader**: 点击Shader行右侧的"Del"按钮
2. **删除变体**: 点击变体行右侧的"Del"按钮
3. **确认删除**: 在弹出的对话框中确认删除操作

### 引用查找
1. **定位Shader**: 点击Shader行右侧的"Locate"按钮
2. **查看引用**: 点击变体行右侧的"Refs"按钮查看引用信息

## 技术实现

### 核心组件
- `ShaderVariantsCollectorWindow`: 主窗口类
- `ShaderVariantsTreeView`: TreeView显示组件
- `ShaderVariantData`: 变体数据模型
- `ShaderVariantCollector`: 收集工具类
- `AddShaderDialog`: 添加Shader对话框
- `AddVariantDialog`: 添加变体对话框

### 关键技术
- **反射**: 使用反射获取ShaderVariantCollection内部数据
- **TreeView**: 自定义TreeView实现层级显示
- **进度条**: 实时显示收集进度
- **引用查找**: 通过AssetDatabase查找资源引用

## 注意事项

1. **性能考虑**: 收集大量变体时可能需要较长时间
2. **内存使用**: 处理大型项目时注意内存使用情况
3. **API限制**: 某些功能可能受到Unity API版本限制
4. **备份建议**: 操作前建议备份ShaderVariantCollection资源

## 版本信息

- **版本**: 1.0.0
- **Unity版本**: 2021.3+
- **许可证**: MIT

## 更新日志

### v1.0.0
- 初始版本发布
- 实现基本的变体收集和管理功能
- 支持手动添加Shader和变体
- 提供引用查找功能
- 支持删除Shader和变体
- 完整的UI界面和交互功能

## 技术支持

如有问题或建议，请联系开发者或提交Issue。

## 许可证

本插件采用MIT许可证，详见LICENSE文件。 