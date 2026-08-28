using System.Collections.Generic;
using Battle;
using Battle.Data;
using GameManager;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Panels
{
    public class ShopPanel : UIPanel
    {
        [SerializeField] private Button      _refreshButton;
        [SerializeField] private Button      _endShoppingButton;
        [SerializeField] private Transform   _itemsContainer;
        [SerializeField] private GameObject  _itemTemplate;

        [Header("商店配置")]
        [SerializeField] private ChessData[] _chessPool;
        [SerializeField] private int         _slotCount = 5;

        private readonly List<ShopSlot>   _slots     = new();
        private readonly List<GameObject> _itemViews = new();

        private struct ShopSlot
        {
            public ChessData Data;
            public bool      IsSold;
        }

        private void Start()
        {
            _refreshButton.onClick.AddListener(RefreshShop);
            _endShoppingButton.onClick.AddListener(() => GameFlowManager.Instance.EndShopping());
        }

        protected override void OnShow() => RefreshShop();

        private void RefreshShop()
        {
            _slots.Clear();
            if (_chessPool != null && _chessPool.Length > 0)
            {
                int count = Mathf.Min(_slotCount, _chessPool.Length);
                for (int i = 0; i < count; i++)
                    _slots.Add(new ShopSlot { Data = _chessPool[Random.Range(0, _chessPool.Length)] });
            }

            EnsureItemViews(_slots.Count);
            for (int i = 0; i < _itemViews.Count; i++)
            {
                bool active = i < _slots.Count;
                _itemViews[i].SetActive(active);
                if (active)
                    BindSlot(i);
            }

            Debug.Log($"[ShopPanel] Refreshed — {_slots.Count} items");
        }

        private void EnsureItemViews(int count)
        {
            while (_itemViews.Count < count)
            {
                var go = Instantiate(_itemTemplate, _itemsContainer);
                _itemViews.Add(go);
            }
        }

        private void BindSlot(int idx)
        {
            var slot = _slots[idx];
            _itemViews[idx].GetComponent<ShopItemView>().Setup(
                $"Chess {slot.Data.Id}",
                slot.Data.Cost,
                slot.IsSold,
                idx,
                OnBuyItem
            );
        }

        private void OnBuyItem(int slotIndex)
        {
            if ((uint)slotIndex >= (uint)_slots.Count) return;
            var slot = _slots[slotIndex];
            if (slot.IsSold) return;

            var chess = new Chess();
            chess.Init(slot.Data, level: 1);
            ChessManager.Instance.ChessInRepository.Add(chess);

            slot.IsSold       = true;
            _slots[slotIndex] = slot;
            BindSlot(slotIndex);

            Debug.Log($"[ShopPanel] Bought Chess {slot.Data.Id} ({slot.Data.Cost}G)");
            // TODO: 扣除玩家金币
        }
    }
}
