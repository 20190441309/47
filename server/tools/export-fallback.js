'use strict';
// 把 src/stages.js 的兜底台词导出为 Unity Resources JSON(单一数据源,防止两边漂移):
//   unity/Assets/Resources/Dialogue/Fallback/<stage>.json → { stage, quickReplies, replies }
// 用法:在 server/ 下执行 `npm run export-fallback`。

const fs = require('fs');
const path = require('path');
const { STAGES } = require('../src/stages');

const OUT_DIR = path.join(__dirname, '..', '..', 'unity', 'Assets', 'Resources', 'Dialogue', 'Fallback');

fs.mkdirSync(OUT_DIR, { recursive: true });
let count = 0;
for (const [id, stage] of Object.entries(STAGES)) {
  const payload = { stage: id, quickReplies: stage.quickReplies, replies: stage.fallback };
  fs.writeFileSync(path.join(OUT_DIR, `${id}.json`), `${JSON.stringify(payload, null, 2)}\n`);
  count += 1;
}
console.log(`exported ${count} fallback files -> ${OUT_DIR}`);
