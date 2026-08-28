using GameManager;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Panels
{
    public class StartScreenPanel : UIPanel
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _quitButton;

        private void Start()
        {
            _startButton.onClick.AddListener(OnStartClicked);
            _quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void OnStartClicked()
        {
            GameFlowManager.Instance.StartGame();
        }

        private void OnQuitClicked()
        {
            Application.Quit();
        }
    }
}
