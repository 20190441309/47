using System;
using System.IO;
using UnityEngine;

namespace Patch47.GameFramework
{
    /// 客户端单存档位(AGENTS.md 4.4):sessionId(恢复凭据)+ 进度 + 设置,JSON 存 persistentDataPath。
    /// 自动存档时机:会话建立/恢复后、每次对话后、bug 修复后(由 DialogueManager 统一调用)。
    /// 记忆(轮次/摘要)在服务端会话里,恢复靠 resumeSessionId,不复制进本地存档。
    public static class SaveManager
    {
        private static string SavePath
        {
            get { return Path.Combine(Application.persistentDataPath, "patch47-save.json"); }
        }

        [Serializable]
        public class SaveData
        {
            public string sessionId = "";
            public string stage = "ch1_arrival";
            public int trust = 50;
            public string[] flagNames = new string[0]; // flags 值全为 true,只存名字
            public string savedAt = "";
        }

        public static SaveData Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return null;
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                return string.IsNullOrEmpty(data.stage) ? null : data;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] 读档失败,视为无存档:{e.Message}");
                return null;
            }
        }

        public static void Save(SaveData data)
        {
            try
            {
                data.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] 存档失败:{e.Message}");
            }
        }

        public static void Clear()
        {
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] 清档失败:{e.Message}");
            }
        }
    }
}
