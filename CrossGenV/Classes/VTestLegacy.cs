using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using System.Numerics;

namespace CrossGenV.Classes
{
    /// <summary>
    /// Legacy code is moved here. It is code that is no longer used but kept around for historical sake.
    /// </summary>
    internal class VTestLegacy
    {
        // Very old early VTest
        private static string[] DebugBuildClassesToVTestPort = new[]
        {
            "PointLight",
            "SpotLight",
        };


        // Removed with Static Lighting changes
        /// <summary>
        /// Creates dynamic lighting but tries to increase performance a bit
        /// </summary>
        /// <param name="Pcc"></param>
        /// <param name="silent"></param>
        public static void CreateDynamicLighting(IMEPackage Pcc, VTestOptions vTestOptions, bool silent = false)
        {
            var fname = Path.GetFileNameWithoutExtension(Pcc.FilePath);
            // Need to check if ledge actors compute lighting while hidden. If they do this will significantly harm performance
            var dynamicableExports = Pcc.Exports.Where(exp => (exp.IsA("MeshComponent") && exp.Parent.IsA("StaticMeshActorBase")) || (exp.IsA("BrushComponent") && !exp.Parent.IsA("Volume"))).ToList();
            foreach (ExportEntry exp in dynamicableExports)
            {
                PropertyCollection props = exp.GetProperties();
                if (props.GetProp<BoolProperty>("bAcceptsLights")?.Value == false)
                //|| props.GetProp<BoolProperty>("CastShadow")?.Value == false) // CROSSGEN- OFF since we don't do dynamic shadows.
                {
                    // shadows/lighting has been explicitly forbidden, don't mess with it.
                    continue;
                }
                Debug.WriteLine($"CHECKING {exp.InstancedFullPath}");
                if (vTestOptions.allowTryingPortedMeshLightMap && !fname.StartsWith("BIOA_PRC2AA"))
                {
                    var sm = exp.GetProperty<ObjectProperty>("StaticMesh"); // name might need changed?
                    if (sm != null)
                    {
                        if (Pcc.TryGetEntry(sm.Value, out var smEntry) && ShouldPortLightmaps(smEntry))
                        {
                            Debug.WriteLine($"Not using dynamic lighting for mesh {smEntry.InstancedFullPath} on {exp.InstancedFullPath}");
                            var tcBin = ObjectBinary.From<StaticMeshComponent>(exp);
                            foreach (var lod in tcBin.LODData)
                            {
                                if (lod.LightMap is LightMap_2D lm2d)
                                {
                                    lm2d.ScaleVector2.X *= vTestOptions.LavaLightmapScalar;
                                    lm2d.ScaleVector2.Y *= vTestOptions.LavaLightmapScalar;
                                    lm2d.ScaleVector2.Z *= vTestOptions.LavaLightmapScalar;
                                }
                            }
                            exp.WriteBinary(tcBin);
                            continue; // We will try to use the original lightmaps for this
                        }
                    }
                }

                props.AddOrReplaceProp(new BoolProperty(false, "bUsePreComputedShadows"));
                props.AddOrReplaceProp(new BoolProperty(false, "bBioForcePreComputedShadows"));
                props.AddOrReplaceProp(new BoolProperty(false, "bCastDynamicShadow"));
                //props.AddOrReplaceProp(new BoolProperty(true, "CastShadow"));
                //props.AddOrReplaceProp(new BoolProperty(true, "bAcceptsDynamicDominantLightShadows"));
                props.AddOrReplaceProp(new BoolProperty(true, "bAcceptsLights"));
                props.AddOrReplaceProp(new BoolProperty(false, "bAcceptsDynamicLights"));

                var lightingChannels = props.GetProp<StructProperty>("LightingChannels") ??
                                       new StructProperty("LightingChannelContainer", false,
                                           new BoolProperty(true, "bIsInitialized"))
                                       {
                                           Name = "LightingChannels"
                                       };
                lightingChannels.Properties.AddOrReplaceProp(new BoolProperty(true, "Static"));
                lightingChannels.Properties.AddOrReplaceProp(new BoolProperty(true, "Dynamic"));
                lightingChannels.Properties.AddOrReplaceProp(new BoolProperty(true, "CompositeDynamic"));
                props.AddOrReplaceProp(lightingChannels);

                exp.WriteProperties(props);
            }

            foreach (ExportEntry exp in Pcc.Exports.Where(exp => exp.IsA("MeshComponent") && exp.Parent.IsA("DynamicSMActor"))) //Make interpactors dynamic
            {
                PropertyCollection props = exp.GetProperties();
                if (props.GetProp<BoolProperty>("bAcceptsLights")?.Value == false ||
                     props.GetProp<BoolProperty>("CastShadow")?.Value == false)
                {
                    // shadows/lighting has been explicitly forbidden, don't mess with it.
                    continue;
                }

                props.AddOrReplaceProp(new BoolProperty(false, "bUsePreComputedShadows"));
                props.AddOrReplaceProp(new BoolProperty(false, "bBioForcePreComputedShadows"));

                var lightingChannels = props.GetProp<StructProperty>("LightingChannels") ??
                                       new StructProperty("LightingChannelContainer", false,
                                           new BoolProperty(true, "bIsInitialized"))
                                       {
                                           Name = "LightingChannels"
                                       };
                lightingChannels.Properties.AddOrReplaceProp(new BoolProperty(true, "Dynamic"));
                props.AddOrReplaceProp(lightingChannels);
                props.AddOrReplaceProp(new BoolProperty(true, "bAcceptsDynamicLights")); // Added in crossgen
                exp.WriteProperties(props);
            }


            foreach (ExportEntry exp in Pcc.Exports.Where(exp => exp.IsA("LightComponent")))
            {
                PropertyCollection props = exp.GetProperties();
                //props.AddOrReplaceProp(new BoolProperty(true, "bCanAffectDynamicPrimitivesOutsideDynamicChannel"));
                //props.AddOrReplaceProp(new BoolProperty(true, "bForceDynamicLight"));

                var lightingChannels = props.GetProp<StructProperty>("LightingChannels") ??
                                       new StructProperty("LightingChannelContainer", false,
                                           new BoolProperty(true, "bIsInitialized"))
                                       {
                                           Name = "LightingChannels"
                                       };
                lightingChannels.Properties.AddOrReplaceProp(new BoolProperty(true, "Static"));
                lightingChannels.Properties.AddOrReplaceProp(new BoolProperty(true, "Dynamic"));
                lightingChannels.Properties.AddOrReplaceProp(new BoolProperty(true, "CompositeDynamic"));
                props.AddOrReplaceProp(lightingChannels);

                exp.WriteProperties(props);
            }
        }

