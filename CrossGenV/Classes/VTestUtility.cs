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
using System.Text;
using System.Threading.Tasks;

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
                Debug.WriteLine("IT DIDN'T WORK!");
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

    }
}
