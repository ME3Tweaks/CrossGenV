using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Pathing;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCCrate : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PostPortingCorrection()
        {
            // needs something to fill framebuffer
            var sourceAsset = vTestOptions.vTestHelperPackage.FindExport(@"CROSSGENV.StaticMeshActor_32000");
            var destLevel = le1File.FindExport("TheWorld.PersistentLevel");
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, sourceAsset, le1File, destLevel, true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out var mesh);
            LevelTools.SetLocation(mesh as ExportEntry, 15864, -25928, -5490);
        }
    }
}
