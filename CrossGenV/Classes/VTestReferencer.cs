using System.IO;
using System.Linq;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal.ObjectInfo;

namespace CrossGenV.Classes
{
    /// <summary>
    /// Methods for the wavelist packages
    /// </summary>
    public static class VTestReferencer
    {
        public static void EnsureReferencesInDecooked(VTestOptions options)
        {
            var sfSimulator = Path.Combine(VTestPaths.VTest_FinalDestDir, "DecookedAssets");
            var packages = Directory.GetFiles(sfSimulator, "*.pcc");
            foreach (var packageF in packages)
            {
                var package = MEPackageHandler.OpenMEPackage(packageF);
                options.SetStatusText($"Ensuring references in {package.FileNameNoExtension}");
                // Seek Free file
                // BioPawnChallengeScaledTypes
                // Classes for dynamic load
                // GFxMovieInfo for faction images
                VTestUtility.EnsureReferenced(package, options.cache, package.Exports.Where(x => x.IsA("MaterialInstanceConstant") || x.ClassName is "SkeletalMesh"));
                if (package.IsModified)
                {
                    package.Save();
                }
            }
        }
        public static void EnsureReferencesInWavelists(VTestOptions options)
        {
            var sfSimulator = Path.Combine(VTestPaths.VTest_FinalDestDir, "SFSimulator");
            var packages = Directory.GetFiles(sfSimulator, "*.pcc");
            foreach (var packageF in packages)
            {
                var package = MEPackageHandler.OpenMEPackage(packageF);
                options.SetStatusText($"Ensuring references in {package.FileNameNoExtension}");
                if (package.Localization == MELocalization.None)
                {
                    // Seek Free file
                    // BioPawnChallengeScaledTypes
                    // Classes for dynamic load
                    // GFxMovieInfo for faction images
                    VTestUtility.EnsureReferenced(package, options.cache, package.Exports.Where(x => x.ClassName is "GFxMovieInfo" or "BioPawnChallengeScaledType" || x.IsClass));
                }
                else
                {
                    // LOC file
                    VTestUtility.EnsureReferenced(package, options.cache, package.Exports.Where(x => x.ClassName == "BioCreatureSoundSet"));
                }

                if (package.IsModified)
                {
                    package.Save();
                }
            }
        }

        public static void EnsureReferencesInBase(VTestOptions options)
        {
            var sfSimulator = Path.Combine(VTestPaths.VTest_FinalDestDir);
            var packages = Directory.GetFiles(sfSimulator, "*.pcc");
            foreach (var packageF in packages)
            {
                var package = MEPackageHandler.OpenMEPackage(packageF);
                options.SetStatusText($"Ensuring references in {package.FileNameNoExtension}");
                VTestUtility.EnsureReferenced(package, options.cache, package.Exports.Where(x => (x.Parent == null && x.ClassName is "BioTlkFile")));

                if (package.IsModified)
                {
                    package.Save();
                }
            }
        }
    }
}
