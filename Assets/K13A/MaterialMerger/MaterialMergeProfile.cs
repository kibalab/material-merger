using System;
using System.Collections.Generic;
using UnityEngine;

namespace K13A.MaterialMerger
{
    [DisallowMultipleComponent]
    public class MaterialMergeProfile : MonoBehaviour
    {
        [Serializable]
        public class RowData
        {
            public string name;
            public int shaderPropIndex;
            public int shaderPropType;

            public bool doAction;
            public int bakeMode;

            public int targetTexIndex;
            public string targetTexProp;

            public bool includeAlpha;
            public bool resetSourceAfterBake;

            public int modOp;
            public int modPropIndex;
            public string modProp;
            public bool modClamp01;
            public float modScale;
            public float modBias;
            public bool modAffectsAlpha;

            public bool expanded;

            public int texNonNull;
            public int texDistinct;
            public int stDistinct;
            public bool isNormalLike;
            public bool isSRGB;
            public int distinctCount;
        }

        [Serializable]
        public class GroupData
        {
            public string shaderGuid;
            public string shaderName;
            public int keywordsHash;
            public int renderQueue;
            public int transparencyKey;

            public string tag;
            public int materialCount;
            public int tilesPerPage;
            public int pageCount;
            public int skippedMultiMat;
            public string outputMaterialName;
            public string mergeKey;

            public bool enabled = true;
            public bool foldout = true;

            public string search = "";
            public bool onlyRelevant = true;
            public bool showTexturesOnly;
            public bool showScalarsOnly;

            public List<RowData> rows = new List<RowData>();
        }

        public bool groupByKeywords = true;
        public bool groupByRenderQueue = true;
        public bool splitOpaqueTransparent = true;

        public bool cloneRootOnApply = true;
        public bool deactivateOriginalRoot = true;
        public bool keepPrefabOnClone = true;

        public int atlasSize = 8192;
        public int grid = 4;
        public int paddingPx = 16;

        public int diffPolicy;
        public Material diffSampleMaterial;
        public string outputFolder = "Assets/_Generated/MultiAtlas";

        public bool globalFoldout = true;

        public long lastScanTicksUtc;

        public List<GroupData> groups = new List<GroupData>();
    }
}
