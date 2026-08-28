namespace Battle
{
    /// <summary>
    /// 棋盘上单个六边形格子的运行时数据。
    /// 由 HexBoard 统一管理，不挂载 MonoBehaviour。
    /// </summary>
    public class HexTile
    {
        public HexCoord Coord    { get; }
        public TileType TileType { get; private set; }

        public Chess OccupyingChess { get; private set; }

        public bool IsEmpty    => OccupyingChess == null;
        public bool CanDeploy  => TileType.CanDeploy();
        public bool IsPassable => TileType.IsPassable();

        public HexTile(HexCoord coord, TileType tileType = TileType.Deployable)
        {
            Coord    = coord;
            TileType = tileType;
        }

        public void PlaceChess(Chess chess) => OccupyingChess = chess;
        public void RemoveChess()           => OccupyingChess = null;

        /// <summary> 运行时动态修改格子类型（例如某些关卡事件） </summary>
        public void SetTileType(TileType type) => TileType = type;
    }
}
