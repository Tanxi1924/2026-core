using System;
using UnityEngine;

namespace Battle.Data
{
    public enum ChessTeam  { Player, Enemy }
    public enum ChessShape { Single }   // 更多形状待扩展

    [Serializable]
    public struct ChessLevelStat
    {
        public int       Hp;
        public int       Atk;
        public int       Mp;
        public int       Mov;
        public int       Spe;
        public SkillData Skill;
    }

    [CreateAssetMenu(fileName = "ChessData", menuName = "Game/ChessData")]
    public class ChessData : ScriptableObject
    {
        public int        Id;
        [Range(1, 10)]
        public int        Cost;
        public ChessTeam  Team;
        public ChessShape Shape;

        [Tooltip("索引 0 对应等级 1，共 27 级")]
        public ChessLevelStat[] LevelStats = new ChessLevelStat[27];

        public ChessLevelStat GetStatForLevel(int level)
        {
            int index = Mathf.Clamp(level - 1, 0, LevelStats.Length - 1);
            return LevelStats[index];
        }
    }
}
