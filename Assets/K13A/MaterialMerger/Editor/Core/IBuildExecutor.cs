#if UNITY_EDITOR
namespace K13A.MaterialMerger.Editor.Core
{
    /// <summary>
    /// Interface for executing build operations.
    /// Used to avoid dynamic typing in ConfirmWindow, preventing potential memory leaks
    /// and improving type safety.
    /// </summary>
    public interface IBuildExecutor
    {
        /// <summary>
        /// Execute the build and apply operation
        /// </summary>
        void BuildAndApply();
    }
}
#endif
