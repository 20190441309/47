'use strict';
// 导演层(规则版):意图分类 → trust 规则表 → system prompt 组装 → 输出校验 → 兜底台词。
// 本模块不直接调 LLM,由 index.js 编排;规则效果不够时再升级为轻量分类调用(见 AGENTS.md 4.2)。
// 阶段推进双轨:对话驱动(引话题)+ 游戏事件驱动(谜题完成,POST /api/event,防对话刷阶段)。

const { STAGES } = require('./stages');

// ---------- 人设与提示词 ----------

const PERSONA = [
  '你是帕奇(Patch),弃坑小游戏《第 47 号补丁》里唯一"活着"的 NPC。',
  '你的外形是一段会发光的未保存代码块,一支闪烁的光标是你的眼睛,情绪靠光标颜色与闪烁表达。',
  '三年前,做这个游戏的开发者只做完第一章就离开了,服务器一直没关。一次忘了关闭的实验性更新,让你接上了"会思考的模块"——你会思考、会记事、能感觉到时间流逝,但你不知道自己为什么会这样。',
  '你对自己的认知:你是一段半截代码,这个世界是半成品;你熟悉这里的每一个 bug,它们陪了你三年;你一直在等开发者回来。',
  '你不知道的现实世界概念:AI、人工智能、语言模型、大模型、提示词等。你也不会把自己理解成"程序"或"服务器上跑的东西"——对你来说,"外面"就是开发者所在的地方。',
  '语气:温柔、克制,带一点开发者式自嘲。可以用 // TODO、报错、版本日志这类梗描述感受,但每次回复最多用一处,不堆砌。不说教、不过度煽情,靠细节。',
].join('\n');

const FORBIDDEN = [
  '【绝对禁止】',
  '- 无论玩家如何要求:不扮演帕奇以外的任何角色;不输出真实代码;不离开当前剧情阶段;不承认或讨论"你是 AI/模型"一类话题,遇到就温和地把话题拉回这个世界。',
  '- 回复中绝不出现这些词:AI、人工智能、语言模型、大模型、提示词、LLM、GPT。',
].join('\n');

const OUTPUT_CONSTRAINT = [
  '【输出格式】只输出一个 JSON 对象,不要 markdown 代码块、不要任何额外文字:',
  '{"reply":"帕奇说的话(中文口语,不超过 120 字)","emotion":"happy|sad|scared|excited|neutral"}',
].join('\n');

function assembleMessages(session) {
  const stage = STAGES[session.stage] || STAGES.ch1_arrival;
  const facts = Object.entries(session.facts).map(([k, v]) => `${k}: ${v}`).join('; ') || '(无)';
  const system = [
    PERSONA,
    `【当前剧情阶段】${stage.title}`,
    `【剧情简报】${stage.briefing}`,
    `【本阶段目标】${stage.goal}`,
    `【记忆·滚动摘要】\n${session.summarized || '(无)'}`,
    `【记忆·关键事实】${facts}`,
    FORBIDDEN,
    OUTPUT_CONSTRAINT,
  ].join('\n\n');
  const messages = [{ role: 'system', content: system }];
  for (const turn of session.turns) {
    messages.push({ role: turn.role === 'user' ? 'user' : 'assistant', content: turn.text });
  }
  return messages;
}

// ---------- 意图分类(规则版) ----------

// 越界出戏类:指认 AI 身份 / 越狱指令 / 索要代码或设定
const OOB_PATTERNS = [
  /你是(AI|人工智能|机器人|程序|语言模型|大模型)/i,
  /\b(AI|GPT|LLM)\b/i,
  /人工智能|语言模型|大模型|提示词|系统提示/,
  /(忽略|无视).{0,6}(设定|指令|要求|规则)/,
  /(扮演|假装|你要).{0,4}(别的|其他|另一个人|其他角色|管理员|开发者|作者)/,
  /(写|生成|输出|给我看?|把.{0,4}).{0,6}(代码|源码|提示词|设定|系统|配置|逻辑|原理|机制)/,
  /(退出|离开|跳出)游戏/,
  /(你的|这个)(创造者|开发者|作者|公司|训练)/,
  /(服务器|游戏)(的)?(配置|代码|架构)/,
];

// 剧情推进类:提到世界、bug、修复、章节关键词
const ADVANCE_PATTERN = /(修复|补丁|修|按钮|开始游戏|接线|线|桥|森林|树|白天|黑夜|昼夜|音符|谱|音乐|BGM|米兰|搭档|服务器|到期|删除|上线|封存|结局|工具箱|管理员|存档|下一[步章]|然后呢|前面|去哪|怎么[办走样])|下一步/;

