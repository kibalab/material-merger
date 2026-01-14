#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace K13A.MaterialMerger.Editor
{
    public class KibaMultiAtlasMergerLog : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string rendererGlobalId;
            public Material[] beforeMaterials;
            public Material[] afterMaterials;
            public Mesh beforeMesh;
            public Mesh afterMesh;
        }

        public string sourceRootGlobalId;
        public string appliedRootGlobalId;

        public List<Entry> entries = new List<Entry>();
        public List<string> createdAssetPaths = new List<string>();
    }
}
#endif
