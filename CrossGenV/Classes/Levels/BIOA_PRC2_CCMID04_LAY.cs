using LegendaryExplorerCore.Packages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCMID04_LAY : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PrePortingCorrection()
        {
            VTestPreCorrections.SetupForWindowMaterialDonor(me1File);
        }
        public void PostPortingCorrection()
        {
            VTestPostCorrections.AddCustomShader(le1File, "BIOA_PRC2_S.BIOA_PRC2_PortWindow_CROSSGEN");
        }
    }
}
