#if UNITY_EDITOR
namespace K13A.MaterialMerger.Editor.Models
{
    public enum DiffPolicy
    {
        미해결이면중단,
        첫번째기준으로진행,
        샘플머테리얼기준으로진행
    }

    public enum BakeMode
    {
        유지,
        리셋_쉐이더기본값,
        색상굽기_텍스처타일,
        스칼라굽기_그레이타일,
        색상곱_텍스처타일
    }

    public enum ModOp
    {
        없음,
        곱셈,
        가산,
        감산
    }
}
#endif
