#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor.Services
{
    public class TextureProcessor : ITextureProcessor
    {
        private Material blitMat;
        private readonly Dictionary<int, Material> defaultMatCache = new Dictionary<int, Material>();

        public Material BlitMaterial => blitMat;

        public void EnsureBlitMaterial(string blitShaderPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(blitShaderPath));
            if (!File.Exists(blitShaderPath))
            {
                File.WriteAllText(blitShaderPath, GetBlitShaderSource());
                AssetDatabase.ImportAsset(blitShaderPath, ImportAssetOptions.ForceUpdate);
            }

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(blitShaderPath);
            if (!shader) return;
            if (!blitMat) blitMat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        public Color32[] SampleWithScaleTiling(Texture2D source, Vector4 st, int width, int height, bool sRGB, bool normalLike)
        {
            if (!source)
            {
                if (normalLike) return CreateSolidPixels(width, height, new Color(0.5f, 0.5f, 1f, 1f));
                return CreateSolidPixels(width, height, Color.white);
            }

            // Blit 머티리얼이 초기화되지 않은 경우 방어 코드
            if (!blitMat)
            {
                Debug.LogError("BlitMaterial is not initialized. Call EnsureBlitMaterial first.");
                if (normalLike) return CreateSolidPixels(width, height, new Color(0.5f, 0.5f, 1f, 1f));
                return CreateSolidPixels(width, height, Color.white);
            }

            blitMat.SetTexture("_MainTex", source);
            blitMat.SetVector("_ScaleOffset", st);

            var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear);
            Graphics.Blit(source, rt, blitMat);

            var prev = RenderTexture.active;
            RenderTexture.active = rt;

            var tmp = new Texture2D(width, height, TextureFormat.RGBA32, false, !sRGB);
            tmp.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            tmp.Apply(false, false);

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            var pixels = tmp.GetPixels32();
            Object.DestroyImmediate(tmp);
            return pixels;
        }

        public Color32[] CreateSolidPixels(int width, int height, Color color)
        {
            var arr = new Color32[width * height];
            var cc = (Color32)color;
            for (int i = 0; i < arr.Length; i++) arr[i] = cc;
            return arr;
        }

        public void MultiplyPixels(Color32[] pixels, Color multiplier)
        {
            byte mr = (byte)Mathf.Clamp(Mathf.RoundToInt(multiplier.r * 255f), 0, 255);
            byte mg = (byte)Mathf.Clamp(Mathf.RoundToInt(multiplier.g * 255f), 0, 255);
            byte mb = (byte)Mathf.Clamp(Mathf.RoundToInt(multiplier.b * 255f), 0, 255);
            byte ma = (byte)Mathf.Clamp(Mathf.RoundToInt(multiplier.a * 255f), 0, 255);

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

        public Color ApplyModifierToColor(Color color, float modValue, Row row)
        {
            if (row.modOp == ModOp.없음) return color;

            if (row.modOp == ModOp.곱셈)
            {
                color.r *= modValue;
                color.g *= modValue;
                color.b *= modValue;
                if (row.modAffectsAlpha) color.a *= modValue;
            }
            else if (row.modOp == ModOp.가산)
            {
                color.r += modValue;
                color.g += modValue;
                color.b += modValue;
                if (row.modAffectsAlpha) color.a += modValue;
            }
            else if (row.modOp == ModOp.감산)
            {
                color.r -= modValue;
                color.g -= modValue;
                color.b -= modValue;
                if (row.modAffectsAlpha) color.a -= modValue;
            }

            color.r = Mathf.Clamp01(color.r);
            color.g = Mathf.Clamp01(color.g);
            color.b = Mathf.Clamp01(color.b);
            color.a = Mathf.Clamp01(color.a);
            return color;
        }

        public float ApplyModifierToScalar(float value, float modValue, Row row)
        {
            if (row.modOp == ModOp.없음) return value;
            if (row.modOp == ModOp.곱셈) value *= modValue;
            else if (row.modOp == ModOp.가산) value += modValue;
            else if (row.modOp == ModOp.감산) value -= modValue;
            return value;
        }

        public float EvaluateModifier(Material material, Row row)
        {
            if (row.modOp == ModOp.없음) return 0f;
            float defaultValue = (row.modOp == ModOp.곱셈) ? 1f : 0f;
            float value = defaultValue;

            if (material && !string.IsNullOrEmpty(row.modProp) && material.HasProperty(row.modProp))
                value = material.GetFloat(row.modProp);

            value = value * row.modScale + row.modBias;
            if (row.modClamp01) value = Mathf.Clamp01(value);
            return value;
        }

        public Material GetDefaultMaterial(Shader shader)
        {
            if (!shader) return null;
            int id = shader.GetInstanceID();
            if (defaultMatCache.TryGetValue(id, out var material) && material) return material;
            var newMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            defaultMatCache[id] = newMaterial;
            return newMaterial;
        }

        public void Cleanup()
        {
            if (blitMat) Object.DestroyImmediate(blitMat);
            foreach (var kv in defaultMatCache)
                if (kv.Value)
                    Object.DestroyImmediate(kv.Value);
            defaultMatCache.Clear();
        }

        private string GetBlitShaderSource()
        {
            return @"Shader ""Hidden/KibaAtlasBlit""
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
    }
}
#endif
