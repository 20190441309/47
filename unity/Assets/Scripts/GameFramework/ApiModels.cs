using System;
using System.Collections.Generic;

namespace Patch47.GameFramework
{
    /// 接口数据模型(契约见 AGENTS.md 第 5 节)。JsonUtility 不支持字典,flags 只在服务端维护。
    [Serializable]
    public class SessionStateDto
    {
        public string stage;
        public int trust;
    }

    [Serializable]
    public class SessionResponseDto
    {
        public string sessionId;
        public SessionStateDto state;
    }

    [Serializable]
    public class ChatRequestDto
    {
        public string sessionId;
        public string text;
    }

    [Serializable]
    public class FlagChangeDto
    {
        public string name;
        public bool value;
    }

    [Serializable]
    public class ChatResponseDto
    {
        public string reply;
        public string emotion;
        public string stage;
        public int trust;
        public List<FlagChangeDto> flagsChanged;
    }

    /// 离线兜底台词文件(Resources/Dialogue/Fallback/<stage>.json,由 server 端导出)。
    [Serializable]
    public class FallbackStageDto
    {
        public string stage;
        public List<string> quickReplies;
        public List<string> replies;
    }
}
