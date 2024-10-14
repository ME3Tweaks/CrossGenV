using LegendaryExplorerCore.Packages;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCSIM : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PostPortingCorrection() // this needs to be in CCSIM05_DSG instead?
        {
            // SCOREBOARD FRAMEBUFFER
            // This is handled by porting model from CCSIM03_LAY
            // needs something to fill framebuffer
            //var sourceAsset = vTestOptions.vTestHelperPackage.FindExport(@"CROSSGENV.StaticMeshActor_32000");
            //var destLevel = le1File.FindExport("TheWorld.PersistentLevel");
            //EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, sourceAsset, le1File, destLevel, true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out var mesh);
            //LevelTools.SetLocation(mesh as ExportEntry, -3750, -1624, -487);
            //LevelTools.SetDrawScale3D(mesh as ExportEntry, 3, 3, 3);
        }
    }
}
