using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Pathing;
using LegendaryExplorerCore.Unreal;
using System.IO;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2AA_00_LAY : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PostPortingCorrection()
        {
            FixPlanters();

            if (!vTestOptions.isBuildForStaticLightingBake)
            {
                VTestTerrain.PortInCorrectedTerrain(me1File, le1File, "PRC2AA.Terrain_1", "BIOA_UNC20_00_LAY.pcc", vTestOptions);
                VTestTerrain.CorrectTerrainSetup(me1File, le1File, vTestOptions);
            }

            // Need to set 'bCanStepUpOn' = false for certain static meshes as the collision is mega jank
            var meshesToFix = new[]
            {
                            "TheWorld.PersistentLevel.StaticMeshActor_0",
                            "TheWorld.PersistentLevel.StaticMeshActor_1",
                            "TheWorld.PersistentLevel.StaticMeshActor_2",
                            "TheWorld.PersistentLevel.StaticMeshActor_3",
                        };

            foreach (var m in meshesToFix)
            {
                var exp = le1File.FindExport(m);
                exp.WriteProperty(new BoolProperty(false, "bCanStepUpOn"));
            }
        }

        private void FixPlanters()
        {
            using var planterSource = MEPackageHandler.OpenMEPackage(Path.Combine(MEDirectories.GetCookedPath(MEGame.LE1), "BIOA_ICE20_03_DSG.pcc"), forceLoadFromDisk: true);

            // PLANTER HIGH (DOOR)
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, planterSource.FindExport("TheWorld.PersistentLevel.InterpActor_67"), le1File, le1File.FindEntry("TheWorld.PersistentLevel"), true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out var leavesHighSMA);
            LevelTools.SetLocation(leavesHighSMA as ExportEntry, -35043.76f, 10664f, 6792.9917f);

            // PLANTER MEDIUM (NEARBED)
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, planterSource.FindExport("TheWorld.PersistentLevel.InterpActor_23"), le1File, le1File.FindEntry("TheWorld.PersistentLevel"), true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out var leavesMedSMA);
            LevelTools.SetLocation(leavesMedSMA as ExportEntry, -35470.273f, 11690.752f, 6687.2974f + (112f * 0.6f)); // 112f is the offset

            // PLANTER MEDIUM (TABLE)
            var leavesMed2SMA = EntryCloner.CloneTree(leavesMedSMA);
            LevelTools.SetLocation(leavesMed2SMA as ExportEntry, -34559.5f, 11378.695f, 6687.457f + (112f * 0.6f)); // 112f is the offset
        }
    }
}
