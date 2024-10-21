using LegendaryExplorerCore.Packages;

namespace CrossGenV.Classes.Levels
{
    public static class LevelCorrectionFactory
    {
        public static ILevelSpecificCorrections GetLevel(string levelName, IMEPackage me1Package, IMEPackage le1Package, VTestOptions options)
        {
            var levelNameUpper = levelName.ToUpper();

            return levelNameUpper switch
            {
                "BIOA_PRC2" => MakeLevel<BIOA_PRC2>(me1Package, le1Package, options),
                
                "BIOA_PRC2_CCAHERN" => MakeLevel<BIOA_PRC2_CCAhern>(me1Package, le1Package, options),
                "BIOA_PRC2_CCAHERN_DSG" => MakeLevel<BIOA_PRC2_CCAhern_DSG>(me1Package, le1Package, options),
                "BIOA_PRC2_CCAHERN_SND" => MakeLevel<BIOA_PRC2_CC_SND>(me1Package, le1Package, options),
                
                "BIOA_PRC2_CCCAVE" => MakeLevel<BIOA_PRC2_CCCave>(me1Package, le1Package, options),
                "BIOA_PRC2_CCCAVE_DSG" => MakeLevel<BIOA_PRC2_CC_DSG>(me1Package, le1Package, options),
                "BIOA_PRC2_CCCAVE_SND" => MakeLevel<BIOA_PRC2_CC_SND>(me1Package, le1Package, options),
                
                "BIOA_PRC2_CCCRATE" => MakeLevel<BIOA_PRC2_CCCrate>(me1Package, le1Package, options),
                "BIOA_PRC2_CCCRATE_DSG" => MakeLevel<BIOA_PRC2_CC_DSG>(me1Package, le1Package, options),
                "BIOA_PRC2_CCCRATE_SND" => MakeLevel<BIOA_PRC2_CC_SND>(me1Package, le1Package, options),

                "BIOA_PRC2_CCLAVA" => MakeLevel<BIOA_PRC2_CCLava>(me1Package, le1Package, options),
                "BIOA_PRC2_CCLAVA_DSG" => MakeLevel<BIOA_PRC2_CC_DSG>(me1Package, le1Package, options),
                "BIOA_PRC2_CCLAVA_SND" => MakeLevel<BIOA_PRC2_CC_SND>(me1Package, le1Package, options),

                "BIOA_PRC2_CCTHAI" => MakeLevel<BIOA_PRC2_CCThai>(me1Package, le1Package, options),
                "BIOA_PRC2_CCTHAI_DSG" => MakeLevel<BIOA_PRC2_CC_DSG>(me1Package, le1Package, options),
                "BIOA_PRC2_CCTHAI_SND" => MakeLevel<BIOA_PRC2_CC_SND>(me1Package, le1Package, options),

                "BIOA_PRC2_CCAIRLOCK" => MakeLevel<BIOA_PRC2_CCAirlock>(me1Package, le1Package, options),


                "BIOA_PRC2_CCMAIN_CONV" => MakeLevel<BIOA_PRC2_CCMAIN_CONV>(me1Package, le1Package, options),
                "BIOA_PRC2_CCMID_ART" => MakeLevel<BIOA_PRC2_CCMID_ART>(me1Package, le1Package, options),
                "BIOA_PRC2_CCMID01_LAY" => MakeLevel<BIOA_PRC2_CCMID01_LAY>(me1Package, le1Package, options),
                "BIOA_PRC2_CCMID02_LAY" => MakeLevel<BIOA_PRC2_CCMID02_LAY>(me1Package, le1Package, options),
                "BIOA_PRC2_CCMID04_LAY" => MakeLevel<BIOA_PRC2_CCMID04_LAY>(me1Package, le1Package, options),

                "BIOA_PRC2_CCSCOREBOARD_DSG" => MakeLevel<BIOA_PRC2_CCScoreboard_DSG>(me1Package, le1Package, options),

                "BIOA_PRC2_CCSIM" => MakeLevel<BIOA_PRC2_CCSIM>(me1Package, le1Package, options),
                "BIOA_PRC2_CCSIM_ART" => MakeLevel<BIOA_PRC2_CCSIM_ART>(me1Package, le1Package, options),
                "BIOA_PRC2_CCSIM03_LAY" => MakeLevel<BIOA_PRC2_CCSIM03_LAY>(me1Package, le1Package, options),
                "BIOA_PRC2_CCSIM04_DSG" => MakeLevel<BIOA_PRC2_CCSIM04_DSG>(me1Package, le1Package, options),
                "BIOA_PRC2_CCSIM05_DSG" => MakeLevel<BIOA_PRC2_CCSIM05_DSG>(me1Package, le1Package, options),

                "BIOA_PRC2_CCSPACE02_DSG" => MakeLevel<BIOA_PRC2_CCSpace02_DSG>(me1Package, le1Package, options),

                "BIOA_PRC2AA" => MakeLevel<BIOA_PRC2AA>(me1Package, le1Package, options),
                "BIOA_PRC2AA_00_DSG" => MakeLevel<BIOA_PRC2AA_00_DSG>(me1Package, le1Package, options),
                "BIOA_PRC2AA_00_LAY" => MakeLevel<BIOA_PRC2AA_00_LAY>(me1Package, le1Package, options),
                _ => MakeLevel<NoLevelCorrections>(me1Package, le1Package, options)
            };
        }

        private static T MakeLevel<T>(IMEPackage me1Package, IMEPackage le1Package, VTestOptions options) where T : ILevelSpecificCorrections, new()
        {
            return new T()
            {
                me1File = me1Package,
                le1File = le1Package,
                vTestOptions = options
            };
        }
    }
}
