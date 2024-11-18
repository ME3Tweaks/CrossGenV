using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
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
            SetSimSettingsConsoleColor();
        }

        /// <summary>
        /// Sets the color of the console to blue to make it more obvious there is something here that is interactable.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        private void SetSimSettingsConsoleColor()
        {
            // 11/17/2024
            var parent = le1File.FindExport("BIOG_V_Env_Hologram_Z.Instances");
            var blue1 = vTestOptions.vTestHelperPackage.FindExport("CCSIM03_LAY.Screen_01_Ins1_Blue");
            var blue3 = vTestOptions.vTestHelperPackage.FindExport("CCSIM03_LAY.Screen_01_Ins3_Blue");

            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, blue1, le1File, parent, true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out var portedBlue1);
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, blue3, le1File, parent, true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out var portedBlue3);

            var consoleScreen = le1File.FindExport("TheWorld.PersistentLevel.StaticMeshCollectionActor_31.StaticMeshActor_138_SMC");
            var materials = new ArrayProperty<ObjectProperty>("Materials");
            materials.Add(new ObjectProperty(portedBlue1));
            materials.Add(new ObjectProperty(portedBlue1));
            materials.Add(new ObjectProperty(portedBlue3));
            materials.Add(new ObjectProperty(portedBlue3));
            consoleScreen.WriteProperty(materials);
        }
    }
}
