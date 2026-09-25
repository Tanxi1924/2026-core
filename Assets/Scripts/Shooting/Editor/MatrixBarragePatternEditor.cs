#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;


    [CustomEditor(typeof(MatrixBarrageWeapon))]
    public class MatrixBarrageWeaponEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox("一次 WeaponUse 发射完整矩阵。节奏使用 Time Between Uses；子弹和伤害来自 Object Pooler。方向角使用世界坐标，90 度向上。", MessageType.Info);
            DrawPropertiesExcluding(serializedObject, "m_Script", "ProjectilesPerShot", "Spread",
                "RandomSpread", "RotateWeaponOnSpread", "ProjectileSpawnTransform", "ProjectileSpawnOffset", "SpawnPosition");
            if (serializedObject.ApplyModifiedProperties()) SceneView.RepaintAll();
        }
    }

    [CustomEditor(typeof(MatrixBarragePattern))]
    public class MatrixBarragePatternEditor : UnityEditor.Editor
    {
        private Vector2 _scroll;
        public override void OnInspectorGUI()
        {
            var pattern = (MatrixBarragePattern)target;
            serializedObject.Update();
            EditorGUILayout.HelpBox("点击格子设置子弹。顶行对应 +Y，图案以整个网格中心为原点。", MessageType.Info);
            EditorGUI.BeginChangeCheck();
            int rows = EditorGUILayout.IntSlider("行数 Rows", pattern.Rows, 1, 25);
            int columns = EditorGUILayout.IntSlider("列数 Columns", pattern.Columns, 1, 25);
            if (EditorGUI.EndChangeCheck())
                Edit(pattern, "Resize matrix", () => pattern.Resize(rows, columns));
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("HorizontalSpacing"), new GUIContent("横向间距"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("VerticalSpacing"), new GUIContent("纵向间距"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ShapeAngle"), new GUIContent("图案旋转（度）"));
            serializedObject.ApplyModifiedProperties();

            using (new EditorGUILayout.HorizontalScope())
            {
                PresetButton(pattern, "全选", MatrixBarragePattern.Preset.Full);
                PresetButton(pattern, "清空", MatrixBarragePattern.Preset.Empty);
                PresetButton(pattern, "实心菱形", MatrixBarragePattern.Preset.Diamond);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                PresetButton(pattern, "空心菱形", MatrixBarragePattern.Preset.HollowDiamond);
                PresetButton(pattern, "十字", MatrixBarragePattern.Preset.Cross);
                PresetButton(pattern, "边框", MatrixBarragePattern.Preset.Frame);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("左右镜像")) Edit(pattern, "Mirror matrix", () => pattern.Mirror(true));
                if (GUILayout.Button("上下镜像")) Edit(pattern, "Mirror matrix", () => pattern.Mirror(false));
            }
            if (pattern.Rows % 2 == 0 || pattern.Columns % 2 == 0)
                EditorGUILayout.HelpBox("对称尖顶菱形推荐奇数行列；偶数尺寸按网格中心对称采样。", MessageType.Info);
            int count = 0;
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(600));
            for (int r = 0; r < pattern.Rows; r++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int c = 0; c < pattern.Columns; c++)
                    {
                        bool on = pattern.IsOn(r, c);
                        if (on) count++;
                        Color old = GUI.backgroundColor;
                        GUI.backgroundColor = on ? new Color(0.2f, 0.85f, 1f) : Color.gray;
                        if (GUILayout.Button(on ? "●" : "·", GUILayout.Width(24), GUILayout.Height(24)))
                        {
                            int row = r, column = c;
                            Edit(pattern, "Toggle matrix cell", () => pattern.SetCell(row, column, !on));
                        }
                        GUI.backgroundColor = old;
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.LabelField("当前子弹数量", count.ToString());
            if (count == 0) EditorGUILayout.HelpBox("空矩阵不会生成子弹。", MessageType.Warning);
        }

        private void PresetButton(MatrixBarragePattern pattern, string label, MatrixBarragePattern.Preset preset)
        {
            if (GUILayout.Button(label)) Edit(pattern, "Fill matrix", () => pattern.Fill(preset));
        }
        private void Edit(MatrixBarragePattern pattern, string label, Action action)
        {
            Undo.RecordObject(pattern, label);
            action();
            EditorUtility.SetDirty(pattern);
            serializedObject.Update();
            SceneView.RepaintAll();
            Repaint();
        }

        [MenuItem("Assets/Create/TwinBody/Barrage/5x5 Diamond Example")]
        private static void CreateDiamond()
        {
            var pattern = CreateInstance<MatrixBarragePattern>();
            pattern.Fill(MatrixBarragePattern.Preset.Diamond);
            ProjectWindowUtil.CreateAsset(pattern, "Diamond5x5.asset");
        }
    }

#endif
