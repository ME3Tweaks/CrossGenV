using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using System.Linq;
using LegendaryExplorerCore.Kismet;
using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCLava : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PrePortingCorrection()
        {
            // Todo: Remove this if lightmap is OK.
            // 08/22/2024
            // Ensure we don't remove lightmaps for terrain component by
            // cloning the textures and updating the references, since
            // static lighting generator will delete them due to them being shared with static meshes

            //foreach (var tc in me1Package.Exports.Where(x => x.ClassName == "TerrainComponent").ToList())
            //{
            //    var bin = ObjectBinary.From<TerrainComponent>(tc);
            //    KeepLightmapTextures(bin.LightMap, me1Package);
            //    tc.WriteBinary(bin);
            //}

            // 08/25/2024 - Fan blade needs changed to static mesh as it does not receive lighting otherwise
            // Tried to make it spin but it just is black no matter what I do
            var fanBladeSMC = me1File.FindExport("TheWorld.PersistentLevel.InterpActor_11.StaticMeshComponent_914");
            var originalIA = fanBladeSMC.Parent;
            var fanBladeSMA = VTestPreCorrections.CreateSMAFromSMC(fanBladeSMC);
            me1File.AddToLevelActorsIfNotThere(fanBladeSMA);
            var rot = fanBladeSMA.GetProperty<StructProperty>("Rotation");
            rot.GetProp<IntProperty>("Roll").Value = 15474; // 85 degrees
            fanBladeSMA.WriteProperty(rot);
            EntryPruner.TrashEntries(me1File, [originalIA]);
        }

        public void PostPortingCorrection()
        {
            // Port in the collision-corrected terrain

            // Only port in corrected terrain if we are not doing a build for static lighting.
            if (!vTestOptions.isBuildForStaticLightingBake)
            {
                VTestTerrain.PortInCorrectedTerrain(me1File, le1File, "CCLava.Terrain_1", "BIOA_LAV60_00_LAY.pcc", vTestOptions);
                VTestTerrain.CorrectTerrainSetup(me1File, le1File, vTestOptions);

                // We don't use the lightmap from UDK as it doesn't work properly for some reason
                // Correct the DirectionalMaxComponent scalar
                var terrainComponents = le1File.Exports.Where(x => x.ClassName == "TerrainComponent");
                foreach (var tc in terrainComponents)
                {
                    var tcBin = ObjectBinary.From<TerrainComponent>(tc);
                    var lm = tcBin.LightMap as LightMap_2D;
                    lm.ScaleVector2.X *= vTestOptions.LavaLightmapScalar;
                    lm.ScaleVector2.Y *= vTestOptions.LavaLightmapScalar;
                    lm.ScaleVector2.Z *= vTestOptions.LavaLightmapScalar;
                    tc.WriteBinary(tcBin);
                }

                // 10/19/2024 - Comment out as we moved this to all materials in all levels.
                // VTestKismet.CreateSignaledTextureStreaming(le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence"), VTestMaterial.cclavaTextureStreamingMaterials, vTestOptions);
            }

            // Don't allow running until wipe effect
            VTestPostCorrections.DisallowRunningUntilModeStarts(le1File, vTestOptions);
        }
    }
}
