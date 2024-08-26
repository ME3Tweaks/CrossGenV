using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Textures;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            #region Check Level has at least 2 actors
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

            // VTestCheckImports(package, vTestOptions);
            VTestCheckTextures(package, vTestOptions);

            #endregion
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
                            Debug.WriteLine($@"FOUND UNUSABLE EMPTY MIP: {exp.InstancedFullPath} IN {Path.GetFileNameWithoutExtension(mePackage.FilePath)}");
                        }
                    }
                }

                if (exp.Parent != null && exp.Parent.ClassName != "TextureCube" && texinfo.Mips.Count(x => x.IsLocallyStored) > 6)
                {
                    Debug.WriteLine($"Externally storable texture: {exp.InstancedFullPath}");
                }
            }
        }

        public static void VTestCheckImports(IMEPackage p, VTestOptions vTestOptions)
        {
            foreach (var import in p.Imports)
            {
                if (import.IsAKnownNativeClass())
                    continue; //skip
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

                    Debug.WriteLine($"Import not resolved in {Path.GetFileName(p.FilePath)}: {import.InstancedFullPath}");
                }
            }
        }
    }
}
