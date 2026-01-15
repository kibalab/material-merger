#if UNITY_EDITOR
using System.Collections.Generic;

namespace K13A.MaterialMerger.Editor.Models
{
    public class GroupScan
    {
        public GroupKey key;
        public string shaderName;
        public string tag;

        public List<MatInfo> mats = new List<MatInfo>();
        public int tilesPerPage;
        public int pageCount;
        public int skippedMultiMat;
        public string outputMaterialName = "";

        public List<string> shaderTexProps = new List<string>();
        public List<string> shaderScalarProps = new List<string>();
        public List<Row> rows = new List<Row>();

        public bool enabled = true;
        public bool foldout = true;

        public string search = "";
        public bool onlyRelevant = true;
        public bool showTexturesOnly = false;
        public bool showScalarsOnly = false;
    }
}
#endif
