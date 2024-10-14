using LegendaryExplorerCore.Packages;

namespace CrossGenV.Classes.Levels
{
    internal class NoLevelCorrections : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }
    }
}
