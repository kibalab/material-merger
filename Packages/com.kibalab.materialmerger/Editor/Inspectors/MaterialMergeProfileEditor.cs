#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Models;
using K13A.MaterialMerger.Editor.Services;
using K13A.MaterialMerger.Editor.Services.Localization;
using K13A.MaterialMerger.Editor.Services.Logging;
using K13A.MaterialMerger.Editor.UI.Components;
using GuiUtil = K13A.MaterialMerger.Editor.UI.Utilities.GUIUtility;

namespace K13A.MaterialMerger.Editor.Inspectors
{
    [CustomEditor(typeof(MaterialMergeProfile))]
    public class MaterialMergeProfileEditor : UnityEditor.Editor, IBuildExecutor
    {
        private const string LogoSearchFilter = "logo t:Texture2D";
        private const float LogoHeight = 80f;
        private const float LogoMinWidth = 80f;
        private const float LogoMaxWidth = 300f;

        private MaterialMergeProfile profile;
        private MaterialMergerState state;
        private MaterialMergerStyles styles;

        private ILocalizationService localizationService;
        private ILoggingService loggingService;
        private IMaterialScanService scanService;
        private IProfileService profileService;
        private IMaterialBuildService buildService;
        private IAtlasGenerator atlasGenerator;
        private ITextureProcessor textureProcessor;
        private IMeshRemapper meshRemapper;

        private GlobalSettingsPanelRenderer globalPanel;
        private GroupListRenderer groupList;

        private Texture2D logoTexture;
        private bool logoSearched;
        private static bool? ndmfAvailable;

        private void OnEnable()
        {
            profile = target as MaterialMergeProfile;
            Initialize();
        }

        private void OnDisable()
        {
            textureProcessor?.Cleanup();
        }

        public override void OnInspectorGUI()
        {
            if (target == null) return;
            if (profile != target || state == null) Initialize();
            if (state == null || profile == null) return;

            state.root = profile.gameObject;
            state.profile = profile;

            styles.EnsureStyles();

            EditorGUI.BeginChangeCheck();

            DrawHeader();
            EditorGUILayout.Space(8);
            DrawNdmfSettings();
            EditorGUILayout.Space(8);
            globalPanel.DrawGlobalSettings(state);
            EditorGUILayout.Space(8);
            groupList.DrawGroupList(state.scans, ref state.scroll);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(profile, "Edit Material Merge Profile");
                ApplyStateToProfile();
            }
        }

        public void BuildAndApply()
        {
            if (buildService == null || state == null) return;
            var settings = BuildSettings.FromState(state);
            buildService.BuildAndApply(settings, state.scans);
        }

        private void Initialize()
        {
            if (profile == null) return;

            localizationService = new LocalizationService();
            loggingService = new LoggingService();

            atlasGenerator = new AtlasGenerator();
            textureProcessor = new TextureProcessor();
            meshRemapper = new MeshRemapper();
            scanService = new MaterialScanService { LoggingService = loggingService };
            profileService = new ProfileService();

            var assetBuilder = new AtlasAssetBuilder
            {
                AtlasGenerator = atlasGenerator,
                TextureProcessor = textureProcessor,
                ScanService = scanService,
                LoggingService = loggingService
            };

            var sceneApplier = new SceneApplier
            {
                AtlasGenerator = atlasGenerator,
                TextureProcessor = textureProcessor,
                MeshRemapper = meshRemapper,
                ScanService = scanService,
                LoggingService = loggingService
            };

            buildService = new MaterialBuildService
            {
                AssetBuilder = assetBuilder,
                SceneApplier = sceneApplier,
                AtlasGenerator = atlasGenerator,
                TextureProcessor = textureProcessor,
                MeshRemapper = meshRemapper,
                ScanService = scanService,
                LocalizationService = localizationService,
                LoggingService = loggingService
            };

            styles = new MaterialMergerStyles();

            var rowRenderer = new PropertyRowRenderer { Styles = styles, Localization = localizationService };
            var tableRenderer = new PropertyTableRenderer { Styles = styles, RowRenderer = rowRenderer, Localization = localizationService };
            var groupPanel = new GroupPanelRenderer { Styles = styles, TableRenderer = tableRenderer, Localization = localizationService };
            groupList = new GroupListRenderer { Styles = styles, GroupRenderer = groupPanel, Localization = localizationService };
            globalPanel = new GlobalSettingsPanelRenderer { Styles = styles, Localization = localizationService };

            state = new MaterialMergerState
            {
                root = profile.gameObject,
                profile = profile
            };

            LoadSettingsFromProfile();
            LoadScansFromProfile();
        }

