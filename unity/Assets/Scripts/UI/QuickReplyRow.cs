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

        /// 由场景构建器调用一次,绑定点击事件。
        public void Bind()
        {
            for (var i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                var index = i;
                buttons[i].onClick.AddListener(() => Clicked?.Invoke(index));
            }
        }
    }
}
