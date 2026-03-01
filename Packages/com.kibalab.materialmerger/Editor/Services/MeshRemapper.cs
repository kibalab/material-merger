#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace K13A.MaterialMerger.Editor.Services
{
    public class MeshRemapper : IMeshRemapper
    {
        public Mesh GetOrCreateRemappedMesh(
            Mesh sourceMesh,
            IReadOnlyList<SubmeshUvTransform> transforms,
            Dictionary<(Mesh, string), Mesh> cache,
            string outputFolder,
            IReadOnlyList<int> submeshMergeMap = null)
        {
            if (!sourceMesh || transforms == null || transforms.Count == 0)
                return sourceMesh;

            string key = BuildCacheKey(transforms, submeshMergeMap);
            var cacheKey = (sourceMesh, key);
            if (cache.TryGetValue(cacheKey, out var cached) && cached) return cached;

            var remappedMesh = RemapMeshBySubmesh(sourceMesh, transforms, submeshMergeMap);

            string meshFolder = Path.Combine(outputFolder, "_Meshes").Replace("\\", "/");
            Directory.CreateDirectory(meshFolder);
            string meshPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(meshFolder, remappedMesh.name + ".asset").Replace("\\", "/"));
            AssetDatabase.CreateAsset(remappedMesh, meshPath);

            cache[cacheKey] = remappedMesh;
            return remappedMesh;
        }

        private static string BuildCacheKey(
            IReadOnlyList<SubmeshUvTransform> transforms,
            IReadOnlyList<int> submeshMergeMap)
        {
            var sb = new StringBuilder(transforms.Count * 32);
            for (int i = 0; i < transforms.Count; i++)
            {
                var t = transforms[i];
                sb.Append(t.subMeshIndex).Append(':')
                    .Append(BitConverter.SingleToInt32Bits(t.scale.x)).Append(',')
                    .Append(BitConverter.SingleToInt32Bits(t.scale.y)).Append(',')
                    .Append(BitConverter.SingleToInt32Bits(t.offset.x)).Append(',')
                    .Append(BitConverter.SingleToInt32Bits(t.offset.y)).Append(';');
            }
            if (submeshMergeMap != null && submeshMergeMap.Count > 0)
            {
                sb.Append("|m:");
                for (int i = 0; i < submeshMergeMap.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(submeshMergeMap[i]);
                }
            }
            return sb.ToString();
        }

        private static Mesh RemapMeshBySubmesh(
            Mesh sourceMesh,
            IReadOnlyList<SubmeshUvTransform> transforms,
            IReadOnlyList<int> submeshMergeMap)
        {
            int subMeshCount = sourceMesh.subMeshCount;
            int srcVertexCount = sourceMesh.vertexCount;

            var transformBySubmesh = new Dictionary<int, SubmeshUvTransform>();
            for (int i = 0; i < transforms.Count; i++)
                transformBySubmesh[transforms[i].subMeshIndex] = transforms[i];

            int[] mergeMap = null;
            int mergedSubmeshCount = subMeshCount;
            if (submeshMergeMap != null && submeshMergeMap.Count >= subMeshCount)
            {
                mergeMap = new int[subMeshCount];
                int maxIndex = -1;
                for (int i = 0; i < subMeshCount; i++)
                {
                    int idx = submeshMergeMap[i];
                    if (idx < 0) idx = 0;
                    mergeMap[i] = idx;
                    if (idx > maxIndex) maxIndex = idx;
                }
                mergedSubmeshCount = maxIndex + 1;
                if (mergedSubmeshCount <= 0)
                {
                    mergeMap = null;
                    mergedSubmeshCount = subMeshCount;
                }
            }

            var srcVertices = sourceMesh.vertices;
            var srcNormals = sourceMesh.normals;
            var srcTangents = sourceMesh.tangents;
            var srcColors32 = sourceMesh.colors32;
            var srcColors = sourceMesh.colors;
            var srcUv = sourceMesh.uv;
            var srcUv2 = sourceMesh.uv2;
            var srcUv3 = sourceMesh.uv3;
            var srcUv4 = sourceMesh.uv4;
            var srcBoneWeights = sourceMesh.boneWeights;

            bool hasNormals = srcNormals != null && srcNormals.Length == srcVertexCount;
            bool hasTangents = srcTangents != null && srcTangents.Length == srcVertexCount;
            bool hasColors32 = srcColors32 != null && srcColors32.Length == srcVertexCount;
            bool hasColors = !hasColors32 && srcColors != null && srcColors.Length == srcVertexCount;
            bool hasUv = srcUv != null && srcUv.Length == srcVertexCount;
            bool hasUv2 = srcUv2 != null && srcUv2.Length == srcVertexCount;
            bool hasUv3 = srcUv3 != null && srcUv3.Length == srcVertexCount;
            bool hasUv4 = srcUv4 != null && srcUv4.Length == srcVertexCount;
            bool hasBoneWeights = srcBoneWeights != null && srcBoneWeights.Length == srcVertexCount;

            var newVertices = new List<Vector3>(srcVertexCount);
            var newNormals = hasNormals ? new List<Vector3>(srcVertexCount) : null;
            var newTangents = hasTangents ? new List<Vector4>(srcVertexCount) : null;
            var newColors32 = hasColors32 ? new List<Color32>(srcVertexCount) : null;
            var newColors = hasColors ? new List<Color>(srcVertexCount) : null;
            var newUv = hasUv ? new List<Vector2>(srcVertexCount) : null;
            var newUv2 = hasUv2 ? new List<Vector2>(srcVertexCount) : null;
            var newUv3 = hasUv3 ? new List<Vector2>(srcVertexCount) : null;
            var newUv4 = hasUv4 ? new List<Vector2>(srcVertexCount) : null;
            var newBoneWeights = hasBoneWeights ? new List<BoneWeight>(srcVertexCount) : null;
            var newToOld = new List<int>(srcVertexCount);

            var mergedIndices = new List<int>[mergedSubmeshCount];
            var mergedTopologies = new MeshTopology[mergedSubmeshCount];
            var topologySet = new bool[mergedSubmeshCount];

            for (int s = 0; s < subMeshCount; s++)
            {
                var indices = sourceMesh.GetIndices(s);
                var topology = sourceMesh.GetTopology(s);
                int target = mergeMap != null ? mergeMap[s] : s;
                if (target < 0 || target >= mergedSubmeshCount)
                    target = s;

                if (!topologySet[target])
                {
                    mergedTopologies[target] = topology;
                    topologySet[target] = true;
                }

                var remap = new Dictionary<int, int>();
                var newIndices = new int[indices.Length];

                bool hasTransform = transformBySubmesh.TryGetValue(s, out var transform);
                Vector2 scale = hasTransform ? transform.scale : Vector2.one;
                Vector2 offset = hasTransform ? transform.offset : Vector2.zero;

                for (int i = 0; i < indices.Length; i++)
                {
                    int srcIndex = indices[i];
                    if (!remap.TryGetValue(srcIndex, out int dstIndex))
                    {
                        dstIndex = newVertices.Count;
                        remap[srcIndex] = dstIndex;

                        newVertices.Add(srcVertices[srcIndex]);
                        newToOld.Add(srcIndex);

                        if (hasNormals) newNormals.Add(srcNormals[srcIndex]);
                        if (hasTangents) newTangents.Add(srcTangents[srcIndex]);
                        if (hasColors32) newColors32.Add(srcColors32[srcIndex]);
                        if (hasColors) newColors.Add(srcColors[srcIndex]);
                        if (hasUv)
                        {
                            var uv = srcUv[srcIndex];
                            if (hasTransform)
                                uv = new Vector2(uv.x * scale.x + offset.x, uv.y * scale.y + offset.y);
                            newUv.Add(uv);
                        }
                        if (hasUv2) newUv2.Add(srcUv2[srcIndex]);
                        if (hasUv3) newUv3.Add(srcUv3[srcIndex]);
                        if (hasUv4) newUv4.Add(srcUv4[srcIndex]);
                        if (hasBoneWeights) newBoneWeights.Add(srcBoneWeights[srcIndex]);
                    }

                    newIndices[i] = dstIndex;
                }

                if (mergedIndices[target] == null)
                    mergedIndices[target] = new List<int>(newIndices.Length);
                mergedIndices[target].AddRange(newIndices);
            }

            var remappedMesh = new Mesh();
            remappedMesh.name = sourceMesh.name + "_AtlasMulti";
            remappedMesh.indexFormat = newVertices.Count > 65535 ? IndexFormat.UInt32 : sourceMesh.indexFormat;

            remappedMesh.vertices = newVertices.ToArray();
            if (hasNormals) remappedMesh.normals = newNormals.ToArray();
            if (hasTangents) remappedMesh.tangents = newTangents.ToArray();
            if (hasColors32) remappedMesh.colors32 = newColors32.ToArray();
            if (hasColors) remappedMesh.colors = newColors.ToArray();
            if (hasUv) remappedMesh.uv = newUv.ToArray();
            if (hasUv2) remappedMesh.uv2 = newUv2.ToArray();
            if (hasUv3) remappedMesh.uv3 = newUv3.ToArray();
            if (hasUv4) remappedMesh.uv4 = newUv4.ToArray();
            if (hasBoneWeights) remappedMesh.boneWeights = newBoneWeights.ToArray();

            var bindposes = sourceMesh.bindposes;
            if (bindposes != null && bindposes.Length > 0)
                remappedMesh.bindposes = bindposes;

            remappedMesh.subMeshCount = mergedSubmeshCount;
            for (int s = 0; s < mergedSubmeshCount; s++)
            {
                var list = mergedIndices[s] ?? new List<int>();
                var topology = topologySet[s] ? mergedTopologies[s] : MeshTopology.Triangles;
                remappedMesh.SetIndices(list.ToArray(), topology, s, false);
            }

            remappedMesh.bounds = sourceMesh.bounds;
            CopyBlendShapes(sourceMesh, remappedMesh, newToOld);

            return remappedMesh;
        }

        private static void CopyBlendShapes(Mesh sourceMesh, Mesh targetMesh, List<int> newToOld)
        {
            int blendShapeCount = sourceMesh.blendShapeCount;
            if (blendShapeCount == 0) return;

            int srcVertexCount = sourceMesh.vertexCount;
            int newVertexCount = newToOld.Count;

            var deltaVertices = new Vector3[srcVertexCount];
            var deltaNormals = new Vector3[srcVertexCount];
            var deltaTangents = new Vector3[srcVertexCount];

            for (int shapeIndex = 0; shapeIndex < blendShapeCount; shapeIndex++)
            {
                string shapeName = sourceMesh.GetBlendShapeName(shapeIndex);
                int frameCount = sourceMesh.GetBlendShapeFrameCount(shapeIndex);

                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    float weight = sourceMesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex);
                    sourceMesh.GetBlendShapeFrameVertices(shapeIndex, frameIndex, deltaVertices, deltaNormals, deltaTangents);

                    var newDeltaVertices = new Vector3[newVertexCount];
                    var newDeltaNormals = new Vector3[newVertexCount];
                    var newDeltaTangents = new Vector3[newVertexCount];

                    for (int i = 0; i < newVertexCount; i++)
                    {
                        int srcIndex = newToOld[i];
                        newDeltaVertices[i] = deltaVertices[srcIndex];
                        newDeltaNormals[i] = deltaNormals[srcIndex];
                        newDeltaTangents[i] = deltaTangents[srcIndex];
                    }

                    targetMesh.AddBlendShapeFrame(shapeName, weight, newDeltaVertices, newDeltaNormals, newDeltaTangents);
                }
            }
        }
    }
}
#endif