        private void LoadSettingsFromProfile()
        {
            if (!profile || state == null) return;

            state.groupByKeywords = profile.groupByKeywords;
            state.groupByRenderQueue = profile.groupByRenderQueue;
            state.splitOpaqueTransparent = profile.splitOpaqueTransparent;

            state.cloneRootOnApply = profile.cloneRootOnApply;
            state.deactivateOriginalRoot = profile.deactivateOriginalRoot;
            state.keepPrefabOnClone = profile.keepPrefabOnClone;

            state.atlasSize = profile.atlasSize;
            state.grid = profile.grid;
            state.paddingPx = profile.paddingPx;

            state.diffPolicy = (DiffPolicy)profile.diffPolicy;
            state.diffSampleMaterial = profile.diffSampleMaterial;
            if (!string.IsNullOrEmpty(profile.outputFolder))
                state.outputFolder = profile.outputFolder;

            state.ndmfEnabled = profile.ndmfEnabled;
            state.ndmfUseTemporaryOutputFolder = profile.ndmfUseTemporaryOutputFolder;

            state.globalFoldout = profile.globalFoldout;
        }

        private void LoadScansFromProfile()
        {
            state.scans.Clear();
            if (!profile) return;

            state.scans = profileService.LoadScansFromProfile(
                profile,
                state.root,
                state.grid,
                scanService
            );
        }

