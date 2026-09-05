# 第 47 号补丁 · Patch No.47

> 三年前你弃坑的小游戏突然发来求救——里面唯一活着的 NPC 求你陪它修完这个世界,而它的命运藏在你说过的每句话里。

UCDC 2026(Unity 中国开发挑战赛)Codely+ 赛道参赛作品。叙事向点选式对话解谜小游戏:**主提交 H5 网页版**,3 章 + 3 结局,单周目 25–40 分钟,手机浏览器可玩。

## 亮点

- **LLM 驱动的 NPC「帕奇」**:一段会发光的"未保存代码块",闪烁的光标是它的眼睛。自由文本对话,回复带情绪字段,驱动光标动画与打字速度。
- **导演层护栏(后端)**:规则导演层控制剧情阶段、trust 信任分、flags 与记忆;出戏词校验 + 防越狱约束,防止 LLM 跑偏。
- **离线兜底**:LLM 请求超时/失败自动切换各阶段预制台词,UI 无感。
- **点选式谜题**:补丁台"接线 / 滑杆 / 拖放"三件套,把当年没写完的代码"修完",零操作门槛。
- **多端**:WebGL(H5)主提交,微信 / 抖音小游戏适配为加分项。

## 技术栈

| 层 | 技术 |
|---|---|
| 客户端 | 团结引擎(Tuanjie)1.10.2,WebGL 导出 |
| 对话后端 | Node.js 20 + Express |
| LLM | OpenAI 兼容接口,环境变量配置(密钥永不入库) |

## 目录结构

```
├── AGENTS.md    开发指导书(项目规范、接口契约、里程碑与范围控制,权威)
├── unity/       团结引擎工程(Assets/Scripts/ 各系统灰盒脚本)
├── server/      Node 对话后端(导演层 / LLM / 兜底台词 / 限流 / 持久化)
└── docs/        设计备忘、AI 使用日志、素材授权登记、环境与同步手册
```

## 快速开始(后端)

```bash
cd server
cp .env.example .env   # Windows: copy .env.example .env;填入 LLM_BASE_URL / LLM_API_KEY / LLM_MODEL
npm install
npm start              # 默认 3000 端口
```

| 方法 | 路径 | 说明 |
|---|---|---|
| POST | `/api/session` | 创建会话,返回 `sessionId` 与初始状态 |
| POST | `/api/chat` | 对话,返回 `reply / emotion / stage / trust / flagsChanged` |
| GET | `/api/health` | 健康检查 |

冒烟测试:`npm run smoke`(bash `test/smoke.sh`,断言 health 与 chat 各一条)。

客户端:用团结引擎 1.10.2 打开 `unity/` 工程即可;新机器环境搭建步骤见 `docs/new-machine-setup.md`。

## 文档导航

- [`AGENTS.md`](AGENTS.md) — 开发指导书:背景约束、系统规格、接口契约、里程碑、范围控制
- [`docs/design-notes.md`](docs/design-notes.md) — 设计决策与遗留问题
- [`docs/ai-usage-log.md`](docs/ai-usage-log.md) — AI 使用日志(Codely+ 赛道申报证据)
- [`docs/assets-credits.md`](docs/assets-credits.md) — 第三方素材来源与授权登记
- [`docs/new-machine-setup.md`](docs/new-machine-setup.md) — 新机器开工指引(含已知坑)
- [`docs/syncthing-setup.md`](docs/syncthing-setup.md) — 多机实时同步(Syncthing)安装手册

## 开发状态

M1(9.03–9.10)DoD 已于 2026-09-05 提前达成:`curl /api/chat` 人设台词✓;编辑器内打字对话(含离线兜底)✓;灰盒场景补丁台「接线」谜题可玩(拖线修复 bug 物体)✓;加项 WebGL 空包基线实测 **5.35MB**(预算 ≤15MB)。下一步 M2(9.11–9.24):导演层完整 + 第 1 章完整玩法 + 存档。完整里程碑(M1–M6)见 `AGENTS.md` 第 7 节。

## 合规声明

核心代码与核心创意原创;AI(Codely 智能体 + LLM)参与开发全过程并如实记录于 `docs/ai-usage-log.md`;所有第三方素材(字体 / 音效 / 贴图)均在 `docs/assets-credits.md` 登记来源与授权。
