using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace UI
{
    public class ShopItemView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private Button          _buyButton;

        private Action<int> _onBuy;
        private int         _slotIndex;

        public void Setup(string chessName, int cost, bool isSold, int slotIndex, Action<int> onBuy)
        {
            _slotIndex              = slotIndex;
            _onBuy                  = onBuy;
            _nameText.text          = chessName;
            _costText.text          = $"{cost}G";
            _buyButton.interactable = !isSold;

            _buyButton.onClick.RemoveAllListeners();
            _buyButton.onClick.AddListener(() => _onBuy?.Invoke(_slotIndex));
        }
    }
}
