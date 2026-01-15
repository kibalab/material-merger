#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Models;
using K13A.MaterialMerger.Editor.Services.Localization;

namespace K13A.MaterialMerger.Editor.UI.Components
{
    /// <summary>
    /// 그룹 목록 (툴바 + 스크롤뷰 + 그룹들)을 렌더링하는 컴포넌트
    /// </summary>
    public class GroupListRenderer
    {
        public MaterialMergerStyles Styles { get; set; }
        public GroupPanelRenderer GroupRenderer { get; set; }
        public ILocalizationService Localization { get; set; }

        private const float MergeIndent = 16f;

        private readonly Dictionary<GroupKey, Rect> headerRects = new Dictionary<GroupKey, Rect>();
        private readonly Dictionary<GroupKey, Rect> handleRects = new Dictionary<GroupKey, Rect>();
        private readonly List<GroupRect> groupRects = new List<GroupRect>();
        private readonly Dictionary<GroupKey, GroupScan> groupByKey = new Dictionary<GroupKey, GroupScan>();
        private readonly Dictionary<GroupScan, GroupScan> mergeRootByGroup = new Dictionary<GroupScan, GroupScan>();
        private readonly Dictionary<GroupScan, int> mergeChildCount = new Dictionary<GroupScan, int>();
        private readonly Dictionary<string, List<GroupScan>> mergeGroups = new Dictionary<string, List<GroupScan>>(System.StringComparer.Ordinal);
        private readonly DragState drag = new DragState();

        private struct GroupRect
        {
            public GroupScan group;
            public Rect rect;
        }

        private class DragState
        {
            public GroupScan group;
            public List<GroupScan> block;
            public bool isDragging;
            public Vector2 mouse;
            public GroupScan mergeTarget;
            public int insertIndex;
            public bool clearMerge;
        }

        /// <summary>
        /// 그룹 목록 렌더링
        /// </summary>
        public void DrawGroupList(List<GroupScan> scans, ref Vector2 scroll)
        {
            if (scans == null || scans.Count == 0)
            {
                using (new EditorGUILayout.VerticalScope(Styles.stBox))
                    EditorGUILayout.HelpBox(Localization.Get(L10nKey.NoScanMessage), MessageType.Info);
                return;
            }

            if (EnsureMergeGroupsContiguous(scans))
                GUI.changed = true;

            scroll = EditorGUILayout.BeginScrollView(scroll);

            var evt = Event.current;
            if (evt != null && evt.type == EventType.Repaint)
            {
                headerRects.Clear();
                handleRects.Clear();
            }

            DrawGroupListToolbar(scans);

            BuildMergeInfo(scans);
            groupRects.Clear();
            groupByKey.Clear();

            for (int gi = 0; gi < scans.Count; gi++)
            {
                var group = scans[gi];
                if (group == null) continue;
                groupByKey[group.key] = group;

                var startRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true));

                bool isChild = mergeRootByGroup.TryGetValue(group, out var root) && root != null && root != group;
                int childCount = mergeChildCount.TryGetValue(group, out var count) ? count : 0;
                float indent = isChild ? MergeIndent : 0f;

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (indent > 0f)
                        GUILayout.Space(indent);

                    GroupRenderer.DrawGroup(group, gi, isChild, childCount, RegisterHeaderRect, RegisterHandleRect);
                }