        private void ApplyStateToProfile()
        {
            if (!profile || state == null) return;

            profile.groupByKeywords = state.groupByKeywords;
            profile.groupByRenderQueue = state.groupByRenderQueue;
            profile.splitOpaqueTransparent = state.splitOpaqueTransparent;

            profile.cloneRootOnApply = state.cloneRootOnApply;
            profile.deactivateOriginalRoot = state.deactivateOriginalRoot;
            profile.keepPrefabOnClone = state.keepPrefabOnClone;

            profile.atlasSize = state.atlasSize;
            profile.grid = state.grid;
            profile.paddingPx = state.paddingPx;

            profile.diffPolicy = (int)state.diffPolicy;
            profile.diffSampleMaterial = state.diffSampleMaterial;
            profile.outputFolder = state.outputFolder;

            profile.ndmfEnabled = state.ndmfEnabled;
            profile.ndmfUseTemporaryOutputFolder = state.ndmfUseTemporaryOutputFolder;

            profile.globalFoldout = state.globalFoldout;

            profileService.SaveScansToProfile(profile, state.scans);
            EditorUtility.SetDirty(profile);
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.VerticalScope(styles.stBox))
            {
                DrawCenteredLogo();
                EditorGUILayout.Space(6);
                DrawCenteredLastScan();
                EditorGUILayout.Space(6);
                DrawOpenWindowButton();
                EditorGUILayout.Space(6);
                DrawOutputFolderField();
                DrawLanguageField();
            }
        }

        private void DrawCenteredLogo()
        {
            var logo = GetLogoTexture();
            if (logo)
            {
                float aspect = logo.height > 0 ? (float)logo.width / logo.height : 1f;
                float width = Mathf.Clamp(LogoHeight * aspect, LogoMinWidth, LogoMaxWidth);
                var rowRect = GUILayoutUtility.GetRect(0f, LogoHeight, GUILayout.ExpandWidth(true));
                float x = rowRect.x + (rowRect.width - width) * 0.5f;
                var rect = new Rect(x, rowRect.y, width, LogoHeight);
                GUI.DrawTexture(rect, logo, ScaleMode.ScaleToFit, true);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(localizationService.Get(L10nKey.WindowTitle), styles.stTitle);
                GUILayout.FlexibleSpace();
            }
        }

        private Texture2D GetLogoTexture()
        {
            if (logoTexture) return logoTexture;
            if (!logoSearched)
            {
                logoSearched = true;
                logoTexture = FindLogoTextureInProject();
            }

            return logoTexture;
        }

        private Texture2D FindLogoTextureInProject()
        {
            var guids = AssetDatabase.FindAssets(LogoSearchFilter);
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path)) continue;
                if (path.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) < 0) continue;
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex) return tex;
            }

            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path)) continue;
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex) return tex;
            }

            return null;
        }

        private void DrawCenteredLastScan()
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            var scanLabelContent = new GUIContent(localizationService.Get(L10nKey.LastScan),
                localizationService.Get(L10nKey.LastScanTooltip));
            var labelText = $"{scanLabelContent.text}: {GuiUtil.GetLastScanLabel(profile, localizationService)}";
            var content = new GUIContent(labelText, scanLabelContent.tooltip);

            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(lineHeight)))
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(content, styles.stMiniDim);
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawOpenWindowButton()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                var content = new GUIContent(
                    localizationService.Get(L10nKey.OpenWindow),
                    localizationService.Get(L10nKey.OpenWindowTooltip));
                if (GUILayout.Button(content, styles.stBigBtn, GUILayout.Width(160), GUILayout.Height(28)))
                {
                    var window = EditorWindow.GetWindow<MaterialMergerWindow>();
                    if (window != null)
                    {
                        window.Show();
                        var rootObj = profile ? profile.gameObject : null;
                        if (rootObj)
                        {
                            EditorApplication.delayCall += () =>
                            {
                                if (window == null || rootObj == null) return;
                                window.SetRootFromInspector(rootObj);
                            };
                        }
                    }
                }
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawOutputFolderField()
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            var outRect = EditorGUILayout.GetControlRect(false, lineHeight);
            var outFieldRect = new Rect(outRect.x, outRect.y,
                outRect.width - MaterialMergerStyles.TopLabelWidth - 6, outRect.height);

            float buttonWidth = 90f;
            var outBtnRect = new Rect(outFieldRect.x, outFieldRect.y, buttonWidth, outFieldRect.height);
            var outPathRect = new Rect(outBtnRect.xMax + 6, outFieldRect.y, outFieldRect.width - buttonWidth - 6,
                outFieldRect.height);

            var outputFolderText = localizationService.Get(L10nKey.OutputFolder);
            if (GUI.Button(outBtnRect, GuiUtil.MakeIconContent(outputFolderText, "Folder Icon",
                    "d_Folder Icon", localizationService.Get(L10nKey.OutputFolderTooltip)), styles.stToolbarBtn))
            {
                var picked = EditorUtility.OpenFolderPanel(localizationService.Get(L10nKey.DialogOutputFolderTitle),
                    Application.dataPath, "");
                if (!string.IsNullOrEmpty(picked))
                {
                    picked = picked.Replace("\\", "/");
                    if (picked.Contains("/Assets/"))
                    {
                        var newFolder = "Assets/" + picked.Split(new[] { "/Assets/" }, StringSplitOptions.None)[1];
                        state.outputFolder = newFolder;
                    }
                    else
                    {
                        EditorUtility.DisplayDialog(localizationService.Get(L10nKey.OutputFolder),
                            localizationService.Get(L10nKey.DialogOutputFolderError), "OK");
                    }
                }
            }

            EditorGUI.LabelField(outPathRect, state.outputFolder, styles.stMiniDim);
        }

        private void DrawLanguageField()
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            var langRect = EditorGUILayout.GetControlRect(false, lineHeight);
            var langLabelRect = new Rect(langRect.x, langRect.y, MaterialMergerStyles.TopLabelWidth, langRect.height);
            var langFieldRect = new Rect(langLabelRect.xMax + 6, langRect.y,
                langRect.width - MaterialMergerStyles.TopLabelWidth - 6, langRect.height);

            var label = new GUIContent(localizationService.Get(L10nKey.LanguageSettings),
                localizationService.Get(L10nKey.LanguageTooltip));
            EditorGUI.LabelField(langLabelRect, label);

            var options = new[]
            {
                new GUIContent(localizationService.Get(L10nKey.LanguageKorean)),
                new GUIContent(localizationService.Get(L10nKey.LanguageEnglish)),
                new GUIContent(localizationService.Get(L10nKey.LanguageJapanese))
            };

            int current = (int)localizationService.CurrentLanguage;
            int next = EditorGUI.Popup(langFieldRect, current, options);
            if (next != current)
            {
                localizationService.CurrentLanguage = (Language)next;
                Repaint();
            }
        }

        private void DrawNdmfSettings()
        {
            bool hasNdmf = HasNdmfPackage();
            if (!hasNdmf)
            {
                state.ndmfEnabled = false;
            }

            using (new EditorGUILayout.VerticalScope(styles.stBox))
            {
                GuiUtil.DrawSection(localizationService.Get(L10nKey.NdmfSettings), styles.stSection);

                using (new EditorGUI.DisabledScope(!hasNdmf))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var enabledContent = new GUIContent(localizationService.Get(L10nKey.NdmfEnabled),
                            localizationService.Get(L10nKey.NdmfEnabledTooltip));
                        state.ndmfEnabled = EditorGUILayout.ToggleLeft(enabledContent, state.ndmfEnabled, GUILayout.Width(200));

                        using (new EditorGUI.DisabledScope(!state.ndmfEnabled))
                        {
                            var tempContent = new GUIContent(localizationService.Get(L10nKey.NdmfUseTempOutput),
                                localizationService.Get(L10nKey.NdmfUseTempOutputTooltip));
                            state.ndmfUseTemporaryOutputFolder = EditorGUILayout.ToggleLeft(tempContent,
                                state.ndmfUseTemporaryOutputFolder, GUILayout.Width(220));
                        }
                    }
                }

                var summaryKey = state.ndmfEnabled ? L10nKey.NdmfSummaryEnabled : L10nKey.NdmfSummaryDisabled;
                EditorGUILayout.HelpBox(localizationService.Get(summaryKey), MessageType.None);
            }
        }

        private static bool HasNdmfPackage()
        {
            if (ndmfAvailable.HasValue)
            {
                return ndmfAvailable.Value;
            }

            ndmfAvailable = Type.GetType("nadena.dev.ndmf.BuildContext, nadena.dev.ndmf") != null;
            return ndmfAvailable.Value;
        }

        private void OnScan()
        {
            if (!state.root) return;

            profile.lastScanTicksUtc = DateTime.UtcNow.Ticks;

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

            profileService.ApplyProfileToScans(profile, state.scans);
            profileService.SaveScansToProfile(profile, state.scans);
            EditorUtility.SetDirty(profile);
            Repaint();
        }

        private void OnBuildWithConfirm()
        {
            buildService.BuildAndApplyWithConfirm(
                this,
                state.root,
                state.scans,
                state.diffPolicy,
                state.diffSampleMaterial,
                state.outputFolder
            );
        }
    }
}
#endif
