using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCThai : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PrePortingCorrection()
        {
            // Forces different donor
            var matFixObject01 = me1File.FindExport("BIOA_JUG40_S.jug40_Object01");
            matFixObject01.ObjectName = "jug40_Object01_Crossgen"; // NaNuke, HenBagle

            // Changes material to use proper emis // Audemus
            // First make the proper material that will be converted for donor
            var preDonor1 = me1File.FindExport("BIOA_PRC2_MatFX.Holodeck_Grid_MAT");
            var preDonor2 = EntryCloner.CloneEntry(preDonor1);
            var cgv = ExportCreator.CreatePackageExport(me1File, "CROSSGENV");
            preDonor2.idxLink = cgv.UIndex;
            preDonor2.ObjectName = "JUG80_POD01_Crossgen";


            var materials = new ArrayProperty<ObjectProperty>("Materials");
            materials.Add(preDonor2);
            var pillarMesh = me1File.FindExport("BIOA_JUG40_S.JUG40_PILLARV00");
            var pillarReferences = pillarMesh.GetEntriesThatReferenceThisOne();
            foreach (var entry in pillarReferences)
            {
                if (entry.Key is ExportEntry exp && exp.ClassName == "StaticMeshComponent")
                {
                    exp.WriteProperty(materials);
                }
            }
        }

        public void PostPortingCorrection()
        {
            // Don't allow running until wipe effect
            VTestPostCorrections.DisallowRunningUntilModeStarts(le1File, vTestOptions);
        }
    }
}
