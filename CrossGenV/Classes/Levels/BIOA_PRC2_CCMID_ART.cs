using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCMID_ART : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PostPortingCorrection()
        {
            // Brighten up the corners that are dead ends
            string[] exports =
            [
                "TheWorld.PersistentLevel.StaticLightCollectionActor_10.PointLight_24_LC",
                "TheWorld.PersistentLevel.StaticLightCollectionActor_10.PointLight_27_LC"
            ];

            foreach (var lightIFP in exports)
            {
                var cornerLight = le1File.FindExport(lightIFP);
                var props = cornerLight.GetProperties();
                props.AddOrReplaceProp(new FloatProperty(0.7f, "Brightness")); // Up from 0.3
                props.GetProp<StructProperty>("LightingChannels").Properties.AddOrReplaceProp(new BoolProperty(true, "CompositeDynamic"));
                cornerLight.WriteProperties(props);
            }
        }
    }
}
