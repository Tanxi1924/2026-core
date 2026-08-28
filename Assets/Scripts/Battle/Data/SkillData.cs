using UnityEngine;

namespace Battle.Data
{
    [CreateAssetMenu(fileName = "SkillData", menuName = "Game/SkillData")]
    public class SkillData : ScriptableObject
    {
        public string SkillName;
        public int    MpCost;
        // TODO: 技能效果
    }
}
