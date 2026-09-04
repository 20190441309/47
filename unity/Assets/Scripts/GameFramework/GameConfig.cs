namespace Patch47.GameFramework
{
    /// 全局配置。联调地址指向本地 server(node src/index.js);部署后改生产域名。
    public static class GameConfig
    {
        public const string ApiBaseUrl = "http://localhost:3000";
        public const int RequestTimeoutSeconds = 8; // 与后端 LLM 超时对齐,超时即走兜底
        public const int MaxPlayerInputLength = 100;
    }
}
