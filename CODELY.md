

## Codely Structured Memories

### User

### Feedback

### Project
- [2026-09-04 20:26:49] 用户跨机器开发《第 47 号补丁》(UCDC 2026,截止 2026-10-31):GitHub 远程 origin = https://github.com/20190441309/47,main 分支是唯一真源;两机(主机+笔记本,笔记本常带出门)另用 SyncTrayzor(D:\program\SyncTrayzor,v1.1.29 绿色版)实时同步 patch-47 全目录(含 .env、未提交改动),.stignore 已入库,流程见 docs/new-machine-setup.md §9。**Why:** 用户在多台机器间切换开发,怕忘 push、要求实时同步且非 git 文件也要走。**How to apply:** 新会话开始先 git pull;里程碑仍建议 push origin;遇 .sync-conflict-* 文件按冲突处理;勿再建议网盘/SMB 等替代同步方案。syncthing 必须锁 v1.x(当前 data\syncthing.exe=v1.30.0,用户级环境变量 STNOUPGRADE=1 禁自升级)——v2 与 SyncTrayzor 1.1.29 命令行不兼容,会陷入 unknown flag -n 报错死循环。


### Reference

