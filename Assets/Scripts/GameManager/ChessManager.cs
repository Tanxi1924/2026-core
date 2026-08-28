using System.Collections.Generic;
using System.Linq;
using Battle;
using Battle.Data;

namespace GameManager
{
    using UnityEngine;

    public class ChessManager : MonoBehaviour
    {
        public static ChessManager Instance { get; private set; }

        public List<Chess> ChessInBattle;
        public List<Chess> ChessInRepository;
        public List<Chess> GraveYard;

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
            ChessInBattle     = new List<Chess>();
            ChessInRepository = new List<Chess>();
            GraveYard         = new List<Chess>();
        }

        // ── 回合状态重置 ─────────────────────────────────────────

        public void ResetPlayerChessTurnState()
        {
            foreach (var chess in ChessInBattle)
                if (chess.Team == ChessTeam.Player) chess.ResetTurnState();
        }

        public void ResetEnemyChessTurnState()
        {
            foreach (var chess in ChessInBattle)
                if (chess.Team == ChessTeam.Enemy) chess.ResetTurnState();
        }

        // ── 胜负判断 ─────────────────────────────────────────────

        /// <summary> 所有存活的玩家单位是否都已完成行动 </summary>
        public bool AreAllPlayerChessActed()
        {
            return ChessInBattle
                .Where(c => c.Team == ChessTeam.Player && c.IsAlive)
                .All(c => c.HasFinished);
        }

        /// <summary> 战场上是否已没有存活的敌方单位 </summary>
        public bool AreAllEnemiesDead()
        {
            return !ChessInBattle.Any(c => c.Team == ChessTeam.Enemy && c.IsAlive);
        }

        // ── 棋子放置 ─────────────────────────────────────────────

        /// <summary> 将棋子从手牌移到战场，并占据指定格子 </summary>
        public void MoveChessToBoard(Chess chess, Battle.HexTile tile)
        {
            ChessInRepository.Remove(chess);
            if (!ChessInBattle.Contains(chess))
                ChessInBattle.Add(chess);
            tile.PlaceChess(chess);
            chess.SetOccupiedTile(tile);
            Debug.Log($"[ChessManager] Chess {chess.Data.Id} placed on {tile.Coord}");
        }

        // ── 选中状态 ─────────────────────────────────────────────

        public Chess SelectedChess { get; private set; }

        public void SelectChess(Chess chess)   => SelectedChess = chess;
        public void DeselectChess()            => SelectedChess = null;

        // ── 场上出售 ─────────────────────────────────────────────

        /// <summary> 出售场上棋子，清空所占格子，返回出售费用值 </summary>
        public int SellChessFromBoard(Chess chess)
        {
            if (!ChessInBattle.Contains(chess)) return 0;

            chess.OccupiedTile?.RemoveChess();
            ChessInBattle.Remove(chess);
            if (SelectedChess == chess) DeselectChess();

            int value = chess.Data.Cost;
            Debug.Log($"[ChessManager] Sold Chess {chess.Data.Id} from board, value: {value}G");
            // TODO: 将 value 加入玩家金币
            return value;
        }

        // ── 死亡处理 ─────────────────────────────────────────────

        /// <summary>
        /// 战斗系统在棋子死亡后调用。
        /// 友方单位进入坟场，敌方单位直接移出战场。
        /// </summary>
        public void HandleChessDied(Chess chess)
        {
            ChessInBattle.Remove(chess);

            if (chess.Team == ChessTeam.Player)
            {
                GraveYard.Add(chess);
                Debug.Log($"[ChessManager] Chess {chess.Data.Id} moved to GraveYard (total: {GraveYard.Count})");
            }
            else
            {
                Debug.Log($"[ChessManager] Enemy {chess.Data.Id} eliminated");
            }
        }
    }
}
