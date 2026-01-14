#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor
{
    public partial class MaterialMerger : EditorWindow
    {

        GameObject root;
        MaterialMergeProfile profile;

        bool groupByKeywords = true;
        bool groupByRenderQueue = true;
        bool splitOpaqueTransparent = true;

        bool cloneRootOnApply = true;
        bool deactivateOriginalRoot = true;

        int atlasSize = 8192;
        int grid = 4;
        int paddingPx = 16;

        DiffPolicy diffPolicy = DiffPolicy.미해결이면중단;
        string outputFolder = "Assets/_Generated/MultiAtlas";

        bool globalFoldout = true;

        List<GroupScan> scans = new List<GroupScan>();
        Vector2 scroll;

        Material blitMat;
        const string BlitShaderPath = "Assets/_Generated/MultiAtlas/Hidden_KibaAtlasBlit.shader";

        readonly Dictionary<int, Material> defaultMatCache = new Dictionary<int, Material>();

        bool suppressAutosaveOnce;
        bool saveQueued;

        [NonSerialized] GUIStyle stTitle;
        [NonSerialized] GUIStyle stSubTitle;
        [NonSerialized] GUIStyle stPill;
        [NonSerialized] GUIStyle stPillWarn;
        [NonSerialized] GUIStyle stMini;
        [NonSerialized] GUIStyle stMiniDim;
        [NonSerialized] GUIStyle stMiniWarn;
        [NonSerialized] GUIStyle stToolbar;
        [NonSerialized] GUIStyle stToolbarBtn;
        [NonSerialized] GUIStyle stRowMoreBtn;
        [NonSerialized] GUIStyle stBox;
        [NonSerialized] GUIStyle stBigBtn;
        [NonSerialized] GUIStyle stSection;

        [NonSerialized] bool stylesReady;
        [NonSerialized] bool lastProSkin;

        [MenuItem("Kiba/렌더링/멀티 아틀라스 머저")]
        static void Open() => GetWindow<MaterialMerger>("멀티 아틀라스 머저");

        [MenuItem("Kiba/렌더링/멀티 아틀라스 롤백...")]
        static void RollbackMenu()
        {
            var path = EditorUtility.OpenFilePanel("멀티 아틀라스 로그 선택", Application.dataPath, "asset");
            if (string.IsNullOrEmpty(path)) return;
            var p = path.Replace("\\", "/");
            if (!p.Contains("/Assets/")) return;
            var rel = "Assets/" + p.Split(new[] { "/Assets/" }, StringSplitOptions.None)[1];
            var log = AssetDatabase.LoadAssetAtPath<KibaMultiAtlasMergerLog>(rel);
            if (!log) return;

            Undo.IncrementCurrentGroup();
            int ug = Undo.GetCurrentGroup();

            foreach (var e in log.entries)
            {
                if (string.IsNullOrEmpty(e.rendererGlobalId)) continue;
                if (!GlobalObjectId.TryParse(e.rendererGlobalId, out var gid)) continue;
                var r = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid) as Renderer;
                if (!r) continue;

                Undo.RecordObject(r, "롤백 머티리얼");
                r.sharedMaterials = e.beforeMaterials;

                if (r is SkinnedMeshRenderer smr && e.beforeMesh)
                {
                    Undo.RecordObject(smr, "롤백 메쉬");
                    smr.sharedMesh = e.beforeMesh;
                }
                else
                {
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf && e.beforeMesh)
                    {
                        Undo.RecordObject(mf, "롤백 메쉬");
                        mf.sharedMesh = e.beforeMesh;
                    }
                }
            }

            Undo.CollapseUndoOperations(ug);
            EditorUtility.DisplayDialog("롤백", "로그 기반 롤백 완료", "OK");
        }

        void OnEnable()
        {
            stylesReady = false;
            lastProSkin = EditorGUIUtility.isProSkin;
            wantsMouseMove = true;
            profile = root ? EnsureProfile(root, false) : null;
            LoadSettingsFromProfile();
            LoadScansFromProfile();
        }

        void OnDisable()
        {
            if (blitMat) DestroyImmediate(blitMat);
            foreach (var kv in defaultMatCache)
                if (kv.Value)
                    DestroyImmediate(kv.Value);
            defaultMatCache.Clear();
        }

        void OnGUI()
        {
            EnsureStyles();

            EditorGUI.BeginChangeCheck();

            DrawTop();
            EditorGUILayout.Space(8);
            DrawGlobal();
            EditorGUILayout.Space(8);
            DrawGroups();

            var changed = EditorGUI.EndChangeCheck();
            if (changed && !suppressAutosaveOnce)
                RequestSave();
            suppressAutosaveOnce = false;
        }
    }
}
#endif
