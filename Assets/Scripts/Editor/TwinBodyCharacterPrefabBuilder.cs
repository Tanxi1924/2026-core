#if UNITY_EDITOR
using MoreMountains.CorgiEngine;
using TwinBody;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 菜单：Tools → Character → Build TwinBody Character Prefab
/// 一键生成原型角色：两个球形 Collider（珍妮佛 / 约翰）+ 一个刚体四边形连接件，
/// 三者共用同一个 Rigidbody2D（复合碰撞体），保存为 Assets/Prefabs/Character/TwinBodyCharacter.prefab。
///
/// 根节点额外挂了 Character + 永久禁用的 CorgiController，只为满足
/// LevelManager.PlayerPrefabs（类型是 Character[]）的拖拽要求，两者都不参与实际物理。
/// </summary>
public static class TwinBodyCharacterPrefabBuilder
{
    private const string PrefabFolder = "Assets/Prefabs/Character";
    private const string PrefabPath = PrefabFolder + "/TwinBodyCharacter.prefab";

    [MenuItem("Tools/Character/Build TwinBody Character Prefab")]
    public static void Build()
    {
        EnsureDir("Assets/Prefabs");
        EnsureDir(PrefabFolder);

        var circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("Sprites/Circle.psd");
        var squareSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("Sprites/Square.psd");
        if (circleSprite == null || squareSprite == null)
            Debug.LogWarning("[TwinBodyCharacterPrefabBuilder] 未找到内置 Circle/Square Sprite，节点可能不显示贴图，可在 Inspector 中手动指定。");

        var root = new GameObject("TwinBodyCharacter");
        root.layer = LayerMask.NameToLayer("Player");
        root.tag = "Player";

        root.AddComponent<Rigidbody2D>().freezeRotation = false; // 不主动锁定水平姿态，允许自由旋转

        // CorgiController 只用来满足 Character 的硬依赖，永久禁用，不参与移动/碰撞。
        // 它的 [RequireComponent(BoxCollider2D)] 会顺带在 root 上自动加一个 BoxCollider2D，
        // 同样禁用掉，避免它成为一块多余的、不受控制的碰撞体。
        var corgiController = root.AddComponent<CorgiController>();
        corgiController.enabled = false;
        var strayBoxCollider = root.GetComponent<BoxCollider2D>();
        if (strayBoxCollider != null) strayBoxCollider.enabled = false;

        var character = root.AddComponent<Character>();
        character.CharacterType = Character.CharacterTypes.Player;
        character.PlayerID = "Player1";

        var jennifer = CreateNode(root.transform, "Jennifer", circleSprite, new Color(1f, 0.35f, 0.6f));
        var john = CreateNode(root.transform, "John", circleSprite, new Color(0.3f, 0.55f, 1f));

        var connector = new GameObject("ConnectorBody");
        connector.layer = root.layer;
        connector.transform.SetParent(root.transform, false);

        var connSr = connector.AddComponent<SpriteRenderer>();
        connSr.sprite = squareSprite;
        connSr.color = new Color(0.75f, 0.75f, 0.75f);
        connSr.sortingOrder = 0;

        var connCol = connector.AddComponent<BoxCollider2D>();
        connCol.size = Vector2.one; // 实际世界尺寸 = size * localScale

        // 所有可调参数（重力、质量、节点半径/间距、连接件厚度…）都在 TwinBodyCharacter 上，
        // 这里只负责接线，具体数值和摆放交给 OnValidate/ApplyLayout 统一处理，避免两处重复定义。
        var twinBody = root.AddComponent<TwinBodyCharacter>();
        twinBody.JenniferNode = jennifer.transform;
        twinBody.JohnNode = john.transform;
        twinBody.ConnectorBody = connector.transform;
        twinBody.SyncPhysics();
        twinBody.ApplyLayout();

        // 两个头的功能：约翰的枪（左键）+ 珍妮佛的舌头（右键），都通过 GetComponent<TwinBodyCharacter>()
        // 找到刚体节点，不需要额外接线。
        root.AddComponent<TwinBodyGun>();
        root.AddComponent<TwinBodyTongue>();
        root.AddComponent<TwinBodyDropThrough>();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Selection.activeObject = savedPrefab;
        Debug.Log($"[TwinBodyCharacterPrefabBuilder] 已生成 {PrefabPath}");
    }

    private static GameObject CreateNode(Transform parent, string name, Sprite sprite, Color color)
    {
        var go = new GameObject(name);
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 1;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f; // 局部半径固定为 0.5，实际世界半径由 TwinBodyCharacter.ApplyLayout 通过 localScale 控制

        return go;
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
}
#endif

