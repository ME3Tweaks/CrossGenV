using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Helpers;

namespace CrossGenV.Classes
{
    /// <summary>
    /// Methods for dealing with the pain that is terrain
    /// </summary>
    public class VTestTerrain
    {
        public static void PortInCorrectedTerrain(IMEPackage me1File, IMEPackage le1File, string vTestIFP, string materialsFile, VTestOptions vTestOptions)
        {
            // Port in the material's file terrain - but not it's subcomponents
            using var le1VanillaTerrainP = MEPackageHandler.OpenMEPackage(Path.Combine(LE1Directory.CookedPCPath, materialsFile));
            var le1DonorTerrain = le1VanillaTerrainP.Exports.FirstOrDefault(x => x.ClassName == "Terrain");
            le1DonorTerrain.RemoveProperty("TerrainComponents");

            var rop = new RelinkerOptionsPackage() { Cache = vTestOptions.cache };
            var le1TerrainBin = ObjectBinary.From<Terrain>(le1DonorTerrain);
            le1TerrainBin.WeightedTextureMaps = [0]; // These don't work with our different data format for these maps
            le1DonorTerrain.WriteBinary(le1TerrainBin);

            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, le1DonorTerrain, le1File,
                le1File.FindExport("TheWorld.PersistentLevel"), true, rop, out var destTerrainEntry);
            var destTerrain = destTerrainEntry as ExportEntry;
            destTerrain.indexValue = 2; // To match UDK for static lighting

            // Port in the precomputed components
            var me1Terrain = me1File.Exports.First(x => x.ClassName == "Terrain");
            var sourceTerrain = vTestOptions.vTestHelperPackage.FindExport(vTestIFP);
            var le1DonorTerrainComponents = sourceTerrain.GetProperty<ArrayProperty<ObjectProperty>>("TerrainComponents");
            var me1TerrainComponents = me1Terrain.GetProperty<ArrayProperty<ObjectProperty>>("TerrainComponents");
            ArrayProperty<ObjectProperty> components = new ArrayProperty<ObjectProperty>("TerrainComponents");
            for (int i = 0; i < me1TerrainComponents.Count; i++)
            {
                var me1SubComp = me1TerrainComponents[i].ResolveToEntry(me1File) as ExportEntry;
                var tcomp = le1DonorTerrainComponents[i];
                var tcompE = vTestOptions.vTestHelperPackage.GetEntry(tcomp.Value);
                var le1SubComp = le1DonorTerrainComponents[i].ResolveToEntry(vTestOptions.vTestHelperPackage) as ExportEntry;
                rop.CrossPackageMap.Clear();
                EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, vTestOptions.vTestHelperPackage.GetUExport(le1SubComp.UIndex), le1File,
                    destTerrain, true, rop, out var newSubComp);
                components.Add(new ObjectProperty(newSubComp.UIndex));
                var portedTC = newSubComp as ExportEntry;
                if (vTestOptions.portTerrainLightmaps)
                {
                    // Install the original lightmaps
                    var me1TC = ObjectBinary.From<TerrainComponent>(me1SubComp);
                    var le1TC = ObjectBinary.From<TerrainComponent>(portedTC);

                    if (me1TC.LightMap is LightMap_2D lm2d)
                    {
                        le1TC.LightMap = me1TC.LightMap;
                        var le1LM = le1TC.LightMap as LightMap_2D; // This is same ref, I suppose...
                                                                   // Port textures
                        if (lm2d.Texture1 > 0)
                        {
                            EntryExporter.ExportExportToPackage(me1File.GetUExport(lm2d.Texture1), le1File, out var tex1, vTestOptions.cache);
                            le1LM.Texture1 = tex1.UIndex;
                        }
                        if (lm2d.Texture2 > 0)
                        {
                            EntryExporter.ExportExportToPackage(me1File.GetUExport(lm2d.Texture2), le1File, out var tex2, vTestOptions.cache);
                            le1LM.Texture2 = tex2.UIndex;
                        }
                        if (lm2d.Texture3 > 0)
                        {
                            EntryExporter.ExportExportToPackage(me1File.GetUExport(lm2d.Texture3), le1File, out var tex3, vTestOptions.cache);
                            le1LM.Texture3 = tex3.UIndex;
                        }
                        if (lm2d.Texture4 > 0)
                        {
                            EntryExporter.ExportExportToPackage(me1File.GetUExport(lm2d.Texture4), le1File, out var tex4, vTestOptions.cache);
                            le1LM.Texture4 = tex4.UIndex;
                        }
                    }

                    portedTC.WriteBinary(le1TC);
                }

