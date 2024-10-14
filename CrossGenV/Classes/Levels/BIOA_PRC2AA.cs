using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Pathing;
using System.IO;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2AA : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PostPortingCorrection()
        {
            // Fix Planter high
            using var planterSource = MEPackageHandler.OpenMEPackage(Path.Combine(MEDirectories.GetCookedPath(MEGame.LE1), "BIOA_ICE20_03_DSG.pcc"), forceLoadFromDisk: true);
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, planterSource.FindExport("TheWorld.PersistentLevel.InterpActor_67"), le1File, le1File.FindEntry("TheWorld.PersistentLevel"), true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out var leavesSMA);
            LevelTools.SetLocation(leavesSMA as ExportEntry, -35797.312f, 10758.975f, 6777.0386f);


            VTestKismet.InstallVTestHelperSequenceNoInput(le1File, "TheWorld.PersistentLevel.Main_Sequence", "HelperSequences.LevelLoadTextureStreaming", vTestOptions);
        }
    }
}
