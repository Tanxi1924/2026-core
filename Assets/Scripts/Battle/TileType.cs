namespace Battle
{
    public enum TileType
    {
        Deployable,    // 可部署格：可从手牌部署棋子，移动可通行
        NonDeployable, // 不可部署格：禁止部署，移动可通行
        Obstacle,      // 障碍格：禁止部署，移动不可通行
    }

    public static class TileTypeExtensions
    {
        /// <summary> 是否允许从手牌区向此格部署棋子 </summary>
        public static bool CanDeploy(this TileType t) => t == TileType.Deployable;

        /// <summary> 移动路径经过此格时是否允许通行 </summary>
        public static bool IsPassable(this TileType t) => t != TileType.Obstacle;
    }
}
