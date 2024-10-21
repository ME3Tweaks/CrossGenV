using LegendaryExplorerCore.Packages;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCAhern : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PostPortingCorrection()
        {
            if (!vTestOptions.isBuildForStaticLightingBake)
            {
                VTestTerrain.PortInCorrectedTerrain(me1File, le1File, "CCAHERN.Terrain_1", "BIOA_LAV60_00_LAY.pcc", vTestOptions);
                VTestTerrain.CorrectTerrainSetup(me1File, le1File, vTestOptions);
            }

            // Don't allow running until wipe effect
            VTestPostCorrections.DisallowRunningUntilModeStarts(le1File, vTestOptions);
        }
    }
}
