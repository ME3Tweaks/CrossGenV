using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;

namespace CrossGenV.Classes.Levels
{
    /// <summary>
    /// We only do this for completionist's sake, we don't really care about this level.
    /// </summary>
    internal class COMBATPERFTEST : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }
        public void PrePortingCorrection()
        {
            // Change strategic ring material name so it picks up donor instead
            var strategicRing = me1File.FindExport("BIOA_PRC2_MatFX.Material.StrategicRing_Cap_MAT");
            if (strategicRing != null)
            {
                var cgv = ExportCreator.CreatePackageExport(me1File, "CROSSGENV");
                strategicRing.idxLink = cgv.UIndex;
                strategicRing.ObjectName = "StrategicRing_Cap_MAT_NEW_matInst";
                strategicRing.Class = EntryImporter.EnsureClassIsInFile(me1File, "MaterialInstanceConstant", new RelinkerOptionsPackage()); // Change class so donor is properly picked up?
            }
        }
        public void PostPortingCorrection()
        {
            // 10/22/2024 - NaNuke - Custom capture ring shader
            VTestPostCorrections.AddCustomShader(le1File, "CROSSGENV.StrategicRing_Cap_MAT_NEW");
        }
    }
}