        // Became unused when we figured out Terrains
        private static Guid? tempDonorGuid = null;
        private static void CorrectTerrainMaterials(IMEPackage le1File)
        {
            // Todo: Improve this... somehow
            if (tempDonorGuid == null)
            {
                using var donorMatP = MEPackageHandler.OpenMEPackage(Path.Combine(LE1Directory.CookedPCPath, "BIOA_PRO10_11_LAY.pcc"));
                var terrain = donorMatP.FindExport("TheWorld.PersistentLevel.Terrain_0");
                var terrbinD = ObjectBinary.From<Terrain>(terrain);
                tempDonorGuid = terrbinD.CachedTerrainMaterials[0].ID;
            }

            var fname = Path.GetFileNameWithoutExtension(le1File.FilePath);
            var terrains = le1File.Exports.Where(x => x.ClassName == "Terrain").ToList();
            foreach (var terrain in terrains)
            {
                var terrbin = ObjectBinary.From<Terrain>(terrain);

                foreach (var terrainMat in terrbin.CachedTerrainMaterials)
                {
                    terrainMat.ID = tempDonorGuid.Value;
                }

                terrain.WriteBinary(terrbin);
            }
        }

        public static void PortME1OptimizationAssets(IMEPackage me1File, IMEPackage le1File, VTestOptions vTestOptions)
        {
            // Disabled 08/18/2024 for lighting rebake.
            return;

#if FALSE
            foreach (var me1oa in ME1OptimizationAssets)
            {
                var le1Version = le1File.FindExport(me1oa);
                if (le1Version != null)
                {
                    var me1Version = me1File.FindExport(me1oa);
                    EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.ReplaceSingularWithRelink, me1Version, le1File, le1Version, true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out _);
                }
            }
#endif
        }

