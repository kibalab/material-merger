#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor.Services
{
    public class MaterialScanService : IMaterialScanService
    {
        public List<Renderer> CollectRenderers(GameObject root)
        {
            if (!root) return new List<Renderer>();
            return root.GetComponentsInChildren<Renderer>(true).ToList();
        }

        public bool IsTransparent(Material material)
        {
            if (!material) return false;
            if (material.renderQueue >= 3000) return true;
            var tag = material.GetTag("RenderType", false, "");
            if (!string.IsNullOrEmpty(tag) && (tag.IndexOf("Transparent", StringComparison.OrdinalIgnoreCase) >= 0 || tag.IndexOf("Fade", StringComparison.OrdinalIgnoreCase) >= 0))
                return true;
            return false;
        }

        public GroupKey CreateGroupKey(Material material, bool groupByKeywords, bool groupByRenderQueue, bool splitOpaqueTransparent)
        {
            int rq = groupByRenderQueue ? material.renderQueue : 0;
            int tr = splitOpaqueTransparent ? (IsTransparent(material) ? 1 : 0) : 0;

            return new GroupKey
            {
                shader = material.shader,
                keywordsHash = CalculateKeywordsHash(material, groupByKeywords),
                renderQueue = rq,
                transparencyKey = tr
            };
        }

        public List<GroupScan> ScanGameObject(GameObject root, bool groupByKeywords, bool groupByRenderQueue, bool splitOpaqueTransparent, int grid)
        {
            var result = new List<GroupScan>();
            var renderers = CollectRenderers(root);

            var groups = new Dictionary<GroupKey, Dictionary<Material, MatInfo>>();

            foreach (var r in renderers)
            {
                if (!r) continue;
                var mats = r.sharedMaterials;
                if (mats == null) continue;

                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (!m || !m.shader) continue;

                    var key = CreateGroupKey(m, groupByKeywords, groupByRenderQueue, splitOpaqueTransparent);

                    if (!groups.TryGetValue(key, out var dict))
                    {
                        dict = new Dictionary<Material, MatInfo>();
                        groups[key] = dict;
                    }

                    if (!dict.TryGetValue(m, out var mi))
                    {
                        mi = new MatInfo { mat = m };
                        dict[m] = mi;
                    }

                    mi.users.Add(r);
                }
            }

            int tilesPerPage = grid * grid;

            foreach (var kv in groups)
            {
                var g = new GroupScan();
                g.key = kv.Key;
                g.shaderName = g.key.shader ? g.key.shader.name : "";
                g.tilesPerPage = tilesPerPage;
                g.tag = (kv.Key.transparencyKey == 1) ? "투명" : "불투명";
                g.mats = kv.Value.Values.ToList();
                g.pageCount = Mathf.CeilToInt(g.mats.Count / (float)tilesPerPage);

                g.shaderTexProps = GetShaderPropertiesByType(g.key.shader, ShaderUtil.ShaderPropertyType.TexEnv);
                g.shaderScalarProps = GetShaderPropertiesByType(g.key.shader, ShaderUtil.ShaderPropertyType.Float)
                    .Concat(GetShaderPropertiesByType(g.key.shader, ShaderUtil.ShaderPropertyType.Range))
                    .Distinct()
                    .ToList();

                g.rows = BuildPropertyRows(g);
                g.skippedMultiMat = EstimateMultiMaterialSkips(g);

                result.Add(g);
            }

            return result;
        }

        public List<string> GetShaderPropertiesByType(Shader shader, ShaderUtil.ShaderPropertyType type)
        {
            var list = new List<string>();
            if (!shader) return list;
            int pc = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < pc; i++)
                if (ShaderUtil.GetPropertyType(shader, i) == type)
                    list.Add(ShaderUtil.GetPropertyName(shader, i));
            return list;
        }

        public int EstimateMultiMaterialSkips(GroupScan group)
        {
            var matSet = new HashSet<Material>(group.mats.Select(x => x.mat));
            var renderers = new HashSet<Renderer>();
            foreach (var mi in group.mats)
            foreach (var u in mi.users)
                if (u)
                    renderers.Add(u);

            int count = 0;
            foreach (var r in renderers)
            {
                var shared = r.sharedMaterials;
                if (shared == null) continue;

                int hits = 0;
                Material hitMat = null;
                foreach (var m in shared)
                {
                    if (m && matSet.Contains(m))
                    {
                        hits++;
                        if (!hitMat) hitMat = m;
                        else if (hitMat != m)
                        {
                            hits = 999;
                            break;
                        }
                    }
                }

                if (hits > 1) count++;
            }

            return count;
        }

        public List<Row> BuildPropertyRows(GroupScan group)
        {
            var rows = new List<Row>();
            var shader = group.key.shader;
            if (!shader) return rows;

            var mats = group.mats.Select(x => x.mat).Where(x => x).ToList();
            int pc = ShaderUtil.GetPropertyCount(shader);

            for (int i = 0; i < pc; i++)
            {
                var type = ShaderUtil.GetPropertyType(shader, i);
                var name = ShaderUtil.GetPropertyName(shader, i);

                var row = new Row();
                row.shaderPropIndex = i;
                row.name = name;
                row.type = type;

                row.doAction = false;

                row.includeAlpha = false;
                row.resetSourceAfterBake = true;

                row.modOp = ModOp.없음;
                row.modPropIndex = 0;
                row.modProp = "";
                row.modClamp01 = true;
                row.modScale = 1f;
                row.modBias = 0f;
                row.modAffectsAlpha = false;

                if (type == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    AnalyzeTextureProperty(row, name, mats);
                }
                else if (type == ShaderUtil.ShaderPropertyType.Color || type == ShaderUtil.ShaderPropertyType.Float || type == ShaderUtil.ShaderPropertyType.Range || type == ShaderUtil.ShaderPropertyType.Vector)
                {
                    AnalyzeScalarProperty(row, name, type, mats, group);
                }

                rows.Add(row);
            }

            return rows;
        }

        private int CalculateKeywordsHash(Material material, bool groupByKeywords)
        {
            if (!groupByKeywords) return 0;
            var arr = material.shaderKeywords != null ? material.shaderKeywords.OrderBy(x => x).ToArray() : Array.Empty<string>();
            unchecked
            {
                int h = 17;
                for (int i = 0; i < arr.Length; i++) h = h * 31 + arr[i].GetHashCode();
                return h;
            }
        }

        private Vector4 GetScaleTiling(Material material, string propName)
        {
            if (!material) return new Vector4(1, 1, 0, 0);
            string stName = propName + "_ST";
            if (!material.HasProperty(stName)) return new Vector4(1, 1, 0, 0);
            return material.GetVector(stName);
        }

        private bool IsLinearTexture(string propName)
        {
            if (string.IsNullOrEmpty(propName)) return false;
            string p = propName.ToLowerInvariant();
            if (p.Contains("mask")) return true;
            if (p.Contains("metal")) return true;
            if (p.Contains("rough")) return true;
            if (p.Contains("smooth")) return true;
            if (p.Contains("occlusion")) return true;
            if (p.Contains("ao")) return true;
            if (p.Contains("spec")) return true;
            if (p.Contains("depth")) return true;
            return false;
        }

        private void AnalyzeTextureProperty(Row row, string name, List<Material> materials)
        {
            int nonNull = 0;
            var texSet = new HashSet<Texture>();
            var stSet = new HashSet<string>();

            bool anyNormal = false;
            bool anySRGB = false;
            bool anyLinear = false;

            foreach (var m in materials)
            {
                if (!m.HasProperty(name)) continue;
                var t = m.GetTexture(name);
                if (t)
                {
                    nonNull++;
                    texSet.Add(t);

                    var path = AssetDatabase.GetAssetPath(t);
                    var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (imp)
                    {
                        if (imp.textureType == TextureImporterType.NormalMap) anyNormal = true;
                        if (imp.sRGBTexture) anySRGB = true;
                        else anyLinear = true;
                    }
                }

                var st = GetScaleTiling(m, name);
                stSet.Add($"{st.x:F5},{st.y:F5},{st.z:F5},{st.w:F5}");
            }

            bool normalLike = anyNormal || name.IndexOf("Bump", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Normal", StringComparison.OrdinalIgnoreCase) >= 0;
            bool sRGB;
            if (normalLike) sRGB = false;
            else if (IsLinearTexture(name)) sRGB = false;
            else sRGB = (anySRGB && !anyLinear);

            row.texNonNull = nonNull;
            row.texDistinct = texSet.Count;
            row.stDistinct = stSet.Count;
            row.isNormalLike = normalLike;
            row.isSRGB = sRGB;

            row.doAction = texSet.Count > 1;
        }

        private void AnalyzeScalarProperty(Row row, string name, ShaderUtil.ShaderPropertyType type, List<Material> materials, GroupScan group)
        {
            var set = new HashSet<string>();

            foreach (var m in materials)
            {
                if (!m.HasProperty(name)) continue;

                if (type == ShaderUtil.ShaderPropertyType.Color)
                {
                    var c = m.GetColor(name);
                    set.Add($"{c.r:F5},{c.g:F5},{c.b:F5},{c.a:F5}");
                }
                else if (type == ShaderUtil.ShaderPropertyType.Vector)
                {
                    var v = m.GetVector(name);
                    set.Add($"{v.x:F5},{v.y:F5},{v.z:F5},{v.w:F5}");
                }
                else
                {
                    set.Add($"{m.GetFloat(name):F6}");
                }

                if (set.Count > 64) break;
            }

            row.distinctCount = set.Count;
            row.bakeMode = BakeMode.리셋_쉐이더기본값;
            row.targetTexIndex = 0;
            row.targetTexProp = group.shaderTexProps.Count > 0 ? group.shaderTexProps[0] : "";
            row.doAction = false;
        }
    }
}
#endif
