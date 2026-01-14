#if UNITY_EDITOR
using System;
using UnityEditor;

namespace K13A.MaterialMerger.Editor.Models
{
    [Serializable]
    public class Row
    {
        public int shaderPropIndex;
        public string name;
        public ShaderUtil.ShaderPropertyType type;

        public bool doAction;

        public int texNonNull;
        public int texDistinct;
        public int stDistinct;
        public bool isNormalLike;
        public bool isSRGB;

        public int distinctCount;
        public BakeMode bakeMode;

        public int targetTexIndex;
        public string targetTexProp;

        public bool includeAlpha;
        public bool resetSourceAfterBake;

        public ModOp modOp;
        public int modPropIndex;
        public string modProp;
        public bool modClamp01 = true;
        public float modScale = 1f;
        public float modBias;
        public bool modAffectsAlpha;

        public bool expanded;
    }
}
#endif
