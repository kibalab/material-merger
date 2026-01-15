#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Models;
using K13A.MaterialMerger.Editor.Services;
using K13A.MaterialMerger.Editor.Services.Localization;
using K13A.MaterialMerger.Editor.UI.Components;

namespace K13A.MaterialMerger.Editor.Core
{
    /// <summary>
    /// Material Merger EditorWindow - SOLID 원칙을 준수하는 오케스트레이터
    /// </summary>
    public class MaterialMergerWindow : EditorWindow
    {
        // State 및 Styles
        private MaterialMergerState state;
        private MaterialMergerStyles styles;

        // Services
        private IMaterialScanService scanService;
        private IMaterialBuildService buildService;
        private IProfileService profileService;
        private ITextureProcessor textureProcessor;
        private IAtlasGenerator atlasGenerator;
        private IMeshRemapper meshRemapper;
        private ILocalizationService localizationService;

        // UI Renderers
        private TopPanelRenderer topPanel;
        private GlobalSettingsPanelRenderer globalPanel;
        private GroupListRenderer groupList;

        [MenuItem("Kiba/렌더링/멀티 아틀라스 머저")]
        static void Open()
        {
            GetWindow<MaterialMergerWindow>();
            // Title will be set in OnEnable after localization service is initialized
        }

        void OnEnable()
        {
            InitializeState();
            InitializeServices();
            InitializeUI();

            // Set window title with localization
            titleContent = new GUIContent(localizationService.Get(L10nKey.WindowTitle));

            wantsMouseMove = true;
            state.profile = state.root ? profileService.EnsureProfile(state.root, false) : null;
            LoadSettingsFromProfile();
            LoadScansFromProfile();
        }

        void OnDisable()
        {
            if (state.blitMat) DestroyImmediate(state.blitMat);
            foreach (var kv in state.defaultMatCache)
                if (kv.Value) DestroyImmediate(kv.Value);
            state.defaultMatCache.Clear();
        }

        void OnGUI()
        {
            styles.EnsureStyles();

            EditorGUI.BeginChangeCheck();

            topPanel.DrawTopPanel(state, OnScan, OnBuildWithConfirm, OnRootChanged, OnOutputFolderChanged, OnLanguageChanged);
            EditorGUILayout.Space(8);
            globalPanel.DrawGlobalSettings(state);
            EditorGUILayout.Space(8);
            groupList.DrawGroupList(state.scans, ref state.scroll);

            var changed = EditorGUI.EndChangeCheck();
            if (changed && !state.suppressAutosaveOnce)
                RequestSave();
            state.suppressAutosaveOnce = false;
        }

        #region Initialization

        private void InitializeState()
        {
            state = new MaterialMergerState();
        }

        private void InitializeServices()
        {
            // Localization service (must be first as other services may use it)
            localizationService = new LocalizationService();

            // 기본 서비스 생성
            atlasGenerator = new AtlasGenerator();
            textureProcessor = new TextureProcessor();
            meshRemapper = new MeshRemapper();
            scanService = new MaterialScanService();
            profileService = new ProfileService();

            // BuildService에 의존성 주입
            buildService = new MaterialBuildService
            {
                AtlasGenerator = atlasGenerator,
                TextureProcessor = textureProcessor,
                MeshRemapper = meshRemapper,
                ScanService = scanService,
                LocalizationService = localizationService
            };
        }

        private void InitializeUI()
        {
            styles = new MaterialMergerStyles();

            // UI 렌더러 생성 및 의존성 주입 (하향식)
            var rowRenderer = new PropertyRowRenderer { Styles = styles, Localization = localizationService };
            var tableRenderer = new PropertyTableRenderer { Styles = styles, RowRenderer = rowRenderer, Localization = localizationService };
            var groupPanel = new GroupPanelRenderer { Styles = styles, TableRenderer = tableRenderer, Localization = localizationService };
            groupList = new GroupListRenderer { Styles = styles, GroupRenderer = groupPanel, Localization = localizationService };
            globalPanel = new GlobalSettingsPanelRenderer { Styles = styles, Localization = localizationService };
            topPanel = new TopPanelRenderer { Styles = styles, Localization = localizationService };
        }

        #endregion

        #region Action Handlers

