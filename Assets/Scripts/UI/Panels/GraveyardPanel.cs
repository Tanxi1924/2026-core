using GameManager;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Panels
{
    public class GraveyardPanel : UIPanel
    {
        [SerializeField] private Transform     _itemContainer;
        [SerializeField] private GraveyardItem _itemPrefab;
        [SerializeField] private Button        _sellButton;

        private void Start()
        {
            _sellButton.onClick.AddListener(OnSellClicked);
        }

        // protected override void OnShow() => Refresh();

        public void Refresh()
        {
            foreach (Transform child in _itemContainer)
                Destroy(child.gameObject);

            foreach (var chess in ChessManager.Instance.GraveYard)
            {
                var item = Instantiate(_itemPrefab, _itemContainer);
                item.Setup(chess);
            }
        }

        private void OnSellClicked()
        {
            var selected = ChessManager.Instance.SelectedChess;
            if (selected == null) return;

            ChessManager.Instance.SellChessFromBoard(selected);
            Refresh();
        }
    }
}
