using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Patch47.Patch
{
    /// 补丁台(AGENTS.md 4.3):第 1 章谜题「接线」——把「开始游戏.SendSignal」
    /// 拖到「On Click ()」空槽,连上即修复该 bug 物体。
    /// 滑杆/拖放两类交互是第 2 章谜题,此处不实现(范围控制,见 AGENTS.md 第 9 节)。
    public class PatchBoard : MonoBehaviour
    {
        [Header("UI 引用(灰盒场景构建器注入)")]
        public RectTransform terminal;   // 左侧端点:SendSignal
        public RectTransform slot;      // 右侧事件槽:On Click()
        public RectTransform wire;       // 拖出的连线
        public Text slotLabel;           // 槽内状态文字(未绑定/已绑定)
        public Button closeButton;

        private PatchBug activeBug;
        private bool connected;

        public bool IsOpen { get { return gameObject.activeSelf; } }

        public void Open(PatchBug bug)
        {
            if (IsOpen) return;
            activeBug = bug;
            connected = false;
            if (slotLabel != null) slotLabel.text = "未绑定";
            if (wire != null) wire.gameObject.SetActive(false);
            gameObject.SetActive(true);
        }

        public void Close()
        {
            if (!IsOpen) return;
            gameObject.SetActive(false);
            activeBug = null;
        }

        public void BeginWire()
        {
            if (connected || wire == null) return;
            wire.gameObject.SetActive(true);
        }

        public void DragWire(Vector2 screenPoint)
        {
            if (connected || wire == null || terminal == null) return;
            Vector2 local;
            // terminal 的父级是面板本体;拖线坐标必须换算到面板坐标系(wire/terminal/slot 同级)
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)terminal.parent, screenPoint, null, out local)) return;
            StretchWire(terminal.localPosition, local);
        }

        public void EndWire(Vector2 screenPoint)
        {
            if (connected) return;
            if (RectTransformUtility.RectangleContainsScreenPoint(slot, screenPoint))
            {
                Connect();
            }
            else if (wire != null)
            {
                wire.gameObject.SetActive(false); // 没接上,线收回
            }
        }

        private void Connect()
        {
            connected = true;
            StretchWire(terminal.localPosition, slot.localPosition);
            if (slotLabel != null) slotLabel.text = "已绑定";
            StartCoroutine(FixAfterDelay());
        }

        private IEnumerator FixAfterDelay()
        {
            yield return new WaitForSeconds(0.6f); // 让玩家看清"接上了"再修
            if (activeBug != null) activeBug.Fix();
            Close();
        }

        private void StretchWire(Vector2 from, Vector2 to)
        {
            var delta = to - from;
            wire.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            wire.sizeDelta = new Vector2(delta.magnitude, 8f);
            wire.localPosition = (from + to) * 0.5f;
        }
    }
}