        private void OnScan()
        {
            if (state.root)
                state.profile = profileService.EnsureProfile(state.root, true);

            if (state.profile)
                state.profile.lastScanTicksUtc = DateTime.UtcNow.Ticks;

            state.scans = scanService.ScanGameObject(
                state.root,
                state.groupByKeywords,
                state.groupByRenderQueue,
                state.splitOpaqueTransparent,
                state.grid
            );

            state.scans = state.scans
                .OrderBy(x => x.key.shader ? x.key.shader.name : "")
                .ThenBy(x => x.tag)
                .ToList();

            profileService.ApplyProfileToScans(state.profile, state.scans);
            RequestSave();
            Repaint();
        }

        private void OnBuildWithConfirm()
        {
            buildService.BuildAndApplyWithConfirm(
                this,
                state.root,
                state.scans,
                state.diffPolicy,
                state.outputFolder
            );
        }

        public void BuildAndApply()
        {
            buildService.BuildAndApply(
                state.root,
                state.scans,
                state.diffPolicy,
                state.cloneRootOnApply,
                state.deactivateOriginalRoot,
                state.outputFolder,
                state.atlasSize,
                state.grid,
                state.paddingPx,
                state.groupByKeywords,
                state.groupByRenderQueue,
                state.splitOpaqueTransparent
            );
        }

        private void OnRootChanged(GameObject newRoot)
        {
            SaveNow();

            state.root = newRoot;
            state.scans.Clear();
            state.profile = null;

            if (state.root)
                state.profile = profileService.EnsureProfile(state.root, false);

            LoadSettingsFromProfile();
            LoadScansFromProfile();
            state.suppressAutosaveOnce = true;

            Repaint();
        }

        private void OnOutputFolderChanged(string newFolder)
        {
            state.outputFolder = newFolder;
        }

        private void OnLanguageChanged(Language language)
        {
            if (localizationService.CurrentLanguage == language) return;
            localizationService.CurrentLanguage = language;
            titleContent = new GUIContent(localizationService.Get(L10nKey.WindowTitle));
            Repaint();
        }

        #endregion

        #region Profile Management

        private void LoadSettingsFromProfile()
        {
            if (!state.profile) return;

            state.groupByKeywords = state.profile.groupByKeywords;
            state.groupByRenderQueue = state.profile.groupByRenderQueue;
            state.splitOpaqueTransparent = state.profile.splitOpaqueTransparent;

            state.cloneRootOnApply = state.profile.cloneRootOnApply;
            state.deactivateOriginalRoot = state.profile.deactivateOriginalRoot;

            state.atlasSize = state.profile.atlasSize;
            state.grid = state.profile.grid;
            state.paddingPx = state.profile.paddingPx;

            state.diffPolicy = (DiffPolicy)state.profile.diffPolicy;
            if (!string.IsNullOrEmpty(state.profile.outputFolder))
                state.outputFolder = state.profile.outputFolder;

            state.globalFoldout = state.profile.globalFoldout;
        }

        private void LoadScansFromProfile()
        {
            state.scans.Clear();
            if (!state.profile) return;

            state.scans = profileService.LoadScansFromProfile(
                state.profile,
                state.root,
                state.grid,
                scanService
            );
        }

        private void RequestSave()
        {
            if (!state.root || !state.profile) return;
            if (state.saveQueued) return;

            state.saveQueued = true;
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                state.saveQueued = false;
                SaveNow();
            };
        }

        private void SaveNow()
        {
            if (!state.root || !state.profile) return;

            state.profile.groupByKeywords = state.groupByKeywords;
            state.profile.groupByRenderQueue = state.groupByRenderQueue;
            state.profile.splitOpaqueTransparent = state.splitOpaqueTransparent;

            state.profile.cloneRootOnApply = state.cloneRootOnApply;
            state.profile.deactivateOriginalRoot = state.deactivateOriginalRoot;

            state.profile.atlasSize = state.atlasSize;
            state.profile.grid = state.grid;
            state.profile.paddingPx = state.paddingPx;

            state.profile.diffPolicy = (int)state.diffPolicy;
            state.profile.outputFolder = state.outputFolder;

            state.profile.globalFoldout = state.globalFoldout;

            profileService.SaveScansToProfile(state.profile, state.scans);
        }

        #endregion
    }
}
#endif
