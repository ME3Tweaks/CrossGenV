using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCThai : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PrePortingCorrection()
        {
            // Task 77 - Unset material in ME1/LE1 mesh shows default texture
            // HenBagle, NaNuke
            // Forces different donor to be picked up that we made fixed version for
            var matFixObject01 = me1File.FindExport("BIOA_JUG40_S.jug40_Object01");
            matFixObject01.ObjectName = "jug40_Object01_Crossgen"; // NaNuke, HenBagle

            // Task 97 - Changes material to use proper emis
            // Audemus
            // We rename object so it picks up donor with correct material applied. Overrides didn't seem to work
            var pillarMesh = me1File.FindExport("BIOA_JUG40_S.JUG40_PILLARV00");
            pillarMesh.ObjectName = " JUG40_PILLARV00_CROSSGEN";
        }

        public void PostPortingCorrection()
        {
            // Don't allow running until wipe effect
            VTestPostCorrections.DisallowRunningUntilModeStarts(le1File, vTestOptions);
        }
    }
}
