using GameManager;
using UnityEngine;

namespace UI.Panels
{
    public class ChessHandPanel : UIPanel
    {
        [SerializeField] private Transform _itemContainer;
        [SerializeField] private ChessHandItem _itemPrefab;

        // protected override void OnShow() => Refresh();

        public void Refresh()
        {
            foreach (Transform child in _itemContainer)
                Destroy(child.gameObject);

            foreach (var chess in ChessManager.Instance.ChessInRepository)
            {
                var item = Instantiate(_itemPrefab, _itemContainer);
                item.Setup(chess);
            }
        }
    }
}