                // Port over component properties
                var propertiesME1 = me1SubComp.GetProperties();
                var shadowMaps = propertiesME1.GetProp<ArrayProperty<ObjectProperty>>("ShadowMaps");
                propertiesME1.RemoveNamedProperty("ShadowMaps");

                if (shadowMaps != null && shadowMaps.Any())
                {
                    var newShadowMaps = new ArrayProperty<ObjectProperty>("ShadowMaps");
                    // We need to port in shadowmaps.
                    foreach (var me1ShadowMap in shadowMaps)
                    {
                        EntryExporter.ExportExportToPackage(me1File.GetUExport(me1ShadowMap.Value), le1File, out var portedSMEntry, vTestOptions.cache);
                        portedSMEntry.idxLink = newSubComp.UIndex; // Move under the component like it does in LE1
                        newShadowMaps.Add(new ObjectProperty(portedSMEntry));
                    }
                    propertiesME1.Add(newShadowMaps);
                }

                portedTC.WriteProperties(propertiesME1); // The original game has no object refs
                                                         //foreach (var prop in propertiesME1.Where(x => x is ArrayProperty<StructProperty> or BoolProperty))
                                                         //{
                                                         //    portedTC.WriteProperty(prop); // Irrelevant lights, some lighting bools
                                                         //}
            }
            destTerrain.WriteProperty(components);

            // Manual fixes for VTest
            destTerrain.RemoveProperty("PrePivot"); // on lav60 donor terrain

