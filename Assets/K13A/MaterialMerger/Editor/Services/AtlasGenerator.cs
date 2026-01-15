#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace K13A.MaterialMerger.Editor.Services
{
    public class AtlasGenerator : IAtlasGenerator
    {
        public Texture2D CreateAtlas(int size, bool sRGB)
        {
            return CreateAtlas(size, size, sRGB);
        }

        public Texture2D CreateAtlas(int width, int height, bool sRGB)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true, !sRGB);
            texture.wrapMode = TextureWrapMode.Clamp;
            var fill = new Color32[width * height];
            texture.SetPixels32(fill);
            return texture;
        }

        public void PutTileWithPadding(Texture2D atlas, int x, int y, int width, int height, Color32[] contentPixels, int padding)
        {
            atlas.SetPixels32(x, y, width, height, contentPixels);
            if (padding <= 0) return;

            // 좌우 경계 확장
            var leftColumn = new Color32[height];
            var rightColumn = new Color32[height];

            for (int yy = 0; yy < height; yy++)
            {
                leftColumn[yy] = contentPixels[yy * width + 0];
                rightColumn[yy] = contentPixels[yy * width + (width - 1)];
            }

            for (int p = 1; p <= padding; p++)
            {
                int dx = x - p;
                if (dx >= 0) atlas.SetPixels32(dx, y, 1, height, leftColumn);
            }

            for (int p = 0; p < padding; p++)
            {
                int dx = x + width + p;
                if (dx < atlas.width) atlas.SetPixels32(dx, y, 1, height, rightColumn);
            }

            // 상하 경계 확장
            var bottomRow = new Color32[width];
            var topRow = new Color32[width];
            Array.Copy(contentPixels, 0, bottomRow, 0, width);
            Array.Copy(contentPixels, (height - 1) * width, topRow, 0, width);

            for (int p = 1; p <= padding; p++)
            {
                int dy = y - p;
                if (dy >= 0) atlas.SetPixels32(x, dy, width, 1, bottomRow);
            }

            for (int p = 0; p < padding; p++)
            {
                int dy = y + height + p;
                if (dy < atlas.height) atlas.SetPixels32(x, dy, width, 1, topRow);
            }

            // 코너 확장
            var bottomLeft = contentPixels[0];
            var bottomRight = contentPixels[width - 1];
            var topLeft = contentPixels[(height - 1) * width];
            var topRight = contentPixels[(height - 1) * width + (width - 1)];

            for (int py = 1; py <= padding; py++)
            for (int px = 1; px <= padding; px++)
            {
                int dx = x - px;
                int dy = y - py;
                if (dx >= 0 && dy >= 0) atlas.SetPixel(dx, dy, bottomLeft);

                dx = x + width - 1 + px;
                dy = y - py;
                if (dx < atlas.width && dy >= 0) atlas.SetPixel(dx, dy, bottomRight);

                dx = x - px;
                dy = y + height - 1 + py;
                if (dx >= 0 && dy < atlas.height) atlas.SetPixel(dx, dy, topLeft);

                dx = x + width - 1 + px;
                dy = y + height - 1 + py;
                if (dx < atlas.width && dy < atlas.height) atlas.SetPixel(dx, dy, topRight);
            }
        }

        public string SaveAtlasPNG(Texture2D atlas, string folder, string fileName)
        {
            string path = Path.Combine(folder, fileName).Replace("\\", "/");
            File.WriteAllBytes(path, atlas.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return path;
        }

        public void ConfigureImporter(string assetPath, int maxSize, bool sRGB, bool isNormalMap)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (!importer) return;

            importer.textureType = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = sRGB;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        public string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "X";
            fileName = fileName.Replace("/", "_").Replace("\\", "_").Replace(":", "_");
            var arr = fileName.ToCharArray();
            for (int i = 0; i < arr.Length; i++)
            {
                char c = arr[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-';
                if (!ok) arr[i] = '_';
            }

            fileName = new string(arr);
            if (fileName.Length > 90) fileName = fileName.Substring(0, 90);
            return fileName;
        }
    }
}
#endif