        // R&D for performance optimizations (2021)
        private static string[] PossibleLightMapUsers = new[]
        {
            "BIOA_Apartment_S.APT_ANgledCeinling_01", // sic
            "BIOA_Apartment_S.APT_BackWall",
            "BIOA_Apartment_S.APT_Central_Floor_01",
            "BIOA_Apartment_S.APT_ExtWall_01",
            "BIOA_Apartment_S.APT_Floor01",
            "BIOA_Apartment_S.APT_GlassBrace_01",
            "BIOA_Apartment_S.APT_HallCeiling_01",
            "BIOA_Apartment_S.APT_HallWall_01",
            "BIOA_Apartment_S.APT_HallWallB_01",
            "BIOA_Apartment_S.APT_KitchenWall_01",
            "BIOA_Apartment_S.APT_MainCeiling_01",
            "BIOA_Apartment_S.APT_MainFloor_01",
            "BIOA_Apartment_S.APT_MainGlass_01",
            "BIOA_Apartment_S.APT_Pillar_01",
            "BIOA_Apartment_S.APT_SMPillar_01",
            "BIOA_Apartment_S.APT_Stairs_01",
            "BIOA_Apartment_S.Apt_Window_01",
            "BIOA_Apartment_S.APT_WindowFrame_01",
            "BIOA_LAV60_S.LAV70_ROCKND01",
            "BIOA_NOR10_S.NOR10_opeingE01",
            "BIOA_NOR10_S.NOR10_poleA01",
            "BIOA_PRC2_MatFX.CCSim_Room_Mesh",
            "BIOA_PRC2_MatFX.LoadEffect_Mesh_S",
            "BIOA_PRC2_MatFX.Scoreboard_CCMesh",
            "BIOA_PRC2_MatFX.StrategicRing_S",
            "BIOA_PRC2_PlatformUI_T.Square_Plane",
            "BIOA_PRC2_S.Asteroids.LAV70_GROUNDROCKA_Dup",
            "BIOA_PRC2_S.Colision.Beach_COLISION01",
            "BIOA_PRC2_S.Colision.Lobby_COLISION",
            "BIOA_PRC2_S.Colision.Mid_COLISION01",
            "BIOA_PRC2_S.Colision.Sim_COLISION",
            "BIOA_PRC2_S.CommandCenter.CCFloorBeams",
            "BIOA_PRC2_S.lobby.LobbyRailings",
            "BIOA_PRC2_S.lobby.LobbyRoom_S",
            "BIOA_PRC2_S.Mid.SpaceVista_Frame_Btm_S",
            "BIOA_PRC2_S.Mid.SpaceVista_Frame_S",
            "BIOA_PRC2_S.Mid.SpaceVista_Window_S",
            "BIOA_PRC2_S.MidRoom01_New",
            "BIOA_PRC2_S.PRC2_SimWallFiller",
            "BIOA_PRC2_S.PRC2_SimWindowFiller",
            "BIOA_PRC2_S.Railing_Corner_Light",
            "BIOA_PRC2_S.ScoreBoardBackground",
            "BIOA_PRC2_S.Ships.Turian_Bomber_01",
            "BIOA_PRC2_S.Sim.SimFloorLines",
            "BIOA_PRC2_S.Sim.SimRoom01_S",
            "BIOA_PRC2_S.Sim.SimRoom01Railings",
            "BIOA_PRC2_S.Sim.SimRoom02_S",
            "BIOA_PRC2_S.Sim.SimRoom03_S",
            "BIOA_PRC2_S.Sim.SimRoom03Railings",
            "BIOA_PRC2_Scoreboard_T.shepard_plane",
            "BIOA_PRC2_Scoreboard_T.Square_Plane",
            "BIOA_PRC2_T.LAVCubemap.LAV60_siloA01_Dup",
            "BIOA_PRC2_T.LAVCubemap.LAV60_silohouseA06_Dup",
            "BIOA_PRC2_T.LAVCubemap.LAV60_silohouseC06_Dup",
            "BIOA_PRC2_T.LAVCubemap.LAV60_silohousewinA01_Dup",
            "BIOA_PRC2_T.LAVCubemap.LAV70_drvingpannel10_Dup",
            "BIOA_PRC2_T.LAVCubemap.LAV70_siloB01_Dup",
            "BIOA_WAR30_S.WAR30_LIGHTSHAFT",
            "BioBaseResources.HUD_Holograms.Meshes.Player_Pos__old",
            "BIOG_v_DLC_Vegas.Meshes.UNC_Vegas_Rings",

            // THE FOLLOWING WERE FOUND TO HAVE SAME LOD COUNT AND VERTICE COUNT
            // Calculated in PackageEditorExperimentsM
            "BIOA_ICE50_S.ice50_ceilingredo01",
            "BIOA_JUG80_S.jug80_damwall04",
            "BIOA_PRO10_S.PRO10_BEAMLIFT00",
            "BIOA_PRO10_S.PRO10_CONCRETEBASE",
            "BIOA_PRO10_S.PRO10_CONCRETEXCONNECTOR00",
            "BIOA_PRO10_S.PRO10_RAILINGCOVER",
            "BIOA_PRO10_S.PRO10_STATIONRAILINGA00",
            "BIOA_PRO10_S.PRO10_STATIONRAILINGB00",
            "BIOA_PRO10_S.PRO10_STATIONWALLPLAIN00",
            "BIOA_PRO10_S.PRO10_STATIONWALLTOPCOVER",
            "BIOA_PRO10_S.PRO10_WALKWAYBLOCK",
            "BIOA_PRO10_S.PRO10_WALLNOCROUCHA",
            "BIOA_STA30_S.sta30_ceilingsim",
            "BIOA_STA30_S.STA30_WALL10",
            "BIOA_STA30_S.sta30_wall16",
            "BIOG__SKIES__.LAV00.lav00_skybox_mld",
            "BIOA_WAR20_S.WAR20_BRIDGERUBBLEPILE",
            "BIOA_FRE10_S.CARGO_CONTAINER",
            "BIOA_FRE10_S.FRE10_CONTAINERCONNECTOR",
            "BIOA_ICE20_S.ice20_signeye",
            "BIOA_ICE50_S.ice50_metalpaneltube00",
            "BIOA_ICE60_S.ICE60_curvejointC00",
            "BIOA_ICE60_S.ICE60_glasspaneltubeC00",
            "BIOA_ICE60_S.ICE60_steelframeA02",
            "BIOA_LAV60_S.LAV60_catwalk_24",
            "BIOA_LAV70_S.LAV70_CABLE-RAIL01",
            "BIOA_LAV70_S.LAV70_CABLE01",
            "BIOA_LAV70_S.LAV70_catwalk_B01",
            "BIOA_LAV70_S.LAV70_CAVE02",
            "BIOA_LAV70_S.LAV70_CAVE02b",
            "BIOA_LAV70_S.LAV70_edgechunkB01",
            "BIOA_LAV70_S.LAV70_edgechunkB02",
            "BIOA_LAV70_S.LAV70_edgechunkB61",
            "BIOA_LAV70_S.LAV70_ELVATOR-METALBAR",
            "BIOA_LAV70_S.LAV70_ELVATOR-PARTA01",
            "BIOA_LAV70_S.LAV70_ground01",
            "BIOA_LAV70_S.LAV70_LIGHT13",
            "BIOA_LAV70_S.LAV70_SmallRocksLowA01",
            "BIOA_LAV70_S.LAV70_TUBE-BLACKD01",
            "BIOA_LAV70_S.PRO10_ROCKCOVERA193",
            "BIOA_LAV70_S.PRO10_ROCKCOVERB253",
            "BIOA_LAV70_S.LAV70_lift03",
            "BIOA_LAV70_S.LAV70_lift_brace02",
            "BIOA_LAV70_S.LAV70_lift_door04",
            "BIOA_LAV70_S.LAV70_lift_fin00",
            "BIOA_LAV70_S.LAV70_mmover01",
            "BIOA_LAV70_S.LAV70_ROCKSLIDELOPOLYC01",
            "BIOG_V_Env_Hologram_Z.Mesh.Holomod_04",
            "BIOG_V_Env_Hologram_Z.Mesh.Screen_01",
            "BIOA_JUG80_S.jug80_rooftopB",
            "BIOA_JUG80_S.jug80_wallA_01",
            "BIOA_WAR30_S.WAR20_STAIRSMIDDLECOLLUM10",
            "BIOA_LAV60_S.LAV60_catwalk_Short05",
            "BIOA_LAV60_S.LAV60_downlightA18",
            "BIOA_LAV60_S.LAV60_drvingpannel100",
            "BIOA_LAV60_S.LAV60_fan-anime01",
            "BIOA_LAV60_S.LAV60_pathbarB112",
            "BIOA_LAV60_S.LAV60_sunroofA01",
            "BIOA_NOR10_S.NOR10_signnumberD01",
            "BIOA_NOR10_S.NOR10D_sidewalkpannel01",
            "BIOA_ICE60_S.ICE60_6x8platA01",
            "BIOA_NOR10_S.NOR10_CAPTLIGHTA01",
            "BIOA_NOR10_S.NOR10_commanderroom_newA",
            "BIOA_NOR10_S.NOR10_SCREENTVA01",
            "BIOA_ICE50_S.ice50_glasspaneltube00",
            "BIOA_NOR10_S.NOR10_FIRSTBHANDLEB00",
            "BIOA_NOR10_S.NOR10_LIFESTAIRS",
            "BIOA_NOR10_S.NOR10Dseat01",
            "BIOA_ICE50_S.ice50_midbeam01",
            "BIOA_ICE60_S.ICE60_sloped00",
            "BIOA_NOR10_S.NOR10_FIRSTBHANDLE01",
            "BIOG_V_Env_Hologram_Z.Mesh.Holomod_02",
            "BIOA_NOR10_S.NOR10_FIRSTC01",
            "BIOA_NOR10_S.NOR10_pilotlink01",
            "BIOA_NOR10_S.NOR10D_console01",
            "BIOG_V_Env_Hologram_Z.Mesh.Holomod_03",
            "BIOG_V_Env_Hologram_Z.Mesh.Holomod_13_Mesh",
            "BIOG_V_Env_Hologram_Z.Mesh.Keyboard_01",
            "BIOA_NOR10_S.NOR10_signnumberB01",
            "BIOG_APL_INT_Chair_Comf_02_L.Chair_Comfortable_02",
            "BIOA_NOR10_S.NOR10_TVSET01",
            "BIOA_NOR10_S.NOR10_table",
            "BIOG_APL_PHY_Glasses_01_L.Glasses_01",
            "BIOG_V_Env_Hologram_Z.Mesh.Holomod_05",
            "BIOG_V_Env_Hologram_Z.Mesh.Holomod_09_Mesh",
            "BIOG_V_Env_Hologram_Z.Mesh.Holomod_11_Mesh",
            "BIOA_UNC50_T.Meshes.UNC53planet",
            "BIOA_JUG40_S.jug40_fenceALONG",
            "BIOA_JUG40_S.JUG40_Sail02",
            "BIOA_JUG40_S.Jug40_Sail20",
            "BIOA_JUG40_S.Jug40_Sail32",
            "BIOA_JUG40_S.JUG40_treebranchesA",
            "BIOA_JUG40_S.Jug40_Wires01",
            "BIOG__SKIES__.Samples.sam02",
            "BIOG_V_Env_Jug_WaveCrash_Z.Meshes.sprayPlanes",
            "BIOA_NOR10_S.NOR10_medbedhandleA01",
            "BIOA_PRO10_S.Pro10_BridgeHindge01",
            "BIOA_PRO10_S.PRO10_ROCKCOVERSHARDA",
            "BIOA_UNC20_T.UNC_HORIZONLINE",

            "BIOA_JUG40_S.CROSSGENFIX_SANDBARB" // This is done via special donor, renamed, but we want the original lightmaps as we use a few less poly version (but def the lightmaps)
        };

