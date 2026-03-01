#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace K13A.MaterialMerger.Editor.Models
{
    public class MatInfo
    {
        public Material mat;
        public List<Renderer> users = new List<Renderer>();
    }
}
#endif
