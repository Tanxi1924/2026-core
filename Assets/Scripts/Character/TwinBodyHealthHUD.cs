using MoreMountains.CorgiEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TwinBody
{
    /// <summary>
    /// 双体角色的血量显示：屏幕右上角分两行显示珍妮佛 / 约翰各自的当前血量（对应文档 3.4，
    /// 两个头的 Health 各自独立，这里只是纯展示，不做扣血/回血逻辑）。
    ///
    /// 运行时自己在 Awake 里搭一个独立的 Screen Space - Overlay Canvas + 两行 TextMeshProUGUI，
    /// 不依赖场景里手摆的 UI，也不跟着角色旋转/移动。
    /// </summary>
    [RequireComponent(typeof(TwinBodyCharacter))]
    [AddComponentMenu("Corgi Engine/Character/Twin Body/Health HUD (Custom)")]
    public class TwinBodyHealthHUD : MonoBehaviour
    {
        [Header("初始血量（由策划调节）")]
        [Tooltip("开局时写入珍妮佛 / 约翰两个头的 Health.InitialHealth / MaximumHealth，" +
                 "两个头各自还是独立血量（互不影响），这里只是让两边共用同一个起始数值，" +
                 "不用去两个 Health 组件上分别改一遍。")]
        [Min(1f)] public float InitialHealth = 100f;

        [Header("HUD 排版（由策划调节）")]
        public Vector2 ScreenPadding = new Vector2(24f, 24f);
        public float LineSpacing = 36f;
        public int FontSize = 28;

        protected TwinBodyCharacter _body;
        protected Health _jenniferHealth;
        protected Health _johnHealth;

        protected TextMeshProUGUI _jenniferLabel;
        protected TextMeshProUGUI _johnLabel;

        private void Awake()
        {
            _body = GetComponent<TwinBodyCharacter>();
            _jenniferHealth = _body.JenniferNode != null ? _body.JenniferNode.GetComponent<Health>() : null;
            _johnHealth = _body.JohnNode != null ? _body.JohnNode.GetComponent<Health>() : null;

            // 必须在 Awake 里写：Health.Start() 会用 InitialHealth 初始化 CurrentHealth，
            // Unity 保证同一帧所有 Awake 先于所有 Start 执行，这里改字段能赶在它前面生效。
            ApplySharedInitialHealth(_jenniferHealth);
            ApplySharedInitialHealth(_johnHealth);

            BuildHud();
        }

        private void ApplySharedInitialHealth(Health health)
        {
            if (health == null) return;
            health.InitialHealth = InitialHealth;
            health.MaximumHealth = InitialHealth;
        }

        private void Update()
        {
            UpdateLabel(_jenniferLabel, "珍妮佛", _jenniferHealth);
            UpdateLabel(_johnLabel, "约翰", _johnHealth);
        }

        private void UpdateLabel(TextMeshProUGUI label, string displayName, Health health)
        {
            if (label == null) return;

            if (health == null)
            {
                label.text = $"{displayName}: --";
                return;
            }

            label.text = $"{displayName}  {Mathf.CeilToInt(health.CurrentHealth)} / {Mathf.CeilToInt(health.MaximumHealth)}";
        }

        private void BuildHud()
        {
            var canvasGO = new GameObject("TwinBodyHealthHUDCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            _jenniferLabel = CreateLabel(canvasGO.transform, "JenniferHealthLabel", 0);
            _johnLabel = CreateLabel(canvasGO.transform, "JohnHealthLabel", 1);
        }

        private TextMeshProUGUI CreateLabel(Transform parent, string name, int lineIndex)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(420f, LineSpacing);
            rt.anchoredPosition = new Vector2(-ScreenPadding.x, -ScreenPadding.y - lineIndex * LineSpacing);

            var label = go.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.TopRight;
            label.fontSize = FontSize;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = string.Empty;

            return label;
        }
    }
}
