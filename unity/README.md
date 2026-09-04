# unity/ —— 团结引擎工程

> **新机器开工的完整步骤见 `docs/new-machine-setup.md`(权威)。**
> 本文件只保留 Unity 侧参考:脚本结构、批处理命令。

## 当前状态

- `Assets/Scripts/` 10 个灰盒脚本已写完(见下表)
- 工程本体待建:模板 2D(cn.tuanjie.template.2d),建到 `unity_new` 后并入本目录(步骤见 new-machine-setup.md 第 4 节)
- 灰盒场景待生成:菜单 `Tools/Patch47/生成灰盒场景`,产物 `Assets/Scenes/Ch1_Greybox.unity`

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
| GameFramework/ | GameConfig(配置)、ApiModels(接口 DTO)、ApiClient(/api/session、/api/chat,超时 8s 走兜底) |
| Dialogue/ | DialogueManager(编排)、Typewriter(本地打字机)、FallbackDialogue(离线兜底台词,Resources 加载) |
| Patch/ | PatchAvatar(方块+闪烁光标,情绪→颜色/频率) |
| UI/ | QuickReplyRow(3 个快捷回复按钮) |
| Editor/ | GreyboxSceneBuilder(程序化生成灰盒场景)、WebGLBaselineBuilder(WebGL 基线构建+体积报告) |
