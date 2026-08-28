using System;
using UnityEngine;

namespace GameManager
{
    public enum GameState
    {
        GameStart,
        GamePlaying,
        GameEnd
    }

    public enum BattlePhase
    {
        None,
        Shop,
        PlayerAction,
        EnemyTurn
    }

    public enum GameResult
    {
        None,
        Win,
        Lose
    }

    public class GameFlowManager : MonoBehaviour
    {
        public static GameFlowManager Instance { get; private set; }

        public GameState  CurrentState  { get; private set; }
        public BattlePhase CurrentPhase { get; private set; }
        public int        CurrentRound  { get; private set; }
        public GameResult Result        { get; private set; }

        public event Action<GameState>   OnStateChanged;
        public event Action<BattlePhase> OnPhaseChanged;
        public event Action<int>         OnRoundChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            ChangeState(GameState.GameStart);
        }

        // ── GameState ────────────────────────────────────────────

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;

            ExitState(CurrentState);
            CurrentState = newState;
            EnterState(CurrentState);

            OnStateChanged?.Invoke(CurrentState);
        }

        public void StartGame()   => ChangeState(GameState.GamePlaying);
        public void EndGame()     => ChangeState(GameState.GameEnd);
        public void RestartGame() => ChangeState(GameState.GameStart);

        private void EnterState(GameState state)
        {
            switch (state)
            {
                case GameState.GameStart:   OnEnterGameStart();   break;
                case GameState.GamePlaying: OnEnterGamePlaying(); break;
                case GameState.GameEnd:     OnEnterGameEnd();     break;
            }
        }

        private void ExitState(GameState state)
        {
            switch (state)
            {
                case GameState.GameStart:   OnExitGameStart();   break;
                case GameState.GamePlaying: OnExitGamePlaying(); break;
                case GameState.GameEnd:     OnExitGameEnd();     break;
            }
        }

        private void OnEnterGameStart()
        {
            CurrentRound = 0;
            Result = GameResult.None;
            Debug.Log("[GameFlowManager] Enter: GameStart");
        }

        private void OnExitGameStart()
        {
            Debug.Log("[GameFlowManager] Exit: GameStart");
        }

        private void OnEnterGamePlaying()
        {
            Debug.Log("[GameFlowManager] Enter: GamePlaying");
            StartNextRound();
        }

        private void OnExitGamePlaying()
        {
            ChangePhase(BattlePhase.None);
            Debug.Log("[GameFlowManager] Exit: GamePlaying");
        }

        private void OnEnterGameEnd()
        {
            Debug.Log($"[GameFlowManager] Enter: GameEnd — Result: {Result}, Total Rounds: {CurrentRound}");
        }

        private void OnExitGameEnd()
        {
            Debug.Log("[GameFlowManager] Exit: GameEnd");
        }

        // ── BattlePhase ──────────────────────────────────────────

        private void ChangePhase(BattlePhase newPhase)
        {
            if (CurrentPhase == newPhase) return;

            ExitPhase(CurrentPhase);
            CurrentPhase = newPhase;
            EnterPhase(CurrentPhase);

            OnPhaseChanged?.Invoke(CurrentPhase);
        }

        private void EnterPhase(BattlePhase phase)
        {
            switch (phase)
            {
                case BattlePhase.Shop:         OnEnterShop();         break;
                case BattlePhase.PlayerAction: OnEnterPlayerAction(); break;
                case BattlePhase.EnemyTurn:    OnEnterEnemyTurn();    break;
            }
        }

        private void ExitPhase(BattlePhase phase)
        {
            switch (phase)
            {
                case BattlePhase.Shop:         OnExitShop();         break;
                case BattlePhase.PlayerAction: OnExitPlayerAction(); break;
                case BattlePhase.EnemyTurn:    OnExitEnemyTurn();    break;
            }
        }

        // Shop
        private void OnEnterShop()
        {
            Debug.Log($"[GameFlowManager] Round {CurrentRound} — Shop Start");
        }

        private void OnExitShop()
        {
            Debug.Log($"[GameFlowManager] Round {CurrentRound} — Shop End");
        }

        // PlayerAction
        private void OnEnterPlayerAction()
        {
            ChessManager.Instance.ResetPlayerChessTurnState();
            Debug.Log($"[GameFlowManager] Round {CurrentRound} — PlayerAction Start");
        }

        private void OnExitPlayerAction()
        {
            Debug.Log($"[GameFlowManager] Round {CurrentRound} — PlayerAction End");
        }

        // EnemyTurn
        private void OnEnterEnemyTurn()
        {
            ChessManager.Instance.ResetEnemyChessTurnState();
            Debug.Log($"[GameFlowManager] Round {CurrentRound} — EnemyTurn Start");
        }

        private void OnExitEnemyTurn()
        {
            Debug.Log($"[GameFlowManager] Round {CurrentRound} — EnemyTurn End");
        }

        // ── 回合推进 ─────────────────────────────────────────────

        private void StartNextRound()
        {
            CurrentRound++;
            OnRoundChanged?.Invoke(CurrentRound);
            Debug.Log($"[GameFlowManager] Round {CurrentRound} Begin");
            ChangePhase(BattlePhase.Shop);
        }

        /// <summary> 玩家结束购物，进入行动阶段 </summary>
        public void EndShopping()
        {
            if (CurrentPhase != BattlePhase.Shop) return;
            ChangePhase(BattlePhase.PlayerAction);
        }

        /// <summary> 玩家结束行动回合（手动触发或全部单位行动完毕时调用） </summary>
        public void EndPlayerTurn()
        {
            if (CurrentPhase != BattlePhase.PlayerAction) return;
            ChangePhase(BattlePhase.EnemyTurn);
        }

        /// <summary> 每次玩家单位行动结束后调用，自动检测是否所有单位已完成 </summary>
        public void CheckPlayerTurnComplete()
        {
            if (CurrentPhase != BattlePhase.PlayerAction) return;
            if (ChessManager.Instance.AreAllPlayerChessActed())
                EndPlayerTurn();
        }

        /// <summary> 敌方行动结束后调用（AI完成后调用） </summary>
        public void EndEnemyTurn()
        {
            if (CurrentPhase != BattlePhase.EnemyTurn) return;
            CheckBattleResult();
        }

        // ── 胜负判断 ─────────────────────────────────────────────

        private void CheckBattleResult()
        {
            if (CheckWinCondition())
            {
                Result = GameResult.Win;
                EndGame();
                return;
            }

            if (CheckLoseCondition())
            {
                Result = GameResult.Lose;
                EndGame();
                return;
            }

            StartNextRound();
        }

        private bool CheckWinCondition()
        {
            return ChessManager.Instance.AreAllEnemiesDead();
        }

        private bool CheckLoseCondition()
        {
            // TODO: 待定义输的条件
            return false;
        }
    }
}
