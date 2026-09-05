'use strict';
// 手动联调脚本(临时,不入库也可):验证 M2 新契约——session 恢复 / 游戏事件推进 / 防对话刷阶段。
const BASE = 'http://localhost:3000';

async function post(path, body) {
  const res = await fetch(BASE + path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  return { status: res.status, json: await res.json().catch(() => null) };
}

(async () => {
  const created = await post('/api/session', {});
  console.log('1) 新建会话:', created.status, JSON.stringify(created.json.state));

  const resumeBad = await post('/api/session', { resumeSessionId: 'no-such-id' });
  console.log('2) 恢复不存在的会话:', resumeBad.status, resumeBad.json.error);

  const resumeOk = await post('/api/session', { resumeSessionId: created.json.sessionId });
  console.log('3) 恢复正确会话:', resumeOk.status, JSON.stringify(resumeOk.json.state));

  const badType = await post('/api/event', { sessionId: created.json.sessionId, type: 'hacked' });
  console.log('4) 未知事件类型:', badType.status, badType.json.error);

  const chat1 = await post('/api/chat', { sessionId: created.json.sessionId, text: '我看到那个开始游戏按钮了' });
  console.log('5) 对话引到按钮后阶段:', chat1.json.stage, '(期望 ch1_puzzle)');

  const chat2 = await post('/api/chat', { sessionId: created.json.sessionId, text: '我修好了,搞定了,可以了' });
  console.log('6) 对话谎称修好:', chat2.json.stage, '(必须仍是 ch1_puzzle,防刷)');

  const evt = await post('/api/event', { sessionId: created.json.sessionId, type: 'bug_fixed', bugId: 'Bug_StartButton' });
  console.log('7) 真实修复事件:', evt.status, 'stage=' + evt.json.stage, 'trust=' + evt.json.trust, '(期望 ch1_done, trust 50+5+对话加成)');

  const evt2 = await post('/api/event', { sessionId: created.json.sessionId, type: 'bug_fixed', bugId: 'Bug_StartButton' });
  console.log('8) 重复修复事件:', evt2.json.stage, 'trust=' + evt2.json.trust, '(阶段不再推进,但信任仍+5?观察幂等性)');

  const chat3 = await post('/api/chat', { sessionId: created.json.sessionId, text: '按钮活过来了!' });
  console.log('9) 修复后对话:', chat3.json.stage, chat3.json.trust, '(ch1_done 内正常聊天)');
})();
