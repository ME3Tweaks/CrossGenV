using LegendaryExplorerCore.Gammtek.Extensions.Collections.Generic;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace CrossGenV.Classes
{
    /// <summary>
    /// Utility methods
    /// </summary>
    class VTestUtility
    {
        /// <summary>
        /// Fetches an embedded file from the Embedded folder.
        /// </summary>
        /// <param name="embeddedFilename"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static bool LoadEmbeddedFile(string embeddedFilename, out Stream stream)
        {
            var assembly = Assembly.GetExecutingAssembly();
            //var resources = assembly.GetManifestResourceNames();
            //debug
            var assetName = $"CrossGenV.Embedded.{embeddedFilename}";
            stream = assembly.GetManifestResourceStream(assetName);
            if (stream == null)
            {
                Debug.WriteLine($"{assetName} not found in embedded resources");
                return false;
            }
            return true;
        }


        public static void AddWorldReferencedObjects(IMEPackage le1File, params ExportEntry[] entriesToReference)
        {
            var world = le1File.FindExport("TheWorld");
            var worldBin = ObjectBinary.From<World>(world);
            var newItems = worldBin.ExtraReferencedObjects.ToList();
            newItems.AddRange(entriesToReference.Select(x => x.UIndex));
            worldBin.ExtraReferencedObjects = newItems.ToArray();
            world.WriteBinary(worldBin);
        }

        public static void RebuildStreamingLevels(IMEPackage Pcc)
        {
            try
            {
                var levelStreamingKismets = new List<ExportEntry>();
                ExportEntry bioworldinfo = null;
                foreach (ExportEntry exp in Pcc.Exports)
                {
                    switch (exp.ClassName)
                    {
                        case "BioWorldInfo" when exp.ObjectName == "BioWorldInfo":
                            bioworldinfo = exp;
                            continue;
                        case "LevelStreamingKismet" when exp.ObjectName == "LevelStreamingKismet":
                            levelStreamingKismets.Add(exp);
                            continue;
                    }
                }

                levelStreamingKismets = levelStreamingKismets
                    .OrderBy(o => o.GetProperty<NameProperty>("PackageName").ToString()).ToList();
                if (bioworldinfo != null)
                {
                    var streamingLevelsProp =
                        bioworldinfo.GetProperty<ArrayProperty<ObjectProperty>>("StreamingLevels") ??
                        new ArrayProperty<ObjectProperty>("StreamingLevels");

                    streamingLevelsProp.Clear();
                    foreach (ExportEntry exp in levelStreamingKismets)
                    {
                        streamingLevelsProp.Add(new ObjectProperty(exp.UIndex));
                    }

                    bioworldinfo.WriteProperty(streamingLevelsProp);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"IT DIDN'T WORK: {ex.Message}");
            }
        }

        public static void AddModLevelExtensions(IMEPackage le1File, string levelName)
        {
            // Todo: Make sure these actually work.

            // Step 1: Make more LevelStreamingKismet objects
            var levelnames = new List<string>();
            var sourceToClone = le1File.Exports.First(x => x.ClassName == "LevelStreamingKismet");
            for (int i = 0; i < 5; i++)
            {
                var levelBaseName = levelName + "_ModGlobal" + (i + 1);
                var newEntry = EntryCloner.CloneEntry(sourceToClone) as ExportEntry;
                newEntry.WriteProperty(new NameProperty(levelBaseName, "PackageName"));
                levelnames.Add(levelBaseName);
            }

            // Step 2: Rebuild StreamingLevels
            VTestUtility.RebuildStreamingLevels(le1File);

            // Step 3: Add levels to states
            // Doing it via a global didn't seem to work
            foreach (var bts in le1File.Exports.Where(x => x.ClassName == "BioTriggerStream"))
            {
                var streamingStates = bts.GetProperty<ArrayProperty<StructProperty>>("StreamingStates");
                if (streamingStates != null)
                {
                    foreach (var ss in streamingStates)
                    {
                        var visibleChunks = ss.GetProp<ArrayProperty<NameProperty>>("VisibleChunkNames");
                        visibleChunks.AddRange(levelnames.Select(x => new NameProperty(x)));
                    }
                }
                bts.WriteProperty(streamingStates);
            }
        }

        /// <summary>
        /// Deletes the contents of the specified folder, as well as the directory itself unless deleteDirectoryItself = false
        /// </summary>
        /// <param name="targetDirectory"></param>
        /// <param name="throwOnFailed"></param>
        /// <returns></returns>
        public static bool DeleteFilesAndFoldersRecursively(string targetDirectory, bool throwOnFailed = false, bool deleteDirectoryItself = true, bool quiet = false)
        {
            if (!Directory.Exists(targetDirectory))
            {
                Debug.WriteLine(@"Directory to delete doesn't exist: " + targetDirectory);
                return true;
            }

            bool result = true;
            foreach (string file in Directory.EnumerateFiles(targetDirectory))
            {
                File.SetAttributes(file, FileAttributes.Normal); //remove read only
                try
                {
                    if (!quiet)
                    {
                        Console.WriteLine($"Deleting file: {file}");
                    }
                    File.Delete(file);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($@"Unable to delete file: {file}. It may be open still: {e.Message}");
                    if (throwOnFailed)
                    {
                        throw;
                    }

                    return false;
                }
            }

            foreach (string subDir in Directory.GetDirectories(targetDirectory))
            {
                result &= DeleteFilesAndFoldersRecursively(subDir, throwOnFailed, true, quiet);
            }

            if (deleteDirectoryItself)
            {
                Thread.Sleep(10); // This makes the difference between whether it works or not. Sleep(0) is not enough.
                try
                {
                    Directory.Delete(targetDirectory);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($@"Unable to delete directory: {targetDirectory}. It may be open still or may not be actually empty: {e.Message}");
                    if (throwOnFailed)
                    {
                        throw;
                    }

                    return false;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns list of all packages in the destination directory (*.pcc)
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetFinalPackages()
        {
            var finalPath = VTestPaths.VTest_FinalDestDir;
            return Directory.GetFiles(finalPath, "*.pcc", SearchOption.AllDirectories);
        }

        /// <summary>
        /// Ensures objects are referenced in the package.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="cache"></param>
        /// <param name="itemsToReference"></param>
        public static void EnsureReferenced(IMEPackage package, PackageCache cache, IEnumerable<ExportEntry> itemsToReference)
        {
            if ((package.Flags & UnrealFlags.EPackageFlags.Map) != 0)
            {
                // Map
                var theWorld = package.FindExport("TheWorld");
                var worldBin = ObjectBinary.From<World>(theWorld);
                worldBin.ExtraReferencedObjects.AddRange(itemsToReference.Select(x => x.UIndex));
                worldBin.ExtraReferencedObjects = worldBin.ExtraReferencedObjects.Distinct().ToArray(); // ToList since it's modifying itself
                theWorld.WriteBinary(worldBin);
            }
            else
            {
                // Not a map
                var referencer = package.CreateObjectReferencer();
                var references = referencer.GetProperty<ArrayProperty<ObjectProperty>>("ReferencedObjects") ?? new ArrayProperty<ObjectProperty>("ReferencedObjects");
                references.AddRange(itemsToReference.Select(x=>new ObjectProperty(x)));
                references.ReplaceAll(references.Distinct().ToList()); // ToList since it's modifying itself
                referencer.WriteProperty(references);
            }
        }
    }
}
