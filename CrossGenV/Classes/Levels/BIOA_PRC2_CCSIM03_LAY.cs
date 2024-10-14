using LegendaryExplorerCore.Packages;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCSIM03_LAY : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PrePortingCorrection()
        {
            // garage door
            // The materials changed in LE so using the original set is wrong. Remove this property to prevent porting donors for it
            me1File.FindExport("TheWorld.PersistentLevel.BioDoor_1.SkeletalMeshComponent_1").RemoveProperty("Materials");
        }

        public void PostPortingCorrection()
        {
            // The door lighting channels needs fixed up.
            //08/24/2024 - Disabled lighting changes due to static lighting bake
            /*
            var door = le1File.FindExport(@"TheWorld.PersistentLevel.BioDoor_1.SkeletalMeshComponent_1");
            var channels = door.GetProperty<StructProperty>("LightingChannels");
            channels.Properties.AddOrReplaceProp(new BoolProperty(false, "Static"));
            channels.Properties.AddOrReplaceProp(new BoolProperty(false, "Dynamic"));
            channels.Properties.AddOrReplaceProp(new BoolProperty(false, "CompositeDynamic"));
            door.WriteProperty(channels); */
        }
    }
}
