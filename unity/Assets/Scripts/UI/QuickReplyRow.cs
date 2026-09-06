using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Patch47.UI
{
    /// 底部三个快捷回复按钮(降低手机输入门槛,AGENTS.md 4.1)。
    public class QuickReplyRow : MonoBehaviour
    {
        public Button[] buttons = new Button[3];
        public event Action<int> Clicked;

        public void SetLabels(List<string> labels)
        {
            for (var i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                var label = labels != null && i < labels.Count ? labels[i] : string.Empty;
                var text = buttons[i].GetComponentInChildren<Text>();
                if (text != null) text.text = label;
                buttons[i].gameObject.SetActive(!string.IsNullOrEmpty(label));
            }
        }

        /// 运行时自愈接线:点击监听必须每次 Play 重建——场景构建器的运行时 AddListener
        /// 不进序列化,编辑器域重载(脚本重编译)会静默清空(2026-09-06 实测按钮全死)。
        private void Awake()
        {
            Bind();
        }

        /// 绑定点击事件(幂等,可重复调用;先清后绑,防域重载残留或重复订阅)。
        public void Bind()
        {
            for (var i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                var index = i;
                buttons[i].onClick.RemoveAllListeners();
                buttons[i].onClick.AddListener(() => Clicked?.Invoke(index));
            }
        }
    }
}
