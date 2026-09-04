# unity/ —— 团结引擎工程(M1 交接状态)

> 本机(旧机器)已完成:编辑器 Tuanjie 1.10.2(2022.3.62t14)安装、10 个灰盒脚本编写。
> **工程本体尚未创建**(旧机器上 tuanjie-cli 的模块安装命令有 bug,不再继续)。
> 新机器按下面步骤从 GitHub 恢复并继续 M1。

## 新机器步骤

1. `git clone https://github.com/20190441309/47 patch-47`
2. 安装团结 Hub + 编辑器 **1.10.2**,并在 Hub 图形界面里给该版本勾选 **WebGL Build Support** 模块(不要用 `tuanjie.exe install-modules`,该子命令当前有 bug)。
3. 后端:`cd server && npm install`,先 `npm run export-fallback`(生成 `unity/Assets/Resources/Dialogue/Fallback/`,本仓库未提交该产物,需要生成)。
4. 用 Hub 新建工程:模板 **2D(cn.tuanjie.template.2d)**,位置选 `patch-47/unity_new`(Hub 要求空目录,`unity/` 里已有脚本)。
5. 把 `unity_new/` 里的 `Assets/`、`Packages/`、`ProjectSettings/` 移入 `unity/`(Assets 与现有 `Assets/Scripts` 直接合并,无冲突;其余文件夹照搬),删除 `unity_new`,提交。
6. 用编辑器打开 `unity/`,菜单 `Tools/Patch47/生成灰盒场景`(或命令行批处理,见下),生成 `Assets/Scenes/Ch1_Greybox.unity`。
7. 联调:先 `cd server && npm start`,再进 Play 模式(GameConfig 里后端地址默认 `http://localhost:3000`)。断后端时右上角出现"离线模式"即兜底生效。

## 命令行批处理(可选,替代第 6 步菜单)

```
<Tuanjie.exe 路径> -batchmode -projectPath <仓库>/unity -executeMethod Patch47.EditorTools.GreyboxSceneBuilder.BuildAndSave -quit
```

WebGL 基线体积测试(M1 加项):

```
<Tuanjie.exe 路径> -batchmode -projectPath <仓库>/unity -executeMethod Patch47.EditorTools.WebGLBaselineBuilder.Build -quit
```

产物:`unity/Builds/WebGLBaseline-SIZE.txt`(gitignored,勿入库;结论记入 docs/design-notes.md)。

## 脚本结构(全部已写完)

| 目录 | 内容 |
|---|---|
| GameFramework/ | GameConfig(配置)、ApiModels(接口 DTO)、ApiClient(/api/session、/api/chat,超时 8s 走兜底) |
| Dialogue/ | DialogueManager(编排)、Typewriter(本地打字机)、FallbackDialogue(离线兜底台词,Resources 加载) |
| Patch/ | PatchAvatar(方块+闪烁光标,情绪→颜色/频率) |
| UI/ | QuickReplyRow(3 个快捷回复按钮) |
| Editor/ | GreyboxSceneBuilder(程序化生成灰盒场景)、WebGLBaselineBuilder(WebGL 基线构建+体积报告) |
