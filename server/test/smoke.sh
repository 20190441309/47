#!/usr/bin/env bash
# 后端冒烟测试(AGENTS.md 第 8 节):启动服务(若未运行)→ /api/health 与 /api/chat 各一条断言。
# 用法:在 server/ 下执行 `bash test/smoke.sh`;可用 BASE_URL 覆盖目标地址。
# 环境适配:本机 bash 为 WSL2 而 node 是 Windows 侧 node.exe——此时 WSL 的 curl 够不到
# Windows 回环,需用 curl.exe;原生 Linux(部署机)则直接 node + curl + localhost。
set -euo pipefail
cd "$(dirname "$0")/.."
BASE_URL="${BASE_URL:-http://localhost:3000}"
STARTED=0
SERVER_PID=""

IS_WSL=0
grep -qi microsoft /proc/version 2>/dev/null && IS_WSL=1

# 解析 node:优先 PATH 里的 node/node.exe,再落常见安装位置
NODE_BIN="$(command -v node || command -v node.exe || true)"
if [ -z "$NODE_BIN" ]; then
  for candidate in "/c/Program Files/nodejs/node.exe" "/mnt/c/Program Files/nodejs/node.exe"; do
    if [ -x "$candidate" ]; then NODE_BIN="$candidate"; break; fi
  done
fi

# WSL 里跑 Windows node.exe → 用 Windows 的 curl.exe 访问回环
CURL_BIN="curl"
if [ "$IS_WSL" = "1" ] && [[ "$NODE_BIN" == *node.exe* ]]; then
  CURL_BIN="$(command -v curl.exe || echo curl)"
fi

cleanup() {
  if [ "$STARTED" = "1" ] && [ -n "$SERVER_PID" ]; then
    kill "$SERVER_PID" 2>/dev/null || true
  fi
}
trap cleanup EXIT

if ! "$CURL_BIN" -sf --max-time 3 "$BASE_URL/api/health" >/dev/null 2>&1; then
  echo "(smoke) 服务未运行,启动 src/index.js ..."
  [ -n "$NODE_BIN" ] || { echo "FAIL: node 不可用"; exit 1; }
  "$NODE_BIN" src/index.js &
  SERVER_PID=$!
  STARTED=1
  for _ in $(seq 1 20); do
    if "$CURL_BIN" -sf --max-time 1 "$BASE_URL/api/health" >/dev/null 2>&1; then break; fi
    sleep 0.5
  done
fi

# 断言 1:健康检查
HEALTH=$("$CURL_BIN" -sf --max-time 5 "$BASE_URL/api/health")
echo "$HEALTH" | grep -q '"ok":true' || { echo "FAIL /api/health: $HEALTH"; exit 1; }
echo "PASS /api/health"

# 断言 2:会话创建
SESSION=$("$CURL_BIN" -sf --max-time 5 -X POST "$BASE_URL/api/session" -H 'Content-Type: application/json' -d '{}')
SID=$(echo "$SESSION" | grep -o '"sessionId":"[^"]*"' | head -n1 | cut -d'"' -f4)
[ -n "$SID" ] || { echo "FAIL /api/session: $SESSION"; exit 1; }
echo "PASS /api/session -> $SID"

# 断言 3:对话(未配 LLM key 时应返回符合人设的兜底台词)
CHAT=$("$CURL_BIN" -sf --max-time 12 -X POST "$BASE_URL/api/chat" -H 'Content-Type: application/json' -d "{\"sessionId\":\"$SID\",\"text\":\"我回来了\"}")
echo "$CHAT" | grep -q '"reply"' || { echo "FAIL /api/chat.reply: $CHAT"; exit 1; }
echo "$CHAT" | grep -q '"emotion"' || { echo "FAIL /api/chat.emotion: $CHAT"; exit 1; }
echo "$CHAT" | grep -q '"stage"' || { echo "FAIL /api/chat.stage: $CHAT"; exit 1; }
echo "PASS /api/chat -> $CHAT"

echo "SMOKE OK"
