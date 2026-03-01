#if UNITY_EDITOR
using UnityEngine;

namespace K13A.MaterialMerger.Editor.Services
{
    public interface IAtlasGenerator
    {
        /// <summary>
        /// 새 아틀라스 텍스처 생성 (정사각형)
        /// </summary>
        Texture2D CreateAtlas(int size, bool sRGB);

        /// <summary>
        /// 새 아틀라스 텍스처 생성 (직사각형)
        /// </summary>
        Texture2D CreateAtlas(int width, int height, bool sRGB);

        /// <summary>
        /// 아틀라스에 타일 배치 (패딩 포함)
        /// </summary>
        void PutTileWithPadding(Texture2D atlas, int x, int y, int width, int height, Color32[] contentPixels, int padding);

        /// <summary>
        /// 아틀라스를 PNG로 저장
        /// </summary>
        string SaveAtlasPNG(Texture2D atlas, string folder, string fileName);

        /// <summary>
        /// 텍스처 임포터 설정
        /// </summary>
        void ConfigureImporter(string assetPath, int maxSize, bool sRGB, bool isNormalMap);

        /// <summary>
        /// 파일명으로 사용 가능한 문자열로 변환
        /// </summary>
        string SanitizeFileName(string fileName);
    }
}
#endif
