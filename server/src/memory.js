'use strict';
// 会话状态与记忆:内存为主,每 5 分钟落盘 data/sessions/*.json(比赛量级不需要数据库)。
// 记忆结构 = 近 8 轮原文 + 超出部分折叠进滚动摘要(规则版,接 LLM 后可升级)+ 关键事实表(由导演层规则写入)。

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { INITIAL_STAGE } = require('./stages');

const SESSIONS_DIR = path.join(__dirname, '..', 'data', 'sessions');
const MAX_RECENT_TURNS = 8;      // 近 8 轮原文(1 轮 = 玩家 + 帕奇各一句)
const SUMMARY_MAX_CHARS = 1200;  // 滚动摘要上限,超出丢最旧的行
const PERSIST_INTERVAL_MS = 5 * 60 * 1000;

const sessions = new Map();

function createSession() {
  const session = {
    id: crypto.randomUUID(),
    createdAt: new Date().toISOString(),
    stage: INITIAL_STAGE,
    trust: 50,
    flags: {},        // { [flagName]: true }
    turns: [],        // { role: 'user' | 'patch', text } 仅近 8 轮
    summarized: '',   // 滚动摘要(每行一条旧对话的压缩)
    facts: {},        // 关键事实表 { [key]: 描述 }
    ruleState: {},    // 导演层 trust 规则的冷却/一次性标记
    fixedBugs: {},    // 已修复 bug 登记(/api/event 幂等用)
    counters: { recentChatTs: [] }, // 限流用滑动窗口
  };
  sessions.set(session.id, session);
  return session;
}

function getSession(id) {
  return sessions.get(id);
}

function appendTurn(session, role, text) {
  session.turns.push({ role, text });
  // 超出近 8 轮的部分折叠进滚动摘要(规则版压缩:截断拼接)
  while (session.turns.length > MAX_RECENT_TURNS * 2) {
    const evicted = session.turns.shift();
    const line = `[${evicted.role === 'user' ? '玩家' : '帕奇'}]${evicted.text.slice(0, 60)}`;
    session.summarized = (session.summarized ? session.summarized + '\n' : '') + line;
    if (session.summarized.length > SUMMARY_MAX_CHARS) {
      const lines = session.summarized.split('\n');
      lines.splice(0, Math.ceil(lines.length / 4));
      session.summarized = lines.join('\n');
    }
  }
}

function persistAll() {
  fs.mkdirSync(SESSIONS_DIR, { recursive: true });
  for (const session of sessions.values()) {
    const file = path.join(SESSIONS_DIR, `${session.id}.json`);
    try {
      fs.writeFileSync(file, JSON.stringify(session, null, 2));
    } catch (err) {
      console.error('[memory] persist failed:', session.id, err.message);
    }
  }
}

function loadAll() {
  if (!fs.existsSync(SESSIONS_DIR)) return;
  for (const name of fs.readdirSync(SESSIONS_DIR)) {
    if (!name.endsWith('.json')) continue;
    try {
      const session = JSON.parse(fs.readFileSync(path.join(SESSIONS_DIR, name), 'utf8'));
      if (session && session.id) sessions.set(session.id, session);
    } catch (err) {
      console.error('[memory] load failed:', name, err.message);
    }
  }
  console.log(`[memory] restored ${sessions.size} session(s)`);
}

module.exports = { createSession, getSession, appendTurn, persistAll, loadAll, PERSIST_INTERVAL_MS };
