using Battle.Data;
using UnityEngine;

namespace Battle
{
    public class Chess
    {
        public ChessData Data { get; private set; }

        private int _id;

        // ── 等级与配置属性 ────────────────────────────────────────

        public int        Level     { get; private set; }
        public int        MaxHp     { get; private set; }
        public int        CurrentHp { get; private set; }
        public int        Atk       { get; private set; }
        public int        MaxMp     { get; private set; }
        public int        CurrentMp { get; private set; }
        public int        Mov       { get; private set; }
        public int        Spe       { get; private set; }
        public SkillData  Skill     { get; private set; }

        // ── 固有属性 ─────────────────────────────────────────────

        public ChessTeam  Team          { get; private set; }
        public ChessShape Shape         { get; private set; }
        public HexTile    OccupiedTile  { get; private set; }
        public bool       IsAlive       => CurrentHp > 0;

        public void SetOccupiedTile(HexTile tile) => OccupiedTile = tile;

        // ── 行动状态 ─────────────────────────────────────────────

        public bool HasMoved    { get; private set; }
        public bool HasActed    { get; private set; }
        public bool HasFinished { get; private set; }

        // ── 初始化 ───────────────────────────────────────────────

        public void Init(ChessData chessData, int level = 1)
        {
            Data  = chessData;
            _id   = chessData.Id;
            Team  = chessData.Team;
            Shape = chessData.Shape;
            SetLevel(level);
        }

        /// <summary> 设置等级并从配置表重新加载该等级的属性 </summary>
        public void SetLevel(int level)
        {
            Level = Mathf.Clamp(level, 1, 27);
            var s = Data.GetStatForLevel(Level);
            MaxHp     = s.Hp;
            CurrentHp = s.Hp;
            Atk       = s.Atk;
            MaxMp     = s.Mp;
            CurrentMp = 0;
            Mov       = s.Mov;
            Spe       = s.Spe;
            Skill     = s.Skill;
        }

        // ── 行动接口 ─────────────────────────────────────────────

        public void Move()
        {
            if (HasFinished) return;
            HasMoved = true;
            if (HasActed) HasFinished = true;
        }

        public void Act()
        {
            if (HasFinished) return;
            HasActed    = true;
            HasFinished = true;
        }

        public void Wait()
        {
            HasFinished = true;
        }

        public void ResetTurnState()
        {
            HasMoved    = false;
            HasActed    = false;
            HasFinished = false;
        }

        // ── 生命值 / 法力值 ──────────────────────────────────────

        /// <summary> 承受伤害，返回 true 表示此次伤害导致死亡 </summary>
        public bool TakeDamage(int amount)
        {
            if (!IsAlive) return false;
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            return !IsAlive;
        }

        public void GainMp(int amount)
        {
            CurrentMp = Mathf.Min(MaxMp, CurrentMp + amount);
        }

        public bool TryConsumeMp(int amount)
        {
            if (CurrentMp < amount) return false;
            CurrentMp -= amount;
            return true;
        }
    }
}