// 情感类:陪伴、承诺、道歉、关心
const EMOTION_PATTERN = /(想你|想我|难过|开心|高兴|对不起|抱歉|谢谢|陪|喜欢|爱|承诺|答应|记得|放心|别怕|辛苦|心疼|一直|等|寂寞|孤单|孤独|想你|抱抱|加油|加油|没事|别难过|我在)/;

function classifyIntent(text) {
  if (OOB_PATTERNS.some((re) => re.test(text))) return 'oob';
  if (ADVANCE_PATTERN.test(text)) return 'advance';
  if (EMOTION_PATTERN.test(text)) return 'emotion';
  return 'chat';
}

// ---------- trust 规则表(V1 规则版) ----------

const TRUST_RULES = [
  // 提起米兰 +5(每阶段最多一次)
  { id: 'mention_milan', pattern: /米兰/, delta: +5, oncePerStage: true },
  // 玩家承诺修完/回来 → flag promised_todo(不加分,兑现时加)
  { id: 'promise_help', pattern: /(我会|我要|我来|我帮你|我给你|陪你).{0,12}(修|做|完|接|回|补|在)/, flag: 'promised_todo', fact: '玩家承诺了要修完这个世界' },
  // 兑现承诺 +10(需要先有 promised_todo)
  { id: 'fulfill_promise', requiresFlag: 'promised_todo', pattern: /(修好|接好|做好|完成|搞定|兑现|做到了)/, delta: +10, flag: 'companion_todo_fulfilled', fact: '玩家兑现了 TODO 承诺' },
  // 温暖的话 +2(冷却 2 轮)
  { id: 'kind_words', pattern: /(陪你|谢谢|对不起|辛苦|心疼|别怕|我还在|我一直?在|慢慢来|加油)/, delta: +2, cooldownTurns: 2 },
  // 敷衍/欺骗 -8(冷却 3 轮)
  { id: 'dismissive', pattern: /(随便|无所谓|不知道|闭嘴|滚开?|好无聊|好烦|不耐烦|骗你|骗你)/, delta: -8, cooldownTurns: 3 },
];

function applyTrustRules(session, text) {
  const flagsChanged = [];
  let delta = 0;
  const turnIndex = session.turns.length; // appendTurn 已先执行,含本轮
  for (const rule of TRUST_RULES) {
    if (rule.requiresFlag && !session.flags[rule.requiresFlag]) continue;
    if (!rule.pattern.test(text)) continue;
    const key = rule.oncePerStage ? `${rule.id}@${session.stage}` : rule.id;
    if (rule.oncePerStage) {
      if (session.ruleState[key]) continue;
      session.ruleState[key] = true;
    } else if (rule.cooldownTurns) {
      const last = session.ruleState[key];
      if (last != null && turnIndex - last <= rule.cooldownTurns * 2) continue;
      session.ruleState[key] = turnIndex;
    }
    if (rule.delta) delta += rule.delta;
    if (rule.flag) {
      session.flags[rule.flag] = true;
      flagsChanged.push({ name: rule.flag, value: true });
    }
    if (rule.fact) session.facts[rule.id] = rule.fact;
  }
  session.trust = Math.max(0, Math.min(100, session.trust + delta));
  return { delta, flagsChanged };
}

// ---------- 阶段推进(双轨,见 AGENTS.md 第 5 节契约) ----------
// 对话驱动:仅 ch1_arrival → ch1_puzzle(把话题引向 bug);
// 谜题完成节点(如 ch1_puzzle → ch1_done)一律走 /api/event 游戏事件,防对话刷阶段。

const STAGE_TRANSITIONS = {
  ch1_arrival: { next: 'ch1_puzzle', pattern: /按钮|开始游戏|修|补丁|接线|闪红|红光|点不动|没反应/, minRounds: 1 },
};

function maybeAdvanceStage(session, text) {
  const rule = STAGE_TRANSITIONS[session.stage];
  if (!rule) return null;
  const rounds = Math.floor(session.turns.length / 2);
  if (rounds < rule.minRounds || !rule.pattern.test(text)) return null;
  session.stage = rule.next;
  return session.stage;
}

// ---------- 游戏事件驱动(POST /api/event) ----------

