using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;

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

            // Task 105 - fix bad collision near ochren by adjusting blocking volumes to prevent player from getting into zipping spots
            // on the right side of the cockpit seat
            var bv = me1File.FindExport("TheWorld.PersistentLevel.BlockingVolume_3");
            bv.WriteProperty(CommonStructs.Vector3Prop(-5336,1184,-1164, "Location"));
            bv.WriteProperty(CommonStructs.RotatorProp(0, 8192, 0, "Rotation"));
            bv.WriteProperty(CommonStructs.Vector3Prop(1, 1.7f, 1, "DrawScale3D"));

            // Task 105 - Mirror left side
            bv = me1File.FindExport("TheWorld.PersistentLevel.BlockingVolume_2");
            bv.WriteProperty(CommonStructs.Vector3Prop(-5386, 1510, -1164, "Location"));
            bv.WriteProperty(CommonStructs.RotatorProp(0, -8192, 0, "Rotation"));
            bv.WriteProperty(CommonStructs.Vector3Prop(1, 2f, 1, "DrawScale3D"));

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
