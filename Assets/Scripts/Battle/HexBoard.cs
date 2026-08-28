using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace Battle
{
    /// <summary>
    /// 六边形棋盘主管理类（pointy-top，Axial 坐标）。
    /// 在 Awake 时读取 Tilemap 建立运行时格子字典，并负责格子选中逻辑。
    ///
    /// 场景配置要求：
    ///   - Grid（Cell Layout = Hexagonal，Stagger Axis = Y）
    ///     ├─ BoardTilemap   ← _boardTilemap（绘制三种格子的视觉 Tile）
    ///     └─ HighlightTilemap ← _highlightTilemap（叠在 BoardTilemap 之上）
    /// </summary>
    public class HexBoard : MonoBehaviour
    {
        public static HexBoard Instance { get; private set; }

        [Header("Tilemap 层级")]
        [SerializeField] private Tilemap _boardTilemap;
        [SerializeField] private Tilemap _highlightTilemap;

        [Header("格子视觉 Tile 资产（用于识别格子类型）")]
        [SerializeField] private TileBase _deployableTileAsset;
        [SerializeField] private TileBase _nonDeployableTileAsset;
        [SerializeField] private TileBase _obstacleTileAsset;

        [Header("选中高亮 Tile 资产")]
        [SerializeField] private TileBase _selectedHighlightAsset;

        private readonly Dictionary<HexCoord, HexTile> _tiles = new();
        private HexCoord? _selectedCoord;

        // ── Lifecycle ────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BuildFromTilemap();
        }

        private void Update()
        {
            HandleClickSelection();
        }

        // ── 初始化：从 Tilemap 读取格子 ──────────────────────────

        private void BuildFromTilemap()
        {
            _tiles.Clear();
            _boardTilemap.CompressBounds();

            foreach (var pos in _boardTilemap.cellBounds.allPositionsWithin)
            {
                if (!_boardTilemap.HasTile(pos)) continue;
                var asset  = _boardTilemap.GetTile(pos);
                var coord  = HexCoord.FromVector3Int(pos);
                _tiles[coord] = new HexTile(coord, ResolveTileType(asset));
            }

            Debug.Log($"[HexBoard] 棋盘初始化完成，共 {_tiles.Count} 个格子");
        }

        private TileType ResolveTileType(TileBase asset)
        {
            if (asset == _deployableTileAsset)    return TileType.Deployable;
            if (asset == _obstacleTileAsset)      return TileType.Obstacle;
            return TileType.NonDeployable;
        }

        // ── 格子查询 ─────────────────────────────────────────────

        /// <summary> 获取指定坐标的格子，不存在时返回 null </summary>
        public HexTile GetTile(HexCoord coord) =>
            _tiles.TryGetValue(coord, out var t) ? t : null;

        public bool HasTile(HexCoord coord) => _tiles.ContainsKey(coord);

        public IReadOnlyCollection<HexTile> AllTiles => _tiles.Values;

        /// <summary> 返回棋盘上存在的所有相邻格子（最多 6 个） </summary>
        public HexTile[] GetNeighbors(HexCoord coord)
        {
            var result = new List<HexTile>(6);
            foreach (var nc in coord.GetAllNeighbors())
            {
                var t = GetTile(nc);
                if (t != null) result.Add(t);
            }
            return result.ToArray();
        }

        /// <summary> 返回相邻且可通行的格子 </summary>
        public HexTile[] GetPassableNeighbors(HexCoord coord)
        {
            var result = new List<HexTile>(6);
            foreach (var nc in coord.GetAllNeighbors())
            {
                var t = GetTile(nc);
                if (t != null && t.IsPassable) result.Add(t);
            }
            return result.ToArray();
        }

        /// <summary> 返回所有空闲的可部署格子 </summary>
        public List<HexTile> GetEmptyDeployableTiles()
        {
            var result = new List<HexTile>();
            foreach (var tile in _tiles.Values)
                if (tile.CanDeploy && tile.IsEmpty) result.Add(tile);
            return result;
        }

        // ── 选中 ─────────────────────────────────────────────────

        public HexCoord? SelectedCoord => _selectedCoord;
        public HexTile   SelectedTile  => _selectedCoord.HasValue ? GetTile(_selectedCoord.Value) : null;

        /// <summary> 选中指定格子，并刷新高亮层 </summary>
        public void SelectTile(HexCoord coord)
        {
            if (!_tiles.ContainsKey(coord)) return;
            _highlightTilemap.ClearAllTiles();
            _selectedCoord = coord;
            _highlightTilemap.SetTile(coord.ToVector3Int(), _selectedHighlightAsset);

            var tile = _tiles[coord];
            Debug.Log($"[HexBoard] 选中格子 {coord}  类型={tile.TileType}  CanDeploy={tile.CanDeploy}  IsPassable={tile.IsPassable}  IsEmpty={tile.IsEmpty}");
        }

        /// <summary> 取消所有选中 </summary>
        public void DeselectAll()
        {
            _selectedCoord = null;
            _highlightTilemap.ClearAllTiles();
        }

        // ── 鼠标点击选中 ─────────────────────────────────────────

        private void HandleClickSelection()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var cam = Camera.main;
            if (cam == null) return;

            var screenPos = mouse.position.ReadValue();
            var worldPos  = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
            var cellPos   = _boardTilemap.WorldToCell(worldPos);
            var coord     = HexCoord.FromVector3Int(cellPos);

            if (_tiles.ContainsKey(coord))
                SelectTile(coord);
            else
                DeselectAll();
        }
    }
}
