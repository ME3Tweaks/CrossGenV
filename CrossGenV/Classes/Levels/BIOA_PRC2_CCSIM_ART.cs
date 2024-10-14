using LegendaryExplorerCore.Packages;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCSIM_ART : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PostPortingCorrection()
        {
            // Lights near the door need fixed up.

            //08/24/2024 - Disabled lighting changes due to static lighting bake
            /*
            var doorPL = le1File.FindExport(@"TheWorld.PersistentLevel.StaticLightCollectionActor_11.PointLight_0_LC");
            var lc = doorPL.GetProperty<StructProperty>("LightColor");
            lc.GetProp<ByteProperty>("B").Value = 158;
            lc.GetProp<ByteProperty>("G").Value = 194;
            lc.GetProp<ByteProperty>("R").Value = 143;
            doorPL.WriteProperty(lc);

            var doorSL = le1File.FindExport(@"TheWorld.PersistentLevel.StaticLightCollectionActor_11.SpotLight_7_LC");
            lc = doorSL.GetProperty<StructProperty>("LightColor");
            lc.GetProp<ByteProperty>("B").Value = 215;
            lc.GetProp<ByteProperty>("G").Value = 203;
            lc.GetProp<ByteProperty>("R").Value = 195;
            doorSL.WriteProperty(lc); */
        }
    }
}
