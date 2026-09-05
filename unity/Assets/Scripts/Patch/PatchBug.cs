using UnityEngine;

namespace Patch47.Patch
{
    /// 场景里的 bug 物体(AGENTS.md 4.3):未修复 = 红色警示脉冲,点击打开补丁台;
    /// 修复成功 = 换实体绿 + 「✓ Patched」贴纸 + 一声轻响(代码合成,无外部音频素材)。
    /// M1 阶段推进走对话驱动,修复不调用后端新接口(见 docs/design-notes.md)。
    public class PatchBug : MonoBehaviour
    {
        [Header("引用(灰盒场景构建器注入)")]
        public Renderer bugRenderer;
        public Material fixedMaterial;
        public PatchBoard board;

        [Range(0.5f, 6f)] public float pulseFrequency = 2.2f;

        private static readonly Color Warn = new Color32(0xFF, 0x6B, 0x57, 255);  // 警示橙红
        private static readonly Color Fixed = new Color32(0x6E, 0xE7, 0xA0, 255); // 修复绿

        private bool repaired;
        private float phase;
        private static AudioClip ding;

        private void Update()
        {
            if (repaired) return;
            phase += Time.deltaTime * pulseFrequency;
            var pulse = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(phase));
            if (bugRenderer != null) bugRenderer.material.color = Warn * pulse; // 闪红光
        }

        private void OnMouseDown()
        {
            if (repaired || board == null || board.IsOpen) return;
            board.Open(this);
        }

        public void Fix()
        {
            if (repaired) return;
            repaired = true;
            if (bugRenderer != null && fixedMaterial != null) bugRenderer.sharedMaterial = fixedMaterial;
            SpawnSticker();
            AudioSource.PlayClipAtPoint(GetDing(), transform.position);
        }

        private void SpawnSticker()
        {
            // 挂在场景根而非 bug 之下,避开 bug 自身非均匀缩放(1.0, 0.55, 0.2)的挤压
            var sticker = new GameObject("PatchedSticker");
            sticker.transform.position = transform.position + new Vector3(0f, 0.75f, 0f);
            sticker.transform.localScale = Vector3.one * 0.025f; // 0.007 太小(截图实测几乎不可读)
            var mesh = sticker.AddComponent<TextMesh>();
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.fontSize = 64;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.color = Fixed;
            mesh.text = "✓ Patched";
            sticker.GetComponent<MeshRenderer>().material = mesh.font.material;
        }

        /// 一声温暖的"叮":正弦 880Hz + 指数衰减,运行时代码合成,不引入第三方音频(无需登记素材来源)。
        private static AudioClip GetDing()
        {
            if (ding != null) return ding;
            const int rate = 44100;
            const float duration = 0.4f;
            var samples = new float[(int)(rate * duration)];
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)rate;
                samples[i] = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.5f * Mathf.Exp(-t * 6f);
            }
            ding = AudioClip.Create("P47_Ding", samples.Length, 1, rate, false);
            ding.SetData(samples, 0);
            return ding;
        }
    }
}
