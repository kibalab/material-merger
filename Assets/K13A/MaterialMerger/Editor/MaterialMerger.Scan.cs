#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor
{
    public partial class MaterialMerger
    {
        List<Renderer> CollectRenderers(GameObject r)
        {
            if (!r) return new List<Renderer>();
            return r.GetComponentsInChildren<Renderer>(true).ToList();
        }

        bool IsTransparent(Material m)
        {
            if (!m) return false;
            if (m.renderQueue >= 3000) return true;
            var tag = m.GetTag("RenderType", false, "");
            if (!string.IsNullOrEmpty(tag) && (tag.IndexOf("Transparent", StringComparison.OrdinalIgnoreCase) >= 0 || tag.IndexOf("Fade", StringComparison.OrdinalIgnoreCase) >= 0))
                return true;
            return false;
        }

        int KeywordsHash(Material m)
        {
            if (!groupByKeywords) return 0;
            var arr = m.shaderKeywords != null ? m.shaderKeywords.OrderBy(x => x).ToArray() : Array.Empty<string>();
            unchecked
            {
                int h = 17;
                for (int i = 0; i < arr.Length; i++) h = h * 31 + arr[i].GetHashCode();
                return h;
            }
        }

        GroupKey MakeKey(Material m)
        {
            int rq = groupByRenderQueue ? m.renderQueue : 0;
            int tr = splitOpaqueTransparent ? (IsTransparent(m) ? 1 : 0) : 0;

            return new GroupKey
            {
                shader = m.shader,
                keywordsHash = KeywordsHash(m),
                renderQueue = rq,
                transparencyKey = tr
            };
        }

        void Scan()
        {
            if (root)
                profile = EnsureProfile(root, true);

            if (profile)
                profile.lastScanTicksUtc = DateTime.UtcNow.Ticks;

            scans = ScanInternal(root);
            scans = scans.OrderBy(x => x.key.shader ? x.key.shader.name : "").ThenBy(x => x.tag).ToList();

            ApplyProfileToScansIfAny();
            RequestSave();

            Repaint();
        }

        List<GroupScan> ScanInternal(GameObject scanRoot)
        {
            var result = new List<GroupScan>();
            var renderers = CollectRenderers(scanRoot);

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

                    var key = MakeKey(m);

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

                g.shaderTexProps = GetShaderPropsOfType(g.key.shader, ShaderUtil.ShaderPropertyType.TexEnv);
                g.shaderScalarProps = GetShaderPropsOfType(g.key.shader, ShaderUtil.ShaderPropertyType.Float)
                    .Concat(GetShaderPropsOfType(g.key.shader, ShaderUtil.ShaderPropertyType.Range))
                    .Distinct()
                    .ToList();

                g.rows = BuildRowsInShaderOrder(g);
                g.skippedMultiMat = EstimateMultiMaterialSkips(g);

                result.Add(g);
            }

            return result;
        }

        List<string> GetShaderPropsOfType(Shader shader, ShaderUtil.ShaderPropertyType type)
        {
            var list = new List<string>();
            if (!shader) return list;
            int pc = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < pc; i++)
                if (ShaderUtil.GetPropertyType(shader, i) == type)
                    list.Add(ShaderUtil.GetPropertyName(shader, i));
            return list;
        }

        Vector4 GetST(Material m, string prop)
        {
            if (!m) return new Vector4(1, 1, 0, 0);
            string stName = prop + "_ST";
            if (!m.HasProperty(stName)) return new Vector4(1, 1, 0, 0);
            return m.GetVector(stName);
        }

        bool HeuristicLinearTex(string prop)
        {
            if (string.IsNullOrEmpty(prop)) return false;
            string p = prop.ToLowerInvariant();
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

        List<Row> BuildRowsInShaderOrder(GroupScan g)
        {
            var rows = new List<Row>();
            var shader = g.key.shader;
            if (!shader) return rows;

            var mats = g.mats.Select(x => x.mat).Where(x => x).ToList();
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
                    int nonNull = 0;
                    var texSet = new HashSet<Texture>();
                    var stSet = new HashSet<string>();

                    bool anyNormal = false;
                    bool anySRGB = false;
                    bool anyLinear = false;

                    foreach (var m in mats)
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

                        var st = GetST(m, name);
                        stSet.Add($"{st.x:F5},{st.y:F5},{st.z:F5},{st.w:F5}");
                    }

                    bool normalLike = anyNormal || name.IndexOf("Bump", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Normal", StringComparison.OrdinalIgnoreCase) >= 0;
                    bool sRGB;
                    if (normalLike) sRGB = false;
                    else if (HeuristicLinearTex(name)) sRGB = false;
                    else sRGB = (anySRGB && !anyLinear);

                    row.texNonNull = nonNull;
                    row.texDistinct = texSet.Count;
                    row.stDistinct = stSet.Count;
                    row.isNormalLike = normalLike;
                    row.isSRGB = sRGB;

                    row.doAction = texSet.Count > 1;
                }
                else if (type == ShaderUtil.ShaderPropertyType.Color || type == ShaderUtil.ShaderPropertyType.Float || type == ShaderUtil.ShaderPropertyType.Range || type == ShaderUtil.ShaderPropertyType.Vector)
                {
                    var set = new HashSet<string>();

                    foreach (var m in mats)
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
                    row.targetTexProp = g.shaderTexProps.Count > 0 ? g.shaderTexProps[0] : "";
                    row.doAction = false;
                }

                rows.Add(row);
            }

            return rows;
        }

        int EstimateMultiMaterialSkips(GroupScan g)
        {
            var matSet = new HashSet<Material>(g.mats.Select(x => x.mat));
            var renderers = new HashSet<Renderer>();
            foreach (var mi in g.mats)
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
    }
}
#endif