                var endRect = GUILayoutUtility.GetLastRect();
                float height = Mathf.Max(0f, endRect.yMax - startRect.y);
                var groupRect = new Rect(0f, startRect.y, EditorGUIUtility.currentViewWidth, height);
                groupRects.Add(new GroupRect { group = group, rect = groupRect });
            }

            HandleDragEvents(scans);

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 그룹 목록 툴바 (전체 펼치기/접기/활성/비활성)
        /// </summary>
        private void DrawGroupListToolbar(List<GroupScan> scans)
        {
            using (new EditorGUILayout.HorizontalScope(Styles.stBox))
            {
                GUILayout.Label(Localization.Get(L10nKey.PlanList), Styles.stSubTitle);
                GUILayout.FlexibleSpace();

                var expandContent = new GUIContent(Localization.Get(L10nKey.ExpandAll),
                    Localization.Get(L10nKey.ExpandAllTooltip));
                if (GUILayout.Button(expandContent, Styles.stToolbarBtn, GUILayout.Width(95)))
                    foreach (var g in scans)
                        g.foldout = true;

                var collapseContent = new GUIContent(Localization.Get(L10nKey.CollapseAll),
                    Localization.Get(L10nKey.CollapseAllTooltip));
                if (GUILayout.Button(collapseContent, Styles.stToolbarBtn, GUILayout.Width(95)))
                    foreach (var g in scans)
                        g.foldout = false;

                var enableContent = new GUIContent(Localization.Get(L10nKey.EnableAll),
                    Localization.Get(L10nKey.EnableAllTooltip));
                if (GUILayout.Button(enableContent, Styles.stToolbarBtn, GUILayout.Width(90)))
                    foreach (var g in scans)
                        g.enabled = true;

                var disableContent = new GUIContent(Localization.Get(L10nKey.DisableAll),
                    Localization.Get(L10nKey.DisableAllTooltip));
                if (GUILayout.Button(disableContent, Styles.stToolbarBtn, GUILayout.Width(90)))
                    foreach (var g in scans)
                        g.enabled = false;
            }
        }

        private string GenerateMergeKey(List<GroupScan> scans)
        {
            var used = new HashSet<string>(
                scans.Where(x => !string.IsNullOrEmpty(x.mergeKey)).Select(x => x.mergeKey),
                System.StringComparer.Ordinal);

            int index = 1;
            string key;
            do
            {
                key = $"merge_{index:00}";
                index++;
            } while (used.Contains(key));

            return key;
        }

        private void RegisterHeaderRect(GroupScan group, Rect rect)
        {
            if (group == null) return;
            headerRects[group.key] = rect;
        }

        private void RegisterHandleRect(GroupScan group, Rect rect)
        {
            if (group == null) return;
            handleRects[group.key] = rect;
        }

        private void BuildMergeInfo(List<GroupScan> scans)
        {
            mergeRootByGroup.Clear();
            mergeChildCount.Clear();
            mergeGroups.Clear();

            foreach (var g in scans)
            {
                if (g == null) continue;
                if (string.IsNullOrEmpty(g.mergeKey)) continue;
                if (!mergeGroups.TryGetValue(g.mergeKey, out var list))
                {
                    list = new List<GroupScan>();
                    mergeGroups[g.mergeKey] = list;
                }
                list.Add(g);
            }

            var rootByKey = new Dictionary<string, GroupScan>(System.StringComparer.Ordinal);
            foreach (var g in scans)
            {
                if (g == null) continue;
                if (string.IsNullOrEmpty(g.mergeKey))
                {
                    mergeRootByGroup[g] = null;
                    continue;
                }

                if (!rootByKey.TryGetValue(g.mergeKey, out var root))
                {
                    root = g;
                    rootByKey[g.mergeKey] = root;
                    mergeChildCount[root] = 0;
                }

                mergeRootByGroup[g] = root;
                if (root != g)
                    mergeChildCount[root] = mergeChildCount[root] + 1;
            }
        }

        private void HandleDragEvents(List<GroupScan> scans)
        {
            var evt = Event.current;
            if (evt == null) return;

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                foreach (var kv in handleRects)
                {
                    if (!kv.Value.Contains(evt.mousePosition)) continue;
                    if (!groupByKey.TryGetValue(kv.Key, out var group) || group == null) break;
                    StartDrag(scans, group);
                    evt.Use();
                    break;
                }
            }
            else if (evt.type == EventType.MouseDrag && drag.group != null)
            {
                drag.isDragging = true;
                drag.mouse = evt.mousePosition;
                drag.mergeTarget = FindMergeTarget(evt.mousePosition);
                drag.insertIndex = FindInsertIndex(evt.mousePosition.y);
                drag.clearMerge = !string.IsNullOrEmpty(drag.group.mergeKey) && evt.mousePosition.x <= 4f;
                if (drag.mergeTarget != null && IsSameMergeGroup(drag.group, drag.mergeTarget))
                    drag.mergeTarget = null;
                if (drag.mergeTarget == null)
                {
                    var excludeKey = drag.clearMerge ? "" : GetBlockMergeKey(drag.block);
                    drag.insertIndex = AdjustInsertIndexForMergeGroups(scans, drag.insertIndex, drag.mouse.y, excludeKey);
                }
                ConstrainChildInsertIndex(scans);
                evt.Use();
            }
            else if (evt.type == EventType.MouseUp && drag.group != null)
            {
                if (drag.isDragging)
                {
                    ApplyDrag(scans);
                    GUI.changed = true;
                }
                ClearDrag();
                evt.Use();
            }

            if (drag.isDragging && evt.type == EventType.Repaint)
                DrawDragIndicators();
        }

        private void StartDrag(List<GroupScan> scans, GroupScan group)
        {
            drag.group = group;
            drag.isDragging = false;
            drag.mergeTarget = null;
            drag.insertIndex = -1;
            drag.clearMerge = false;

            if (!string.IsNullOrEmpty(group.mergeKey) &&
                mergeRootByGroup.TryGetValue(group, out var root) &&
                root == group &&
                mergeGroups.TryGetValue(group.mergeKey, out var list))
            {
                drag.block = new List<GroupScan>(list);
            }
            else
            {
                drag.block = new List<GroupScan> { group };
            }
        }

        private void ClearDrag()
        {
            drag.group = null;
            drag.block = null;
            drag.isDragging = false;
            drag.mergeTarget = null;
            drag.insertIndex = -1;
            drag.clearMerge = false;
        }

        private GroupScan FindMergeTarget(Vector2 mouse)
        {
            foreach (var kv in headerRects)
            {
                if (!kv.Value.Contains(mouse)) continue;
                if (!groupByKey.TryGetValue(kv.Key, out var group) || group == null) continue;
                if (drag.block != null && drag.block.Contains(group)) continue;

                if (mergeRootByGroup.TryGetValue(group, out var root) && root != null)
                    return root;
                return group;
            }

            return null;
        }

        private int FindInsertIndex(float mouseY)
        {
            for (int i = 0; i < groupRects.Count; i++)
            {
                var rect = groupRects[i].rect;
                if (mouseY < rect.center.y)
                    return i;
            }
            return groupRects.Count;
        }

        private void ApplyDrag(List<GroupScan> scans)
        {
            if (drag.group == null || drag.block == null || drag.block.Count == 0) return;

            if (drag.mergeTarget != null)
            {
                MergeIntoTarget(scans, drag.mergeTarget);
                return;
            }

            int insertIndex = Mathf.Clamp(drag.insertIndex, 0, scans.Count);
            bool shouldClearMerge = drag.clearMerge;
            if (drag.block.Count == 1 && !string.IsNullOrEmpty(drag.group.mergeKey))
            {
                if (TryGetMergeBounds(scans, drag.group.mergeKey, out var minIndex, out var maxIndex))
                {
                    if (IsMergeChild(drag.group))
                    {
                        if (insertIndex > maxIndex + 1)
                            shouldClearMerge = true;
                        else
                            insertIndex = Mathf.Clamp(insertIndex, minIndex + 1, maxIndex + 1);
                    }
                    else if (!shouldClearMerge && IsInsertOutsideMergeGroup(scans, drag.group.mergeKey, insertIndex))
                    {
                        shouldClearMerge = true;
                    }
                }
            }

            var blockMergeKey = GetBlockMergeKey(drag.block);
            var excludeMergeKey = (!shouldClearMerge && !string.IsNullOrEmpty(blockMergeKey)) ? blockMergeKey : "";
            insertIndex = AdjustInsertIndexForMergeGroups(scans, insertIndex, drag.mouse.y, excludeMergeKey);

            if (shouldClearMerge)
            {
                foreach (var g in drag.block)
                    g.mergeKey = "";
            }

            MoveBlock(scans, drag.block, insertIndex);
            EnsureMergeGroupsContiguous(scans);
        }

        private void MergeIntoTarget(List<GroupScan> scans, GroupScan target)
        {
            if (target == null) return;

            if (string.IsNullOrEmpty(target.mergeKey))
                target.mergeKey = GenerateMergeKey(scans);

            foreach (var g in drag.block)
                g.mergeKey = target.mergeKey;

            int targetIndex = scans.IndexOf(target);
            int insertIndex = targetIndex + 1;
            while (insertIndex < scans.Count && scans[insertIndex].mergeKey == target.mergeKey)
                insertIndex++;

            MoveBlock(scans, drag.block, insertIndex);
            EnsureMergeGroupsContiguous(scans);
        }

        private void MoveBlock(List<GroupScan> scans, List<GroupScan> block, int insertIndex)
        {
            if (block == null || block.Count == 0) return;

            int minIndex = scans.Count;
            foreach (var g in block)
            {
                int idx = scans.IndexOf(g);
                if (idx >= 0 && idx < minIndex) minIndex = idx;
            }

            foreach (var g in block)
                scans.Remove(g);

            if (insertIndex > minIndex)
                insertIndex -= block.Count;

            insertIndex = Mathf.Clamp(insertIndex, 0, scans.Count);
            scans.InsertRange(insertIndex, block);
        }

        private bool IsSameMergeGroup(GroupScan a, GroupScan b)
        {
            if (a == null || b == null) return false;
            if (string.IsNullOrEmpty(a.mergeKey) || string.IsNullOrEmpty(b.mergeKey)) return false;
            return string.Equals(a.mergeKey, b.mergeKey, System.StringComparison.Ordinal);
        }

        private string GetBlockMergeKey(List<GroupScan> block)
        {
            if (block == null || block.Count == 0) return "";

            string key = null;
            foreach (var g in block)
            {
                if (g == null) continue;
                if (string.IsNullOrEmpty(g.mergeKey)) return "";
                if (key == null)
                    key = g.mergeKey;
                else if (!string.Equals(key, g.mergeKey, System.StringComparison.Ordinal))
                    return "";
            }

            return key ?? "";
        }

        private int AdjustInsertIndexForMergeGroups(List<GroupScan> scans, int insertIndex, float mouseY, string excludeMergeKey)
        {
            if (scans == null || scans.Count == 0 || mergeGroups.Count == 0)
                return Mathf.Clamp(insertIndex, 0, scans.Count);

            int clamped = Mathf.Clamp(insertIndex, 0, scans.Count);
            int safety = 0;
            bool adjusted;

            do
            {
                adjusted = false;

                foreach (var kv in mergeGroups)
                {
                    var key = kv.Key;
                    if (string.IsNullOrEmpty(key)) continue;
                    if (!string.IsNullOrEmpty(excludeMergeKey) &&
                        string.Equals(key, excludeMergeKey, System.StringComparison.Ordinal))
                        continue;

                    if (!TryGetMergeBounds(scans, key, out var minIndex, out var maxIndex)) continue;
                    if (clamped <= minIndex || clamped > maxIndex) continue;

                    var centerY = GetMergeBlockCenterY(key);
                    if (!float.IsNaN(centerY) && mouseY < centerY)
                        clamped = minIndex;
                    else
                        clamped = maxIndex + 1;

                    clamped = Mathf.Clamp(clamped, 0, scans.Count);
                    adjusted = true;
                    break;
                }

                safety++;
            } while (adjusted && safety < 5);

            return clamped;
        }

        private float GetMergeBlockCenterY(string mergeKey)
        {
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            bool found = false;

            for (int i = 0; i < groupRects.Count; i++)
            {
                var g = groupRects[i].group;
                if (g == null || g.mergeKey != mergeKey) continue;
                var rect = groupRects[i].rect;
                if (rect.yMin < minY) minY = rect.yMin;
                if (rect.yMax > maxY) maxY = rect.yMax;
                found = true;
            }

            if (!found) return float.NaN;
            return (minY + maxY) * 0.5f;
        }

        private bool TryGetMergeBounds(List<GroupScan> scans, string mergeKey, out int minIndex, out int maxIndex)
        {
            minIndex = int.MaxValue;
            maxIndex = -1;
            if (string.IsNullOrEmpty(mergeKey)) return false;

            for (int i = 0; i < scans.Count; i++)
            {
                var g = scans[i];
                if (g == null || g.mergeKey != mergeKey) continue;
                if (i < minIndex) minIndex = i;
                if (i > maxIndex) maxIndex = i;
            }

            return maxIndex >= 0;
        }

        private bool IsMergeChild(GroupScan group)
        {
            if (group == null) return false;
            return mergeRootByGroup.TryGetValue(group, out var root) && root != null && root != group;
        }

        private void ConstrainChildInsertIndex(List<GroupScan> scans)
        {
            if (drag.group == null || drag.block == null || drag.block.Count != 1) return;
            if (string.IsNullOrEmpty(drag.group.mergeKey)) return;
            if (drag.clearMerge) return;
            if (!IsMergeChild(drag.group)) return;
            if (!TryGetMergeBounds(scans, drag.group.mergeKey, out var minIndex, out _)) return;

            if (drag.insertIndex <= minIndex)
                drag.insertIndex = minIndex + 1;
        }

        private bool EnsureMergeGroupsContiguous(List<GroupScan> scans)
        {
            if (scans == null || scans.Count <= 1) return false;

            var grouped = new Dictionary<string, List<GroupScan>>(System.StringComparer.Ordinal);
            foreach (var g in scans)
            {
                if (g == null || string.IsNullOrEmpty(g.mergeKey)) continue;
                if (!grouped.TryGetValue(g.mergeKey, out var list))
                {
                    list = new List<GroupScan>();
                    grouped[g.mergeKey] = list;
                }
                list.Add(g);
            }

            if (grouped.Count == 0) return false;

            var seen = new HashSet<string>(System.StringComparer.Ordinal);
            var result = new List<GroupScan>(scans.Count);

            foreach (var g in scans)
            {
                if (g == null)
                {
                    result.Add(null);
                    continue;
                }

                if (string.IsNullOrEmpty(g.mergeKey))
                {
                    result.Add(g);
                    continue;
                }

                if (seen.Add(g.mergeKey) && grouped.TryGetValue(g.mergeKey, out var list))
                    result.AddRange(list);
            }

            if (result.Count != scans.Count) return false;

            bool changed = false;
            for (int i = 0; i < scans.Count; i++)
            {
                if (!ReferenceEquals(scans[i], result[i]))
                {
                    changed = true;
                    break;
                }
            }

            if (!changed) return false;

            scans.Clear();
            scans.AddRange(result);
            return true;
        }

        private bool IsInsertOutsideMergeGroup(List<GroupScan> scans, string mergeKey, int insertIndex)
        {
            if (string.IsNullOrEmpty(mergeKey)) return false;

            int min = int.MaxValue;
            int max = -1;
            for (int i = 0; i < scans.Count; i++)
            {
                var g = scans[i];
                if (g == null) continue;
                if (g.mergeKey != mergeKey) continue;
                if (i < min) min = i;
                if (i > max) max = i;
            }

            if (max < 0) return false;
            return insertIndex < min || insertIndex > max + 1;
        }

        private void DrawDragIndicators()
        {
            if (drag.mergeTarget != null && headerRects.TryGetValue(drag.mergeTarget.key, out var rect))
            {
                var color = EditorGUIUtility.isProSkin
                    ? new Color(0.3f, 0.6f, 0.9f, 0.2f)
                    : new Color(0.2f, 0.5f, 0.8f, 0.2f);
                EditorGUI.DrawRect(rect, color);
                return;
            }

            int index = Mathf.Clamp(drag.insertIndex, 0, groupRects.Count);
            float y;
            if (groupRects.Count == 0)
            {
                y = 0f;
            }
            else if (index >= groupRects.Count)
            {
                y = groupRects[groupRects.Count - 1].rect.yMax + 2f;
            }
            else
            {
                y = groupRects[index].rect.y;
            }

            var line = new Rect(0f, y, EditorGUIUtility.currentViewWidth, 2f);
            var colorLine = EditorGUIUtility.isProSkin
                ? new Color(0.3f, 0.6f, 0.9f, 0.6f)
                : new Color(0.2f, 0.5f, 0.8f, 0.6f);
            EditorGUI.DrawRect(line, colorLine);
        }
    }
}
#endif
