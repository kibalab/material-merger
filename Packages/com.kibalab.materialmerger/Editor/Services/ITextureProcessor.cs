#if UNITY_EDITOR
using UnityEngine;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor.Services
{
    public interface ITextureProcessor
    {
        /// <summary>
        /// Blit 머티리얼
        /// </summary>
        Material BlitMaterial { get; }

        /// <summary>
        /// Blit 셰이더 및 머티리얼 초기화
        /// </summary>
        void EnsureBlitMaterial(string blitShaderPath);

        /// <summary>
        /// ST(Scale/Tiling) 변환 포함하여 텍스처 샘플링
        /// </summary>
        Color32[] SampleWithScaleTiling(Texture2D source, Vector4 st, int width, int height, bool sRGB, bool normalLike);

        /// <summary>
        /// 단색 픽셀 배열 생성
        /// </summary>
        Color32[] CreateSolidPixels(int width, int height, Color color);

        /// <summary>
        /// 픽셀 배열에 색상 곱셈 적용
        /// </summary>
        void MultiplyPixels(Color32[] pixels, Color multiplier);

        /// <summary>
        /// Row 설정에 따라 색상에 모디파이어 적용
        /// </summary>
        Color ApplyModifierToColor(Color color, float modValue, Row row);

        /// <summary>
        /// Row 설정에 따라 스칼라에 모디파이어 적용
        /// </summary>
        float ApplyModifierToScalar(float value, float modValue, Row row);

        /// <summary>
        /// 머티리얼과 Row로부터 모디파이어 값 평가
        /// </summary>
        float EvaluateModifier(Material material, Row row);

        /// <summary>
        /// 기본 머티리얼 가져오기 (캐싱)
        /// </summary>
        Material GetDefaultMaterial(Shader shader);

        /// <summary>
        /// 정리 (임시 머티리얼 등)
        /// </summary>
        void Cleanup();
    }
}
#endif
