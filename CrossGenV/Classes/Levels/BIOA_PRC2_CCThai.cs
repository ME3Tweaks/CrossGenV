using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using System.Numerics;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCThai : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PrePortingCorrection()
        {
            // Task 77 - Unset material in ME1/LE1 mesh shows default texture
            // HenBagle, NaNuke
            // Forces different donor to be picked up that we made fixed version for
            var matFixObject01 = me1File.FindExport("BIOA_JUG40_S.jug40_Object01");
            matFixObject01.ObjectName = "jug40_Object01_Crossgen"; // NaNuke, HenBagle

            // Task 97 - Changes material to use proper emis
            // Audemus
            // We rename object so it picks up donor with correct material applied. Overrides didn't seem to work
            var pillarMesh = me1File.FindExport("BIOA_JUG40_S.JUG40_PILLARV00");
            pillarMesh.ObjectName = "JUG40_PILLARV00_CROSSGEN";
        }

        public void PostPortingCorrection()
        {
            // Don't allow running until wipe effect
            VTestPostCorrections.DisallowRunningUntilModeStarts(le1File, vTestOptions);
            FixRockCoverZ();

            // 11/11/2024 - Fix duplicate material 
            var checker = le1File.FindExport("BIOA_JUG80_T.JUG80_SAIL", "Material");
            checker.ObjectName = "JUG80_SAIL_mat";
        }

        private void FixRockCoverZ()
        {
            // Part of Task 95 - Collision issues on S platform corner
            var smca = le1File.FindExport("TheWorld.PersistentLevel.StaticMeshCollectionActor_35");
            var smc = le1File.FindExport("TheWorld.PersistentLevel.StaticMeshCollectionActor_35.StaticMeshActor_1216_SMC");
            var bin = ObjectBinary.From<StaticMeshCollectionActor>(smca);
            var index = bin.Components.IndexOf(smc.UIndex);
            var dsd = bin.LocalToWorldTransforms[index].UnrealDecompose();
            dsd.translation = new Vector3(260,-31189,-2546); // Down a few units
            bin.LocalToWorldTransforms[index] = ActorUtils.ComposeLocalToWorld(dsd.translation, dsd.rotation, dsd.scale);
            smca.WriteBinary(bin);
        }
    }
}
