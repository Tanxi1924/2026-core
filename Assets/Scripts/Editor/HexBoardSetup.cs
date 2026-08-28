#if UNITY_EDITOR
using Battle;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 菜单：Tools → HexBoard → Setup Test Board
/// 一键创建完整测试棋盘（Grid + Tilemap + 自动生成六边形 Sprite + HexBoard 组件）
/// </summary>
public static class HexBoardSetup
{
    private const float CellX = 0.866025f;  // √3/2，正六边形列间距
    private const float CellY = 1.0f;

    [MenuItem("Tools/HexBoard/Setup Test Board")]
    public static void SetupTestBoard()
    {
        EnsureDir("Assets/Art");
        EnsureDir("Assets/Art/Tiles");
        EnsureDir("Assets/Art/Shaders");

        // ── 1. 占位 Sprite（形状完全由 Shader 画）──────────────────
        var dummy = GetOrCreateDummySprite();

        // ── 2. 材质（棋盘层 + 高亮层各一个）──────────────────────
        var boardMat = GetOrCreateHexMaterial(
            "Assets/Art/Tiles/Mat_HexBoard.mat",
            new Color(0.08f, 0.08f, 0.08f, 1f), 0.05f);
        var hlMat = GetOrCreateHexMaterial(
            "Assets/Art/Tiles/Mat_HexHighlight.mat",
            new Color(1f, 1f, 0.2f, 1f), 0.08f);

        // ── 3. 创建四种 Tile 资产（颜色 = 顶点色，传入 Shader）────
        var deployable    = MakeTile("Assets/Art/Tiles/Tile_Deployable.asset",    dummy, new Color(0.25f, 0.80f, 0.25f));
        var nonDeployable = MakeTile("Assets/Art/Tiles/Tile_NonDeployable.asset", dummy, new Color(0.60f, 0.60f, 0.60f));
        var obstacle      = MakeTile("Assets/Art/Tiles/Tile_Obstacle.asset",      dummy, new Color(0.80f, 0.25f, 0.25f));
        var highlight     = MakeTile("Assets/Art/Tiles/Tile_Highlight.asset",     dummy, new Color(1.00f, 0.90f, 0.15f, 0.7f));

        // ── 4. Grid ────────────────────────────────────────────────
        var gridGO = new GameObject("HexBoard_Grid");
        var grid   = gridGO.AddComponent<Grid>();
        grid.cellLayout  = GridLayout.CellLayout.Hexagon;
        grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
        grid.cellSize    = new Vector3(CellX, CellY, 0f);
        grid.cellGap     = Vector3.zero;

        // ── 5. BoardTilemap ────────────────────────────────────────
        var boardGO      = new GameObject("BoardTilemap");
        boardGO.transform.SetParent(gridGO.transform, false);
        var boardTilemap = boardGO.AddComponent<Tilemap>();
        var boardRend    = boardGO.AddComponent<TilemapRenderer>();
        boardRend.sortingOrder = 0;
        if (boardMat != null) boardRend.material = boardMat;

        // ── 6. HighlightTilemap ────────────────────────────────────
        var hlGO      = new GameObject("HighlightTilemap");
        hlGO.transform.SetParent(gridGO.transform, false);
        var hlTilemap = hlGO.AddComponent<Tilemap>();
        var hlRend    = hlGO.AddComponent<TilemapRenderer>();
        hlRend.sortingOrder = 1;
        if (hlMat != null) hlRend.material = hlMat;

        // ── 7. 256×256 程序化地图 ─────────────────────────────────
        var noiseTex = GenerateNoiseTex();
        PaintProceduralMap(boardTilemap, noiseTex, deployable, nonDeployable, obstacle);

        // ── 8. HexBoard 组件 + 字段赋值 ───────────────────────────
        var hexBoard = gridGO.AddComponent<HexBoard>();
        var so = new SerializedObject(hexBoard);
        so.FindProperty("_boardTilemap").objectReferenceValue           = boardTilemap;
        so.FindProperty("_highlightTilemap").objectReferenceValue       = hlTilemap;
        so.FindProperty("_deployableTileAsset").objectReferenceValue    = deployable;
        so.FindProperty("_nonDeployableTileAsset").objectReferenceValue = nonDeployable;
        so.FindProperty("_obstacleTileAsset").objectReferenceValue      = obstacle;
        so.FindProperty("_selectedHighlightAsset").objectReferenceValue = highlight;
        so.ApplyModifiedProperties();

        // ── 9. 摄像机：正交俯视，视野覆盖整张地图 ───────────────────
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 0f, -200f);
            cam.orthographic       = true;
            cam.orthographicSize   = 130f;   // 约覆盖 256 行 × CellY×0.75 ≈ 192 单位高
        }

        Undo.RegisterCreatedObjectUndo(gridGO, "Setup HexBoard");
        Selection.activeGameObject = gridGO;

        Debug.Log("[HexBoardSetup] 256×256 地图生成完成。噪声贴图已保存至 Assets/Art/Tiles/NoiseMap.png。");
        EditorUtility.DisplayDialog(
            "HexBoard Setup 完成",
            "256×256 程序化地图已创建！\n\n" +
            "• 绿色 = Deployable\n" +
            "• 灰色 = NonDeployable\n" +
            "• 红色 = Obstacle\n\n" +
            "噪声阈值：noise<0.65→部署 / 0.65-0.82→不可部署 / >0.82→障碍\n" +
            "噪声贴图：Assets/Art/Tiles/NoiseMap.png",
            "OK");
    }

    // ────────────────────────────────────────────────────────────
    // 占位 Sprite（2×2 白色，形状由 Shader 决定）
    // ────────────────────────────────────────────────────────────

    private static Sprite GetOrCreateDummySprite()
    {
        const string path = "Assets/Art/Tiles/HexDummy.png";
        const int    size = 64;

        // 每次都重写文件并强制重新导入，确保尺寸和导入设置始终正确
        var tex   = new Texture2D(size, size);
        var white = new Color[size * size];
        for (int i = 0; i < white.Length; i++) white[i] = Color.white;
        tex.SetPixels(white);
        tex.Apply();

        var fullPath = System.IO.Path.Combine(Application.dataPath, "Art", "Tiles", "HexDummy.png");
        System.IO.File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType          = TextureImporterType.Sprite;
        imp.spriteImportMode     = SpriteImportMode.Single;   // 明确单 Sprite 模式
        imp.spritePixelsPerUnit  = size;
        imp.mipmapEnabled        = false;
        imp.alphaIsTransparency  = true;
        imp.SaveAndReimport();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogError($"[HexBoardSetup] HexDummy Sprite 加载失败，请检查 {path} 的导入设置。");
        return sprite;
    }

    // ────────────────────────────────────────────────────────────
    // 程序化六边形材质
    // ────────────────────────────────────────────────────────────

    private static Material GetOrCreateHexMaterial(string assetPath, Color borderColor, float borderWidth)
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(assetPath) is { } m) return m;

        var shader = Shader.Find("Custom/HexTile");
        if (shader == null)
        {
            Debug.LogError("[HexBoardSetup] 找不到 Custom/HexTile Shader，请确认 Assets/Art/Shaders/HexTile.shader 已导入。");
            return null;
        }
        var mat = new Material(shader);
        mat.SetColor("_BorderColor", borderColor);
        mat.SetFloat("_BorderWidth", borderWidth);
        mat.SetFloat("_Feather",     0.012f);
        AssetDatabase.CreateAsset(mat, assetPath);
        return mat;
    }

    // ────────────────────────────────────────────────────────────
    // Tile / Dir helpers
    // ────────────────────────────────────────────────────────────

    private static Tile MakeTile(string path, Sprite sprite, Color color)
    {
        var t = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (t == null)
        {
            t = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(t, path);
        }
        t.sprite = sprite;
        t.color  = color;
        EditorUtility.SetDirty(t);
        AssetDatabase.SaveAssets();
        return t;
    }

    private static void EnsureDir(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            var folder = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    // ────────────────────────────────────────────────────────────
    // 256×256 程序化地图
    // ────────────────────────────────────────────────────────────

    private const int   MapSize       = 256;
    private const float ThresholdND   = 0.65f;   // ≥ 此值 → NonDeployable
    private const float ThresholdObs  = 0.82f;   // ≥ 此值 → Obstacle
    private const float NoiseScale    = 5f;       // 值越小，色块越大

    /// <summary>
    /// 生成并保存一张 256×256 Perlin 噪声贴图（每次随机偏移，结果不同）。
    /// </summary>
    private static Texture2D GenerateNoiseTex()
    {
        const string path = "Assets/Art/Tiles/NoiseMap.png";
        float ox = Random.Range(0f, 9999f);
        float oy = Random.Range(0f, 9999f);

        var tex    = new Texture2D(MapSize, MapSize, TextureFormat.RGBA32, false);
        var pixels = new Color32[MapSize * MapSize];

        for (int row = 0; row < MapSize; row++)
        for (int col = 0; col < MapSize; col++)
        {
            float n = Mathf.PerlinNoise(ox + col / (float)MapSize * NoiseScale,
                                        oy + row / (float)MapSize * NoiseScale);
            byte  b = (byte)(Mathf.Clamp01(n) * 255);
            pixels[row * MapSize + col] = new Color32(b, b, b, 255);
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        var absPath = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(Application.dataPath, "..", path));
        System.IO.File.WriteAllBytes(absPath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        // 导入为普通纹理（不需要 Sprite，CPU 端 GetPixels32 读取用）
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType    = TextureImporterType.Default;
        imp.isReadable     = true;
        imp.mipmapEnabled  = false;
        imp.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    /// <summary>
    /// 按噪声灰度批量绘制 256×256 地图。
    /// 格子布局：offset 坐标 (col, row)，col/row ∈ [-128, 127]，地图中心 ≈ 世界原点。
    /// </summary>
    private static void PaintProceduralMap(
        Tilemap map, Texture2D noise,
        Tile deployable, Tile nonDeployable, Tile obstacle)
    {
        int total     = MapSize * MapSize;
        var positions = new Vector3Int[total];
        var tiles     = new TileBase[total];
        var rawPixels = noise.GetPixels32();   // 左下角起

        int idx = 0;
        for (int row = 0; row < MapSize; row++)
        for (int col = 0; col < MapSize; col++)
        {
            // 偏移坐标居中，使地图中心对齐世界原点
            int  offsetCol = col - MapSize / 2;
            int  offsetRow = row - MapSize / 2;
            positions[idx] = new Vector3Int(offsetCol, offsetRow, 0);

            float n = rawPixels[row * MapSize + col].r / 255f;
            tiles[idx] = n >= ThresholdObs ? (TileBase)obstacle
                       : n >= ThresholdND  ? nonDeployable
                       :                     deployable;
            idx++;
        }

        map.SetTiles(positions, tiles);
        map.RefreshAllTiles();
        Debug.Log($"[HexBoardSetup] 已绘制 {total:N0} 个格子（使用 SetTiles 批量写入）");
    }
}
#endif