        // These assets will be used from ME1 (not using LE1's version). They must exist with same IFP in LE1 however. 
        // Objects that match these will use static lighting
        private static string[] ME1OptimizationAssets = new[]
        {
            "BIOA_JUG40_S.JUG40_ROCKCOVERA", // rock cube things
            "BIOA_JUG40_S.JUG40_ROCKCOVERSHARDA", // thing rock cover
            //"BIOA_JUG40_S.CROSSGENFIX_SANDBARB", // sandbars
            "BIOA_JUG40_S.jug40_pillarD_02", // Pillar things under those other things
            "BIOA_JUG40_S.jug40_fenceALONG", // fence piece
            "BIOA_JUG40_S.JUG40_BASERAILING00",
            "BIOA_JUG40_S.JUG40_BASERAILINGRAMP",
            "BIOA_JUG40_S.JUG20_RAMPSHEET",
            "BIOA_JUG40_S.jug40_Object01", // Pathway to the 'hole of doom'
        };

        // Not relevant anymore as static lighting build does not use this interp actor anymore
        private static void FixLighting(IMEPackage le1File, VTestOptions vTestOptions)
        {
            if (vTestOptions.useDynamicLighting)
            {
                var fname = Path.GetFileNameWithoutExtension(le1File.FilePath);
                switch (fname)
                {
                    case "BIOA_PRC2_CCAHERN":
                        // Truck shadow
                        var truckDLE = le1File.FindExport("TheWorld.PersistentLevel.InterpActor_0.DynamicLightEnvironmentComponent_6");
                        truckDLE.WriteProperty(new EnumProperty("LightShadow_ModulateBetter", "ELightShadowMode", MEGame.LE1, "LightShadowMode"));
                        break;
                }
            }
        }

