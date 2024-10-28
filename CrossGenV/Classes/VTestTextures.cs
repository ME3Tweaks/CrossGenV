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
using LegendaryExplorerCore.Unreal.ObjectInfo;
using LegendaryExplorerCore.Kismet;
using System.Threading.Tasks;

namespace CrossGenV.Classes
{
    /// <summary>
    /// Handles textures for CrossGenV
    /// </summary>
    public static class VTestTextures
    {
        /// <summary>
        /// Forces textures to stream in around the player for 10 seconds
        /// </summary>
        private static void InstallPrepTextures(IMEPackage package, VTestOptions vTestOptions)
        {
            var mainSeq = package.FindExport("TheWorld.PersistentLevel.Main_Sequence");
            if (mainSeq == null)
                return; // Not a level
            var remoteEvent = SequenceObjectCreator.CreateSeqEventRemoteActivated(mainSeq, "CROSSGEN_PrepTextures");
            var player = SequenceObjectCreator.CreatePlayerObject(mainSeq, true, vTestOptions.cache);
            var sin = SequenceObjectCreator.CreateStreamInTextures(mainSeq, location: player, cache: vTestOptions.cache); // We use the player location actor. We want to stream in location not mesh.
            // No way to set distance factor, so guess this will have to do...

            sin.WriteProperty(new IntProperty(10, "Seconds"));

            List<IEntry> materialsInLevel = new List<IEntry>();
            materialsInLevel.AddRange(package.Exports.Where(x => !x.IsDefaultObject && x.IsA("MaterialInterface")));
            materialsInLevel.AddRange(package.Imports.Where(x => !x.ObjectNameString.StartsWith("Default__") && x.IsA("MaterialInterface")));

            sin.WriteProperty(new ArrayProperty<ObjectProperty>(materialsInLevel.Select(x=>new ObjectProperty(x)), "ForceMaterials"));

            KismetHelper.CreateOutputLink(remoteEvent, "Out", sin);
        }


        /// <summary>
        /// Moves textures that can be TFC stored out of a package and into a TFC for performance. This creates a new TFC file, it will wipe out existing!!
        /// </summary>
        /// <param name="tfcType">TFC name prefix, e.g. Textures, Lighting</param>
        /// <param name="dlcName">Name of the DLC for TFC naming.</param>
        /// <param name="rootTextures">if we should externalize root textures (lighting) only or non-root textures (everything else)</param>
        /// <param name="logCategoryBase"></param>
        public static void MoveTexturesToTFC(string tfcType, string dlcName, bool rootTextures, VTestOptions vTestOptions)
        {
            bool ShouldExternalize(bool rootTexture, int idxLink)
            {
                return rootTexture ? idxLink == 0 : idxLink != 0;
            }

            var tfcName = $"{tfcType}_{dlcName}";
            var tfcPath = Path.Combine(VTestPaths.VTest_FinalDestDir, $"{tfcName}.tfc");
            vTestOptions.SetStatusText($"Coalescing textures into {tfcPath}");
            using var tfcStream = File.Open(tfcPath, FileMode.CreateNew, FileAccess.ReadWrite);
            var guid = Guid.NewGuid();
            tfcStream.WriteGuid(guid);

            foreach (var packagePath in Directory.GetFiles(VTestPaths.VTest_FinalDestDir, "*.pcc", SearchOption.AllDirectories))
            {
                vTestOptions.SetStatusText($"Externalizing textures in {Path.GetFileName(packagePath)}");
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
                                vTestOptions.SetStatusText($"  Moving texture to external TFC: {tex.InstancedFullPath}");
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
                    vTestOptions.SetStatusText("  Saving package");
                    package.Save();
                }
            }
        }

        /// <summary>
        /// This will probably be moved to LEC at some point for convenience
        /// </summary>
        public static void CompactTFC(string tfcType, string dlcName, VTestOptions vTestOptions)
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

            vTestOptions.SetStatusText($"Compacting {tfcType} TFC");
            TFCCompactor.CompactTFC(info, x => Console.Error.WriteLine(x), (text, done, total) =>
            {
                if (text != null)
                {
                    vTestOptions.SetStatusText($"  {text}");
                }
                if (total > 0)
                {
                    vTestOptions.SetStatusText($"  Progress: {done}/{total}");
                }
                else if (done == -1 && total == -1)
                {

                }
            });
        }

        public static void InstallAllPrepTextureSignals(VTestOptions vTestOptions)
        {
            vTestOptions.SetStatusText("Installing PrepTextures listeners");
            Parallel.ForEach(VTestUtility.GetFinalPackages(), new ParallelOptions() { MaxDegreeOfParallelism = (vTestOptions.parallelizeLevelBuild ? 6 : 1) },
                f =>
                {
                    var quick = MEPackageHandler.UnsafePartialLoad(f, x => false);
                    if (quick.FindExport("TheWorld.PersistentLevel.Main_Sequence") != null)
                    {
                        var le1File = MEPackageHandler.OpenMEPackage(f);
                        vTestOptions.SetStatusText($"\t Installing signal to {le1File.FileNameNoExtension}");
                        VTestTextures.InstallPrepTextures(le1File, vTestOptions);
                        le1File.Save();
                    }
                });
        }
    }
}
