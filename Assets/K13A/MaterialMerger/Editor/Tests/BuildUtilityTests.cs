#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Models;
using K13A.MaterialMerger.Editor.Services;

namespace K13A.MaterialMerger.Editor.Tests
{
    public class BuildUtilityTests
    {
        [Test]
        public void ValidateBuildSettings_RequiresSampleMaterialForPolicy()
        {
            var settings = new BuildSettings(
                null,
                Constants.DefaultOutputFolder,
                false,
                false,
                false,
                Constants.DefaultAtlasSize,
                Constants.DefaultGrid,
                Constants.DefaultPaddingPx,
                true,
                true,
                true,
                DiffPolicy.UseSampleMaterial,
                null);

            var (isValid, errorMessage) = BuildUtility.ValidateBuildSettings(settings);

            Assert.IsFalse(isValid);
            Assert.IsTrue(errorMessage.Contains("Sample material"));
        }

        [Test]
        public void ValidateBuildSettings_RequiresAssetsOutputFolder()
        {
            var settings = new BuildSettings(
                null,
                "C:/Temp",
                false,
                false,
                false,
                Constants.DefaultAtlasSize,
                Constants.DefaultGrid,
                Constants.DefaultPaddingPx,
                true,
                true,
                true,
                DiffPolicy.StopIfUnresolved,
                null);

            var (isValid, errorMessage) = BuildUtility.ValidateBuildSettings(settings);

            Assert.IsFalse(isValid);
            Assert.IsTrue(errorMessage.Contains("Assets"));
        }

        [Test]
        public void BuildSettings_UsesSafeGridForCalculatedValues()
        {
            var settings = new BuildSettings(
                null,
                Constants.DefaultOutputFolder,
                false,
                false,
                false,
                Constants.DefaultAtlasSize,
                0,
                Constants.DefaultPaddingPx,
                true,
                true,
                true,
                DiffPolicy.StopIfUnresolved,
                null);

            Assert.AreEqual(Constants.DefaultAtlasSize, settings.CellSize);
            Assert.AreEqual(1, settings.TilesPerPage);
            Assert.Greater(settings.ContentSize, 0);
        }
    }
}
#endif
