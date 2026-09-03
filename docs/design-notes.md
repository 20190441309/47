# 设计备忘(design-notes)

> 「顺手加一下」的功能先记在这里,**不实现**(见 AGENTS.md 第 9 节范围控制)。设计决策与遗留问题也记录在此。

## 决策记录

- 2026-09-03:M1 采纳建议,新增「WebGL 空包基线体积测试」——把首屏 ≤15MB 从假设变成实测数据(提前于 M4 验证)。
- 2026-09-03:M1 灰盒 UI 暂用 legacy `UnityEngine.UI.Text`(编辑器内中文正常);**WebGL 下动态字体不可用,中文会变豆腐块**。M2 切换 TextMeshPro + 思源黑体 SDF 字体资产后再做 WebGL 中文验证。

## 遗留问题 / 临时方案

- 剧情阶段推进 M1 为「对话驱动」(玩家提到按钮/修复即切阶段);M2 计划接入游戏事件(谜题完成)驱动阶段切换——涉及新增接口,按 AGENTS.md 要求需先改接口契约再实现。
- 客户端存档 M1 只存 sessionId + stage + trust + 设置;flags/memory 由服务端持久化(每 5 分钟落盘)。M2 评估是否把 flags 同步进客户端存档(防服务端数据丢失)。
- 兜底台词单一数据源:以 `server/src/stages.js` 为准,`server/tools/export-fallback.js` 导出到 `unity/Assets/Resources/Dialogue/Fallback/`,改台词后需重跑导出。

## 「不实现」清单(范围控制缓冲区)

- (暂无)
