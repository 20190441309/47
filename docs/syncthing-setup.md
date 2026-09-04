# 主机安装两机实时同步(SyncTrayzor)操作手册

> 适用场景:第二台机器(主机)从零安装到与笔记本配对完成,照抄即可,全程约 15 分钟。
> 背景与原理见 `new-machine-setup.md` §9;git/GitHub 仍是唯一真源,Syncthing 只负责实时接力(未提交改动、`server/.env` 等)。

## 0. 前置检查

- 主机已有仓库 `D:\hki\patch-47`(没有 → 按 `new-machine-setup.md` §1 clone)
- **笔记本已完成 `git add -A` + commit + push**(`.stignore` 和本文档要跟着过去)
- 主机 `git pull`,确认 `D:\hki\patch-47\.stignore` 存在
- 两台同时开机、连同一 WiFi(首次配对最顺)

## 1. 安装 SyncTrayzor(顺序不能乱!)

> **为什么顺序重要**:SyncTrayzor v1.1.29 捆绑的 syncthing 是 2021 年的 v1.18.1,首次启动会自动升级到 v2.x;v2 命令行与 SyncTrayzor 不兼容,会陷入 `unknown flag -n` 报错死循环(笔记本 2026-09-04 实测踩过)。所以**必须先禁升级再运行**。

### 1.1 下载解压(PowerShell,主机上执行)

```powershell
curl.exe -L -o "$env:TEMP\SyncTrayzorPortable-x64.zip" "https://github.com/canton7/SyncTrayzor/releases/download/v1.1.29/SyncTrayzorPortable-x64.zip"
Expand-Archive "$env:TEMP\SyncTrayzorPortable-x64.zip" -DestinationPath "D:\program\SyncTrayzor" -Force
Get-ChildItem "D:\program\SyncTrayzor\SyncTrayzorPortable-x64" -Force | Move-Item -Destination "D:\program\SyncTrayzor" -Force
Remove-Item "D:\program\SyncTrayzor\SyncTrayzorPortable-x64" -Recurse -Force
```

> 最后两行是把解压出来的内层文件夹拉平,让程序位于 `D:\program\SyncTrayzor\SyncTrayzor.exe`。

### 1.2 禁用自动升级(首次运行**之前**执行)

```powershell
setx STNOUPGRADE 1
```

### 1.3 首次运行

1. 双击 `D:\program\SyncTrayzor\SyncTrayzor.exe`
2. 过首次向导(语言、介绍,一路下一步)
3. **防火墙弹窗**:两个网络类型(专用+公用)都勾 → 允许访问
4. 托盘出现图标即成功。若出现 `unknown flag -n` 报错循环 → 别慌,直接做完第 2 节换二进制再开,一样能救回来

### 1.4 完全退出

托盘图标右键 → 退出(下一步要覆盖文件,必须先退出)

## 2. 替换 syncthing 为 v1.30.0

### 2.1 下载覆盖(确认 SyncTrayzor 已退出)

```powershell
curl.exe -L -o "$env:TEMP\syncthing-v1.30.0.zip" "https://github.com/syncthing/syncthing/releases/download/v1.30.0/syncthing-windows-amd64-v1.30.0.zip"
Expand-Archive "$env:TEMP\syncthing-v1.30.0.zip" -DestinationPath "$env:TEMP\syncthing-v1.30.0" -Force
Copy-Item "$env:TEMP\syncthing-v1.30.0\syncthing-windows-amd64-v1.30.0\syncthing.exe" "D:\program\SyncTrayzor\data\syncthing.exe" -Force
```

### 2.2 重开并验证

1. 重新打开 `SyncTrayzor.exe`
2. 菜单 **操作 → 显示日志**,确认出现这行且无报错:
   ```
   syncthing v1.30.0 "Gold Grasshopper"
   ```

### 2.3 收尾设置

- 设置里开启**开机自启**
- 左栏文件夹列表若有 "Default Folder"(指向 `C:\Users\...\Sync`)→ 编辑 → 删除
- **永远别删 `STNOUPGRADE` 环境变量、别升级 syncthing 到 v2**

## 3. 两台配对

笔记本(已装好那台)的设备 ID:

```
IDHYCVH-3CHQPMF-UMSYTMS-JRNIP4V-KFPN4DA-K56UGY3-KDS4RZO-ZSWJ5QN
```

(拿不准可在笔记本上 SyncTrayzor → 操作 → 显示 ID 复核)

1. 主机:SyncTrayzor → **操作 → 显示 ID**,复制自己的 ID
2. 笔记本:左栏**设备** → 添加设备 → 粘贴主机 ID → 保存
3. 主机:左栏**设备** → 添加设备 → 粘贴上面的笔记本 ID → 保存
   (哪边先做都行,另一边会弹"新设备"提示,接受即可)
4. 主机:**添加文件夹** → 路径选 `D:\hki\patch-47` → "共享"页勾选笔记本设备 → 保存
5. 笔记本:弹出共享请求 → 接受,路径核对为 `D:\hki\patch-47` → 保存
6. 两台都:文件夹 → 编辑 → **版本控制 → 回收站式版本控制,保留 7 天**(防误删扩散)

## 4. 验证

1. 两台托盘图标转一会儿后显示"最新"
2. 主机建个测试文件 `D:\hki\patch-47\.sync-test.txt`,几秒内笔记本出现 → 删掉
3. `server/.env` 两边一致(不进 git 但会同步,这正是装它的意义)

## 5. 使用纪律(3 条,违反会丢东西)

1. **永远不要两台同时编辑**同一文件。极端冲突时 Syncthing 生成 `.sync-conflict-*` 副本而不是覆盖,能救但很烦
2. **开工前看一眼托盘图标**,绿色(最新)再动手
3. **到里程碑照常 push GitHub**——笔记本出门丢了/摔了,GitHub 是唯一异地副本

## 6. 出问题查这里

| 症状 | 原因 / 解法 |
|---|---|
| `unknown flag -n` 报错死循环 | `STNOUPGRADE` 没生效,或 `data\syncthing.exe` 被换成 v2;重做 §1.2 + §2.1 |
| 两台互相找不到(状态一直"断开") | 防火墙没放行;Windows 设置 → 防火墙 → 允许应用,给 syncthing.exe 勾专用+公用 |
| 同步很慢 | 走了公网中继(出门属正常);家里确认两台同一 WiFi |
| 公网中继也连不通 | 两台装 Tailscale 组虚拟局域网 |
| 想确认同步是否活着 | 托盘右键 → 显示日志,末尾无 ERROR 即正常 |
