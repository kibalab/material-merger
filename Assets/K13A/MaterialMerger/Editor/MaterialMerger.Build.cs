#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor
{
    public partial class MaterialMerger
    {
        void EnsureBlitMaterial()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BlitShaderPath));
            if (!File.Exists(BlitShaderPath))
            {
                File.WriteAllText(BlitShaderPath, GetBlitShaderSource());
                AssetDatabase.ImportAsset(BlitShaderPath, ImportAssetOptions.ForceUpdate);
            }

            var s = AssetDatabase.LoadAssetAtPath<Shader>(BlitShaderPath);
            if (!s) return;
            if (!blitMat) blitMat = new Material(s) { hideFlags = HideFlags.HideAndDontSave };
        }

        string GetBlitShaderSource()
        {
            return
                @"Shader ""Hidden/KibaAtlasBlit""
{
    SubShader
    {
        Pass
        {
            ZWrite Off ZTest Always Cull Off Blend Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""
            sampler2D _MainTex;
            float4 _ScaleOffset;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * _ScaleOffset.xy + _ScaleOffset.zw;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}";
        }

        Material GetDefaultMaterial(Shader shader)
        {
            if (!shader) return null;
            int id = shader.GetInstanceID();
            if (defaultMatCache.TryGetValue(id, out var m) && m) return m;
            var nm = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            defaultMatCache[id] = nm;
            return nm;
        }

        float EvalMod(Material mat, Row r)
        {
            if (r.modOp == ModOp.없음) return 0f;
            float def = (r.modOp == ModOp.곱셈) ? 1f : 0f;
            float v = def;
            if (mat && !string.IsNullOrEmpty(r.modProp) && mat.HasProperty(r.modProp))
                v = mat.GetFloat(r.modProp);
            v = v * r.modScale + r.modBias;
            if (r.modClamp01) v = Mathf.Clamp01(v);
            return v;
        }

        Color ApplyModToColor(Color c, float m, Row r)
        {
            if (r.modOp == ModOp.없음) return c;
            if (r.modOp == ModOp.곱셈)
            {
                c.r *= m;
                c.g *= m;
                c.b *= m;
                if (r.modAffectsAlpha) c.a *= m;
            }
            else if (r.modOp == ModOp.가산)
            {
                c.r += m;
                c.g += m;
                c.b += m;
                if (r.modAffectsAlpha) c.a += m;
            }
            else if (r.modOp == ModOp.감산)
            {
                c.r -= m;
                c.g -= m;
                c.b -= m;
                if (r.modAffectsAlpha) c.a -= m;
            }

            c.r = Mathf.Clamp01(c.r);
            c.g = Mathf.Clamp01(c.g);
            c.b = Mathf.Clamp01(c.b);
            c.a = Mathf.Clamp01(c.a);
            return c;
        }

        float ApplyModToScalar(float v, float m, Row r)
        {
            if (r.modOp == ModOp.없음) return v;
            if (r.modOp == ModOp.곱셈) v *= m;
            else if (r.modOp == ModOp.가산) v += m;
            else if (r.modOp == ModOp.감산) v -= m;
            return v;
        }

        void BuildAndApplyWithConfirm()
        {
            if (scans == null || scans.Count == 0)
            {
                EditorUtility.DisplayDialog("멀티 아틀라스", "스캔 결과가 없습니다.", "OK");
                return;
            }

            var list = new List<ConfirmWindow.GroupInfo>();

            foreach (var g in scans)
            {
                if (!g.enabled) continue;

                var shaderName = g.key.shader ? g.key.shader.name : (!string.IsNullOrEmpty(g.shaderName) ? g.shaderName : "NULL_SHADER");
                var title = $"{shaderName} [{g.tag}]  (mat:{g.mats.Count}, pages:{g.pageCount})";

                bool hasUnresolved = g.rows.Any(r =>
                    (r.type == ShaderUtil.ShaderPropertyType.Color ||
                     r.type == ShaderUtil.ShaderPropertyType.Float ||
                     r.type == ShaderUtil.ShaderPropertyType.Range ||
                     r.type == ShaderUtil.ShaderPropertyType.Vector) &&
                    r.distinctCount > 1 && !r.doAction);

                bool willRun = !(hasUnresolved && diffPolicy == DiffPolicy.미해결이면중단);

                var atlasProps = g.rows
                    .Where(r => r.type == ShaderUtil.ShaderPropertyType.TexEnv && r.doAction)
                    .Select(r => r.name)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToList();

                bool NeedsTarget(Row r) =>
                    r.bakeMode == BakeMode.색상굽기_텍스처타일 ||
                    r.bakeMode == BakeMode.스칼라굽기_그레이타일 ||
                    r.bakeMode == BakeMode.색상곱_텍스처타일;

                var generatedProps = g.rows
                    .Where(r => r.doAction && r.type != ShaderUtil.ShaderPropertyType.TexEnv && NeedsTarget(r))
                    .Select(r => r.targetTexProp)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToList();

                var gi = new ConfirmWindow.GroupInfo();
                gi.title = title;
                gi.willRun = willRun;
                gi.skipReason = willRun ? "" : "미해결 diff가 있는데 정책이 '미해결이면중단'이라 이 Material Plan은 스킵됩니다.";
                gi.atlasProps = atlasProps;
                gi.generatedProps = generatedProps;

                list.Add(gi);
            }

            if (list.Count == 0)
            {
                EditorUtility.DisplayDialog("멀티 아틀라스", "활성화된 Material Plan이 없습니다.", "OK");
                return;
            }

            ConfirmWindow.Open(this, list);
        }

        public void BuildAndApply()
        {
            EnsureBlitMaterial();
            if (!blitMat)
            {
                EditorUtility.DisplayDialog("멀티 아틀라스", "Blit 머티리얼 생성 실패", "OK");
                return;
            }

            if (cloneRootOnApply && !root)
            {
                EditorUtility.DisplayDialog("멀티 아틀라스", "루트가 필요합니다(적용 시 루트 복제 옵션).", "OK");
                return;
            }

            Directory.CreateDirectory(outputFolder);

            var log = CreateInstance<KibaMultiAtlasMergerLog>();
            string logPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(outputFolder, $"MultiAtlasLog_{DateTime.Now:yyyyMMdd_HHmmss}.asset").Replace("\\", "/"));
            AssetDatabase.CreateAsset(log, logPath);
            log.createdAssetPaths.Add(logPath);

            GameObject applyRootObj = root;
            if (cloneRootOnApply && root)
            {
                applyRootObj = CloneRootForApply(root);
                if (!applyRootObj) return;
            }

            log.sourceRootGlobalId = root ? GlobalObjectId.GetGlobalObjectIdSlow(root).ToString() : "";
            log.appliedRootGlobalId = applyRootObj ? GlobalObjectId.GetGlobalObjectIdSlow(applyRootObj).ToString() : "";

            var applyScans = ScanInternal(applyRootObj);
            CopySettings(scans, applyScans);

            int cell = atlasSize / grid;
            int content = cell - paddingPx * 2;
            if (content <= 0)
            {
                content = cell;
                paddingPx = 0;
            }

            Undo.IncrementCurrentGroup();
            int ug = Undo.GetCurrentGroup();

            foreach (var g in applyScans)
            {
                if (!g.enabled) continue;

                bool hasUnresolved = g.rows.Any(r =>
                    (r.type == ShaderUtil.ShaderPropertyType.Color || r.type == ShaderUtil.ShaderPropertyType.Float || r.type == ShaderUtil.ShaderPropertyType.Range || r.type == ShaderUtil.ShaderPropertyType.Vector) &&
                    r.distinctCount > 1 && !r.doAction);

                if (hasUnresolved && diffPolicy == DiffPolicy.미해결이면중단)
                    continue;

                BuildGroup(g, log, cell, content);
            }

            EditorUtility.SetDirty(log);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Undo.CollapseUndoOperations(ug);

            EditorUtility.DisplayDialog("멀티 아틀라스", $"완료\n로그: {logPath}", "OK");
        }

        GameObject CloneRootForApply(GameObject src)
        {
            var parent = src.transform.parent;
            var clone = Instantiate(src, parent);
            clone.name = src.name + "_AtlasMerged";
            clone.transform.localPosition = src.transform.localPosition;
            clone.transform.localRotation = src.transform.localRotation;
            clone.transform.localScale = src.transform.localScale;
            Undo.RegisterCreatedObjectUndo(clone, "루트 복제");

            if (deactivateOriginalRoot)
            {
                Undo.RecordObject(src, "원본 루트 비활성화");
                src.SetActive(false);
            }

            Selection.activeGameObject = clone;
            return clone;
        }

        void CopySettings(List<GroupScan> from, List<GroupScan> to)
        {
            var map = new Dictionary<GroupKey, GroupScan>();
            foreach (var g in from) map[g.key] = g;

            foreach (var tg in to)
            {
                if (!map.TryGetValue(tg.key, out var sg)) continue;

                tg.enabled = sg.enabled;
                tg.foldout = sg.foldout;
                tg.search = sg.search;
                tg.onlyRelevant = sg.onlyRelevant;
                tg.showTexturesOnly = sg.showTexturesOnly;
                tg.showScalarsOnly = sg.showScalarsOnly;

                var srcRow = sg.rows
                    .Where(r => r != null && !string.IsNullOrEmpty(r.name) && r.name != "_DummyProperty")
                    .GroupBy(r => r.name, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

                foreach (var tr in tg.rows)
                {
                    if (tr == null) continue;
                    if (string.IsNullOrEmpty(tr.name)) continue;
                    if (tr.name == "_DummyProperty") continue;

                    if (!srcRow.TryGetValue(tr.name, out var sr)) continue;

                    tr.doAction = sr.doAction;

                    tr.bakeMode = sr.bakeMode;
                    tr.includeAlpha = sr.includeAlpha;
                    tr.resetSourceAfterBake = sr.resetSourceAfterBake;

                    tr.targetTexProp = sr.targetTexProp;

                    if (tg.shaderTexProps != null && tg.shaderTexProps.Count > 0)
                    {
                        var maxTex = Mathf.Max(0, tg.shaderTexProps.Count - 1);
                        tr.targetTexIndex = Mathf.Clamp((int)sr.targetTexIndex, 0, maxTex);
                        tr.targetTexProp = tg.shaderTexProps[tr.targetTexIndex];
                    }
                    else
                    {
                        tr.targetTexIndex = 0;
                        tr.targetTexProp = null;
                    }

                    tr.modOp = sr.modOp;
                    tr.modClamp01 = sr.modClamp01;
                    tr.modScale = sr.modScale;
                    tr.modBias = sr.modBias;
                    tr.modAffectsAlpha = sr.modAffectsAlpha;

                    tr.modProp = sr.modProp;

                    if (tg.shaderScalarProps != null && tg.shaderScalarProps.Count > 0)
                    {
                        var maxSca = Mathf.Max(0, tg.shaderScalarProps.Count - 1);
                        tr.modPropIndex = Mathf.Clamp((int)sr.modPropIndex, 0, maxSca);
                        tr.modProp = tg.shaderScalarProps[tr.modPropIndex];
                    }
                    else
                    {
                        tr.modPropIndex = 0;
                        tr.modProp = null;
                    }

                    tr.expanded = sr.expanded;
                }
            }
        }

        void BuildGroup(GroupScan g, KibaMultiAtlasMergerLog log, int cell, int content)
        {
            string shaderFolder = Sanitize(g.key.shader ? g.key.shader.name : "NullShader");
            string groupFolder = Path.Combine(outputFolder, g.tag, shaderFolder).Replace("\\", "/");
            Directory.CreateDirectory(groupFolder);

            var mats = g.mats.ToList();
            int tilesPerPage = g.tilesPerPage;

            var texMeta = g.rows.Where(r => r.type == ShaderUtil.ShaderPropertyType.TexEnv).ToDictionary(r => r.name, r => r, StringComparer.Ordinal);

            var enabledTexProps = g.rows.Where(r => r.type == ShaderUtil.ShaderPropertyType.TexEnv && r.doAction).Select(r => r.name).ToList();

            var bakeRows = g.rows.Where(r =>
                r.doAction &&
                r.type != ShaderUtil.ShaderPropertyType.TexEnv &&
                (r.bakeMode == BakeMode.색상굽기_텍스처타일 || r.bakeMode == BakeMode.스칼라굽기_그레이타일 || r.bakeMode == BakeMode.색상곱_텍스처타일)).ToList();

            var resetRows = g.rows.Where(r =>
                r.doAction &&
                r.type != ShaderUtil.ShaderPropertyType.TexEnv &&
                r.bakeMode == BakeMode.리셋_쉐이더기본값).ToList();

            var allAtlasProps = new HashSet<string>(enabledTexProps, StringComparer.Ordinal);
            foreach (var r in bakeRows)
            {
                if (string.IsNullOrEmpty(r.targetTexProp)) continue;
                allAtlasProps.Add(r.targetTexProp);
            }

            for (int page = 0; page < g.pageCount; page++)
            {
                int start = page * tilesPerPage;
                int end = Mathf.Min(mats.Count, start + tilesPerPage);
                var pageItems = mats.GetRange(start, end - start);

                string pageFolder = Path.Combine(groupFolder, $"Page_{page:00}").Replace("\\", "/");
                Directory.CreateDirectory(pageFolder);

                var atlasByProp = new Dictionary<string, Texture2D>(StringComparer.Ordinal);

                foreach (var prop in allAtlasProps)
                {
                    bool normalLike = texMeta.TryGetValue(prop, out var meta) ? meta.isNormalLike : (prop.IndexOf("Bump", StringComparison.OrdinalIgnoreCase) >= 0 || prop.IndexOf("Normal", StringComparison.OrdinalIgnoreCase) >= 0);
                    bool sRGB = texMeta.TryGetValue(prop, out meta) ? meta.isSRGB : !HeuristicLinearTex(prop);
                    if (normalLike) sRGB = false;
                    atlasByProp[prop] = NewAtlasTex(atlasSize, sRGB);
                }

                for (int i = 0; i < pageItems.Count; i++)
                {
                    int gx = i % grid;
                    int gy = i / grid;

                    int px = gx * cell + paddingPx;
                    int py = gy * cell + paddingPx;

                    var mi = pageItems[i];
                    var mat = mi.mat;

                    foreach (var prop in allAtlasProps)
                    {
                        var atlas = atlasByProp[prop];

                        var solidColorRules = bakeRows.Where(d =>
                            d.bakeMode == BakeMode.색상굽기_텍스처타일 &&
                            d.type == ShaderUtil.ShaderPropertyType.Color &&
                            d.targetTexProp == prop).ToList();

                        var solidScalarRules = bakeRows.Where(d =>
                            d.bakeMode == BakeMode.스칼라굽기_그레이타일 &&
                            (d.type == ShaderUtil.ShaderPropertyType.Float || d.type == ShaderUtil.ShaderPropertyType.Range) &&
                            d.targetTexProp == prop).ToList();

                        if (solidColorRules.Count > 0)
                        {
                            var rule = solidColorRules[0];
                            Color c = Color.white;
                            if (mat && mat.HasProperty(rule.name)) c = mat.GetColor(rule.name);
                            if (!rule.includeAlpha) c.a = 1f;
                            float m = EvalMod(mat, rule);
                            c = ApplyModToColor(c, m, rule);
                            var solid = SolidPixels(content, content, c);
                            PutWithPadding(atlas, px, py, content, content, solid, paddingPx);
                            continue;
                        }

                        if (solidScalarRules.Count > 0)
                        {
                            var rule = solidScalarRules[0];
                            float v = 1f;
                            if (mat && mat.HasProperty(rule.name)) v = mat.GetFloat(rule.name);
                            float m = EvalMod(mat, rule);
                            v = ApplyModToScalar(v, m, rule);
                            v = Mathf.Clamp01(v);
                            var solid = SolidPixels(content, content, new Color(v, v, v, 1f));
                            PutWithPadding(atlas, px, py, content, content, solid, paddingPx);
                            continue;
                        }

                        Texture2D src = (mat && mat.HasProperty(prop)) ? (mat.GetTexture(prop) as Texture2D) : null;
                        Vector4 st = GetST(mat, prop);

                        bool normalLike = texMeta.TryGetValue(prop, out var meta) ? meta.isNormalLike : (prop.IndexOf("Bump", StringComparison.OrdinalIgnoreCase) >= 0 || prop.IndexOf("Normal", StringComparison.OrdinalIgnoreCase) >= 0);
                        bool sRGB = texMeta.TryGetValue(prop, out meta) ? meta.isSRGB : !HeuristicLinearTex(prop);
                        if (normalLike) sRGB = false;

                        var pixels = SampleWithST(src, st, content, content, sRGB, normalLike);

                        foreach (var mulRule in bakeRows.Where(d =>
                                     d.bakeMode == BakeMode.색상곱_텍스처타일 &&
                                     d.type == ShaderUtil.ShaderPropertyType.Color &&
                                     d.targetTexProp == prop))
                        {
                            if (mat && mat.HasProperty(mulRule.name))
                            {
                                Color mul = mat.GetColor(mulRule.name);
                                if (!mulRule.includeAlpha) mul.a = 1f;
                                float mm = EvalMod(mat, mulRule);
                                mul = ApplyModToColor(mul, mm, mulRule);
                                MultiplyPixels(pixels, mul);
                            }
                        }

                        PutWithPadding(atlas, px, py, content, content, pixels, paddingPx);
                    }
                }

                foreach (var kv in atlasByProp)
                {
                    kv.Value.Apply(true, false);
                    string p = SaveAtlasPNG(kv.Value, pageFolder, $"{Sanitize(kv.Key)}.png");
                    log.createdAssetPaths.Add(p);

                    bool normalLike = texMeta.TryGetValue(kv.Key, out var meta) ? meta.isNormalLike : (kv.Key.IndexOf("Bump", StringComparison.OrdinalIgnoreCase) >= 0 || kv.Key.IndexOf("Normal", StringComparison.OrdinalIgnoreCase) >= 0);
                    bool sRGB = texMeta.TryGetValue(kv.Key, out meta) ? meta.isSRGB : !HeuristicLinearTex(kv.Key);
                    if (normalLike) ConfigureImporterNormal(p, atlasSize);
                    else ConfigureImporter(p, atlasSize, sRGB);
                }

                var template = pageItems[0].mat;
                var merged = new Material(template);

                foreach (var prop in allAtlasProps)
                {
                    var atlasPath = Path.Combine(pageFolder, $"{Sanitize(prop)}.png").Replace("\\", "/");
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
                    if (tex && merged.HasProperty(prop)) merged.SetTexture(prop, tex);

                    string stName = prop + "_ST";
                    if (merged.HasProperty(stName)) merged.SetVector(stName, new Vector4(1, 1, 0, 0));
                }

                var defMat = GetDefaultMaterial(g.key.shader);

                foreach (var r in resetRows)
                {
                    if (!merged || !merged.HasProperty(r.name) || !defMat) continue;

                    if (r.type == ShaderUtil.ShaderPropertyType.Color)
                        merged.SetColor(r.name, defMat.GetColor(r.name));
                    else if (r.type == ShaderUtil.ShaderPropertyType.Vector)
                        merged.SetVector(r.name, defMat.GetVector(r.name));
                    else if (r.type == ShaderUtil.ShaderPropertyType.Float || r.type == ShaderUtil.ShaderPropertyType.Range)
                        merged.SetFloat(r.name, defMat.GetFloat(r.name));
                }

                foreach (var r in g.rows)
                {
                    if (!r.doAction) continue;
                    if (!r.resetSourceAfterBake) continue;
                    if (!merged || !merged.HasProperty(r.name) || !defMat) continue;

                    bool bakedOrMul =
                        r.bakeMode == BakeMode.색상굽기_텍스처타일 ||
                        r.bakeMode == BakeMode.스칼라굽기_그레이타일 ||
                        r.bakeMode == BakeMode.색상곱_텍스처타일;

                    if (!bakedOrMul) continue;

                    if (r.type == ShaderUtil.ShaderPropertyType.Color)
                        merged.SetColor(r.name, defMat.GetColor(r.name));
                    else if (r.type == ShaderUtil.ShaderPropertyType.Vector)
                        merged.SetVector(r.name, defMat.GetVector(r.name));
                    else if (r.type == ShaderUtil.ShaderPropertyType.Float || r.type == ShaderUtil.ShaderPropertyType.Range)
                        merged.SetFloat(r.name, defMat.GetFloat(r.name));
                }

                string matPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(pageFolder, "Merged.mat").Replace("\\", "/"));
                AssetDatabase.CreateAsset(merged, matPath);
                log.createdAssetPaths.Add(matPath);

                ApplyToRenderers(g, pageItems, merged, log, cell, content);
            }
        }

        void ApplyToRenderers(GroupScan g, List<MatInfo> pageItems, Material mergedMat, KibaMultiAtlasMergerLog log, int cell, int content)
        {
            int atlasSizePx = atlasSize;
            float tileScale = content / (float)atlasSizePx;

            var matToIndex = new Dictionary<Material, int>();
            for (int i = 0; i < pageItems.Count; i++) matToIndex[pageItems[i].mat] = i;

            var meshCache = new Dictionary<(Mesh, int), Mesh>();

            foreach (var mi in pageItems)
            {
                foreach (var r in mi.users)
                {
                    if (!r) continue;

                    var shared = r.sharedMaterials;
                    if (shared == null || shared.Length == 0) continue;

                    int hitCount = 0;
                    int tileIndex = -1;

                    for (int s = 0; s < shared.Length; s++)
                    {
                        if (shared[s] && matToIndex.TryGetValue(shared[s], out var idx))
                        {
                            hitCount++;
                            if (tileIndex < 0) tileIndex = idx;
                            else if (tileIndex != idx)
                            {
                                tileIndex = -2;
                                break;
                            }
                        }
                    }

                    if (tileIndex < 0) continue;
                    if (tileIndex == -2) continue;
                    if (hitCount > 1) continue;

                    var beforeMats = shared.ToArray();
                    bool replaced = false;

                    Undo.RecordObject(r, "머지 머티리얼 적용");
                    for (int s = 0; s < shared.Length; s++)
                    {
                        if (shared[s] == mi.mat)
                        {
                            shared[s] = mergedMat;
                            replaced = true;
                        }
                    }

                    if (!replaced) continue;
                    r.sharedMaterials = shared;

                    int gx = tileIndex % grid;
                    int gy = tileIndex / grid;
                    float ox = (gx * cell + paddingPx) / (float)atlasSizePx;
                    float oy = (gy * cell + paddingPx) / (float)atlasSizePx;

                    var scale = new Vector2(tileScale, tileScale);
                    var offset = new Vector2(ox, oy);

                    Mesh beforeMesh = null;
                    Mesh afterMesh = null;

                    if (r is SkinnedMeshRenderer smr)
                    {
                        beforeMesh = smr.sharedMesh;
                        if (beforeMesh)
                        {
                            Undo.RecordObject(smr, "메쉬 UV 리맵");
                            afterMesh = GetOrCreateRemappedMesh(beforeMesh, tileIndex, scale, offset, meshCache);
                            smr.sharedMesh = afterMesh;
                        }
                    }
                    else
                    {
                        var mf = r.GetComponent<MeshFilter>();
                        if (mf && mf.sharedMesh)
                        {
                            beforeMesh = mf.sharedMesh;
                            Undo.RecordObject(mf, "메쉬 UV 리맵");
                            afterMesh = GetOrCreateRemappedMesh(beforeMesh, tileIndex, scale, offset, meshCache);
                            mf.sharedMesh = afterMesh;
                        }
                    }

                    var entry = new KibaMultiAtlasMergerLog.Entry();
                    entry.rendererGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(r).ToString();
                    entry.beforeMaterials = beforeMats;
                    entry.afterMaterials = r.sharedMaterials.ToArray();
                    entry.beforeMesh = beforeMesh;
                    entry.afterMesh = afterMesh;
                    log.entries.Add(entry);
                }
            }
        }

        Mesh GetOrCreateRemappedMesh(Mesh src, int tileIndex, Vector2 scale, Vector2 offset, Dictionary<(Mesh, int), Mesh> cache)
        {
            var key = (src, tileIndex);
            if (cache.TryGetValue(key, out var cached) && cached) return cached;

            var dst = Instantiate(src);
            dst.name = src.name + $"_Atlas_{tileIndex:D3}";
            var uv = dst.uv;
            for (int i = 0; i < uv.Length; i++)
                uv[i] = new Vector2(uv[i].x * scale.x + offset.x, uv[i].y * scale.y + offset.y);
            dst.uv = uv;

            string meshFolder = Path.Combine(outputFolder, "_Meshes").Replace("\\", "/");
            Directory.CreateDirectory(meshFolder);
            string meshPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(meshFolder, dst.name + ".asset").Replace("\\", "/"));
            AssetDatabase.CreateAsset(dst, meshPath);

            cache[key] = dst;
            return dst;
        }

        Texture2D NewAtlasTex(int size, bool sRGB)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true, !sRGB);
            t.wrapMode = TextureWrapMode.Clamp;
            var fill = new Color32[size * size];
            t.SetPixels32(fill);
            return t;
        }

        Color32[] SampleWithST(Texture2D src, Vector4 st, int w, int h, bool sRGB, bool normalLike)
        {
            if (!src)
            {
                if (normalLike) return SolidPixels(w, h, new Color(0.5f, 0.5f, 1f, 1f));
                return SolidPixels(w, h, Color.white);
            }

            blitMat.SetTexture("_MainTex", src);
            blitMat.SetVector("_ScaleOffset", st);

            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear);
            Graphics.Blit(src, rt, blitMat);

            var prev = RenderTexture.active;
            RenderTexture.active = rt;

            var tmp = new Texture2D(w, h, TextureFormat.RGBA32, false, !sRGB);
            tmp.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
            tmp.Apply(false, false);

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            var px = tmp.GetPixels32();
            DestroyImmediate(tmp);
            return px;
        }

        Color32[] SolidPixels(int w, int h, Color c)
        {
            var arr = new Color32[w * h];
            var cc = (Color32)c;
            for (int i = 0; i < arr.Length; i++) arr[i] = cc;
            return arr;
        }

        void MultiplyPixels(Color32[] pixels, Color mul)
        {
            byte mr = (byte)Mathf.Clamp(Mathf.RoundToInt(mul.r * 255f), 0, 255);
            byte mg = (byte)Mathf.Clamp(Mathf.RoundToInt(mul.g * 255f), 0, 255);
            byte mb = (byte)Mathf.Clamp(Mathf.RoundToInt(mul.b * 255f), 0, 255);
            byte ma = (byte)Mathf.Clamp(Mathf.RoundToInt(mul.a * 255f), 0, 255);

            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                p.r = (byte)((p.r * mr) / 255);
                p.g = (byte)((p.g * mg) / 255);
                p.b = (byte)((p.b * mb) / 255);
                p.a = (byte)((p.a * ma) / 255);
                pixels[i] = p;
            }
        }

        void PutWithPadding(Texture2D atlas, int x, int y, int w, int h, Color32[] contentPixels, int pad)
        {
            atlas.SetPixels32(x, y, w, h, contentPixels);
            if (pad <= 0) return;

            var leftCol = new Color32[h];
            var rightCol = new Color32[h];

            for (int yy = 0; yy < h; yy++)
            {
                leftCol[yy] = contentPixels[yy * w + 0];
                rightCol[yy] = contentPixels[yy * w + (w - 1)];
            }

            for (int p = 1; p <= pad; p++)
            {
                int dx = x - p;
                if (dx >= 0) atlas.SetPixels32(dx, y, 1, h, leftCol);
            }

            for (int p = 0; p < pad; p++)
            {
                int dx = x + w + p;
                if (dx < atlas.width) atlas.SetPixels32(dx, y, 1, h, rightCol);
            }

            var bottomRow = new Color32[w];
            var topRow = new Color32[w];
            Array.Copy(contentPixels, 0, bottomRow, 0, w);
            Array.Copy(contentPixels, (h - 1) * w, topRow, 0, w);

            for (int p = 1; p <= pad; p++)
            {
                int dy = y - p;
                if (dy >= 0) atlas.SetPixels32(x, dy, w, 1, bottomRow);
            }

            for (int p = 0; p < pad; p++)
            {
                int dy = y + h + p;
                if (dy < atlas.height) atlas.SetPixels32(x, dy, w, 1, topRow);
            }

            var bl = contentPixels[0];
            var br = contentPixels[w - 1];
            var tl = contentPixels[(h - 1) * w];
            var tr = contentPixels[(h - 1) * w + (w - 1)];

            for (int py = 1; py <= pad; py++)
            for (int px = 1; px <= pad; px++)
            {
                int dx = x - px;
                int dy = y - py;
                if (dx >= 0 && dy >= 0) atlas.SetPixel(dx, dy, bl);

                dx = x + w - 1 + px;
                dy = y - py;
                if (dx < atlas.width && dy >= 0) atlas.SetPixel(dx, dy, br);

                dx = x - px;
                dy = y + h - 1 + py;
                if (dx >= 0 && dy < atlas.height) atlas.SetPixel(dx, dy, tl);

                dx = x + w - 1 + px;
                dy = y + h - 1 + py;
                if (dx < atlas.width && dy < atlas.height) atlas.SetPixel(dx, dy, tr);
            }
        }

        string SaveAtlasPNG(Texture2D atlas, string folder, string fileName)
        {
            string path = Path.Combine(folder, fileName).Replace("\\", "/");
            File.WriteAllBytes(path, atlas.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return path;
        }

        void ConfigureImporter(string assetPath, int maxSize, bool sRGB)
        {
            var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (!ti) return;
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = sRGB;
            ti.mipmapEnabled = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.maxTextureSize = maxSize;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
        }

        void ConfigureImporterNormal(string assetPath, int maxSize)
        {
            var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (!ti) return;
            ti.textureType = TextureImporterType.NormalMap;
            ti.sRGBTexture = false;
            ti.mipmapEnabled = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.maxTextureSize = maxSize;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
        }

        string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "X";
            s = s.Replace("/", "_").Replace("\\", "_").Replace(":", "_");
            var arr = s.ToCharArray();
            for (int i = 0; i < arr.Length; i++)
            {
                char c = arr[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-';
                if (!ok) arr[i] = '_';
            }

            s = new string(arr);
            if (s.Length > 90) s = s.Substring(0, 90);
            return s;
        }
    }
}
#endif
