using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Gammtek.Extensions.Collections.Generic;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;

namespace CrossGenV.Classes
{
    /// <summary>
    /// These classes were part of other experiments that are here as they are not present in LEC.
    /// </summary>
    class VTestSupport
    {
        internal static void IndexFileForObjDB(ObjectInstanceDB objectDB, string path)
        {
            // Index package path
            /*int packageNameIndex;
            if (package.FilePath.StartsWith(MEDirectories.GetDefaultGamePath(game)))
            {
                // Get relative path
                packageNameIndex = objectDB.GetNameTableIndex(package.FilePath.Substring(MEDirectories.GetDefaultGamePath(game).Length + 1));
            }
            else
            {
                // Store full path
                packageNameIndex = objectDB.GetNameTableIndex(package.FilePath);
            }

            // Index objects
            foreach (var exp in package.Exports)
            {
                var ifp = exp.InstancedFullPath;

                // Things to ignore
                if (ifp.StartsWith(@"TheWorld"))
                    continue;
                if (ifp.StartsWith(@"ObjectReferencer"))
                    continue;

                // Index it
                objectDB.AddFileToDB(ifp, packageNameIndex, true);
            }*/
            objectDB.AddFileToDB(path, true); // Donors go first
        }

        public static void ImportUDKTerrainData(ExportEntry udkTerrain, ExportEntry targetTerrain, bool removeExistingComponents = true)
        {
            // Binary (Terrain)
            var udkBin = ObjectBinary.From<Terrain>(udkTerrain);
            var destBin = ObjectBinary.From<Terrain>(targetTerrain);
            destBin.Heights = udkBin.Heights;
            destBin.InfoData = udkBin.InfoData;
            destBin.CachedDisplacements = new byte[udkBin.Heights.Length];
            targetTerrain.WriteBinary(destBin);

            // Properties (Terrain)
            var terrainProps = targetTerrain.GetProperties();
            var udkProps = udkTerrain.GetProperties();
            terrainProps.RemoveNamedProperty("DrawScale3D");
            var udkDS3D = udkProps.GetProp<StructProperty>("DrawScale3D");
            if (udkDS3D != null)
            {
                terrainProps.AddOrReplaceProp(udkDS3D);
            }

            terrainProps.RemoveNamedProperty("DrawScale");
            var udkDS = udkProps.GetProp<FloatProperty>("DrawScale");
            if (udkDS != null)
            {
                terrainProps.AddOrReplaceProp(udkDS);
            }

            terrainProps.RemoveNamedProperty("Location");
            var loc = udkProps.GetProp<StructProperty>("Location");
            if (loc != null)
            {
                terrainProps.AddOrReplaceProp(loc);
            }

            // All Ints
            terrainProps.RemoveAll(x => x is IntProperty);
            terrainProps.AddRange(udkProps.Where(x => x is IntProperty));

            // Components
            if (removeExistingComponents)
            {
                var components = terrainProps.GetProp<ArrayProperty<ObjectProperty>>("TerrainComponents");
                EntryPruner.TrashEntries(targetTerrain.FileRef, components.Select(x => x.ResolveToEntry(targetTerrain.FileRef))); // Trash the components
                components.Clear();

                // Port over the UDK ones
                var udkComponents = udkTerrain.GetProperty<ArrayProperty<ObjectProperty>>("TerrainComponents");
                foreach (var tc in udkComponents)
                {
                    var entry = tc.ResolveToEntry(udkTerrain.FileRef);
                    EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, entry, targetTerrain.FileRef, targetTerrain, true, new RelinkerOptionsPackage(), out var portedComp);
                    components.Add(new ObjectProperty(portedComp.UIndex));
                }
            }

            targetTerrain.WriteProperties(terrainProps);
        }
    }
}
