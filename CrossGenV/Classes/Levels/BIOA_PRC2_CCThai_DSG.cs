using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCThai_DSG : BIOA_PRC2_CC_DSG, ILevelSpecificCorrections
    {
        public override void PrePortingCorrection()
        {
            base.PrePortingCorrection();
        }

        public override void PostPortingCorrection()
        {
            base.PostPortingCorrection();
            SetCaptureRingChanger("Thai");
        }
    }
}