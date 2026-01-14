#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace K13A.MaterialMerger.Editor.Services
{
    public class MeshRemapper : IMeshRemapper
    {
        public Mesh GetOrCreateRemappedMesh(
            Mesh sourceMesh,
            int tileIndex,
            Vector2 scale,
            Vector2 offset,
            Dictionary<(Mesh, int), Mesh> cache,
            string outputFolder)
        {
            var key = (sourceMesh, tileIndex);
            if (cache.TryGetValue(key, out var cached) && cached) return cached;

            var remappedMesh = Object.Instantiate(sourceMesh);
            remappedMesh.name = sourceMesh.name + $"_Atlas_{tileIndex:D3}";

            var uv = remappedMesh.uv;
            for (int i = 0; i < uv.Length; i++)
                uv[i] = new Vector2(uv[i].x * scale.x + offset.x, uv[i].y * scale.y + offset.y);
            remappedMesh.uv = uv;

            string meshFolder = Path.Combine(outputFolder, "_Meshes").Replace("\\", "/");
            Directory.CreateDirectory(meshFolder);
            string meshPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(meshFolder, remappedMesh.name + ".asset").Replace("\\", "/"));
            AssetDatabase.CreateAsset(remappedMesh, meshPath);

            cache[key] = remappedMesh;
            return remappedMesh;
        }
    }
}
#endif
