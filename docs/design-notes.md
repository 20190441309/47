# 设计备忘(design-notes)

> 「顺手加一下」的功能先记在这里,**不实现**(见 AGENTS.md 第 9 节范围控制)。设计决策与遗留问题也记录在此。

## 决策记录

- 2026-09-03:M1 采纳建议,新增「WebGL 空包基线体积测试」——把首屏 ≤15MB 从假设变成实测数据(提前于 M4 验证)。
- 2026-09-03:M1 灰盒 UI 暂用 legacy `UnityEngine.UI.Text`(编辑器内中文正常);**WebGL 下动态字体不可用,中文会变豆腐块**。M2 切换 TextMeshPro + 思源黑体 SDF 字体资产后再做 WebGL 中文验证。
- 2026-09-05:**WebGL 基线体积实测完成**(E:\hki\47 机,当前灰盒场景含补丁台,批处理两次复现一致):总量 **5.35 MB**(Brotli;其中 wasm 4.23MB、data 1.01MB)——首屏 ≤15MB 预算余量约 3 倍,后续美术/音频资产空间充足。另:同代码重跑走 Library/Bee 增量缓存,秒级完成。

## 开发环境备忘(本机坑)

- 本机 bash 为 **WSL2**,而 node 是 **Windows 侧 node.exe**:WSL 里的 curl 访问不到 Windows 回环(localhost 隔离),需用 `curl.exe`;`server/test/smoke.sh` 已内置该适配。
- 本机 PowerShell 为 **5.1**:`Invoke-RestMethod` 的字符串请求体按 ISO-8859-1 发送,中文静默变乱码。**测中文接口一律用 node fetch(临时脚本)或 curl.exe + 文件体**,不要用 Invoke-RestMethod。

## 遗留问题 / 临时方案

- ~~2026-09-04 跨机器交接:WebGL 模块未装上~~ → 2026-09-05 已解决:E:\hki\47 机已装 WebGL Build Support(顺带已装 WeixinMiniGameSupport,M4 可用),基线体积测试已跑通;旧机器若重建环境仍需在 Hub **图形界面**给 1.10.2 勾选 WebGL Build Support(CLI `install-modules` 有 bug)。工程创建与灰盒场景生成步骤见 `unity/README.md`。
- 剧情阶段推进 M1 为「对话驱动」(玩家提到按钮/修复即切阶段);M2 计划接入游戏事件(谜题完成)驱动阶段切换——涉及新增接口,按 AGENTS.md 要求需先改接口契约再实现。
- 客户端存档 M1 只存 sessionId + stage + trust + 设置;flags/memory 由服务端持久化(每 5 分钟落盘)。M2 评估是否把 flags 同步进客户端存档(防服务端数据丢失)。
- 客户端一旦进入离线兜底就不再重试后端(M1 简化);M2 加"手动重连"入口。
- 兜底台词单一数据源:以 `server/src/stages.js` 为准,`server/tools/export-fallback.js` 导出到 `unity/Assets/Resources/Dialogue/Fallback/`,改台词后需重跑导出(生成物不进 git)。

## 「不实现」清单(范围控制缓冲区)

- (暂无)
