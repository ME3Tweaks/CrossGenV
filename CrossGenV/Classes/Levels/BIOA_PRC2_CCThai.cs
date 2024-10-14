using LegendaryExplorerCore.Packages;

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
            matFixObject01.ObjectName = "jug40_Object01_Crossgen";
        }
    }
}
