using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Pathing;
using LegendaryExplorerCore.Unreal;
namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCCave : ILevelSpecificCorrections
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
            LevelTools.SetLocation(mesh as ExportEntry, -16430, -28799, -2580);

            // Needs a second one to hide the top too
            var fb2 = EntryCloner.CloneTree(mesh) as ExportEntry;
            var rotation = fb2.GetProperty<StructProperty>("Rotation");
            rotation.Properties.AddOrReplaceProp(new FloatProperty(0f, "Pitch"));
            fb2.WriteProperty(rotation);

            // Don't allow running until wipe effect
            VTestPostCorrections.DisallowRunningUntilModeStarts(le1File, vTestOptions);
        }
    }
}
