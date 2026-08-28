using Battle;
using TMPro;
using UnityEngine;

namespace UI
{
    public class GraveyardItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _levelText;

        public void Setup(Chess chess)
        {
            _nameText.text  = $"Chess {chess.Data.Id}";
            _levelText.text = $"Lv.{chess.Level}";
        }
    }
}
