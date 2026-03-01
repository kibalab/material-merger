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
    /// Texture import settings to apply after assets are written
    /// </summary>
    public class TextureImportRequest
    {
        public string assetPath;
        public int maxSize;
        public bool sRGB;
        public bool isNormalMap;
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
        public List<TextureImportRequest> textureImports;
        public KibaMultiAtlasMergerLog log;
        public string logPath;
        public int processedCount;
        public int skippedCount;
        public bool success;
        public string errorMessage;

        public AssetBuildResult()
        {
            groupBuildData = new List<GroupBuildData>();
            textureImports = new List<TextureImportRequest>();
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

    /// <summary>
    /// Async build context for incremental asset building
    /// </summary>
    public class AssetBuildContext
    {
        public BuildSettings settings;
        public List<GroupScan> mergedScans;
        public AssetBuildResult result;
        public KibaMultiAtlasMergerLog log;
        public int groupIndex;
        public GroupBuildContext groupContext;
        public bool autoRefreshDisabled;
        public bool completed;
        public string errorMessage;
    }

    /// <summary>
    /// Async group build context
    /// </summary>
    public class GroupBuildContext
    {
        public GroupScan group;
        public GroupBuildData buildData;
        public List<MatInfo> mats;
        public int tilesPerPage;
        public int pageCount;
        public int pageIndex;
        public Dictionary<string, Row> texMeta;
        public List<Row> bakeRows;
        public HashSet<string> allAtlasProps;
        public List<string> atlasPropList;
        public int cell;
        public int content;
        public int atlasSize;
        public int grid;
        public int paddingPx;
        public string groupFolder;
        public string planFolder;
        public PageBuildContext pageContext;
    }

    /// <summary>
    /// Async page build context
    /// </summary>
    public class PageBuildContext
    {
        public int pageIndex;
        public List<MatInfo> pageItems;
        public int actualGridCols;
        public int actualAtlasSize;
        public Dictionary<string, Texture2D> atlasByProp;
        public int bakeWorkIndex;
        public int bakeWorkTotal;
        public int saveIndex;
        public bool materialCreated;
        public string pageFolder;
    }

    /// <summary>
    /// Async apply context for incremental scene application
    /// </summary>
    public class SceneApplyContext
    {
        public BuildSettings settings;
        public List<GroupScan> scans;
        public AssetBuildResult buildResult;
        public GameObject applyRoot;
        public List<GroupScan> applyScans;
        public List<GroupScan> mergedApplyScans;
        public Dictionary<GroupKey, GroupBuildData> buildDataMap;
        public int groupIndex;
        public ApplyGroupContext groupContext;
        public int renderersProcessed;
        public int undoGroup;
        public bool prepared;
        public bool completed;
        public string errorMessage;
    }

    /// <summary>
    /// Async group apply context
    /// </summary>
    public class ApplyGroupContext
    {
        public GroupScan group;
        public GroupBuildData buildData;
        public KibaMultiAtlasMergerLog log;
        public HashSet<string> allAtlasProps;
        public List<Row> bakeRows;
        public List<Row> resetRows;
        public List<Row> resetAfterBakeRows;
        public List<string> atlasPropList;
        public int pageIndex;
        public int pageStage;
        public int propIndex;
        public int resetIndex;
        public int bakeResetIndex;
        public Material currentMaterial;
        public Material defaultMaterial;
        public List<Renderer> renderers;
        public int rendererIndex;
        public Dictionary<(Mesh, string), Mesh> meshCache;
        public HashSet<Material> mergeCandidates;
        public string groupFolder;
        public int cell;
        public int content;
        public int paddingPx;
        public string outputFolder;
        public DiffPolicy diffPolicy;
        public Material sampleMaterial;
        public bool hasMissingMaterial;
    }
}
#endif
