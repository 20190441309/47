using UnityEngine;
using UnityEngine.EventSystems;

namespace Patch47.Patch
{
    /// 端点拖拽:挂在 terminal 上,把指针/触摸事件转交给 PatchBoard。
    /// 必须独立成文件:曾与 PatchBoard 同文件,团结引擎保存场景时对「同文件第二个
    /// MonoBehaviour 类」写不出正常 GUID 引用,嵌了个空 MonoScript 存根导致拖拽
    /// 事件全部失效(2026-09-05 实测),拆文件后按 PatchBug 同样方式正常引用。
    public class PatchWireDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public PatchBoard board;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (board != null) board.BeginWire();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (board != null) board.DragWire(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (board != null) board.EndWire(eventData.position);
        }
    }
}
