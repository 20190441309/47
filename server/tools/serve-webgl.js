#!/usr/bin/env node
// 本地静态服务器:服务 unity/Builds 下的 WebGL 构建产物(开发联调用)。
// 关键点:Unity WebGL 的 Brotli 压缩产物(.wasm.br/.framework.js.br/.data.br)必须
// 带 Content-Encoding: br 响应头浏览器才会解压,否则加载报错——这是 WebGL 本地
// 实测最常见的坑(生产环境由 Caddy/Nginx 同样处理)。
// 用法:node server/tools/serve-webgl.js [构建目录] [端口=8080]
//   默认目录 unity/Builds/WebGLBaseline;手机同 Wi-Fi 访问 http://<本机IP>:端口
const http = require('http');
const fs = require('fs');
const path = require('path');

const root = path.resolve(
  process.argv[2] || path.join(__dirname, '..', '..', 'unity', 'Builds', 'WebGLBaseline'));
const port = Number(process.argv[3]) || 8080;

const types = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'application/javascript',
  '.wasm': 'application/wasm',
  '.css': 'text/css',
  '.png': 'image/png',
  '.ico': 'image/x-icon',
};

http.createServer((req, res) => {
  let pathname = decodeURIComponent((req.url || '/').split('?')[0]);
  if (pathname.endsWith('/')) pathname += 'index.html';
  const file = path.join(root, pathname);
  if (!file.startsWith(root)) { res.writeHead(403); res.end(); return; }
  fs.readFile(file, (err, data) => {
    if (err) { res.writeHead(404); res.end('not found: ' + pathname); return; }
    const ext = path.extname(file);
    const headers = {};
    if (ext === '.br') {
      // 去掉 .br 后取真实扩展名定 Content-Type,并声明压缩编码
      const realExt = path.extname(file.slice(0, -3));
      headers['Content-Type'] = types[realExt] || 'application/octet-stream';
      headers['Content-Encoding'] = 'br';
    } else {
      headers['Content-Type'] = types[ext] || 'application/octet-stream';
    }
    res.writeHead(200, headers);
    res.end(data);
  });
}).listen(port, () => {
  console.log(`WebGL 本地服务已启动`);
  console.log(`  目录: ${root}`);
  console.log(`  地址: http://localhost:${port}`);
});
