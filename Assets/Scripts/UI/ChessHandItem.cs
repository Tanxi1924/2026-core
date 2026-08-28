using Battle;
using GameManager;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ChessHandItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _levelText;

        private Chess _chess;
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Canvas _rootCanvas;
        private Transform _originalParent;
        private int _originalSiblingIndex;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup  = GetComponent<CanvasGroup>();
        }

        private void Start()
        {
            _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        }

        public void Setup(Chess chess)
        {
            _chess = chess;
            _nameText.text  = $"Chess {chess.Data.Id}";
            _levelText.text = $"Lv.{chess.Level}";
        }

        // ── 拖拽 ─────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            _originalParent       = transform.parent;
            _originalSiblingIndex = transform.GetSiblingIndex();

            // 移到根Canvas，确保拖拽时渲染在最上层
            transform.SetParent(_rootCanvas.transform, true);

            _canvasGroup.alpha           = 0.7f;
            _canvasGroup.blocksRaycasts  = false; // 拖拽中穿透，让下方格子可以被检测到
        }

        public void OnDrag(PointerEventData eventData)
        {
            _rectTransform.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _canvasGroup.alpha          = 1f;
            _canvasGroup.blocksRaycasts = true;

            // 向3D世界射线检测HexTile
            Ray ray = Camera.main.ScreenPointToRay(eventData.position);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                var tile = hit.collider.GetComponent<HexTile>();
                if (tile != null && tile.IsEmpty)
                {
                    ChessManager.Instance.MoveChessToBoard(_chess, tile);
                    Destroy(gameObject);
                    return;
                }
            }

            // 放置无效，回到手牌原位
            transform.SetParent(_originalParent, true);
            transform.SetSiblingIndex(_originalSiblingIndex);
        }
    }
}
