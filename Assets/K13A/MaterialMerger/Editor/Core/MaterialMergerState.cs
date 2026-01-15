#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor.Core
{
    /// <summary>
    /// MaterialMerger 윈도우의 모든 상태를 관리하는 클래스
    /// </summary>
    [Serializable]
    public class MaterialMergerState
    {
        // 루트 및 프로필
        public GameObject root;
        public MaterialMergeProfile profile;

        // 그룹핑 설정
        public bool groupByKeywords = true;
        public bool groupByRenderQueue = true;
        public bool splitOpaqueTransparent = true;

        // 적용 설정
        public bool cloneRootOnApply = true;
        public bool deactivateOriginalRoot = true;
        public bool keepPrefabOnClone = true;

        // 아틀라스 설정
        public int atlasSize = 8192;
        public int grid = 4;
        public int paddingPx = 16;

        // 머지 정책
        public DiffPolicy diffPolicy = DiffPolicy.미해결이면중단;
        public Material diffSampleMaterial;
        public string outputFolder = "Assets/_Generated/MultiAtlas";

        // UI 상태
        public bool globalFoldout = true;
        public Vector2 scroll;
        public bool showLogConsole = false;

        // 스캔 결과
        public List<GroupScan> scans = new List<GroupScan>();

        // 런타임 전용 (직렬화하지 않음)
        [NonSerialized] public Material blitMat;
        [NonSerialized] public Dictionary<int, Material> defaultMatCache = new Dictionary<int, Material>();

        // 저장 관련
        [NonSerialized] public bool suppressAutosaveOnce;
        [NonSerialized] public bool saveQueued;

        // 상수
        public const string BlitShaderPath = "Assets/_Generated/MultiAtlas/Hidden_KibaAtlasBlit.shader";
    }
}
#endif
