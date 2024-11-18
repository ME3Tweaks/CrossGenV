using System.Linq;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCMID04_LAY : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PrePortingCorrection()
        {
            // We want to use the VTest v1 window as the frost effect does not look good on the huge window.
            var matToClone = me1File.Exports.FirstOrDefault(x => x.ClassName == "Material");
            var clonedMat = EntryCloner.CloneEntry(matToClone);
            clonedMat.Parent = me1File.FindExport("BIOA_PRC2_S");
            clonedMat.ObjectName = "BIOA_PRC2_MainPortWindow"; // Pick up the donor
            var mainWindow = me1File.FindExport("TheWorld.PersistentLevel.StaticMeshCollectionActor_26.StaticMeshActor_0_SMC");
            mainWindow.WriteProperty(new ArrayProperty<ObjectProperty>("Materials"){ clonedMat});


            // Other Windows for this map
            VTestPreCorrections.SetupForWindowMaterialDonor(me1File);
        }
        public void PostPortingCorrection()
        {
            VTestPostCorrections.AddCustomShader(le1File, "BIOA_PRC2_S.BIOA_PRC2_PortWindow_CROSSGEN");
        }
    }
}
