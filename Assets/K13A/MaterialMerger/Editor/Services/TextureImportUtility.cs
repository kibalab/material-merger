#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor.Services
{
    public static class TextureImportUtility
    {
        public static void ApplySettings(IEnumerable<TextureImportRequest> requests)
        {
            if (requests == null) return;
            foreach (var request in requests)
                ApplySettings(request);
        }

        public static bool ApplySettings(TextureImportRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.assetPath)) return false;

            var importer = AssetImporter.GetAtPath(request.assetPath) as TextureImporter;
            if (!importer)
            {
                AssetDatabase.ImportAsset(request.assetPath, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(request.assetPath) as TextureImporter;
                if (!importer) return false;
            }
            bool dirty = false;
            var desiredType = request.isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;

            if (importer.textureType != desiredType)
            {
                importer.textureType = desiredType;
                dirty = true;
            }

            if (importer.sRGBTexture != request.sRGB)
            {
                importer.sRGBTexture = request.sRGB;
                dirty = true;
            }

            if (!importer.mipmapEnabled)
            {
                importer.mipmapEnabled = true;
                dirty = true;
            }

            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                dirty = true;
            }

            if (importer.maxTextureSize != request.maxSize)
            {
                importer.maxTextureSize = request.maxSize;
                dirty = true;
            }

            if (importer.textureCompression != TextureImporterCompression.CompressedHQ)
            {
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                dirty = true;
            }

            if (dirty)
                importer.SaveAndReimport();

            return dirty;
        }
    }
}
#endif
