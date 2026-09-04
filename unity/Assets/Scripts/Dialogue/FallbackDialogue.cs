using System.Collections.Generic;
using Patch47.GameFramework;
using UnityEngine;

namespace Patch47.Dialogue
{
    /// 离线兜底台词:Resources/Dialogue/Fallback/<stage>.json(由 server 的 export-fallback.js 导出,单一数据源)。
    /// 请求失败/超时自动切换,UI 无感,只亮右上角离线图标(AGENTS.md 4.1)。
    public static class FallbackDialogue
    {
        private static readonly Dictionary<string, FallbackStageDto> Cache = new Dictionary<string, FallbackStageDto>();
        private static readonly Dictionary<string, int> Cursor = new Dictionary<string, int>();

        public static FallbackStageDto Load(string stage)
        {
            if (Cache.TryGetValue(stage, out var dto)) return dto;
            var asset = Resources.Load<TextAsset>($"Dialogue/Fallback/{stage}");
            if (asset == null) return null;
            dto = JsonUtility.FromJson<FallbackStageDto>(asset.text);
            if (dto != null) Cache[stage] = dto;
            return dto;
        }

        public static string NextReply(string stage)
        {
            var dto = Load(stage);
            if (dto == null || dto.replies == null || dto.replies.Count == 0)
            {
                return "……(这里静得能听见电流声。帕奇暂时没有回音。)";
            }
            Cursor.TryGetValue(stage, out var index);
            var reply = dto.replies[index % dto.replies.Count];
            Cursor[stage] = (index + 1) % dto.replies.Count; // 顺序轮换,不连续重复
            return reply;
        }

        public static List<string> QuickReplies(string stage)
        {
            var dto = Load(stage);
            return dto != null && dto.quickReplies != null && dto.quickReplies.Count > 0
                ? dto.quickReplies
                : new List<string> { "……", "嗯。", "我在。" };
        }
    }
}