        // Used during development while researching how terrains worked
        public static void ConvertME1TerrainComponent(ExportEntry exp)
        {
            // Strip Lightmap
            var b = ObjectBinary.From<TerrainComponent>(exp);
            b.LightMap = new LightMap() { LightMapType = ELightMapType.LMT_None };
            // Convert the tesselation... something... idk something that makes it multiply
            // by what appears to be 16x16 (256)
            var props = exp.GetProperties();
            //var sizeX = props.GetProp<IntProperty>("SectionSizeX");
            //var sizeY = props.GetProp<IntProperty>("SectionSizeY");
            //var trueSizeX = props.GetProp<IntProperty>("TrueSectionSizeX");
            //var trueSizeY = props.GetProp<IntProperty>("TrueSectionSizeY");

            //var factorSize = sizeX * sizeY; // idk
            //for (int i = 0; i < trueSizeY; i++)
            //{
            //    for (int j = 0; j < trueSizeX; j++)
            //    {
            //        // uh... idk?
            //        var collisionIdx = (i * trueSizeY) + j;
            //        var vtx = b.CollisionVertices[collisionIdx];
            //        b.CollisionVertices[collisionIdx] = new Vector3(vtx.X * factorSize, vtx.Y * factorSize, vtx.Z);
            //        Debug.WriteLine(collisionIdx + " " + b.CollisionVertices[collisionIdx].ToString());
            //    }
            //}

            // Correct collision vertices as they've changed from local to world in LE
            float scaleX = 256; // Default DrawScale3D for terrain is 256
            float scaleY = 256;
            float scaleZ = 256;

            float basex = 0;
            float basey = 0;
            float basez = 0;

            var ds3d = (exp.Parent as ExportEntry).GetProperty<StructProperty>("DrawScale3D");
            if (ds3d != null)
            {
                scaleX = ds3d.GetProp<FloatProperty>("X").Value;
                scaleY = ds3d.GetProp<FloatProperty>("Y").Value;
                scaleZ = ds3d.GetProp<FloatProperty>("Z").Value;
            }

            var ds = (exp.Parent as ExportEntry).GetProperty<FloatProperty>("DrawScale");
            if (ds != null)
            {
                scaleX *= ds.Value;
                scaleY *= ds.Value;
                scaleZ *= ds.Value;
            }

            var loc = (exp.Parent as ExportEntry).GetProperty<StructProperty>("Location");
            if (loc != null)
            {
                basex = loc.GetProp<FloatProperty>("X").Value;
                basey = loc.GetProp<FloatProperty>("Y").Value;
                basez = loc.GetProp<FloatProperty>("Z").Value;
            }

            // COLLISION VERTICES
            for (int i = 0; i < b.CollisionVertices.Length; i++)
            {
                var cv = b.CollisionVertices[i];
                Vector3 newV = new Vector3();

                newV.X = basex - (cv.X * scaleX);
                newV.Y = basey - (cv.Y * scaleY);
                newV.Z = basez + (cv.Z * scaleZ); // Is this right?
                b.CollisionVertices[i] = newV;
            }

            // Bounding Volume Tree
            Vector3 dif = new Vector3(86806.1f, -70072.58f, -6896.561f);
            for (int i = 0; i < b.BVTree.Length; i++)
            {
                var box = b.BVTree[i].BoundingVolume;
                box.Min = new Vector3 { X = basex - (box.Min.X * scaleX), Y = basey /*- (box.Min.Y * scaleY)*/, Z = basez + (box.Min.Z * scaleZ) };
                box.Max = new Vector3 { X = basex /*+ (box.Max.X * scaleX)*/, Y = basey + (box.Max.Y * scaleY), Z = basez + (box.Max.Z * scaleZ) };
            }

            exp.WriteBinary(b);

            // Make dynamic lighting
            //var props = exp.GetProperties();
            //props.RemoveNamedProperty("BlockRigidBody"); // make collidable?
            props.RemoveNamedProperty("ShadowMaps");
            props.AddOrReplaceProp(new BoolProperty(false, "bForceDirectLightMap"));
            props.AddOrReplaceProp(new BoolProperty(true, "bCastDynamicShadow"));
            props.AddOrReplaceProp(new BoolProperty(true, "bAcceptDynamicLights"));

            var lightingChannels = props.GetProp<StructProperty>("LightingChannels") ??
                                   new StructProperty("LightingChannelContainer", false,
                                       new BoolProperty(true, "bIsInitialized"))
                                   {
                                       Name = "LightingChannels"
                                   };
            lightingChannels.Properties.AddOrReplaceProp(new BoolProperty(true, "Static"));
            lightingChannels.Properties.AddOrReplaceProp(new BoolProperty(true, "Dynamic"));
            lightingChannels.Properties.AddOrReplaceProp(new BoolProperty(true, "CompositeDynamic"));
            props.AddOrReplaceProp(lightingChannels);

            exp.WriteProperties(props);
        }

        private static bool ShouldPortLightmaps(IEntry smEntry)
        {
            // 08/18/2024 - Lighting rebake via UDK means we should try to port all lightmaps, so when we rebake in UDK the lightmap data is present
            return true;

            // Disabled
            if (PossibleLightMapUsers.Contains(smEntry.InstancedFullPath))
            {
                return true; // We will try to use the original lightmaps for this
            }

            // Thai specific stuff as this map really needs some help
            // TEST ONLY
            // Install lightmap for the cover boxes
            if (ME1OptimizationAssets.Contains(smEntry.InstancedFullPath))
            {
                // Allow this as we will replace with ME1 mesh as it's lightmaps and lower tris
                Debug.WriteLine($"Not using dynamic lighting for THAI optimization, mesh {smEntry.InstancedFullPath}");
                return true;
            }

            return false;
        }
    }
}
