'use strict';
// 《第 47 号补丁》对话后端入口。
// 接口契约见 AGENTS.md 第 5 节:POST /api/session、POST /api/chat、GET /api/health。

require('dotenv').config();
const express = require('express');

const memory = require('./memory');
const director = require('./director');
const { llmConfigured, chatCompletion } = require('./llm');
const { sanitize } = require('./filter');

const PORT = Number(process.env.PORT || 3000);
const MAX_CALLS_DAY = Number(process.env.MAX_CALLS_DAY || 2000); // 全局日配额(成本护栏)
const RATE_WINDOW_MS = 10 * 1000; // 单会话限流:10 秒内 ≤3 次
const RATE_MAX = 3;
const TIRED_LINE = '帕奇有点累了,稍后再聊。';

const app = express();
app.disable('x-powered-by');
app.use(express.json({ limit: '16kb' }));

// CORS 全开(WebGL 构建跨域调用;生产由 Caddy 同域反代)
app.use((req, res, next) => {
  res.set({
    'Access-Control-Allow-Origin': '*',
    'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
    'Access-Control-Allow-Headers': 'Content-Type',
  });
  if (req.method === 'OPTIONS') return res.sendStatus(204);
  next();
});

app.get('/api/health', (_req, res) => {
  res.json({ ok: true });
});

app.post('/api/session', (_req, res) => {
  const session = memory.createSession();
  res.status(201).json({
    sessionId: session.id,
    state: { stage: session.stage, trust: session.trust, flags: session.flags },
  });
});

app.post('/api/chat', async (req, res) => {
  try {
    const { sessionId, text } = req.body || {};
    if (typeof sessionId !== 'string' || typeof text !== 'string' || !text.trim()) {
      return res.status(400).json({ error: 'bad_request' });
    }
    const session = memory.getSession(sessionId);
    if (!session) return res.status(404).json({ error: 'session_not_found' });

    // 单会话限流(超限返回固定台词,不报错,保证体验)
    const now = Date.now();
    session.counters.recentChatTs = (session.counters.recentChatTs || []).filter((t) => now - t < RATE_WINDOW_MS);
    if (session.counters.recentChatTs.length >= RATE_MAX) {
      return res.json({ reply: TIRED_LINE, emotion: 'sad', stage: session.stage, trust: session.trust, flagsChanged: [] });
    }
    session.counters.recentChatTs.push(now);

    const clean = sanitize(text).trim().slice(0, 200);
    memory.appendTurn(session, 'user', clean);
    const intent = director.classifyIntent(clean);
    const { flagsChanged } = director.applyTrustRules(session, clean);
    director.maybeAdvanceStage(session, clean);

    let out = null;
    if (intent === 'oob') {
      out = director.pickGuard(session); // 硬越界:直接挡回,不消耗 LLM
    } else if (llmConfigured() && dailyState.llmCalls < MAX_CALLS_DAY) {
      const raw = await chatCompletion(director.assembleMessages(session));
      if (raw) {
        dailyState.llmCalls += 1;
        out = director.validateLlmOutput(raw); // 校验失败(null)→ 兜底
      }
    }
    if (!out) out = director.pickFallback(session);

    memory.appendTurn(session, 'patch', out.reply);
    res.json({
      reply: out.reply,
      emotion: out.emotion,
      stage: session.stage,
      trust: session.trust,
      flagsChanged,
    });
  } catch (err) {
    console.error('[chat] internal error:', err);
    res.status(500).json({ error: 'internal' }); // Unity 侧收到 5xx/超时即走兜底
  }
});

// ---------- 全局日配额(内存计数,重启重置;比赛量级可接受) ----------

const dailyState = { date: new Date().toISOString().slice(0, 10), llmCalls: 0 };
function rollDaily() {
  const today = new Date().toISOString().slice(0, 10);
  if (dailyState.date !== today) {
    dailyState.date = today;
    dailyState.llmCalls = 0;
  }
}
setInterval(rollDaily, 60 * 1000).unref();

// ---------- 启动 ----------

memory.loadAll();
setInterval(memory.persistAll, memory.PERSIST_INTERVAL_MS).unref();

function shutdown() {
  memory.persistAll();
  process.exit(0);
}
process.on('SIGINT', shutdown);
process.on('SIGTERM', shutdown);

app.listen(PORT, () => {
  console.log(`[patch47-server] listening on :${PORT} (llm ${llmConfigured() ? 'enabled' : 'fallback-only'})`);
});