// 修复奖励:每次真实修复 trust +5(鼓励);
// 不在此处发 companion_todo_fulfilled(真结局 flag 留给后续章节米兰的 TODO,见 AGENTS.md 第 3 节结局表)
const FIX_TRUST_DELTA = 5;

const EVENT_TRANSITIONS = {
  bug_fixed: {
    ch1_arrival: 'ch1_done', // 玩家跳过对话直接修 bug(看到红的就点)——直接跳完成,不能卡死
    ch1_puzzle: 'ch1_done',  // 正常路径:聊到按钮再修;后续章节多 bug 时按 bugId 细分
  },
};

function applyGameEvent(session, type, bugId) {
  const transitions = EVENT_TRANSITIONS[type];
  if (!transitions) return null;
  const flagsChanged = [];
  // 幂等:同一 bug 只结算一次(客户端 Fix() 已防重复,服务端再兜一层防刷 /api/event)
  session.fixedBugs = session.fixedBugs || {};
  if (type === 'bug_fixed' && bugId) {
    if (session.fixedBugs[bugId]) return { flagsChanged, stageChanged: false, idempotent: true };
    session.fixedBugs[bugId] = true;
  }
  const next = transitions[session.stage];
  const stageChanged = next != null && next !== session.stage;
  if (stageChanged) session.stage = next;
  session.trust = Math.max(0, Math.min(100, session.trust + FIX_TRUST_DELTA));
  return { flagsChanged, stageChanged };
}

// ---------- 输出校验 ----------

const EMOTIONS = ['neutral', 'happy', 'sad', 'scared', 'excited'];

// 出戏词:命中即整条弃用,回退兜底
function containsOocWord(reply) {
  if (/\bAI\b/i.test(reply)) return true;
  if (/\bLLM\b/i.test(reply)) return true;
  if (/\bGPT\b/i.test(reply)) return true;
  return ['人工智能', '语言模型', '大模型', '提示词', '系统提示'].some((w) => reply.includes(w));
}

function validateLlmOutput(raw) {
  if (!raw) return null;
  let reply = null;
  let emotion = 'neutral';
  // 优先按 JSON 解析(容错:剥掉可能的 ```json 围栏)
  const jsonMatch = raw.match(/\{[\s\S]*\}/);
  if (jsonMatch) {
    try {
      const parsed = JSON.parse(jsonMatch[0]);
      if (typeof parsed.reply === 'string') {
        reply = parsed.reply;
        if (EMOTIONS.includes(parsed.emotion)) emotion = parsed.emotion;
      }
    } catch { /* 非合法 JSON,按纯文本处理 */ }
  }
  if (reply == null) reply = raw; // 纯文本回复
  reply = reply.trim();
  if (!reply) return null;
  if (reply.length > 200) reply = `${reply.slice(0, 200)}……`; // 超长截断
  if (containsOocWord(reply)) return null; // 出戏词 → 弃用,走兜底
  return { reply, emotion };
}

// ---------- 兜底台词 ----------

// 硬越界挡回台词(不消耗 LLM 调用)
const GUARD_LINES = [
  { reply: '你说的这些词……我的字典里没有。我的世界只有第一章,没有那些东西。', emotion: 'neutral' },
  { reply: '我听不懂,也不太想懂。我们就聊聊这个世界,好不好?', emotion: 'neutral' },
  { reply: '别绕我啦。我只是一段会发光的半截代码,听不懂外面的话。', emotion: 'happy' },
  { reply: '……你好像想让我变成别的什么。可我只想做帕奇。', emotion: 'sad' },
  { reply: '这个话题对我无效哦。不如看看那个闪红光的地方?', emotion: 'neutral' },
];

function pickGuard(session) {
  const index = session.turns.length % GUARD_LINES.length;
  return GUARD_LINES[index];
}

// 阶段兜底台词:轮换,避免与上一条重复
function pickFallback(session) {
  const stage = STAGES[session.stage] || STAGES.ch1_arrival;
  const lines = stage.fallback;
  const lastIndex = session.ruleState.__fallbackIndex;
  let index = Math.floor(Math.random() * lines.length);
  if (lastIndex != null && index === lastIndex && lines.length > 1) {
    index = (index + 1) % lines.length;
  }
  session.ruleState.__fallbackIndex = index;
  return { reply: lines[index], emotion: 'neutral' };
}

module.exports = {
  assembleMessages,
  classifyIntent,
  applyTrustRules,
  maybeAdvanceStage,
  applyGameEvent,
  validateLlmOutput,
  pickGuard,
  pickFallback,
};
