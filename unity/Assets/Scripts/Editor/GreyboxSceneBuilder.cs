#if UNITY_EDITOR
using System.IO;
using Patch47.Dialogue;
using Patch47.GameFramework;
using Patch47.Patch;
using Patch47.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Patch47.EditorTools
{
    /// 程序化生成第 1 章灰盒场景(帕奇方块+光标、打字机对话 UI)并保存为 Assets/Scenes/Ch1_Greybox.unity。
    /// 菜单:Tools/Patch47/生成灰盒场景;
    /// 命令行:Tuanjie.exe -batchmode -projectPath ... -executeMethod Patch47.EditorTools.GreyboxSceneBuilder.BuildAndSave -quit
    public static class GreyboxSceneBuilder
    {
        // 色板(AGENTS.md 第 6 节)
        private static readonly Color Bg = FromRgb(0x0E, 0x0F, 0x12);        // 背景近黑
        private static readonly Color Paper = FromRgb(0xE8, 0xE2, 0xD6);    // 纸感米白
        private static readonly Color PatchBlue = FromRgb(0x9A, 0xE6, 0xFF); // 帕奇青蓝
        private static readonly Color Warn = FromRgb(0xFF, 0x6B, 0x57);     // 警示橙红
        private static readonly Color PanelBg = FromRgb(0x16, 0x18, 0x1D);  // 对话框底
        private static readonly Color FixGreen = FromRgb(0x6E, 0xE7, 0xA0); // 修复绿(调试信息)

        private static Font uiFont;

        private static Font UiFont
        {
            get { return uiFont != null ? uiFont : uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
        }

        [MenuItem("Tools/Patch47/生成灰盒场景")]
        public static void BuildAndSave()
        {
            Build();
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/Ch1_Greybox.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("[GreyboxSceneBuilder] 已保存 Assets/Scenes/Ch1_Greybox.unity");
        }

        private static void Build()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---- 世界 ----
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Bg;
            cam.orthographic = true;
            cam.orthographicSize = 3.2f;
            camGo.transform.position = new Vector3(0f, 1.6f, -10f);

            var lightGo = new GameObject("Directional Light", typeof(Light));
            var light = lightGo.GetComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(8f, 1f, 8f);
            ground.GetComponent<Renderer>().sharedMaterial = MakeMaterial("P47_Ground", FromRgb(0x14, 0x18, 0x20));

            // 帕奇:未保存代码块(深色方块)+ 闪烁光标(青蓝细条,眼睛)
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "Patch";
            block.transform.position = new Vector3(-0.4f, 1.5f, 0f);
            block.transform.localScale = new Vector3(1.1f, 1.4f, 0.6f);
            block.GetComponent<Renderer>().sharedMaterial = MakeMaterial("P47_PatchBlock", FromRgb(0x1E, 0x24, 0x2E));

            var cursor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cursor.name = "Cursor";
            cursor.transform.SetParent(block.transform, false);
            cursor.transform.localPosition = new Vector3(0f, 0.92f, -0.05f);
            cursor.transform.localScale = new Vector3(0.07f, 0.55f, 0.07f);
            cursor.GetComponent<Renderer>().sharedMaterial = MakeMaterial("P47_PatchCursor", PatchBlue);

            var avatar = block.AddComponent<PatchAvatar>();
            avatar.blockRenderer = block.GetComponent<Renderer>();
            avatar.cursorRenderer = cursor.GetComponent<Renderer>();
            avatar.cursor = cursor.transform;

            // 第一个 bug 物体:没绑 OnClick 的"开始游戏"按钮(红色线框示意)
            var bug = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bug.name = "Bug_StartButton";
            bug.transform.position = new Vector3(1.2f, 1.0f, 1.2f); // 竖屏 9:16 下半宽≈1.8,须留在画面内
            bug.transform.localScale = new Vector3(1.0f, 0.55f, 0.2f);
            bug.GetComponent<Renderer>().sharedMaterial = MakeMaterial("P47_BugRed", Warn);

            var bugController = bug.AddComponent<PatchBug>();
            bugController.bugRenderer = bug.GetComponent<Renderer>();
            bugController.fixedMaterial = MakeMaterial("P47_FixedGreen", FixGreen);

            BuildUi(avatar, bugController);
        }

        private static void BuildUi(PatchAvatar avatar, PatchBug bugController)
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f); // 手机竖屏优先
            scaler.matchWidthOrHeight = 1f; // 按高度匹配:横屏下按宽度缩放会把 640 高的对话面板放大到盖住大半屏,挡住 3D 世界(bug/帕奇)

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // ---- 对话面板(底部) ----
            var panelGo = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image));
            var panelRect = (RectTransform)panelGo.transform;
            panelRect.SetParent(canvasGo.transform, false);
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = new Vector2(0f, 640f);
            panelRect.anchoredPosition = Vector2.zero;
            panelGo.GetComponent<Image>().color = PanelBg;

            // 回复文本(上半区)
            var replyText = MakeText(panelRect, "ReplyText", string.Empty, 40, Paper, TextAnchor.UpperLeft);
            var replyRect = (RectTransform)replyText.transform;
            replyRect.anchorMin = new Vector2(0f, 0.5f);
            replyRect.anchorMax = new Vector2(1f, 1f);
            replyRect.offsetMin = new Vector2(36f, 24f);
            replyRect.offsetMax = new Vector2(-36f, -36f);

            var typewriter = panelGo.AddComponent<Typewriter>();
            typewriter.targetText = replyText;

            // 快捷回复三连(中部)
            var rowGo = new GameObject("QuickReplies", typeof(RectTransform));
            var rowRect = (RectTransform)rowGo.transform;
            rowRect.SetParent(panelRect, false);
            rowRect.anchorMin = new Vector2(0f, 0.32f);
            rowRect.anchorMax = new Vector2(1f, 0.48f);
            rowRect.offsetMin = new Vector2(36f, 8f);
            rowRect.offsetMax = new Vector2(-36f, -8f);
            var layout = rowGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var quickReplyRow = rowGo.AddComponent<QuickReplyRow>();
            quickReplyRow.buttons = new Button[3];
            for (var i = 0; i < 3; i++)
            {
                quickReplyRow.buttons[i] = MakeButton(rowRect, $"Quick_{i}", "……");
            }

            // 输入行(底部)
            var inputGo = new GameObject("PlayerInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            var inputRect = (RectTransform)inputGo.transform;
            inputRect.SetParent(panelRect, false);
            inputRect.anchorMin = new Vector2(0f, 0.05f);
            inputRect.anchorMax = new Vector2(0.78f, 0.3f);
            inputRect.offsetMin = new Vector2(36f, 0f);
            inputRect.offsetMax = new Vector2(-16f, 0f);
            inputGo.GetComponent<Image>().color = FromRgb(0x20, 0x24, 0x2E);

            var input = inputGo.GetComponent<InputField>();
            input.characterLimit = GameConfig.MaxPlayerInputLength;
            var inputText = MakeText(inputRect, "Text", string.Empty, 36, Paper, TextAnchor.MiddleLeft);
            StretchWithMargins((RectTransform)inputText.transform, 20f, 16f, 20f, 16f);
            input.textComponent = inputText;
            var placeholder = MakeText(inputRect, "Placeholder", "对帕奇说点什么……(100 字内)", 32,
                FromRgb(0x6A, 0x70, 0x7C), TextAnchor.MiddleLeft);
            StretchWithMargins((RectTransform)placeholder.transform, 20f, 16f, 20f, 16f);
            input.placeholder = placeholder;
            input.customCaretColor = true;
            input.caretColor = PatchBlue;

            var send = MakeButton(panelRect, "SendButton", "发送");
            var sendRect = (RectTransform)send.transform;
            sendRect.anchorMin = new Vector2(0.8f, 0.05f);
            sendRect.anchorMax = new Vector2(1f, 0.3f);
            sendRect.offsetMin = new Vector2(0f, 0f);
            sendRect.offsetMax = new Vector2(-36f, 0f);

            // 顶部调试:阶段 + trust(M1 验证用)
            var stageLabel = MakeText(canvasGo.transform, "StageLabel", "ch1_arrival  trust 50", 28, FixGreen, TextAnchor.UpperLeft);
            var stageRect = (RectTransform)stageLabel.transform;
            stageRect.anchorMin = new Vector2(0f, 1f);
            stageRect.anchorMax = new Vector2(0f, 1f);
            stageRect.pivot = new Vector2(0f, 1f);
            stageRect.anchoredPosition = new Vector2(24f, -24f);
            stageRect.sizeDelta = new Vector2(720f, 48f);

            // 离线指示(右上角,兜底激活;M2:可点击重连)
            var offlineGo = new GameObject("OfflineIndicator", typeof(RectTransform), typeof(Text), typeof(Button));
            var offlineRect = (RectTransform)offlineGo.transform;
            offlineRect.SetParent(canvasGo.transform, false);
            offlineRect.anchorMin = new Vector2(1f, 1f);
            offlineRect.anchorMax = new Vector2(1f, 1f);
            offlineRect.pivot = new Vector2(1f, 1f);
            offlineRect.anchoredPosition = new Vector2(-24f, -24f);
            offlineRect.sizeDelta = new Vector2(420f, 48f);
            var offlineText = offlineGo.GetComponent<Text>();
            offlineText.font = UiFont;
            offlineText.fontSize = 30;
            offlineText.color = Warn;
            offlineText.alignment = TextAnchor.UpperRight;
            offlineText.raycastTarget = true; // Text 即点击热区(灰盒偷懒,美术阶段换按钮皮肤)
            offlineText.text = "离线 · 点此重连";
            offlineGo.SetActive(false);

            // ---- 编排器 ----
            var managerGo = new GameObject("DialogueManager");
            var manager = managerGo.AddComponent<DialogueManager>();
            manager.inputField = input;
            manager.sendButton = send;
            manager.quickReplies = quickReplyRow;
            manager.typewriter = typewriter;
            manager.patch = avatar;
            manager.stageLabel = stageLabel;
            manager.offlineIndicator = offlineGo;

            send.onClick.AddListener(manager.OnSend);
            offlineGo.GetComponent<Button>().onClick.AddListener(manager.OnReconnect); // 手动重连(M2)
            quickReplyRow.Bind();
            quickReplyRow.Clicked += manager.OnQuickReply;
            bugController.dialogue = manager; // 修复事件 → 对话编排器上报 /api/event

            var board = BuildPatchBoard(canvasGo.transform);
            bugController.board = board;
        }

        /// 补丁台面板(默认隐藏;点 bug 物体弹出):接线谜题 UI(AGENTS.md 4.3)。
        /// 根节点 = 全屏半透明遮罩(点空白处关闭),面板本体为其子物体(点面板不关闭)。
        private static PatchBoard BuildPatchBoard(Transform canvas)
        {
            var rootGo = new GameObject("PatchBoard", typeof(RectTransform), typeof(Image), typeof(Button));
            var rootRect = (RectTransform)rootGo.transform;
            rootRect.SetParent(canvas, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            rootGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f); // 压暗背景,突出面板

            var boardGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            var boardRect = (RectTransform)boardGo.transform;
            boardRect.SetParent(rootRect, false);
            boardRect.anchorMin = boardRect.anchorMax = Vector2.one * 0.5f;
            boardRect.sizeDelta = new Vector2(960f, 820f);
            boardGo.GetComponent<Image>().color = FromRgb(0x1A, 0x1D, 0x23);

            var title = MakeText(boardRect, "Title", "补丁台 · 把事件线接上", 40, Paper, TextAnchor.MiddleCenter);
            var titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -28f);
            titleRect.sizeDelta = new Vector2(0f, 72f);

            var hint = MakeText(boardRect, "Hint", "按住左侧 SendSignal 端点,拖到右侧 On Click() 空槽", 26,
                FromRgb(0x8A, 0x90, 0x9A), TextAnchor.MiddleCenter);
            var hintRect = (RectTransform)hint.transform;
            hintRect.anchorMin = new Vector2(0f, 1f);
            hintRect.anchorMax = new Vector2(1f, 1f);
            hintRect.pivot = new Vector2(0.5f, 1f);
            hintRect.anchoredPosition = new Vector2(0f, -110f);
            hintRect.sizeDelta = new Vector2(0f, 48f);

            var compLabel = MakeText(boardRect, "CompLabel", "组件:开始游戏按钮", 30, Paper, TextAnchor.MiddleLeft);
            var compRect = (RectTransform)compLabel.transform;
            compRect.anchorMin = compRect.anchorMax = new Vector2(0f, 0.5f);
            compRect.pivot = new Vector2(0f, 0.5f);
            compRect.anchoredPosition = new Vector2(64f, 110f);
            compRect.sizeDelta = new Vector2(360f, 44f);

            // 左端点:SendSignal(可拖拽出线,圆点把手)
            var terminalGo = new GameObject("Terminal", typeof(RectTransform), typeof(Image));
            var terminalRect = (RectTransform)terminalGo.transform;
            terminalRect.SetParent(boardRect, false);
            terminalRect.anchorMin = terminalRect.anchorMax = new Vector2(0f, 0.5f);
            terminalRect.pivot = new Vector2(0.5f, 0.5f);
            terminalRect.anchoredPosition = new Vector2(170f, -20f);
            terminalRect.sizeDelta = new Vector2(72f, 72f);
            var terminalImage = terminalGo.GetComponent<Image>();
            terminalImage.color = PatchBlue;
            terminalImage.sprite = MakeCircleSprite();

            var terminalLabel = MakeText(boardRect, "TerminalLabel", "SendSignal", 26, PatchBlue, TextAnchor.MiddleCenter);
            var terminalLabelRect = (RectTransform)terminalLabel.transform;
            terminalLabelRect.anchorMin = terminalLabelRect.anchorMax = new Vector2(0f, 0.5f);
            terminalLabelRect.pivot = new Vector2(0.5f, 0.5f);
            terminalLabelRect.anchoredPosition = new Vector2(170f, -90f);
            terminalLabelRect.sizeDelta = new Vector2(300f, 40f);

            var slotTitle = MakeText(boardRect, "SlotTitle", "事件:On Click()", 30, Paper, TextAnchor.MiddleRight);
            var slotTitleRect = (RectTransform)slotTitle.transform;
            slotTitleRect.anchorMin = slotTitleRect.anchorMax = new Vector2(1f, 0.5f);
            slotTitleRect.pivot = new Vector2(1f, 0.5f);
            slotTitleRect.anchoredPosition = new Vector2(-64f, 110f);
            slotTitleRect.sizeDelta = new Vector2(360f, 44f);

            // 右侧空槽:On Click()(接线目标)
            var slotGo = new GameObject("Slot", typeof(RectTransform), typeof(Image));
            var slotRect = (RectTransform)slotGo.transform;
            slotRect.SetParent(boardRect, false);
            slotRect.anchorMin = slotRect.anchorMax = new Vector2(1f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = new Vector2(-230f, -20f);
            slotRect.sizeDelta = new Vector2(300f, 110f);
            slotGo.GetComponent<Image>().color = FromRgb(0x24, 0x28, 0x33);

            var slotLabel = MakeText(slotRect, "SlotLabel", "未绑定", 32, FromRgb(0x9A, 0xA0, 0xAA), TextAnchor.MiddleCenter);
            StretchWithMargins((RectTransform)slotLabel.transform, 8f, 8f, 8f, 8f);

            // 连线(拖拽时显示)
            var wireGo = new GameObject("Wire", typeof(RectTransform), typeof(Image));
            var wireRect = (RectTransform)wireGo.transform;
            wireRect.SetParent(boardRect, false);
            wireRect.anchorMin = wireRect.anchorMax = Vector2.one * 0.5f;
            wireRect.pivot = new Vector2(0.5f, 0.5f);
            wireRect.sizeDelta = new Vector2(0f, 10f);
            var wireImage = wireGo.GetComponent<Image>();
            wireImage.color = PatchBlue;
            wireImage.raycastTarget = false;
            wireGo.SetActive(false);

            // 关闭按钮:贴面板右上角内侧
            var close = MakeButton(boardRect, "CloseButton", "×");
            var closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(0.5f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-92f, -92f);
            closeRect.sizeDelta = new Vector2(120f, 120f);

            var board = rootGo.AddComponent<PatchBoard>();
            board.terminal = terminalRect;
            board.slot = slotRect;
            board.wire = wireRect;
            board.slotLabel = slotLabel;
            close.onClick.AddListener(board.Close);
            rootGo.GetComponent<Button>().onClick.AddListener(board.Close); // 点遮罩空白处也关闭

            var drag = terminalGo.AddComponent<PatchWireDrag>();
            drag.board = board;

            rootGo.SetActive(false);
            return board;
        }

        // ---- 构建辅助 ----

        private static Text MakeText(Transform parent, string name, string content, int size, Color color, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = UiFont;
            text.fontSize = size;
            text.color = color;
            text.text = content;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Button MakeButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            go.GetComponent<Image>().color = Paper;
            var button = go.GetComponent<Button>();
            var text = MakeText(rect, "Label", label, 34, Bg, TextAnchor.MiddleCenter);
            StretchWithMargins((RectTransform)text.transform, 8f, 4f, 8f, 4f);
            return button;
        }

        private static void StretchWithMargins(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static Material MakeMaterial(string name, Color color)
        {
            const string dir = "Assets/Materials";
            var path = $"{dir}/{name}.mat";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                ApplyColor(existing, color);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            var material = new Material(FindUnlitShader()) { name = name };
            ApplyColor(material, color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// 端点圆点贴图:64×64 软边白圆(由 Image.color 染色),存 Assets/Art/terminal-dot.png。
        /// 纯代码生成,不引入第三方素材(无需登记授权)。文件 IO 用绝对路径(不依赖进程工作目录)。
        private static Sprite MakeCircleSprite()
        {
            const string assetPath = "Assets/Art/terminal-dot.png";
            var absolutePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "Assets", "Art", "terminal-dot.png");
            if (!File.Exists(absolutePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
                const int size = 64;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var center = Vector2.one * (size - 1) * 0.5f;
                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var alpha = Mathf.Clamp01((size * 0.5f - Vector2.Distance(new Vector2(x, y), center)) / 1.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                tex.Apply();
                File.WriteAllBytes(absolutePath, tex.EncodeToPNG());
            }
            AssetDatabase.ImportAsset(assetPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void ApplyColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color); // URP
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);         // 内置管线
        }

        private static Shader FindUnlitShader()
        {
            // 按模板管线自动适配:URP Unlit → 内置 Unlit → Sprites 兜底
            var names = new[] { "Universal Render Pipeline/Unlit", "Unlit/Color", "Sprites/Default" };
            foreach (var name in names)
            {
                var shader = Shader.Find(name);
                if (shader != null && shader.isSupported) return shader;
            }
            return Shader.Find("Standard");
        }

        private static Color FromRgb(byte r, byte g, byte b)
        {
            return new Color32(r, g, b, 255);
        }
    }
}
#endif
