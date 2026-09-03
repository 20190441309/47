'use strict';
// 敏感词过滤:玩家输入先过这里再进导演层。
// 词表从 data/sensitive-words.txt 加载(每行一个,# 开头为注释),改完重启生效。

const fs = require('fs');
const path = require('path');

const WORDS_FILE = path.join(__dirname, '..', 'data', 'sensitive-words.txt');

let words = [];

function loadWords() {
  try {
    words = fs
      .readFileSync(WORDS_FILE, 'utf8')
      .split(/\r?\n/)
      .map((line) => line.trim())
      .filter((line) => line && !line.startsWith('#'));
  } catch {
    words = [];
  }
}

function sanitize(text) {
  let out = text;
  for (const word of words) {
    out = out.split(word).join('*'.repeat(Math.max(2, word.length)));
  }
  return out;
}

loadWords();

module.exports = { sanitize, reload: loadWords };
