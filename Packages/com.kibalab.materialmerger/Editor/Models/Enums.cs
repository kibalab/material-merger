#if UNITY_EDITOR
namespace K13A.MaterialMerger.Editor.Models
{
    /// <summary>
    /// Policy for handling unresolved property differences between materials
    /// </summary>
    public enum DiffPolicy
    {
        /// <summary>Stop build if unresolved differences exist</summary>
        StopIfUnresolved,
        /// <summary>Use values from the first material</summary>
        UseFirstMaterial,
        /// <summary>Use values from a sample material</summary>
        UseSampleMaterial
    }

    /// <summary>
    /// How to process a property during atlas baking
    /// </summary>
    public enum BakeMode
    {
        /// <summary>Keep the original value (no action)</summary>
        Keep,
        /// <summary>Reset to shader default value</summary>
        ResetToDefault,
        /// <summary>Bake color into texture tile</summary>
        BakeColorToTexture,
        /// <summary>Bake scalar to grayscale tile</summary>
        BakeScalarToGrayscale,
        /// <summary>Multiply color with existing texture</summary>
        MultiplyColorWithTexture
    }

    /// <summary>
    /// Modifier operation for property baking
    /// </summary>
    public enum ModOp
    {
        /// <summary>No modification</summary>
        None,
        /// <summary>Multiply values</summary>
        Multiply,
        /// <summary>Add values</summary>
        Add,
        /// <summary>Subtract values</summary>
        Subtract
    }
}
#endif
