#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace K13A.MaterialMerger.Editor.Models
{
    /// <summary>
    /// Information about a tile's position within a page
    /// </summary>
    public struct PageTileInfo
    {
        public int pageIndex;
        public int tileIndex;
    }

    /// <summary>
    /// Build information for a single atlas page
    /// </summary>
    public class PageBuildInfo
    {
        public int atlasSize;
        public int gridCols;
        public Material mergedMaterial;
        public string materialPath;
    }

    /// <summary>
    /// Complete build data for a material group
    /// </summary>
    public class GroupBuildData
    {
        public GroupKey groupKey;
        public List<PageBuildInfo> pageInfos;
        public Dictionary<Material, PageTileInfo> matToPage;

        public GroupBuildData()
        {
            pageInfos = new List<PageBuildInfo>();
            matToPage = new Dictionary<Material, PageTileInfo>();
        }

        public bool IsValid =>
            pageInfos != null && pageInfos.Count > 0 &&
            matToPage != null && matToPage.Count > 0;
    }

    /// <summary>
    /// Result of asset building phase
    /// </summary>
    public class AssetBuildResult
    {
        public List<GroupBuildData> groupBuildData;
        public KibaMultiAtlasMergerLog log;
        public string logPath;
        public int processedCount;
        public int skippedCount;
        public bool success;
        public string errorMessage;

        public AssetBuildResult()
        {
            groupBuildData = new List<GroupBuildData>();
        }
    }

    /// <summary>
    /// Result of scene apply phase
    /// </summary>
    public class SceneApplyResult
    {
        public GameObject appliedRoot;
        public int renderersProcessed;
        public bool success;
        public string errorMessage;
    }
}
#endif
