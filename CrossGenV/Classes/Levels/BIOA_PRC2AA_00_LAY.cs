using System.Collections.Generic;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Pathing;
using LegendaryExplorerCore.Unreal;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Unreal.ObjectInfo;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2AA_00_LAY : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PrePortingCorrection()
        {
            // 11/11/2024 - Remove the skeletal mesh versions of the weapons on the wall so we can static light bake them
            // 11/11/2024 - Attempted to use static lighting for these, but material doesn't support static meshes
            //foreach (var ex in me1File.Exports.Where(x => x.ClassName == "SkeletalMeshActor").ToList())
            //{
            //    me1File.RemoveFromLevelActors(ex);
            //}

            UpgradeLightmapSettings();
        }

        private void UpgradeLightmapSettings()
        {
            // Set properties to upgrade lighting resolution when baking lighting. This is fine to ship in release builds as it does nothing,
            // but if user tries to rebake lighting, it will be correct already.
            var lightMapRes = new CaseInsensitiveDictionary<int>();
            lightMapRes["TheWorld.PersistentLevel.StaticMeshCollectionActor_45.StaticMeshActor_54_SMC"] = 256; // Weapon bench
            lightMapRes["TheWorld.PersistentLevel.StaticMeshCollectionActor_45.StaticMeshActor_208_SMC"] = 1024; // Bottom floor
            lightMapRes["TheWorld.PersistentLevel.StaticMeshCollectionActor_45.StaticMeshActor_204_SMC"] = 1024; // Upper floor
            lightMapRes["TheWorld.PersistentLevel.StaticMeshCollectionActor_45.StaticMeshActor_205_SMC"] = 512; // Back wall
            lightMapRes["TheWorld.PersistentLevel.StaticMeshCollectionActor_45.StaticMeshActor_200_SMC"] = 512; // Weapon bench wall

            foreach (var lmr in lightMapRes)
            {
                var smc = me1File.FindExport(lmr.Key);
                smc.WriteProperty(new BoolProperty(true, "bOverrideLightMapRes"));
                smc.WriteProperty(new IntProperty(lmr.Value, "OverriddenLightMapRes"));
            }
        }

        public void PostPortingCorrection()
        {
            FixPlanters();

            // 11/11/2024 - Attempted to use static lighting for these, but material doesn't support static meshes
            // InstallStaticMeshWeapons();

            // 11/11/2024 - Force all lights in this level to compile static
            // This is for static lighting build
            foreach (var exp in le1File.Exports.Where(x => x.IsA("LightComponent")))
            {
                var lc = exp.GetProperty<StructProperty>("LightingChannels");
                if (lc == null)
                    continue;
                lc.Properties.AddOrReplaceProp(new BoolProperty(true, "Static"));
                exp.WriteProperty(lc);

                if (vTestOptions.isBuildForStaticLightingBake)
                {
                    // For static lighting bake, turn down all lighting as it turns level into light theme
                    var brightness = exp.GetProperty<FloatProperty>("Brightness") ?? new FloatProperty(1, "Brightness");
                    brightness.Value *= vTestOptions.ApartmentStaticLightScalar;
                    exp.WriteProperty(brightness);
                }
            }

            if (!vTestOptions.isBuildForStaticLightingBake)
            {
                VTestTerrain.PortInCorrectedTerrain(me1File, le1File, "PRC2AA_LAY.Terrain_1", "BIOA_UNC20_00_LAY.pcc", vTestOptions);
                VTestTerrain.CorrectTerrainSetup(me1File, le1File, vTestOptions);
                FixupLighting();
            }

            // Need to set 'bCanStepUpOn' = false for certain static meshes as the collision is mega jank
            var meshesToFix = new[]
            {
                            "TheWorld.PersistentLevel.StaticMeshActor_0",
                            "TheWorld.PersistentLevel.StaticMeshActor_1",
                            "TheWorld.PersistentLevel.StaticMeshActor_2",
                            "TheWorld.PersistentLevel.StaticMeshActor_3",
                        };

            foreach (var m in meshesToFix)
            {
                var exp = le1File.FindExport(m);
                exp.WriteProperty(new BoolProperty(false, "bCanStepUpOn"));
            }
        }

        private void InstallStaticMeshWeapons()
        {
            var layPackage = vTestOptions.vTestHelperPackage.FindExport("PRC2AA_LAY");
            var actors = vTestOptions.vTestHelperPackage.Exports.Where(x => x.Parent == layPackage && x.ClassName == "StaticMeshActor");
            foreach (var actor in actors)
            {
                EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, actor, le1File, le1File.FindExport("TheWorld.PersistentLevel"), true,
                    new RelinkerOptionsPackage() { Cache = vTestOptions.cache, PortExportsAsImportsWhenPossible = true }, out var levelActorI);

                le1File.AddToLevelActorsIfNotThere(levelActorI as ExportEntry);
            }
        }

        private void FixupLighting()
        {
            List<string> objectsToDynamicLight =
            [
                "TheWorld.PersistentLevel.BioUseable_0.StaticMeshComponent_38",
                "TheWorld.PersistentLevel.BioInert_1.StaticMeshComponent_41",
            ];

            objectsToDynamicLight.AddRange(le1File.Exports.Where(x => x.ClassName == "SkeletalMeshActor").Select(x => x.InstancedFullPath));


            foreach (var ifp in objectsToDynamicLight)
            {
                var comp = le1File.FindExport(ifp);
                comp.SetLightingChannels("bInitialized", "Dynamic", "CompositeDynamic", "Unnamed_1");
            }

            List<string> lightsToMakeDynamic =
            [
                "TheWorld.PersistentLevel.StaticLightCollectionActor_16.PointLight_54_LC",
                "TheWorld.PersistentLevel.StaticLightCollectionActor_16.PointLight_57_LC",
                "TheWorld.PersistentLevel.StaticLightCollectionActor_16.PointLight_58_LC",
                "TheWorld.PersistentLevel.StaticLightCollectionActor_16.PointLight_59_LC",
                "TheWorld.PersistentLevel.StaticLightCollectionActor_16.SpotLight_8_LC",
                "TheWorld.PersistentLevel.StaticLightCollectionActor_16.SpotLight_5_LC",
            ];

            foreach (var lightIFP in lightsToMakeDynamic)
            {
                var lightComp = le1File.FindExport(lightIFP);
                lightComp.SetLightingChannels("bInitialized", "Static", "Dynamic", "CompositeDynamic", "Unnamed_1");
            }
        }

        private void FixPlanters()
        {
            using var planterSource = MEPackageHandler.OpenMEPackage(Path.Combine(MEDirectories.GetCookedPath(MEGame.LE1), "BIOA_ICE20_03_DSG.pcc"), forceLoadFromDisk: true);

            // PLANTER HIGH (DOOR)
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, planterSource.FindExport("TheWorld.PersistentLevel.InterpActor_67"), le1File, le1File.FindEntry("TheWorld.PersistentLevel"), true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out var leavesHighSMA);
            LevelTools.SetLocation(leavesHighSMA as ExportEntry, -35043.76f, 10664f, 6792.9917f);

            // PLANTER MEDIUM (NEARBED)
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, planterSource.FindExport("TheWorld.PersistentLevel.InterpActor_23"), le1File, le1File.FindEntry("TheWorld.PersistentLevel"), true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out var leavesMedSMA);
            LevelTools.SetLocation(leavesMedSMA as ExportEntry, -35470.273f, 11690.752f, 6687.2974f + (112f * 0.6f)); // 112f is the offset

            // PLANTER MEDIUM (TABLE)
            var leavesMed2SMA = EntryCloner.CloneTree(leavesMedSMA);
            LevelTools.SetLocation(leavesMed2SMA as ExportEntry, -34559.5f, 11378.695f, 6687.457f + (112f * 0.6f)); // 112f is the offset
        }
    }
}
