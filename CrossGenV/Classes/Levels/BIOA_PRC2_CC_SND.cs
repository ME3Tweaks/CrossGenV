using LegendaryExplorerCore.Packages;

namespace CrossGenV.Classes.Levels
{
    /// <summary>
    /// BIOA_PRC2_CCTHAI_SND, BIOA_PRC2_CCCAVE_SND, BIOA_PRC2_CCLAVA_SND, BIOA_PRC2_CCCRATE_SND, BIOA_PRC2_CCAHERN_SND, 
    /// </summary>
    internal class BIOA_PRC2_CC_SND : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PostPortingCorrection()
        {
            VTestAudio.InstallMusicVolume(le1File, vTestOptions);
        }
    }
}
