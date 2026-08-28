using System;
using System.Collections.Generic;
using GameManager;
using UI.Panels;
using UnityEngine;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private readonly Dictionary<Type, UIPanel> _panels = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            foreach (var panel in GetComponentsInChildren<UIPanel>(true))
                _panels[panel.GetType()] = panel;
        }

        private void Start()
        {
            var gfm = GameFlowManager.Instance;
            gfm.OnStateChanged += HandleStateChanged;
            gfm.OnPhaseChanged += HandlePhaseChanged;

            // 补偿：若 GameFlowManager.Start 比 UIManager.Start 先执行，手动同步一次
            HandleStateChanged(gfm.CurrentState);
        }

        private void OnDestroy()
        {
            if (GameFlowManager.Instance == null) return;
            GameFlowManager.Instance.OnStateChanged -= HandleStateChanged;
            GameFlowManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
        }

        // ── 状态驱动 UI ──────────────────────────────────────────

        private void HandleStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.GameStart:
                    HideAll();
                    Show<StartScreenPanel>();
                    Hide<GraveyardPanel>();
                    Hide<ChessHandPanel>();
                    Hide<ShopPanel>();
                    break;
                case GameState.GamePlaying:
                    Hide<StartScreenPanel>();
                    Show<GraveyardPanel>();
                    Show<ChessHandPanel>();
                    Show<ShopPanel>();
                    break;
                case GameState.GameEnd:
                    HideAll();
                    // TODO: Show<GameEndPanel>()
                    break;
            }
        }

        private void HandlePhaseChanged(BattlePhase phase)
        {
            switch (phase)
            {
                case BattlePhase.None:
                    HideAll();
                    break;
                case BattlePhase.Shop:
                    Hide<ChessHandPanel>();
                    Show<ShopPanel>();
                    break;
                case BattlePhase.PlayerAction:
                    Hide<ShopPanel>();
                    Show<ChessHandPanel>();
                    break;
                case BattlePhase.EnemyTurn:
                    Hide<ChessHandPanel>();
                    break;
            }
        }

        // ── 面板访问 ─────────────────────────────────────────────

        public T Get<T>() where T : UIPanel
        {
            _panels.TryGetValue(typeof(T), out var panel);
            return panel as T;
        }

        public void Show<T>() where T : UIPanel
        {
            Get<T>()?.Show();
        }

        public void Hide<T>() where T : UIPanel
        {
            Get<T>()?.Hide();
        }

        public void HideLayer(UILayer layer)
        {
            foreach (var panel in _panels.Values)
                if (panel.Layer == layer) panel.Hide();
        }

        public void HideAll()
        {
            foreach (var panel in _panels.Values)
                panel.Hide();
        }
    }
}