            // Update the main terrain with our data, without touching anything about materials or layers
            VTestSupport.ImportUDKTerrainData(sourceTerrain, destTerrain, false);
        }

        public static void CorrectTerrainSetup(IMEPackage me1File, IMEPackage le1File, VTestOptions vTestOptions)
        {
            if (vTestOptions.isBuildForStaticLightingBake)
                return; // Do not make corrections.

            // Correct AlphaMaps to match the original
            var mTerrain = me1File.Exports.First(x => x.ClassName == "Terrain");
            var me1Terrain = ObjectBinary.From<Terrain>(mTerrain);
            var lTerrain = le1File.Exports.First(x => x.ClassName == "Terrain");
            var le1Terrain = ObjectBinary.From<Terrain>(lTerrain);

            var properties = lTerrain.GetProperties();

            var fName = le1File.FileNameNoExtension;
            var alphamaps = new List<AlphaMap>();

            if (fName == "BIOA_PRC2_CCLAVA")
            {
                // 5 maps (vs 4 in the source)
                alphamaps.Add(me1Terrain.AlphaMaps[0]); // Default
                alphamaps.Add(me1Terrain.AlphaMaps[1]); // RiverOverride
                alphamaps.Add(new AlphaMap() { Data = new byte[me1Terrain.Heights.Length] }); // Water Rock Override (NOT USED)
                alphamaps.Add(me1Terrain.AlphaMaps[2]); // Rock02 Override (?)
                alphamaps.Add(new AlphaMap() { Data = new byte[me1Terrain.Heights.Length] }); // BLANK (NOT USED)

                CorrectTerrainMaterialsAndSlopes(mTerrain, lTerrain, false, vTestOptions); // Needs changes to avoid patches
            }
            else if (fName == "BIOA_PRC2AA_00_LAY")
            {

                // THERE ARE NO ALPHAMAPS
                CorrectTerrainMaterialsAndSlopes(mTerrain, lTerrain, true, vTestOptions); // Needs changes to avoid patches
            }
            else if (fName == "BIOA_PRC2_CCAHERN")
            {
                // The only layer used is Rock02. The other terrain layer is not used
                // We must correct the AlphaMapIndexes
                alphamaps.Add(me1Terrain.AlphaMaps[0]);
                var layers = properties.GetProp<ArrayProperty<StructProperty>>("Layers");
                for (int i = 0; i < layers.Count; i++)
                {
                    var layer = layers[i];
                    if (i == 3)
                    {
                        // ROCK02
                        layer.Properties.AddOrReplaceProp(new IntProperty(0, "AlphaMapIndex"));
                    }
                    else
                    {
                        layer.Properties.AddOrReplaceProp(new IntProperty(-1, "AlphaMapIndex"));
                    }
                }
            }

            le1Terrain.AlphaMaps = alphamaps.ToArray();
            lTerrain.WritePropertiesAndBinary(properties, le1Terrain);
        }

        private static void CorrectTerrainMaterialsAndSlopes(ExportEntry mTerrain, ExportEntry lTerrain, bool prc2aa, VTestOptions vTestOptions)
        {
            var le1File = lTerrain.FileRef;
            var me1File = mTerrain.FileRef;

            var mLayers = mTerrain.GetProperty<ArrayProperty<StructProperty>>("Layers");
            var lLayers = lTerrain.GetProperty<ArrayProperty<StructProperty>>("Layers");


            foreach (var lLayer in lLayers)
            {
                // Find matching mLayer
                var lSetup = lLayer.GetProp<ObjectProperty>("Setup").ResolveToEntry(le1File) as ExportEntry;
                ExportEntry mSetup = null;
                foreach (var mSetupStruct in mLayers)
                {
                    var mSetupTmp = mSetupStruct.GetProp<ObjectProperty>("Setup").ResolveToEntry(me1File) as ExportEntry;
                    if (mSetupTmp != null && mSetupTmp.InstancedFullPath == lSetup.InstancedFullPath)
                    {
                        mSetup = mSetupTmp;
                        break;
                    }
                }

                if (mSetup == null)
                    continue; // Don't update this

                var mMaterials = mSetup.GetProperty<ArrayProperty<StructProperty>>("Materials");
                var lMaterials = lSetup.GetProperty<ArrayProperty<StructProperty>>("Materials");

                if (prc2aa && mMaterials.Count == lMaterials.Count)
                {
                    // ONLY PRC2AA WILL RUN THIS CODE
                    for (int i = 0; i < mMaterials.Count; i++)
                    {
                        var mMat = mMaterials[i];
                        var lMat = lMaterials[i];

                        foreach (var prop in mMat.Properties)
                        {
                            if (prop is ObjectProperty)
                                continue; // Do not change

                            lMat.Properties.AddOrReplaceProp(prop);
                        }
                    }
                }

                // CORRECTIONS
                var setupName = mSetup.ObjectName.Name;
                switch (setupName)
                {
                    case "UNC20_TLSetup_lessDispl": // PRC2AA_00_LAY
                        {
                            // Memory Unique
                            var cgv = le1File.FindExport("CROSSGENV");
                            if (cgv == null)
                            {
                                cgv = ExportCreator.CreatePackageExport(lTerrain.FileRef, "CROSSGENV", null);
                                cgv.indexValue = 0;
                            }

                            lSetup.Parent = cgv;
                            lSetup.ObjectName = "CROSSGENV_PRC2AA_TerrainLayerSetup";

                            // Slope Changes
                            lMaterials[0].GetProp<StructProperty>("MinSlope").GetProp<FloatProperty>("Base").Value = 2; // 90 degrees never
                            lMaterials[1].GetProp<StructProperty>("MinSlope").GetProp<FloatProperty>("Base").Value = 0; // 0 degrees always
                        }

                        break;
                    case "lav60_RIVER01_terrain_setup": // PRC2_CCLAVA
                        {
                            if (!vTestOptions.isBuildForStaticLightingBake)
                            {
                                // Memory Unique
                                var cgv = le1File.FindExport("CROSSGENV");
                                if (cgv == null)
                                {
                                    cgv = ExportCreator.CreatePackageExport(lTerrain.FileRef, "CROSSGENV", null);
                                    cgv.indexValue = 0;
                                }

                                lSetup.Parent = cgv;
                                lSetup.ObjectName = "CROSSGENV_PRC2_CCLAVA_TerrainLayerSetup";

                                // No idea why this fixes it tbh but it does
                                lMaterials[0].GetProp<FloatProperty>("Alpha").Value = 0;
                                lMaterials[1].GetProp<FloatProperty>("Alpha").Value = 0;
                            }
                        }
                        break;
                }


                lSetup.WriteProperty(lMaterials);

            }
        }
    }
}
