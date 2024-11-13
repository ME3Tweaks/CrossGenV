using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Textures;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Localization;
using LegendaryExplorerCore.Shaders;
using LegendaryExplorerCore.Unreal.ObjectInfo;

namespace CrossGenV.Classes
{
    /// <summary>
    /// Verification methods for VTest
    /// </summary>
    public class VTestVerify
    {
        public static void VTest_CheckFile(IMEPackage package, VTestOptions vTestOptions)
        {
            // Make sure we inventory package so references check is accurate
            // Since this goes alphabetically it should have masters go first
            VTestExperiment.InventoryPackage(package, vTestOptions);

            // Legacy: Check levels have at least 2 actors in the list
            var level = package.FindExport("TheWorld.PersistentLevel");
            {
                if (level != null)
                {
                    var levelBin = ObjectBinary.From<Level>(level);
                    if (levelBin.Actors.Count < 2)
                        Debugger.Break(); // THIS SHOULD NOT OCCUR OR GAME WILL DIE
                    //Debug.WriteLine($"{Path.GetFileName(package.FilePath)} actor list count: {levelBin.Actors.Count}");
                }
            }

            VTestCheckImports(package, vTestOptions);
            VTestCheckTextures(package, vTestOptions);
            VTestCheckMaterials(package, vTestOptions);
            VTestCheckDuplicates(package, vTestOptions);
            VTestCheckReferences(package, vTestOptions);

        }

        private static void VTestCheckReferences(IMEPackage package, VTestOptions vTestOptions)
        {
            var refCheck = new ReferenceCheckPackage();
            EntryChecker.CheckReferences(refCheck, package, LECLocalizationShim.NonLocalizedStringConverter);
            foreach (var issue in refCheck.GetSignificantIssues().Concat(refCheck.GetBlockingErrors()))
            {
                vTestOptions.SetStatusText($"{issue.Entry.FileRef.FileNameNoExtension} {issue.Entry.InstancedFullPath}: {issue.Message}");
            }
        }

        private static void VTestCheckDuplicates(IMEPackage package, VTestOptions vTestOptions)
        {
            var duplicates = EntryChecker.CheckForDuplicateIndices(package);
            foreach (var dup in duplicates)
            {
                vTestOptions.SetStatusText($"{package.FileNameNoExtension}: Duplicate object {dup.Entry.InstancedFullPath}");
            }
        }

        private static void VTestCheckMaterials(IMEPackage package, VTestOptions vTestOptions)
        {
            var brokenMaterials = ShaderCacheManipulator.GetBrokenMaterials(package);
            foreach (var brokenMaterial in brokenMaterials)
            {
                vTestOptions.SetStatusText($"Error: Broken material detected: {brokenMaterial.InstancedFullPath} in {brokenMaterial.FileRef.FileNameNoExtension}");
            }
        }

        public static void VTestCheckTextures(IMEPackage mePackage, VTestOptions vTestOptions)
        {
            var maxLodInfo = TextureLODInfo.LEMaxLodSizes(mePackage.Game);
            foreach (var exp in mePackage.Exports.Where(x => x.IsTexture()))
            {
                var texinfo = ObjectBinary.From<UTexture2D>(exp);
                if (texinfo.Mips.Any(x => x.StorageType == StorageTypes.empty))
                {
                    // Check LOD bias if this will render in game
                    // Adjust the internal lod bias.
                    var props = exp.GetProperties();
                    var texGroup = props.GetProp<EnumProperty>(@"LODGroup");
                    if (texGroup != null && maxLodInfo.TryGetValue(texGroup.Value.Instanced, out var maxDimension))
                    {
                        // cubemaps will have null texture group. we don't want to update these
                        if (texinfo.Mips[0].SizeX > maxDimension || texinfo.Mips[0].SizeY > maxDimension)
                        {
                            vTestOptions.SetStatusText($@"FOUND UNUSABLE EMPTY MIP: {exp.InstancedFullPath} IN {Path.GetFileNameWithoutExtension(mePackage.FilePath)}");
                        }
                    }
                }

                if (exp.Parent != null && exp.Parent.ClassName != "TextureCube" && texinfo.Mips.Count(x => x.IsLocallyStored) > 6)
                {
                    vTestOptions.SetStatusText($"Externally storable texture: {exp.InstancedFullPath}");
                }
            }
        }


        public static void VTestCheckImports(IMEPackage p, VTestOptions vTestOptions)
        {
            foreach (var import in p.Imports)
            {
                if (import.IsAKnownNativeClass())
                    continue; //skip
        
                // Import resolver should be able to resolve imports as the master packages _should_ be in the cache
                // But I also haven't checked, so....
                var resolvedExp = EntryImporter.ResolveImport(import, vTestOptions.cache);
                if (resolvedExp == null)
                {
                    // 08/18/2024 - Upgrade to LEC6.4 - disable similar import guessing.

                    // Look in DB for objects that have same suffix
                    // This is going to be VERY slow

                    //var instancedNameSuffix = "." + import.ObjectName.Instanced;
                    //string similar = "";
                    //foreach (var name in vTestOptions.objectDB.Names)
                    //{
                    //    if (name.EndsWith(instancedNameSuffix, StringComparison.InvariantCultureIgnoreCase))
                    //    {
                    //        similar += ", " + name;
                    //    }
                    //}

                    vTestOptions.SetStatusText($"Import not resolved in {Path.GetFileName(p.FilePath)}: {import.InstancedFullPath}");
                }
            }
        }
    }
}
