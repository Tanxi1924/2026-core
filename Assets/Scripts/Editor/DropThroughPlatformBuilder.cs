#if UNITY_EDITOR
using TwinBody;
using UnityEditor;
using UnityEngine;

public static class DropThroughPlatformBuilder
{
    [MenuItem("Tools/Environment/Create Drop Through Platform")]
    public static void Create()
    {
        int layer = LayerMask.NameToLayer("Platforms");
        if (layer < 0) { Debug.LogError("Missing Platforms layer."); return; }
        var root = new GameObject("DropThroughPlatform");
        Undo.RegisterCreatedObjectUndo(root, "Create Drop Through Platform");
        root.layer = layer;
        var trigger = root.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(5f, 1.2f);
        trigger.offset = new Vector2(0f, 0.55f);
        var platform = root.AddComponent<DropThroughPlatform>();

        var surface = new GameObject("Surface");
        surface.transform.SetParent(root.transform, false);
        surface.layer = layer;
        platform.Surface = surface.AddComponent<BoxCollider2D>();
        platform.Surface.size = new Vector2(5f, 0.25f);
        var visual = new GameObject("Visual");
        visual.transform.SetParent(surface.transform, false);
        visual.transform.localScale = new Vector3(5f, 0.25f, 1f);
        var renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("Sprites/Square.psd");
        renderer.color = new Color(0.2f, 0.8f, 0.65f);
        Selection.activeGameObject = root;
    }
}
#endif
