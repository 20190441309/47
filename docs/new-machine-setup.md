# 新机器开工指引(patch47)

> 旧机器交接于 2026-09-04。本文件是新机器开工的**唯一权威步骤**,照抄即可。
> 项目背景、规范、范围控制见 `AGENTS.md`(每次会话开始先通读);Unity 细节参考 `unity/README.md`。

## 0. 前置条件

- Windows,`git`、`node ≥ 20`(`node -v` 验证)
- 团结引擎 **Tuanjie Hub**(国内版,官网下载,登录团结账号)——别装成国际版 Unity Hub
- 编辑器版本固定 **1.10.2**(内部号 2022.3.62t14)
- 若 git 身份未配置:`git config --global user.name "你的名字"`、`git config --global user.email "你的邮箱"`

## 1. 拿代码

```bash
git clone https://github.com/20190441309/47 patch-47
cd patch-47
```

## 2. 装 WebGL 模块(重要:用图形界面,别用 CLI)

团结 Hub → 安装(Installs)→ 1.10.2 → Add Modules → 勾选 **WebGL Build Support** 安装。

> CLI 命令 `tuanjie.exe install-modules 1.10.2 ...` 在旧机器实测有 bug(`undefined is not an object`),不要用。

## 3. 起后端(先于 Unity 联调)

```bash
cd server
npm install
npm run export-fallback   # 生成 unity/Assets/Resources/Dialogue/Fallback/(兜底台词,产物不进 git)
npm start                 # 默认 :3000,日志应出现 "(llm fallback-only)"
```

验证(任选其一):

```bash
bash test/smoke.sh                          # Git Bash/WSL 下,脚本已适配 WSL2 回环问题,期望 SMOKE OK
curl.exe http://localhost:3000/api/health   # PowerShell,期望 {"ok":true}
```

接真实 LLM(联调导演层用,可选):复制 `server/.env.example` 为 `server/.env`,填 `LLM_BASE_URL / LLM_API_KEY / LLM_MODEL`(OpenAI 兼容接口)。**密钥永不入库**(`.env` 已被 .gitignore 忽略)。

## 4. 建团结工程

Hub → 新建工程 → 模板选 **2D**(cn.tuanjie.template.2d)→ 位置选 `patch-47/unity_new`(Hub 要求空目录,而 `unity/` 里已有脚本)。

创建完成后,把 `unity_new/` 的内容并入 `unity/`:

```bash
# 仓库根目录下执行
robocopy unity_new\Assets unity\Assets /E /MOVE
robocopy unity_new\Packages unity\Packages /E /MOVE
robocopy unity_new\ProjectSettings unity\ProjectSettings /E /MOVE
rmdir /s /q unity_new
```

> 用资源管理器手动拖拽合并也可以(Assets/Packages/ProjectSettings 三个文件夹)。`unity/Assets/Scripts` 已有 10 个脚本,合并不冲突。
> 可选:打开 ProjectSettings 把 Product Name 改成「第 47 号补丁」。

## 5. 生成灰盒场景

用编辑器打开 `patch-47/unity`,等脚本编译完,菜单执行:

**Tools → Patch47 → 生成灰盒场景** → 生成 `Assets/Scenes/Ch1_Greybox.unity` 与 `Assets/Materials/*.mat`

或命令行批处理(`Tuanjie.exe` 路径按实际安装位置替换,编辑器在 `D:\program\2022.3.62t14\Editor\Tuanjie.exe` 这类位置):

```bash
"<Tuanjie.exe>" -batchmode -projectPath "<仓库>\unity" -executeMethod Patch47.EditorTools.GreyboxSceneBuilder.BuildAndSave -quit
```

## 6. 验证 M1(对照 AGENTS.md 的 M1 DoD)

1. 保持 `npm start` 运行,Play 模式进入 `Ch1_Greybox`
2. 逐项检查:
   - 帕奇 = 深色方块 + 青蓝光标闪烁;右上角**没有**"离线模式"
   - 输入框发一句 → 帕奇打字机逐字回复;顶部标签 `[ch1_arrival trust 50]` 随对话更新
   - 说「那个开始游戏按钮怎么了」→ 阶段切到 `ch1_puzzle`(顶部标签变化、快捷回复变化)
   - **关掉 server 再发一句** → 右上角出现"离线模式",回复走兜底台词,体验不中断(离线兜底 DoD)
3. WebGL 基线体积测试(M1 加项,需已装 WebGL 模块):

```bash
"<Tuanjie.exe>" -batchmode -projectPath "<仓库>\unity" -executeMethod Patch47.EditorTools.WebGLBaselineBuilder.Build -quit
```

→ 读 `unity/Builds/WebGLBaseline-SIZE.txt`(gitignored,勿入库),把实测体积结论记进 `docs/design-notes.md`(预算 ≤15MB)。

## 7. 已知坑(旧机器实测)

| 坑 | 规避 |
|---|---|
| tuanjie-cli 模块命令报 `undefined is not an object` | 用 Hub 图形界面装模块 |
| WSL2 bash 访问不到 Windows node 的 localhost | `smoke.sh` 已内置 `curl.exe` 适配;手测一律 `curl.exe` |
| Windows PowerShell 5.1 `Invoke-RestMethod` 中文请求体静默乱码 | 测中文接口用 node fetch 或 curl.exe + 文件体 |
| legacy `Text` 在 WebGL 下中文变豆腐块 | 已知问题,M2 换 TextMeshPro + 思源黑体 SDF(见 design-notes) |
| 装成了国际版 Unity Hub | 卸载,改装国内版团结 Hub |

## 8. 每次收工

```bash
git add -A
git commit -m "系统: 摘要"   # 提交信息规范见 AGENTS.md 第 8 节
git push                     # 多机器开发,必须保持 GitHub 同步
```

并在 `docs/ai-usage-log.md` 追加一行会话记录(Codely+ 赛道申报证据,不许漏)。
