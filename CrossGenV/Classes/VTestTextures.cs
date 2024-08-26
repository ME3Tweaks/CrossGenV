using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Textures;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Unreal.ObjectInfo;

namespace CrossGenV.Classes
{
    /// <summary>
    /// Handles externalization of textures for CrossGenV
    /// </summary>
    public static class VTestTextures
    {

        /// <summary>
        /// Moves textures that can be TFC stored out of a package and into a TFC for performance. This creates a new TFC file, it will wipe out existing!!
        /// </summary>
        /// <param name="tfcType">TFC name prefix, e.g. Textures, Lighting</param>
        /// <param name="dlcName">Name of the DLC for TFC naming.</param>
        /// <param name="rootTextures">if we should externalize root textures (lighting) only or non-root textures (everything else)</param>
        /// <param name="logCategoryBase"></param>
        public static void MoveTexturesToTFC(string tfcType, string dlcName, bool rootTextures)
        {
            bool ShouldExternalize(bool rootTexture, int idxLink)
            {
                return rootTexture ? idxLink == 0 : idxLink != 0;
            }

            var tfcName = $"{tfcType}_{dlcName}";
            var tfcPath = Path.Combine(VTestPaths.VTest_FinalDestDir, $"{tfcName}.tfc");
            Console.WriteLine($"Coalescing textures into {tfcPath}");
            using var tfcStream = File.Open(tfcPath, FileMode.CreateNew, FileAccess.ReadWrite);
            var guid = Guid.NewGuid();
            tfcStream.WriteGuid(guid);

            foreach (var packagePath in Directory.GetFiles(VTestPaths.VTest_FinalDestDir, "*.pcc", SearchOption.AllDirectories))
            {
                Console.WriteLine($"Externalizing textures in {Path.GetFileName(packagePath)}");
                var package = MEPackageHandler.OpenMEPackage(packagePath);

                foreach (var tex in package.Exports.Where(x => x.IsTexture() && ShouldExternalize(rootTextures, x.idxLink)))
                {
                    if (tex.ObjectName.Name.StartsWith("Cubemap", StringComparison.OrdinalIgnoreCase))
                        continue; // These are never moved to TFC as far as I know...

                    var bin = (UTexture2D)ObjectBinary.From(tex); // Cast since we want it to make the lowest-tier class but we only need the upper one.

                    var startedMovingTexture = false;
                    var changedMips = bin.Mips.RemoveAll(x => x.StorageType == StorageTypes.empty);

                    if (bin.Mips.Count <= 6 && changedMips == 0)
                        continue;

                    bool updateTFCName = false;
                    for (int i = 0; i < bin.Mips.Count - 6; i++)
                    {
                        // Move data to external
                        var mip = bin.Mips[i];
                        if (mip.StorageType == StorageTypes.pccUnc)
                        {
                            if (!startedMovingTexture)
                            {
                                Console.WriteLine($"  Moving texture to external TFC: {tex.InstancedFullPath}");
                            }
                            startedMovingTexture = true;

                            var compressed = TextureCompression.CompressTexture(mip.Mip, StorageTypes.extOodle);
                            var offset = tfcStream.Position;
                            tfcStream.Write(compressed);
                            mip.StorageType = StorageTypes.extOodle;
                            mip.DataOffset = (int)offset;
                            mip.CompressedSize = compressed.Length;
                            mip.Mip = [];
                            changedMips++;
                            updateTFCName = true;
                        }
                    }

                    if (updateTFCName)
                    {
                        // Update TFC properties
                        var properties = tex.GetProperties();
                        properties.AddOrReplaceProp(new FGuid(guid).ToStructProperty("TFCFileGuid"));
                        properties.AddOrReplaceProp(new NameProperty(tfcName, "TextureFileCacheName"));
                        properties.RemoveNamedProperty("NeverStream");
                        tex.WriteProperties(properties);

                        if (tex.IsA("LightMapTexture2D"))
                        {
                            // Set to streaming
                            var tbin = ObjectBinary.From<LightMapTexture2D>(tex);
                            tbin.LightMapFlags |= ELightMapFlags.LMF_Streamed;
                            tex.WriteBinary(tbin);
                        }
                        

                    }

                    if (changedMips > 0)
                    {
                        tex.WriteBinary(bin);
                    }
                }

                if (package.IsModified)
                {
                    Console.WriteLine("  Saving package");
                    package.Save();
                }
            }
        }

        /// <summary>
        /// This will probably be moved to LEC at some point for convenience
        /// </summary>
        public static void CompactTFC(string tfcType, string dlcName)
        {
            // Step 2: TFC Compactor
            var staging = Directory.CreateDirectory(Path.Combine(VTestPaths.VTest_FinalDestDir, "Staging")).FullName;
            TFCCompactorInfoPackage info = new TFCCompactorInfoPackage()
            {
                Game = MEGame.LE1,
                BaseCompactionPath = VTestPaths.VTest_FinalDestDir,
                DLCName = dlcName,
                GamePath = LE1Directory.DefaultGamePath,
                TFCsToCompact = [
                    $"{tfcType}_{dlcName}",
                ],
                UseIndexing = false,
                StagingPath = staging,
                TFCType = tfcType,
            };

            Console.WriteLine($"Compacting {tfcType} TFC");
            TFCCompactor.CompactTFC(info, x => Console.Error.WriteLine(x), (text, done, total) =>
            {
                if (text != null)
                {
                    Console.WriteLine($"  {text}");
                }
                if (total > 0)
                {
                    Console.WriteLine($"  Progress: {done}/{total}");
                }
                else if (done == -1 && total == -1)
                {

                }
            });
        }
    }
}
