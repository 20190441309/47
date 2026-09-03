'use strict';
// LLM 调用:OpenAI 兼容接口,全部走环境变量配置(任何密钥不得写进代码或文档)。
// 未配置 key、超时、网络错误一律返回 null,由上层走兜底台词。

const LLM_TIMEOUT_MS = 8000; // 与 Unity 侧兜底超时对齐(>8s 自动切离线)

function llmConfigured() {
  return Boolean(process.env.LLM_BASE_URL && process.env.LLM_API_KEY && process.env.LLM_MODEL);
}

async function chatCompletion(messages, { temperature = 0.8, maxTokens = 400 } = {}) {
  if (!llmConfigured()) return null;
  const url = `${process.env.LLM_BASE_URL.replace(/\/+$/, '')}/chat/completions`;
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), LLM_TIMEOUT_MS);
  try {
    const res = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${process.env.LLM_API_KEY}`,
      },
      body: JSON.stringify({
        model: process.env.LLM_MODEL,
        messages,
        temperature,
        max_tokens: maxTokens,
      }),
      signal: controller.signal,
    });
    if (!res.ok) {
      console.error('[llm] http', res.status, await res.text().catch(() => ''));
      return null;
    }
    const data = await res.json();
    const content = data && data.choices && data.choices[0] && data.choices[0].message
      ? data.choices[0].message.content
      : null;
    return typeof content === 'string' ? content.trim() : null;
  } catch (err) {
    console.error('[llm] request failed:', err.message);
    return null; // 超时/网络错误 → 兜底
  } finally {
    clearTimeout(timer);
  }
}

module.exports = { llmConfigured, chatCompletion };
