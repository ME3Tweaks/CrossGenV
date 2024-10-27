namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCCave_DSG : BIOA_PRC2_CC_DSG
    {
        public override void PrePortingCorrection()
        {
            base.PrePortingCorrection();
        }

        public override void PostPortingCorrection()
        {
            base.PostPortingCorrection();
            SetCaptureRingChanger("Cave");
        }
    }
}
