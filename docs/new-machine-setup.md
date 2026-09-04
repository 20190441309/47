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
| SyncTrayzor 捆绑 syncthing 首启自升级到 v2,陷入 `unknown flag -n` 报错死循环 | 首启前先 `setx STNOUPGRADE 1`,再把 `data\syncthing.exe` 换成 v1.30.0(见 §9.1) |

## 8. 每次收工

```bash
git add -A
git commit -m "系统: 摘要"   # 提交信息规范见 AGENTS.md 第 8 节
git push                     # 多机器开发,必须保持 GitHub 同步
```

并在 `docs/ai-usage-log.md` 追加一行会话记录(Codely+ 赛道申报证据,不许漏)。

## 9. 两机实时同步(Syncthing,装一次,推荐)

> git/GitHub 仍是唯一真源;Syncthing 负责把**未提交的改动、`server/.env`、`server/data/`** 等不进 git 的内容在两台机器间实时对齐,忘记 push 不再致命。
> 第二台机器的完整分步操作手册见 `docs/syncthing-setup.md`(含笔记本设备 ID 与排障表)。

### 9.1 安装(每台机器,一次性,**顺序不能乱**)

> 坑:SyncTrayzor v1.1.29 捆绑的 syncthing 是 2021 年的 v1.18.1,首次启动会自动升级到 v2.x,而 v2 命令行接口与 SyncTrayzor 不兼容,陷入 `unknown flag -n` 报错死循环(主机 2026-09-04 实测踩过)。必须**先禁升级、再换 v1 系列最后版二进制**。

1. 从 GitHub `canton7/SyncTrayzor` releases 下载 `SyncTrayzorPortable-x64.zip`(v1.1.29),解压到 `D:\program\SyncTrayzor`(绿色版,无需安装器)
2. **首次运行前**,PowerShell 执行 `setx STNOUPGRADE 1`(禁用 syncthing 自动升级,防跳 v2)
3. 运行 `SyncTrayzor.exe` 过向导,防火墙弹窗一律点"允许访问",然后完全退出(托盘右键 → 退出)
4. 从 GitHub `syncthing/syncthing` releases 下载 `syncthing-windows-amd64-v1.30.0.zip`,用其中的 `syncthing.exe` 覆盖 `D:\program\SyncTrayzor\data\syncthing.exe`
5. 重开 SyncTrayzor,日志(操作 → 显示日志)应显示 `syncthing v1.30.0` 且无报错
6. 建议在 SyncTrayzor 设置里开启"开机自启";**永远别删 `STNOUPGRADE` 环境变量、别升级 syncthing 到 v2**

### 9.2 两台配对(一次性)

1. **配对前先对齐**:两台都 `git add -A` + commit + push + pull,保证工作树一致,首次同步才不会打架
2. 两边 SyncTrayzor → 操作 → 显示 ID,互相把对方设备 ID 添加进"设备"
3. 添加文件夹 `D:\hki\patch-47`,共享给对方设备,两边都是"发送并接收"
4. 笔记本接受共享时,路径指向自己已 clone 好的 `D:\hki\patch-47`
5. 文件夹 → 版本控制 → **回收站式,保留 7 天**(防误删扩散)

`.stignore`(忽略 node_modules / unity 缓存 / .codely-cli 等)已入库随 git 走,无需手配,别删。

### 9.3 使用纪律(3 条,违反会丢东西)

1. **永远不要两台同时编辑**同一文件。极端冲突时 Syncthing 生成 `.sync-conflict-*` 副本而不是覆盖,能救但很烦
2. **开工前看一眼托盘图标**,绿色(最新)再动手;回家开主机后等它变绿再写
3. **到里程碑照常 push GitHub**——笔记本出门丢了/摔了,GitHub 是唯一异地副本;公网中继偶尔抽风,git 是兜底

> 出门场景:笔记本的改动先攒着本地,两台同时在线时自动对齐(含公网中继,速度慢但够代码文件用)。若中继连不通,两台装 Tailscale 组虚拟局域网解决。
