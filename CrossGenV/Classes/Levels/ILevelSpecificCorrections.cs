using LegendaryExplorerCore.Packages;

namespace CrossGenV.Classes.Levels
{
    public interface ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PrePortingCorrection()
        {

        }

        public virtual void PostPortingCorrection()
        {

        }
    }
}
