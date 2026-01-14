#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace K13A.MaterialMerger.Editor.Services
{
    public interface IMeshRemapper
    {
        /// <summary>
        /// UV 좌표가 리매핑된 메시 생성 또는 캐시에서 가져오기
        /// </summary>
        Mesh GetOrCreateRemappedMesh(
            Mesh sourceMesh,
            int tileIndex,
            Vector2 scale,
            Vector2 offset,
            Dictionary<(Mesh, int), Mesh> cache,
            string outputFolder
        );
    }
}
#endif
