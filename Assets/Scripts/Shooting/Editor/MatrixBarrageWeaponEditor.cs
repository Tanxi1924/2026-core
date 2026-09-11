#if UNITY_EDITOR
using System.Collections.Generic;
using MoreMountains.CorgiEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

    [CustomEditor(typeof(MatrixBarrageWeapon))]
    [CanEditMultipleObjects]
    public class MatrixBarrageWeaponEditor : WeaponEditor
    {
        private static readonly string[] FirstGroups =
        {
            "Matrix · Direction / 方向",
            "Matrix · Shape & Origin / 图案与位置",
            "Matrix · Motion / 运动",
            "Matrix · Preview / 预览"
        };
        private static readonly HashSet<string> Hidden = new HashSet<string>
        {
            "ProjectilesPerShot", "Spread", "RandomSpread", "RotateWeaponOnSpread",
            "ProjectileSpawnTransform", "ProjectileSpawnOffset", "SpawnPosition",
            "WeaponOnMissFeedback", "ApplyRecoilOnHitDamageable", "ApplyRecoilOnHitNonDamageable",
            "ApplyRecoilOnHitNothing", "ApplyRecoilOnKill"
        };

        public override VisualElement CreateInspectorGUI()
        {
            Initialization();
            var root = new VisualElement();
            // Custom editors do not inherit the base editor script's default asset references.
            if (EditorStyleSheet == null)
            {
                EditorStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/CorgiEngine/ThirdParty/MoreMountains/MMTools/Core/Editor/MMAttributes/MMMonoBehaviourUITKEditorStylesheet.uss");
                if (EditorStyleSheet == null)
                    foreach (string guid in AssetDatabase.FindAssets("MMMonoBehaviourUITKEditorStylesheet t:StyleSheet"))
                    {
                        EditorStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(guid));
                        if (EditorStyleSheet != null) break;
                    }
            }
            if (EditorStyleSheet != null) root.styleSheets.Add(EditorStyleSheet);
            var script = new PropertyField(serializedObject.FindProperty("m_Script"));
            script.SetEnabled(false);
            root.Add(script);
            root.Add(new HelpBox("方向可调：0°右 / 45°右上 / 90°上 / 180°左 / 270°下。整组平行发射请选 Parallel。图案旋转在 Pattern 资源中设置。", HelpBoxMessageType.Info));

            foreach (var pair in GroupData)
                pair.Value.PropertiesList.RemoveAll(p => Hidden.Contains(p.name));

            var rendered = new HashSet<string>();
            foreach (string name in FirstGroups)
                if (GroupData.TryGetValue(name, out var group) && group.PropertiesList.Count > 0)
                {
                    DrawGroup(group, root);
                    rendered.Add(name);
                }
            // Keep all remaining parent groups in their original order and native style.
            foreach (var pair in GroupData)
                if (!rendered.Contains(pair.Key) && pair.Value.PropertiesList.Count > 0)
                    DrawGroup(pair.Value, root);
            // Preserve ungrouped properties if a Corgi version adds any.
            foreach (var property in PropertiesList)
                if (property.name != "m_Script" && !Hidden.Contains(property.name))
                    root.Add(new PropertyField(property));
            root.RegisterCallback<SerializedPropertyChangeEvent>(_ => SceneView.RepaintAll());
            return root;
        }
    }

#endif
