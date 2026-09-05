# unity/ —— 团结引擎工程

> **新机器开工的完整步骤见 `docs/new-machine-setup.md`(权威)。**
> 本文件只保留 Unity 侧参考:脚本结构、批处理命令。

## 当前状态

- 工程本体已建立(2026-09-04,2D 模板并入本目录,详见 new-machine-setup.md 第 4 节)
- `Assets/Scripts/` 12 个灰盒脚本已写完(见下表)
- 灰盒场景已生成:`Assets/Scenes/Ch1_Greybox.unity`;补丁台交互(2026-09-05)入库后需重跑菜单 `Tools/Patch47/生成灰盒场景` 覆盖生效

## 命令行批处理

生成灰盒场景:

```
<Tuanjie.exe 路径> -batchmode -projectPath <仓库>/unity -executeMethod Patch47.EditorTools.GreyboxSceneBuilder.BuildAndSave -quit
```

WebGL 基线体积测试(M1 加项,需 WebGL 模块):

```
<Tuanjie.exe 路径> -batchmode -projectPath <仓库>/unity -executeMethod Patch47.EditorTools.WebGLBaselineBuilder.Build -quit
```

产物:`Builds/WebGLBaseline-SIZE.txt`(gitignored,勿入库;结论记入 docs/design-notes.md)。

## 脚本结构

| 目录 | 内容 |
|---|---|
| GameFramework/ | GameConfig(配置)、ApiModels(接口 DTO)、ApiClient(/api/session 新建+恢复、/api/chat、/api/event,超时 8s 走兜底)、SaveManager(单存档位,自动存/读 persistentDataPath) |
| Dialogue/ | DialogueManager(编排)、Typewriter(本地打字机)、FallbackDialogue(离线兜底台词,Resources 加载) |
| Patch/ | PatchAvatar(方块+闪烁光标,情绪→颜色/频率)、PatchBug(bug 物体:红光脉冲/点击开补丁台/修复反馈)、PatchBoard+PatchWireDrag(补丁台「接线」谜题交互) |
| UI/ | QuickReplyRow(3 个快捷回复按钮) |
| Editor/ | GreyboxSceneBuilder(程序化生成灰盒场景)、WebGLBaselineBuilder(WebGL 基线构建+体积报告) |
