

## Codely Structured Memories

### User
- [2026-09-04 23:33:35] 用户是团结引擎(Tuanjie/Unity)编辑器新手:不熟悉编辑器基本操作,2026-09-04 已把编辑器界面切成中文;报错或界面问题偏好直接发截图(而非文字描述),期待给到「点哪里、点什么」的一步步具体操作指引,少用编辑器术语黑话。
- [2026-09-05 08:25:45] 用户对 UI 观感敏感(会直白说「丑」),但接受灰盒阶段的功能优先;给用户的编辑器操作指引必须把前置条件写死(如「必须先停止播放再执行菜单」——用户在 Play 模式里跑「生成灰盒场景」导致 InvalidOperationException 且改动未保存,连续两次踩坑)。

### Feedback

### Project
- [2026-09-04 20:49:26] 用户跨机器开发《第 47 号补丁》(UCDC 2026,截止 2026-10-31):GitHub 远程 origin = https://github.com/20190441309/47,main 分支是唯一真源;多机另用 SyncTrayzor(v1.1.29 绿色版)实时同步 patch-47 全目录(含 .env、未提交改动),.stignore 已入库,流程见 docs/new-machine-setup.md §9。安装目录各机不同:原两机(主机+笔记本,笔记本常带出门)在 D:\program\SyncTrayzor,E:\hki\47 所在机 2026-09-04 新装于 D:\Programs\SyncTrayzor(首启前已换好 v1.30.0 二进制)。**Why:** 用户在多台机器间切换开发,怕忘 push、要求实时同步且非 git 文件也要走。**How to apply:** 新会话开始先 git pull;里程碑仍建议 push origin;遇 .sync-conflict-* 文件按冲突处理;勿再建议网盘/SMB 等替代同步方案。syncthing 必须锁 v1.x(当前 data\syncthing.exe=v1.30.0,用户级环境变量 STNOUPGRADE=1 禁自升级)——v2 与 SyncTrayzor 1.1.29 命令行不兼容,会陷入 unknown flag -n 报错死循环。
- [2026-09-04 21:24:19] Tuanjie Hub 网页登录失败的根因（2026-09-04 诊断）：Codely/cowork 每个会话启动时自带 tuanjie-cli 守护进程（D:\Programs\hub\tuanjie.exe），它抢占命名管道 \\.\pipe\Tuanjie-hubIPCService 且被宿主 watchdog 秒级拉活（杀不死）；浏览器登录回调 tuanjiehub://login/?code=... 经此管道被守护进程截胡，桌面 Hub 永远收不到授权码（日志表现为 EADDRINUSE + 反复重试）。CLI 守护进程与桌面 Hub 令牌存储不互通（CLI 登录≠Hub 登录）。**Why:** 团结引擎生态里 CLI 守护进程与桌面 Hub 的 IPC 设计冲突。**How to apply:** 遇到 Hub 登录不上：①让用户关闭所有 codely/cowork 窗口后登录一次（令牌持久化，之后可共存）；②或用 Hub 窗口内原生的扫码登录（不等于浏览器里弹的二维码，后者仍走回调管道）；③CLI 的 auth status/login 走设备授权流，不受影响。日志位置 %APPDATA%\TuanjieHub\logs\info-log.json。
- [2026-09-04 23:33:35] unity 工程本体已于 2026-09-04 在 E:\hki\47 机建成并编译通过(2D 模板 CLI 建 → robocopy 并入 unity/,灰盒场景已生成,后端联调已跑通)。若在其他机器重建工程需知的坑:① tuanjie.exe projects create 返回后 Hub 仍派生编辑器进程(-createproject -cloneFromTemplate)在后台写 unity_new\Library,移动/删除 unity_new 前须先结束该 Tuanjie.exe 进程;② 模板 tgz 不含 ProjectSettings\ProjectVersion.txt,需手写(m_EditorVersion: 2022.3.62t14,修订号从 Tuanjie.exe 文件属性 ProductVersion 取,如 1f04f7aba499);③ 2D 模板场景文件扩展名是 .scene(SampleScene.scene)不是 .unity;④ PowerShell 下 rmdir 不认 /s /q,要用 Remove-Item -Recurse -Force。
- [2026-09-05 08:25:45] 灰盒场景 UI 关键约束(2026-09-05 定):CanvasScaler matchWidthOrHeight 必须 =1(按高度匹配),否则横屏编辑器窗口下对话面板(640 高,参考分辨率 1080x1920)按宽度缩放会盖住大半屏、挡住 3D 世界(bug/帕奇),手机竖屏反而正常——排查「3D 物体消失」时先查面板遮挡再查渲染。PatchBoard 为模态结构:全屏半透明遮罩(点击关闭)+ 面板子物体。改完场景生成器后必须让用户在非 Play 状态重跑菜单,否则修复不进场景文件。
- [2026-09-05 09:38:53] 团结引擎批处理坑(2026-09-05 实测):Tuanjie.exe 是 GUI 子系统程序,PowerShell 里 `& Tuanjie.exe -batchmode ...` 会立即返回、后台才真正跑构建——不能信命令返回/退出码,要靠轮询产物时间戳(如 unity/Builds/WebGLBaseline-SIZE.txt 的 LastWriteTime)或等编辑器进程退出来判断完成。同代码重跑走 Library/Bee 增量缓存,秒级完成(首次约 4-5 分钟)。
- [2026-09-05 09:38:53] 团结 .meta 的 guid 是加密 base64(如 Wnwdtyz4...),而场景文件里引用脚本/材质用的是明文 hex GUID(如 d733ffe4...)——拿 meta 的 guid 去场景里 grep 会得到 False、误判「脚本没挂上」。校验场景组件是否挂载要搜 GameObject 名(m_Name: PatchBoard)或序列化字段名(bugRenderer/terminal/slot),字段名出现即说明编辑器已成功解析脚本。
- [2026-09-05 09:59:13] 团结引擎脚本序列化硬约束(2026-09-05 实测):一个 .cs 文件只放一个 MonoBehaviour 类。同文件的第二个 MonoBehaviour 类(如 PatchWireDrag 曾与 PatchBoard 同文件)被场景引用时,存盘写不出正常 `{fileID: 11500000, guid: ..., type: 3}` 引用,而是嵌一个空 MonoScript 存根进场景 → 运行时相当于 Missing Script,事件回调全部失效且无报错。排查法:场景里搜组件的 m_Script 行,凡是没有 guid 的场景内嵌引用(fileID 指向 !u!115 MonoScript 块)即为断链。

### Reference

