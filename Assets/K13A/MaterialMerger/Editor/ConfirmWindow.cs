#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Services.Localization;

namespace K13A.MaterialMerger.Editor
{
    /// <summary>
    /// Confirmation window shown before build execution
    /// </summary>
    public class ConfirmWindow : EditorWindow
    {
        [Serializable]
        public class GroupInfo
        {
            public string title;
            public bool willRun;
            public string skipReason;
            public List<string> atlasProps = new List<string>();
            public List<string> generatedProps = new List<string>();
        }

        private IBuildExecutor buildExecutor;
        private List<GroupInfo> groups;
        private Vector2 scroll;
        private ILocalizationService localization;

        /// <summary>
        /// Open the confirmation window
        /// </summary>
        /// <param name="executor">Build executor (must implement IBuildExecutor)</param>
        /// <param name="groups">Group information to display</param>
        /// <param name="localization">Localization service</param>
        public static void Open(IBuildExecutor executor, List<GroupInfo> groups, ILocalizationService localization)
        {
            if (executor == null)
            {
                Debug.LogError("ConfirmWindow.Open: executor cannot be null");
                return;
            }
            
            var w = CreateInstance<ConfirmWindow>();
            w.buildExecutor = executor;
            w.groups = groups ?? new List<GroupInfo>();
            w.localization = localization ?? new LocalizationService();
            w.titleContent = new GUIContent(w.localization.Get(L10nKey.ConfirmTitle));
            w.minSize = new Vector2(680, 520);
            w.ShowUtility();
        }

        void OnGUI()
        {
            if (buildExecutor == null)
            {
                EditorGUILayout.HelpBox(localization.Get(L10nKey.RollbackOwnerClosed), MessageType.Error);
                if (GUILayout.Button(localization.Get(L10nKey.Close), GUILayout.Height(28))) Close();
                return;
            }

            var runCount = groups.Count(x => x.willRun);
            var skipCount = groups.Count - runCount;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(localization.Get(L10nKey.ConfirmHeader), EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(localization.Get(L10nKey.ConfirmMessage, runCount, skipCount), MessageType.Info);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            foreach (var g in groups)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(g.title, EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                        var st = g.willRun ? localization.Get(L10nKey.Run) : localization.Get(L10nKey.Skipped);
                        var c = GUI.color;
                        GUI.color = g.willRun ? new Color(0.7f, 1f, 0.7f) : new Color(1f, 0.85f, 0.55f);
                        GUILayout.Label(st, EditorStyles.miniButton, GUILayout.Width(64));
                        GUI.color = c;
                    }

                    if (!g.willRun && !string.IsNullOrEmpty(g.skipReason))
                        EditorGUILayout.HelpBox(g.skipReason, MessageType.Warning);

                    EditorGUILayout.Space(2);

                    EditorGUILayout.LabelField(localization.Get(L10nKey.AtlasIncludedTexEnv, g.atlasProps.Count), EditorStyles.miniBoldLabel);
                    if (g.atlasProps.Count == 0) EditorGUILayout.LabelField(localization.Get(L10nKey.None), EditorStyles.miniLabel);
                    else EditorGUILayout.LabelField(string.Join(", ", g.atlasProps), EditorStyles.wordWrappedMiniLabel);

                    EditorGUILayout.Space(4);

                    EditorGUILayout.LabelField(localization.Get(L10nKey.GeneratedTexEnv, g.generatedProps.Count), EditorStyles.miniBoldLabel);
                    if (g.generatedProps.Count == 0) EditorGUILayout.LabelField(localization.Get(L10nKey.None), EditorStyles.miniLabel);
                    else EditorGUILayout.LabelField(string.Join(", ", g.generatedProps), EditorStyles.wordWrappedMiniLabel);
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(localization.Get(L10nKey.Cancel), GUILayout.Height(32)))
                {
                    Close();
                    return;
                }

                using (new EditorGUI.DisabledScope(runCount == 0))
                {
                    if (GUILayout.Button(localization.Get(L10nKey.Execute), GUILayout.Height(32)))
                    {
                        Close();
                        buildExecutor.BuildAndApply();
                    }
                }
            }
        }
    }
}
#endif
