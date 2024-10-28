using System.Linq;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCScoreboard_DSG_LOC : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PrePortingCorrection()
        {
            var clonedMIC = EntryCloner.CloneEntry(me1File.FindExport("BIOA_PRC2_Scoreboard_T.Blue_Mission_Resolved"));
            clonedMIC.ObjectName = "Blue_Mission_Failed";
            
            // Add to object referencer
            var objReferencer = me1File.Exports.First(x => x.idxLink == 0 && x.ClassName == "ObjectReferencer");
            var objs = objReferencer.GetProperty<ArrayProperty<ObjectProperty>>("ReferencedObjects");
            objs.Add(new ObjectProperty(clonedMIC));
            objReferencer.WriteProperty(objs);
        }
    }
}
