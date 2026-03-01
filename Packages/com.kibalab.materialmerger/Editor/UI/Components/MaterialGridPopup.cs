#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Models;
using K13A.MaterialMerger.Editor.Services.Localization;

namespace K13A.MaterialMerger.Editor.UI.Components
{
    /// <summary>
    /// Popup window that shows materials in a grid.
    /// </summary>
    public class MaterialGridPopup : EditorWindow
    {
        private const float TileSize = 64f;
        private const float TilePadding = 10f;
        private const float LabelHeight = 16f;

        private readonly List<Material> materials = new List<Material>();
        private ILocalizationService localization;
        private Vector2 scroll;
        private GUIStyle labelStyle;

        public static void Show(GroupScan group, ILocalizationService localization, Rect anchorRect)
        {
            if (group == null) return;

            var window = CreateInstance<MaterialGridPopup>();
            window.localization = localization ?? new LocalizationService();

            window.materials.Clear();
            window.materials.AddRange(group.mats
                .Select(x => x.mat)
                .Where(x => x != null)
                .Distinct());
            window.materials.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

            var shaderName = Utilities.GUIUtility.GetGroupShaderName(group);
            window.titleContent = new GUIContent(window.localization.Get(L10nKey.PlanMaterialsTitle, shaderName));

            var size = new Vector2(420f, 300f);
            window.minSize = size;
            var screenPos = GUIUtility.GUIToScreenPoint(new Vector2(anchorRect.x, anchorRect.y));
            var screenRect = new Rect(screenPos, anchorRect.size);
            window.ShowAsDropDown(screenRect, size);
            window.Focus();
        }

        private void OnEnable()
        {
            if (localization == null)
                localization = new LocalizationService();
        }

        private void OnLostFocus()
        {
            Close();
        }

        private void OnGUI()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.UpperCenter,
                    wordWrap = true
                };
            }

            var header = $"{localization.Get(L10nKey.PlanMaterials)} ({materials.Count})";
            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);

            if (materials.Count == 0)
            {
                EditorGUILayout.HelpBox(localization.Get(L10nKey.PlanMaterialsEmpty), MessageType.Info);
                return;
            }

            float tileHeight = TileSize + LabelHeight + 4f;
            float viewWidth = Mathf.Max(1f, position.width - 16f);
            int columns = Mathf.Max(1, Mathf.FloorToInt((viewWidth - TilePadding) / (TileSize + TilePadding)));
            int rows = Mathf.CeilToInt(materials.Count / (float)columns);

            float gridHeight = rows * (tileHeight + TilePadding) + TilePadding;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            var gridRect = GUILayoutUtility.GetRect(viewWidth, gridHeight);

            bool needsRepaint = false;
            for (int i = 0; i < materials.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;

                float x = gridRect.x + TilePadding + col * (TileSize + TilePadding);
                float y = gridRect.y + TilePadding + row * (tileHeight + TilePadding);

                var tileRect = new Rect(x, y, TileSize, tileHeight);
                var iconRect = new Rect(x, y, TileSize, TileSize);
                var labelRect = new Rect(x - 6f, y + TileSize + 2f, TileSize + 12f, LabelHeight);

                var mat = materials[i];
                bool selected = Selection.activeObject == mat;
                if (selected)
                {
                    EditorGUI.DrawRect(tileRect, new Color(0.24f, 0.49f, 0.9f, 0.18f));
                }

                if (GUI.Button(tileRect, GUIContent.none, GUIStyle.none))
                {
                    Selection.activeObject = mat;
                    EditorGUIUtility.PingObject(mat);
                }

                var preview = AssetPreview.GetAssetPreview(mat);
                if (!preview)
                {
                    preview = AssetPreview.GetMiniThumbnail(mat);
                    if (AssetPreview.IsLoadingAssetPreview(mat.GetInstanceID()))
                        needsRepaint = true;
                }

                if (preview)
                    GUI.DrawTexture(iconRect, preview, ScaleMode.ScaleToFit);
                else
                    EditorGUI.DrawRect(iconRect, new Color(0f, 0f, 0f, 0.1f));

                GUI.Label(labelRect, mat.name, labelStyle);
            }

            EditorGUILayout.EndScrollView();

            if (needsRepaint)
                Repaint();
        }
    }
}
#endif
