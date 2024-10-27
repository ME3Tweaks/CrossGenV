using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCCrate_DSG : BIOA_PRC2_CC_DSG, ILevelSpecificCorrections
    {
        public override void PrePortingCorrection()
        {
            base.PrePortingCorrection();
        }

        public override void PostPortingCorrection()
        {
            // Leave in case we have something else to add
            base.PostPortingCorrection();
        }
    }
}
