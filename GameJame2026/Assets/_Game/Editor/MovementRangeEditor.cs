using GameJamRAC.Grid;
using UnityEditor;
using UnityEngine;

namespace GameJamRAC.Editor
{
    /// <summary>为 MovementRange 提供可点击的能力范围编辑网格。</summary>
    [CustomEditor(typeof(MovementRange))]
    public class MovementRangeEditor : UnityEditor.Editor
    {
        private const int GridRadius = 3;
        private SerializedProperty preset;
        private SerializedProperty offsets;
        private bool showAdvanced;

        private void OnEnable()
        {
            preset = serializedObject.FindProperty("preset");
            offsets = serializedObject.FindProperty("offsets");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(preset, new GUIContent("能力预设"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                MovementRange range = (MovementRange)target;
                if (CurrentPreset != MovementRange.Preset.Custom)
                    range.ApplyPreset();
                EditorUtility.SetDirty(range);
                serializedObject.Update();
                RefreshVisualizer();
            }

            bool isCustom = CurrentPreset == MovementRange.Preset.Custom;
            if (isCustom)
            {
                EditorGUILayout.HelpBox("点击下方格子即可绘制能力范围。中心是角色脚下，最上方是前方（+Y）。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(GetPresetDescription(CurrentPreset), MessageType.Info);
                if (GUILayout.Button("应用当前预设"))
                {
                    Undo.RecordObject(target, "应用能力范围预设");
                    ((MovementRange)target).ApplyPreset();
                    EditorUtility.SetDirty(target);
                    serializedObject.Update();
                    SceneView.RepaintAll();
                    RefreshVisualizer();
                }

                if (GUILayout.Button("转为自定义后手绘（保留当前形状）"))
                {
                    Undo.RecordObject(target, "转为自定义能力范围");
                    preset.enumValueIndex = (int)MovementRange.Preset.Custom;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                    serializedObject.Update();
                }
            }

            DrawAbilityGrid(isCustom);

            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "高级：直接编辑 Offset 列表");
            if (showAdvanced)
            {
                using (new EditorGUI.DisabledScope(!isCustom))
                    EditorGUILayout.PropertyField(offsets, new GUIContent("Offsets"), true);

                if (!isCustom)
                    EditorGUILayout.HelpBox("预设模式会自动覆盖 Offset 列表；先转为自定义才能手动修改。", MessageType.None);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private MovementRange.Preset CurrentPreset
        {
            get { return (MovementRange.Preset)preset.enumValueIndex; }
        }

        private void DrawAbilityGrid(bool isCustom)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("能力格绘制", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("前方  ↑", EditorStyles.centeredGreyMiniLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                for (int y = GridRadius; y >= -GridRadius; y--)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        for (int x = -GridRadius; x <= GridRadius; x++)
                        {
                            Vector2Int offset = new Vector2Int(x, y);
                            bool isOrigin = offset == Vector2Int.zero;
                            bool selected = isOrigin || HasOffset(offset);
                            Color previousColor = GUI.backgroundColor;
                            GUI.backgroundColor = isOrigin
                                ? new Color(1f, 0.78f, 0.2f)
                                : selected ? new Color(1f, 0.34f, 0.34f) : new Color(0.74f, 0.74f, 0.74f);

                            using (new EditorGUI.DisabledScope(isOrigin || !isCustom))
                            {
                                string label = isOrigin ? "角色" : selected ? "■" : string.Empty;
                                if (GUILayout.Button(label, GUILayout.Width(34f), GUILayout.Height(26f)))
                                    ToggleOffset(offset);
                            }

                            GUI.backgroundColor = previousColor;
                        }
                        GUILayout.FlexibleSpace();
                    }
                }
            }

            EditorGUILayout.LabelField("← 左                     右 →", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.HelpBox("红色格为可选目标。运行时，落在道路外的红格会自动隐藏，角色也不能走到那里。", MessageType.None);
        }

        private bool HasOffset(Vector2Int offset)
        {
            for (int i = 0; i < offsets.arraySize; i++)
            {
                if (offsets.GetArrayElementAtIndex(i).vector2IntValue == offset)
                    return true;
            }

            return false;
        }

        private void ToggleOffset(Vector2Int offset)
        {
            Undo.RecordObject(target, "绘制能力范围格");

            for (int i = 0; i < offsets.arraySize; i++)
            {
                if (offsets.GetArrayElementAtIndex(i).vector2IntValue != offset) continue;

                offsets.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                SceneView.RepaintAll();
                RefreshVisualizer();
                return;
            }

            int index = offsets.arraySize;
            offsets.InsertArrayElementAtIndex(index);
            offsets.GetArrayElementAtIndex(index).vector2IntValue = offset;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            SceneView.RepaintAll();
            RefreshVisualizer();
        }

        private void RefreshVisualizer()
        {
            MovementRange range = (MovementRange)target;
            MovementRangeVisualizer visualizer = range.GetComponentInChildren<MovementRangeVisualizer>(true);
            if (visualizer != null)
                visualizer.Refresh();
        }

        private static string GetPresetDescription(MovementRange.Preset value)
        {
            switch (value)
            {
                case MovementRange.Preset.EightNeighbors:
                    return "八方向：角色周围 8 个相邻格均可选择。";
                case MovementRange.Preset.CardinalTwoCells:
                    return "十字两格：前、后、左、右各距离 2 格。";
                case MovementRange.Preset.SideOneForwardTwo:
                    return "左右各一格，前方一格与两格。";
                case MovementRange.Preset.ForwardTriangle:
                    return "前方三角形：前方第一排 3 格、第二排 5 格。B 当前使用此规则。";
                default:
                    return "自定义范围。";
            }
        }
    }
}
