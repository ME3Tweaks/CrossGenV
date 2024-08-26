using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Gammtek.Extensions;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Localization;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Pathing;
using LegendaryExplorerCore.UDK;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal.ObjectInfo;

namespace CrossGenV.Classes
{
    public class VTestExperiment
    {

        #region Vars
        /// <summary>
        /// List of things to port when porting a level with VTest
        /// </summary>
        private static string[] ClassesToVTestPort = new[]
        {
            "InterpActor",
            "BioInert",
            "BioUsable",
            "BioPawn",
            "SkeletalMeshActor",
            "PostProcessVolume",
            "BioMapNote",
            "Note",
            "BioTrigger",
            "BioSunActor",
            "BlockingVolume",
            "BioDoor",
            "StaticMeshCollectionActor",
            "StaticLightCollectionActor",
            "ReverbVolume",
            "BioAudioVolume",
            "AmbientSound",
            "BioLedgeMeshActor",
            "BioStage",
            "HeightFog",
            "PrefabInstance",
            "CameraActor",
            //"Terrain", // Do not port in - we will specifically port this with a special donor system
            //"Model", // Do not port in - we will specifically port the level model in to prevent donor system 

            // Pass 2
            "StaticMeshActor",
            "TriggerVolume",
            "BioSquadCombat",
            "PhysicsVolume",
            "BioWp_ActionStation",
            "BioLookAtTarget",
            "BioUsable",
            "BioContainer",

            // Pass 3
            //"Brush", // R A G E
            "PathNode",
            "BioCoverVolume",
            "BioTriggerVolume",
            "BioWp_AssaultPoint",
            "BioSquadPlayer",
            "BioUseable",
            "BioSquadSitAndShoot",
            "CoverLink",
            "BioWaypointSet",
            "BioPathPoint",
            "Emitter"
        };

        /// <summary>
        /// Classes to port only for master level files
        /// </summary>
        private static string[] ClassesToVTestPortMasterOnly = new[]
        {
            "PlayerStart",
            "BioTriggerStream"
        };

        #endregion

        #region Main porting methods

        private static string GetAssetCachePath()
        {
            return Path.Combine(VTestPaths.VTest_DonorsDir, "Z_CrossgenV_AssetCache.pcc");
        }

        private static string GetObjectDBPath()
        {
            var lexPath = Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\LegendaryExplorer\").FullName;
            var dbPath = Directory.CreateDirectory(Path.Combine(lexPath, "ObjectDatabases")).FullName;
            return Path.Combine(dbPath, $"{MEGame.LE1}.bin");
        }

        /// <summary>
        /// Internal single-thread VTest session
        /// </summary>
        /// <param name="vTestOptions"></param>
        public static void RunVTest(VTestOptions vTestOptions)
        {
            Debug.WriteLine("Beginning VTest");
            Debug.WriteLine($"Cache GUID: {vTestOptions.cache.guid}");
            //string matPath = AppDirectories.GetMaterialGuidMapPath(MEGame.ME1);
            //Dictionary<Guid, string> me1MaterialMap = null;
            vTestOptions.SetStatusText("Loading databases");

            vTestOptions.objectDB = ObjectInstanceDB.Deserialize(MEGame.LE1, new MemoryStream(File.ReadAllBytes(GetObjectDBPath())));

            vTestOptions.SetStatusText("Inventorying donors");

            // Add extra donors and VTestHelper package
            foreach (var file in Directory.GetFiles(VTestPaths.VTest_DonorsDir))
            {
                if (file.RepresentsPackageFilePath())
                {
                    if (Path.GetFileNameWithoutExtension(file) == "VTestHelper")
                    {
                        // Load the VTestHelper, don't index it
                        vTestOptions.SetStatusText($@"Inventorying VTestHelper");
                        vTestOptions.vTestHelperPackage = MEPackageHandler.OpenMEPackage(file, forceLoadFromDisk: true); // Do not put into cache

                        // Inventory the classes from vtest helper to ensure they can be created without having to be in the 
                        // code for LEC
                        var vTestSeqObjClasses = vTestOptions.vTestHelperPackage.Exports.Where(x =>
                            x.IsClass && x.InheritsFrom("SequenceObject")).ToList();
                        foreach (var e in vTestSeqObjClasses)
                        {
                            var classInfo = GlobalUnrealObjectInfo.generateClassInfo(e);
                            var defaults = vTestOptions.vTestHelperPackage.GetUExport(ObjectBinary.From<UClass>(e).Defaults);
                            vTestOptions.SetStatusText($@"  Inventorying class {e.InstancedFullPath}");
                            GlobalUnrealObjectInfo.GenerateSequenceObjectInfoForClassDefaults(defaults);
                            GlobalUnrealObjectInfo.InstallCustomClassInfo(e.ObjectName, classInfo, e.Game);
                        }
                    }
                    else
                    {
                        // Inventory
                        vTestOptions.SetStatusText($@"Inventorying {Path.GetFileName(file)}");
                        using var p = MEPackageHandler.OpenMEPackage(file);
                        vTestOptions.objectDB.AddFileToDB(p, p.FilePath);
                    }
                }
            }

            if (!vTestOptions.debugBuildAssetCachePackage && File.Exists(GetAssetCachePath()))
            {
                // Make the asset package resident so it won't be dropped
                var resident = vTestOptions.cache.GetCachedPackage(GetAssetCachePath(), true);
                vTestOptions.cache.AddResidentPackage(resident);
            }

            vTestOptions.SetStatusText("Clearing mod folder");
            // Clear out dest dir
            foreach (var f in Directory.GetFiles(VTestPaths.VTest_FinalDestDir))
            {
                File.Delete(f);
            }

            // Copy in precomputed files
            vTestOptions.SetStatusText("Copying precomputed files");
            foreach (var f in Directory.GetFiles(VTestPaths.VTest_PrecomputedDir))
            {
                File.Copy(f, Path.Combine(VTestPaths.VTest_FinalDestDir, Path.GetFileName(f)));
            }

            // If we are building an asset cache package we initialize it here
            if (vTestOptions.debugBuildAssetCachePackage)
            {
                var assetCachePath = GetAssetCachePath();
                CreateEmptyLevel(assetCachePath);
                vTestOptions.assetCachePackage = MEPackageHandler.OpenMEPackage(assetCachePath, forceLoadFromDisk: true);
            }

            vTestOptions.SetStatusText("Running VTest");

            // VTest File Loop ---------------------------------------
            var rootCache = vTestOptions.cache;
            foreach (var vTestLevel in vTestOptions.vTestLevels)
            {
                var levelFiles = Directory.GetFiles(Path.Combine(VTestPaths.VTest_SourceDir, vTestLevel)).ToList();
                PortFile(levelFiles[0], vTestLevel, vTestOptions); // Master file is first in the list.
                levelFiles.RemoveAt(0);

                //foreach (var f in levelFiles)
                Parallel.ForEach(levelFiles, f =>
                {
                    // Uncomment to filter for iteration
                    //if (!f.Contains("lobby02_lay", StringComparison.OrdinalIgnoreCase))
                    //    return;

                    if (f.Contains("_LOC_", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Check if we should port this in this session.
                        if (!f.Contains("_LOC_INT", StringComparison.InvariantCultureIgnoreCase) && !vTestOptions.portAudioLocalizations)
                        {
                            return; // Do not port this non-int file.
                        }
                    }

                    vTestOptions.cache = vTestOptions.cache.ChainNewCache();
                    PortFile(f, vTestLevel, vTestOptions);
                });
            }

            vTestOptions.cache = rootCache.ChainNewCache();

            // TLKS ARE DONE POST ONLY
            VTestTLK.PostUpdateTLKs(vTestOptions);

            // 08/23/2024 - Externalize lightmap textures. There are not enough new textures to
            // make it worth doing non-lighting (also annoying bugs I don't want to fix in TFC Compactor)
            if (!vTestOptions.isBuildForStaticLightingBake)
            {
                VTestTextures.MoveTexturesToTFC("Lighting", "DLC_MOD_Vegas", true);
                // Lighting doesn't need compacted as it won't have dupes (or if it does, very, very few)
            }

            // 08/23/2024 - Add package resynthesis for cleaner output
            if (vTestOptions.resynthesizePackages)
            {
                foreach (var packagePath in Directory.GetFiles(VTestPaths.VTest_FinalDestDir)
                             .Where(x => x.RepresentsPackageFilePath()))
                {
                    var package = MEPackageHandler.OpenMEPackage(packagePath);
                    PackageResynthesizer.ResynthesizePackage(package, vTestOptions.cache);
                }
            }

            // VTest post QA
            vTestOptions.SetStatusText("Performing checks");

            // Perform checks on all files
            foreach (var f in Directory.GetFiles(VTestPaths.VTest_FinalDestDir))
            {
                if (f.RepresentsPackageFilePath())
                {
                    using var p = MEPackageHandler.OpenMEPackage(f);
                    VTestVerify.VTest_CheckFile(p, vTestOptions);
                }
            }

            // If we are building an asset cache package we initialize it here
            if (vTestOptions.debugBuildAssetCachePackage)
            {
                vTestOptions.SetStatusText("Saving Asset Cache");

                // Remove 'TheWorld' so it's just assets
                EntryPruner.TrashEntries(vTestOptions.assetCachePackage, vTestOptions.assetCachePackage.Exports.Where(x => x.InstancedFullPath.StartsWith("TheWorld")).ToList());

                vTestOptions.assetCachePackage.Save();
            }

            if (vTestOptions.isBuildForStaticLightingBake)
            {
                string[] levels = ["ccthai", "cccrate", "cccave", "cclava", "ccahern", "prc2aa"];

                foreach (var level in levels)
                {
                    vTestOptions.SetStatusText($"Generating UDK lighting master for {level}");
                    var path = Path.Combine(VTestPaths.VTest_FinalDestDir, $"BioP_UDKLighting_{level.UpperFirst()}.pcc");
                    MEPackageHandler.CreateEmptyLevel(path, MEGame.LE1);
                    using var destPackage = (MEPackage)MEPackageHandler.OpenMEPackage(path);

                    foreach (var f in Directory.GetFiles(VTestPaths.VTest_FinalDestDir).Where(x => x.Contains(level, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (f.Contains("UDKLighting"))
                            continue;
                        if (f.Contains("_LOC_"))
                            continue;
                        destPackage.AdditionalPackagesToCook.Add(Path.GetFileNameWithoutExtension(f));
                    }

                    destPackage.Save();
                }

                // PRC2 LOBBY (NON-SIMULATOR)
                {
                    vTestOptions.SetStatusText($"Generating UDK lighting master for PRC2 NON-SIMULATOR");
                    var path = Path.Combine(VTestPaths.VTest_FinalDestDir, $"BioP_UDKLighting_NonSimulator.pcc");
                    MEPackageHandler.CreateEmptyLevel(path, MEGame.LE1);
                    using var destPackage = (MEPackage)MEPackageHandler.OpenMEPackage(path);
                    destPackage.AdditionalPackagesToCook.AddRange(File.ReadAllLines(Path.Combine(VTestPaths.VTest_StaticLightingDir, "SLLevels-NonSimulator.txt")).Where(x => x.GetUnrealLocalization() == MELocalization.None));
                    destPackage.Save();
                }

                // PRC2AA
                {
                    vTestOptions.SetStatusText($"Generating UDK lighting master for PRC2AA");
                    var path = Path.Combine(VTestPaths.VTest_FinalDestDir, $"BioP_UDKLighting_PRC2AA.pcc");
                    MEPackageHandler.CreateEmptyLevel(path, MEGame.LE1);
                    using var destPackage = (MEPackage)MEPackageHandler.OpenMEPackage(path);
                    destPackage.AdditionalPackagesToCook.AddRange(File.ReadAllLines(Path.Combine(VTestPaths.VTest_StaticLightingDir, "SLLevels-PRC2AA.txt")).Where(x => x.GetUnrealLocalization() == MELocalization.None));
                    destPackage.Save();
                }
            }
        }

        private static void PortFile(string levelFileName, string masterMapName, VTestOptions vTestOptions)
        {
            var levelName = Path.GetFileNameWithoutExtension(levelFileName);
            //if (!levelName.CaseInsensitiveEquals("BIOA_PRC2_CCLAVA"))
            //    return;

            if (levelFileName.Contains("_LOC_", StringComparison.InvariantCultureIgnoreCase))
            {
                //if (levelFileName.Contains("ccsim", StringComparison.InvariantCultureIgnoreCase))
                PortLOCFile(levelFileName, vTestOptions);
            }
            else
            {
                //if (levelName.CaseInsensitiveEquals("BIOA_PRC2_CCLAVA"))
                PortVTestLevel(masterMapName, levelName, vTestOptions, levelName is "BIOA_PRC2" or "BIOA_PRC2AA", true);
            }
        }

        

        /// <summary>
        /// Ports a level file for VTest. Saves package at the end.
        /// </summary>
        /// <param name="mapName">Overarching map name</param>
        /// <param name="sourceName">Full map file name</param>
        /// <param name="finalDestDir"></param>
        /// <param name="sourceDir"></param>
        /// <param name="db"></param>
        /// <param name="pe"></param>
        /// <param name="syncBioWorldInfo"></param>
        /// <param name="portMainSequence"></param>
        private static void PortVTestLevel(string mapName, string sourceName, VTestOptions vTestOptions, bool syncBioWorldInfo = false, bool portMainSequence = false)
        {
            var outputFile = $@"{VTestPaths.VTest_FinalDestDir}\{sourceName.ToUpper()}.pcc";
            CreateEmptyLevel(outputFile);

            using var le1File = MEPackageHandler.OpenMEPackage(outputFile, forceLoadFromDisk: true);
            var name = $@"{VTestPaths.VTest_SourceDir}\{mapName}\{sourceName}.SFM";
            if (!File.Exists(name))
            {
                name = $@"{VTestPaths.VTest_SourceDir}\{mapName}\{sourceName}.u";
            }
            using var me1File = MEPackageHandler.OpenMEPackage(name, forceLoadFromDisk: true);

            var levelName = Path.GetFileNameWithoutExtension(le1File.FilePath);

            vTestOptions.SetStatusText($"Preparing {levelName}");

            // BIOC_BASE -> SFXGame
            var bcBaseIdx = me1File.findName("BIOC_Base");
            me1File.replaceName(bcBaseIdx, "SFXGame");

            // BIOG_StrategicAI -> SFXStrategicAI
            var bgsaiBaseIdx = me1File.findName("BIOG_StrategicAI");
            if (bgsaiBaseIdx >= 0)
                me1File.replaceName(bgsaiBaseIdx, "SFXStrategicAI");

            CorrectFileForLEXMapFileDefaults(me1File, le1File, vTestOptions);
            PrePortingCorrections(me1File, vTestOptions);

            //if (levelName == "BIOA_PRC2_CCMAIN_CONV")
            //{
            //    me1File.Save(@"C:\Users\Mgame\Desktop\conv.sfm");
            //}

            // Once we are confident in porting we will just take the actor list from PersistentLevel
            // For now just port these
            var itemsToPort = new List<ExportEntry>();

            var me1PL = me1File.FindExport(@"TheWorld.PersistentLevel");
            var me1PersistentLevel = ObjectBinary.From<Level>(me1PL);

            itemsToPort.AddRange(me1PersistentLevel.Actors.Where(x => x != 0) // Skip blanks
                .Select(x => me1File.GetUExport(x))
                .Where(x => ClassesToVTestPort.Contains(x.ClassName) || (syncBioWorldInfo && ClassesToVTestPortMasterOnly.Contains(x.ClassName))
                                                                     // Allow porting terrain if doing static lighting build because we don't care about the collision data
                                                                     || (vTestOptions.isBuildForStaticLightingBake && x.ClassName == "Terrain")));

            if (vTestOptions.debugBuild && vTestOptions.debugConvertStaticLightingToNonStatic)
            {
                // Lights are baked into the files but they are not part of the actors list. We have to manually find these
                var lights = me1File.Exports.Where(x => x.Parent == me1PL && x.IsA("Light") && x.ClassName != "StaticLightCollectionActor").ToList();
                foreach (var light in lights)
                {
                    // Lights lose their settings when coalesced into a SLCA
                    light.ObjectFlags = 0; // Clear
                    light.ObjectFlags |= UnrealFlags.EObjectFlags.Transactional | UnrealFlags.EObjectFlags.LoadForClient | UnrealFlags.EObjectFlags.LoadForServer | UnrealFlags.EObjectFlags.LoadForEdit | UnrealFlags.EObjectFlags.HasStack;
                }
                itemsToPort.AddRange(lights);
            }

            // WIP: Find which classes we have yet to port
            // BioWorldInfo is not ported except on the level master. Might need to see if there's things
            // like scene desaturation in it worth porting.
            //foreach (var v in me1PersistentLevel.Actors)
            //{
            //    var entry = v != 0 ? v.GetEntry(me1File) : null;
            //    if (entry != null && !actorTypesNotPorted.Contains(entry.ClassName) && !ClassesToVTestPort.Contains(entry.ClassName) && !ClassesToVTestPortMasterOnly.Contains(entry.ClassName) && entry.ClassName != "BioWorldInfo")
            //    {
            //        actorTypesNotPorted.Add(entry.ClassName);
            //    }
            //}

            // End WIP

            VTestFilePorting(me1File, le1File, itemsToPort, vTestOptions);

            RelinkerOptionsPackage rop = new RelinkerOptionsPackage()
            {
                Cache = vTestOptions.cache,
                IsCrossGame = true,
                ImportExportDependencies = true,
                TargetGameDonorDB = vTestOptions.objectDB,
                ErrorOccurredCallback = x =>
                {
                    Debug.WriteLine($"Error relinking: {x}");
                    Debugger.Break();
                },

            };

            // Replace BioWorldInfo if requested
            if (syncBioWorldInfo)
            {
                var me1BWI = me1File.Exports.FirstOrDefault(x => x.ClassName == "BioWorldInfo");
                if (me1BWI != null)
                {
                    me1BWI.indexValue = 1;
                    EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.ReplaceSingular, me1BWI, le1File, le1File.FindExport(@"TheWorld.PersistentLevel.BioWorldInfo_0"), true, rop, out _);
                }
            }

            // Replace Main_Sequence if requested

            if (portMainSequence)
            {
                vTestOptions.SetStatusText($"Porting sequencing");
                var dest = le1File.FindExport(@"TheWorld.PersistentLevel.Main_Sequence");
                var source = me1File.FindExport(@"TheWorld.PersistentLevel.Main_Sequence");
                if (source != null && dest != null)
                {
                    EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.ReplaceSingular, source, le1File, dest, true, rop, out _);
                }
                else
                {
                    Debug.WriteLine($"No sequence to port in {sourceName}");
                }
            }


            var le1PL = le1File.FindExport("TheWorld.PersistentLevel");
            var le1PersistentLevel = ObjectBinary.From<Level>(le1PL);

            // Port over ModelComponents
            if (/*vTestOptions.portModels && */ShouldPortModel(sourceName.ToUpper()))
            {
                var me1ModelUIndex = ObjectBinary.From<Level>(me1PL).Model;
                List<int> modelComponents = new List<int>();
                foreach (var mc in me1File.Exports.Where(x => x.ClassName == "ModelComponent"))
                {
                    var mcb = ObjectBinary.From<ModelComponent>(mc);
                    if (mcb.Model == me1ModelUIndex)
                    {
                        IEntry modelComp = le1File.FindExport(mc.InstancedFullPath);
                        if (modelComp == null)
                        {
                            rop.CrossPackageMap.Clear();
                            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, mc, le1File, le1PL, true, rop, out modelComp);
                        }

                        modelComponents.Add(modelComp.UIndex);
                    }
                }
                le1PersistentLevel.ModelComponents = modelComponents.ToArray();
            }

            // Port over StreamableTextures
            if (vTestOptions.installTexturesInstanceMap)
            {
                foreach (var textureInstance in me1PersistentLevel.TextureToInstancesMap)
                {
                    //le1PersistentLevel.ForceStreamTextures
                    var me1Tex = me1File.GetEntry(textureInstance.Key);
                    var le1Tex = le1File.FindEntry(me1Tex.InstancedFullPath);
                    if (le1Tex != null)
                    {
                        le1PersistentLevel.TextureToInstancesMap[le1Tex.UIndex] = textureInstance.Value;
                    }
                }
            }

            // Port over ForceStreamTextures
            if (vTestOptions.installForceTextureStreaming)
            {
                foreach (var fst in me1PersistentLevel.ForceStreamTextures)
                {
                    //le1PersistentLevel.ForceStreamTextures
                    var me1Tex = me1File.GetEntry(fst.Key);
                    var le1Tex = le1File.FindEntry(me1Tex.InstancedFullPath);
                    if (le1Tex != null)
                    {
                        le1PersistentLevel.ForceStreamTextures[le1Tex.UIndex] = fst.Value;
                    }
                }
            }

            // FINALIZE PERSISTENTLEVEL
            le1PL.WriteBinary(le1PersistentLevel);

            // Removed - no longer used with static lighting
            //if (vTestOptions.useDynamicLighting)
            //{
            //    vTestOptions.SetStatusText($"Generating Dynamic Lighting");
            //    VTestLegacy.CreateDynamicLighting(le1File, vTestOptions, true);
            //}

            // This must come after dynamic lighting as we correct a few dynamic lightings
            VTestCorrections.PostPortingCorrections(me1File, le1File, vTestOptions);

            if (vTestOptions.debugBuild)
            {
                vTestOptions.SetStatusText($"Enabling debug features");
                VTest_EnableDebugOptionsOnPackage(le1File, vTestOptions);
            }

            // 08/21/2024 - Add static lighting import
            // PRC2AA lightmaps look awful
            if (!levelName.Contains("PRC2AA"))
            {
                // No point importing static lighting if we are going to bake it again
                if (!vTestOptions.isBuildForStaticLightingBake)
                {
                    vTestOptions.SetStatusText($"Importing static lighting for {levelName}");

                    var lightmapSetup = new LightingImportSetup();
                    lightmapSetup.UDKMapsBasePath = VTestPaths.VTest_StaticLightingDir;
                    lightmapSetup.IncludeSubLevels = false;
                    lightmapSetup.KeptLightmapPrefix = "Original_";
                    lightmapSetup.ShouldKeepLightMap = entry => entry.IsA("TerrainComponent");
                    StaticLightingImporter.ImportStaticLighting(le1File, lightmapSetup);
                }
            }

            // 08/21/2024 - Add texture to instances map calculator
            vTestOptions.SetStatusText($"Generating texture to instances map for {levelName}");
            LevelTools.CalculateTextureToInstancesMap(le1File, vTestOptions.cache);

            vTestOptions.SetStatusText($"Saving package {levelName}");
            le1File.Save();

            // Save the extra localization versions too for GE, FE, IE
            switch (levelName)
            {
                case "BIOA_PRC2_CCSIM05_DSG_LOC_FR": // French English VO
                    le1File.Save(Path.Combine(VTestPaths.VTest_FinalDestDir, "BIOA_PRC2_CCSIM05_DSG_LOC_FE"));
                    break;
                case "BIOA_PRC2_CCSIM05_DSG_LOC_DE": // German English VO
                    le1File.Save(Path.Combine(VTestPaths.VTest_FinalDestDir, "BIOA_PRC2_CCSIM05_DSG_LOC_GE"));
                    break;
                case "BIOA_PRC2_CCSIM05_DSG_LOC_IT": // Italian English VO
                    le1File.Save(Path.Combine(VTestPaths.VTest_FinalDestDir, "BIOA_PRC2_CCSIM05_DSG_LOC_IE"));
                    break;

                case "BIOA_PRC2_CCSCOREBOARD_DSG_LOC_FR": // French English VO
                    le1File.Save(Path.Combine(VTestPaths.VTest_FinalDestDir, "BIOA_PRC2_CCSCOREBOARD_DSG_LOC_FE"));
                    break;
                case "BIOA_PRC2_CCSCOREBOARD_DSG_LOC_DE": // German English VO
                    le1File.Save(Path.Combine(VTestPaths.VTest_FinalDestDir, "BIOA_PRC2_CCSCOREBOARD_DSG_LOC_GE"));
                    break;
                case "BIOA_PRC2_CCSCOREBOARD_DSG_LOC_IT": // Italian English VO
                    le1File.Save(Path.Combine(VTestPaths.VTest_FinalDestDir, "BIOA_PRC2_CCSCOREBOARD_DSG_LOC_IE"));
                    break;
            }

            vTestOptions.SetStatusText($"RCP CHECK");

            Debug.WriteLine($"RCP CHECK FOR {Path.GetFileNameWithoutExtension(le1File.FilePath)} -------------------------");
            ReferenceCheckPackage rcp = new ReferenceCheckPackage();
            EntryChecker.CheckReferences(rcp, le1File, LECLocalizationShim.NonLocalizedStringConverter);

            foreach (var err in rcp.GetBlockingErrors())
            {
                Debug.WriteLine($"RCP: [ERROR] {err.Entry.InstancedFullPath} {err.Message}");
            }

            foreach (var err in rcp.GetSignificantIssues())
            {
                Debug.WriteLine($"RCP: [WARN] {err.Entry.InstancedFullPath} {err.Message}");
            }
        }

        private static bool ShouldPortModel(string uppercaseBaseName)
        {
            switch (uppercaseBaseName)
            {
                case "BIOA_PRC2_CCLOBBY02_LAY": // Fixes hole above vidinos
                case "BIOA_PRC2_CCSIM": // Fixes holes in roof of ccsim room
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Updates the level's Model, Polys, as they likely have a name collision
        /// </summary>
        /// <param name="me1File"></param>
        /// <param name="le1File"></param>
        /// <param name="vTestOptions"></param>
        private static void CorrectFileForLEXMapFileDefaults(IMEPackage me1File, IMEPackage le1File, VTestOptions vTestOptions)
        {
            RelinkerOptionsPackage rop = new RelinkerOptionsPackage() { Cache = vTestOptions.cache, TargetGameDonorDB = vTestOptions.objectDB }; // We do not set game db here as we will not be donating anything.

            // Port in the level's Model in the main file
            var me1PL = me1File.FindExport("TheWorld.PersistentLevel");
            var me1PersistentLevel = ObjectBinary.From<Level>(me1PL);

            var le1PL = le1File.FindExport("TheWorld.PersistentLevel");
            var le1PersistentLevel = ObjectBinary.From<Level>(le1PL);

            if (me1PersistentLevel.Model != 0)
            {
                // Ensure model names match
                var me1ModelExp = me1File.GetUExport(me1PersistentLevel.Model);
                var le1ModelExp = le1File.GetUExport(le1PersistentLevel.Model);
                le1ModelExp.indexValue = me1ModelExp.indexValue;

                // Binaries
                var me1Model = ObjectBinary.From<Model>(me1ModelExp);
                var le1Model = ObjectBinary.From<Model>(le1ModelExp);

                // Ensure polys names match
                var me1Polys = me1File.GetUExport(me1Model.Polys);
                var le1Polys = le1File.GetUExport(le1Model.Polys);
                le1Polys.indexValue = me1Polys.indexValue;

                // Copy over the Model data.
                if (/*vTestOptions.portModels*/ShouldPortModel(Path.GetFileNameWithoutExtension(le1File.FilePath).ToUpper()))
                {
                    EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.ReplaceSingularWithRelink, me1ModelExp, le1File, le1ModelExp, true, rop, out _);
                }
            }
        }


        private static void VTest_EnableDebugOptionsOnPackage(IMEPackage le1File, VTestOptions vTestOptions)
        {
            // This is no longer necessary with m_aObjLog Enabler mod
            //SequenceEditorExperimentsM.ConvertSeqAct_Log_objComments(le1File, vTestOptions.cache);
        }

        /// <summary>
        /// Ports a list of actors between levels with VTest
        /// </summary>
        /// <param name="sourcePackage"></param>
        /// <param name="destPackage"></param>
        /// <param name="itemsToPort"></param>
        /// <param name="db"></param>
        /// <param name="pe"></param>
        private static void VTestFilePorting(IMEPackage sourcePackage, IMEPackage destPackage, IEnumerable<ExportEntry> itemsToPort, VTestOptions vTestOptions)
        {
            // PRECORRECTION - CORRECTIONS TO THE SOURCE FILE BEFORE PORTING
            var levelName = Path.GetFileNameWithoutExtension(destPackage.FilePath);

            // PORTING ACTORS
            var le1PL = destPackage.FindExport("TheWorld.PersistentLevel");
            foreach (var e in itemsToPort)
            {
                vTestOptions.SetStatusText($"Porting {e.ObjectName.Instanced}");
                RelinkerOptionsPackage rop = new RelinkerOptionsPackage()
                {
                    Cache = vTestOptions.cache,
                    ImportExportDependencies = true,
                    IsCrossGame = true,
                    TargetGameDonorDB = vTestOptions.objectDB,
                    RelinkAllowDifferingClassesInRelink = true // Allows swapping Material and MaterialInstanceConstants
                };
                var report = EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, e, destPackage,
                    le1PL, true, rop, out _);

                if (vTestOptions.debugBuildAssetCachePackage)
                {
                    rop.CrossPackageMap.Clear();
                    var originalIndexValue = e.indexValue;
                    var assetPL = vTestOptions.assetCachePackage.FindExport("TheWorld.PersistentLevel");
                    e.indexValue = vTestOptions.assetCacheIndex++; // We do this to ensure no collisions so the cache is built
                    EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, e, vTestOptions.assetCachePackage,
                        assetPL, true, rop, out _);
                    e.indexValue = originalIndexValue;
                }
            }
        }

        /// <summary>
        /// Ports a LOC file by porting the ObjectReferencer within it
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="db"></param>
        /// <param name="pc"></param>
        /// <param name="pe"></param>
        private static void PortLOCFile(string sourceFile, VTestOptions vTestOptions)
        {
            var packName = Path.GetFileNameWithoutExtension(sourceFile);
            vTestOptions.SetStatusText($"Porting {packName}");

            var destPackagePath = Path.Combine(VTestPaths.VTest_FinalDestDir, $"{packName.ToUpper()}.pcc");
            MEPackageHandler.CreateAndSavePackage(destPackagePath, MEGame.LE1);
            using var package = MEPackageHandler.OpenMEPackage(destPackagePath);
            using var sourcePackage = MEPackageHandler.OpenMEPackage(sourceFile);

            PrePortingCorrections(sourcePackage, vTestOptions);

            var bcBaseIdx = sourcePackage.findName("BIOC_Base");
            if (bcBaseIdx >= 0)
            {
                sourcePackage.replaceName(bcBaseIdx, "SFXGame");
            }

            // Port packages
            var objReferencer = sourcePackage.Exports.First(x => x.idxLink == 0 && x.ClassName == "ObjectReferencer");
            var objs = objReferencer.GetProperty<ArrayProperty<ObjectProperty>>("ReferencedObjects");
            objs.RemoveAll(x => x.ResolveToEntry(sourcePackage).IsA("MaterialExpression")); // These will be referenced as needed. Don't port the originals. If these aren't removed we will get debug logger output.
            objReferencer.WriteProperty(objs);

            var rop = new RelinkerOptionsPackage()
            {
                Cache = vTestOptions.cache,
                TargetGameDonorDB = vTestOptions.objectDB,
                PortExportsAsImportsWhenPossible = true,
                GenerateImportsForGlobalFiles = true,
            };

            var results = EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, objReferencer,
                package, null, true, rop, out var newEntry);


            foreach (var e in sourcePackage.Exports.Where(x => x.ClassName == "BioMorphFace"))
            {
                rop = new RelinkerOptionsPackage()
                {
                    IsCrossGame = true,
                    ImportExportDependencies = true,
                    Cache = vTestOptions.cache,
                    TargetGameDonorDB = vTestOptions.objectDB
                };
                var link = e.Parent != null ? package.FindEntry(e.ParentFullPath) : null;
                var report = EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, e, package, link, true, rop, out _);
                if (report.Any())
                {
                    //Debugger.Break();
                }
            }

            //CorrectSequences(package, vTestOptions);
            VTestCorrections.PostPortingCorrections(sourcePackage, package, vTestOptions);

            vTestOptions.SetStatusText($"Saving {packName}");
            package.Save();
        }
        #endregion

        #region Utility methods
        public static void CreateEmptyLevel(string outpath)
        {
            var emptyLevelName = "LE1EmptyLevel.pcc";
            if (VTestUtility.LoadEmbeddedFile(emptyLevelName, out var stream))
            {
                using var fs = File.OpenWrite(outpath);
                stream.CopyTo(fs);
            }

            emptyLevelName = Path.GetFileNameWithoutExtension(emptyLevelName);
            using var Pcc = MEPackageHandler.OpenMEPackage(outpath);
            for (int i = 0; i < Pcc.Names.Count; i++)
            {
                string name = Pcc.Names[i];
                if (name.Equals(emptyLevelName))
                {
                    var newName = name.Replace(emptyLevelName, Path.GetFileNameWithoutExtension(outpath));
                    Pcc.replaceName(i, newName);
                }
            }

            var packguid = Guid.NewGuid();
            var package = Pcc.GetUExport(4);
            package.PackageGUID = packguid;
            Pcc.PackageGuid = packguid;
            Pcc.Save();
        }
        #endregion

        #region Correction methods
        internal static IEntry GetImportArchetype(IMEPackage package, string packageFile, string ifp)
        {
            IEntry result = package.FindExport($"{packageFile}.{ifp}");
            if (result != null)
                return result;

            var file = $"{packageFile}.{(package.Game is MEGame.ME1 or MEGame.UDK ? "u" : "pcc")}";
            var fullPath = Path.Combine(MEDirectories.GetCookedPath(package.Game), file);
            using var lookupPackage = MEPackageHandler.UnsafePartialLoad(fullPath, x => false);
            var entry = lookupPackage.FindExport(ifp) as IEntry;
            if (entry == null)
                Debugger.Break();

            Stack<IEntry> children = new Stack<IEntry>();
            children.Push(entry); // Must port at least the found IFP.
            while (entry.Parent != null && package.FindEntry(entry.ParentInstancedFullPath) == null)
            {
                children.Push(entry.Parent);
                entry = entry.Parent;
            }

            // Create imports from top down.
            var packageExport = (IEntry)ExportCreator.CreatePackageExport(package, packageFile);

            // This doesn't work if the part of the parents already exist.
            var attachParent = packageExport;
            foreach (var item in children)
            {
                ImportEntry imp = new ImportEntry(item as ExportEntry, packageExport.UIndex, package);
                imp.idxLink = attachParent.UIndex;
                package.AddImport(imp);
                attachParent = imp;
                result = imp;
            }

            return result;
        }


        /// <summary>
        /// Creates a static mesh actor from an SMC (that is not under collection - e.g. interpactor)
        /// </summary>
        /// <param name="smc"></param>
        /// <returns></returns>
        public static ExportEntry CreateSMAFromSMC(ExportEntry smc)
        {
            var level = smc.FileRef.GetLevel();
            var sma = ExportCreator.CreateExport(smc.FileRef, "StaticMeshActor", "StaticMeshActor", level, createWithStack: true);

            PropertyCollection props = new PropertyCollection();
            props.AddOrReplaceProp(new ObjectProperty(smc, "StaticMeshComponent"));
            props.AddOrReplaceProp(new ObjectProperty(smc, "CollisionComponent"));
            props.AddOrReplaceProp(new BoolProperty(true, "bCollideActors"));
            sma.WriteProperties(props);

            var parent = smc.Parent as ExportEntry;

            var loc = parent.GetProperty<StructProperty>("Location");
            if (loc != null) sma.WriteProperty(loc);

            var rot = parent.GetProperty<StructProperty>("Rotation");
            if (rot != null) sma.WriteProperty(rot);

            smc.Archetype = GetImportArchetype(smc.FileRef, "Engine", "Default__StaticMeshActor.StaticMeshComponent0");
            var lightingChannels = smc.GetProperty<StructProperty>("LightingChannels")?.Properties ?? new PropertyCollection();
            lightingChannels.AddOrReplaceProp(new BoolProperty(true, "bInitialized"));
            lightingChannels.AddOrReplaceProp(new BoolProperty(true, "Static")); // Add static
            smc.WriteProperty(new StructProperty("LightingChannelContainer", lightingChannels, "LightingChannels"));
            smc.RemoveProperty("bUsePrecomputedShadows"); // Required for static lighting
            smc.Parent = sma; // Move under SMA

            return sma;
        }

        public static void PrePortingCorrections(IMEPackage sourcePackage, VTestOptions vTestOptions)
        {
            // FILE SPECIFIC
            var sourcePackageName = Path.GetFileNameWithoutExtension(sourcePackage.FilePath).ToUpper();
            if (sourcePackageName == "BIOA_PRC2_CCSIM03_LAY")
            {
                // garage door
                sourcePackage.FindExport("TheWorld.PersistentLevel.BioDoor_1.SkeletalMeshComponent_1").RemoveProperty("Materials"); // The materials changed in LE so using the original set is wrong. Remove this property to prevent porting donors for it
            }

            // Todo: Remove this if lightmap is OK.
            if (sourcePackageName == "BIOA_PRC2_CCLAVA")
            {
                // 08/22/2024
                // Ensure we don't remove lightmaps for terrain component by
                // cloning the textures and updating the references, since
                // static lighting generator will delete them due to them being shared with static meshes

                //foreach (var tc in sourcePackage.Exports.Where(x => x.ClassName == "TerrainComponent").ToList())
                //{
                //    var bin = ObjectBinary.From<TerrainComponent>(tc);
                //    KeepLightmapTextures(bin.LightMap, sourcePackage);
                //    tc.WriteBinary(bin);
                //}

                // 08/25/2024 - Fan blade needs changed to static mesh as it does not receive lighting otherwise
                // Tried to make it spin but it just is black no matter what I do
                var fanBladeSMC = sourcePackage.FindExport("TheWorld.PersistentLevel.InterpActor_11.StaticMeshComponent_914");
                var originalIA = fanBladeSMC.Parent;
                var fanBladeSMA = CreateSMAFromSMC(fanBladeSMC);
                sourcePackage.AddToLevelActorsIfNotThere(fanBladeSMA);
                var rot = fanBladeSMA.GetProperty<StructProperty>("Rotation");
                rot.GetProp<IntProperty>("Roll").Value = 15474; // 85 degrees
                fanBladeSMA.WriteProperty(rot);
                EntryPruner.TrashEntries(sourcePackage, [originalIA]);
            }

            // Strip static mesh light maps since they don't work crossgen. Strip them from
            // the source so they don't port
            foreach (var exp in sourcePackage.Exports.ToList())
            {
                PruneUnusedProperties(exp);
                #region Remove Light and Shadow Maps
                if (exp.ClassName == "StaticMeshComponent")
                {
                    //if Non-Collection Static add LE Bool
                    if (exp.Parent.ClassName == "StaticMeshActor")
                    {
                        var props = exp.GetProperties();
                        props.AddOrReplaceProp(new BoolProperty(true, "bIsOwnerAStaticMeshActor"));
                        exp.WriteProperties(props);
                    }

                    if (vTestOptions == null || vTestOptions.useDynamicLighting || vTestOptions.stripShadowMaps)
                    {
                        if (vTestOptions != null && vTestOptions.allowTryingPortedMeshLightMap && !sourcePackageName.StartsWith("BIOA_PRC2AA")) // BIOA_PRC2AA doesn't seem to work with lightmaps
                        {
                            var sm = exp.GetProperty<ObjectProperty>("StaticMesh"); // name might need changed?
                            if (sm != null)
                            {
                                if (sourcePackage.TryGetEntry(sm.Value, out var smEntry))
                                {
                                    // Disabled due to static lighting build
                                    //if (ShouldPortLightmaps(smEntry))
                                    //{
                                    //    continue; // Do not port
                                    //}
                                }
                            }
                        }

                        var b = ObjectBinary.From<StaticMeshComponent>(exp);
                        foreach (var lod in b.LODData)
                        {
                            // Clear light and shadowmaps
                            if (vTestOptions == null || vTestOptions.stripShadowMaps || vTestOptions.useDynamicLighting)
                            {
                                lod.ShadowMaps = [0];
                            }

                            if (vTestOptions == null || vTestOptions.useDynamicLighting)
                            {
                                lod.LightMap = new LightMap() { LightMapType = ELightMapType.LMT_None };
                            }
                        }

                        exp.WriteBinary(b);
                    }
                }
                #endregion
                //// These are precomputed and stored in VTestHelper.pcc 
                //else if (exp.ClassName == "Terrain")
                //{
                //    exp.RemoveProperty("TerrainComponents"); // Don't port the components; we will port them ourselves in post
                //}
                else if (exp.ClassName == "BioTriggerStream")
                {
                    VTestCorrections.PreCorrectBioTriggerStream(exp);
                }
                else if (exp.ClassName == "BioWorldInfo")
                {
                    // Remove streaminglevels that don't do anything
                    //PreCorrectBioWorldInfoStreamingLevels(exp);
                }
                else if (exp.ClassName == "MaterialInstanceConstant")
                {
                    VTestCorrections.PreCorrectMaterialInstanceConstant(exp);
                }
                else if (exp.ClassName == "ModelComponent")
                {
                    var mcb = ObjectBinary.From<ModelComponent>(exp);
                    if (vTestOptions.useDynamicLighting)
                    {
                        foreach (var elem in mcb.Elements)
                        {
                            elem.ShadowMaps = [0]; // We want no shadowmaps
                            elem.LightMap = new LightMap() { LightMapType = ELightMapType.LMT_None }; // Strip the lightmaps
                        }
                    }
                    else
                    {
                        //foreach (var elem in mcb.Elements)
                        //{
                        //    KeepLightmapTextures(elem.LightMap, sourcePackage);
                        //}
                    }

                    exp.WriteBinary(mcb);

                }
                else if (exp.ClassName == "Sequence" && exp.GetProperty<StrProperty>("ObjName")?.Value == "PRC2_KillTriggerVolume")
                {
                    // Done before porting to prevent Trash from appearing in target
                    VTestCorrections.PreCorrectKillTriggerVolume(exp, vTestOptions);
                }
                else if (exp.IsA("LightComponent"))
                {
                    // 08/24/2024 - Enable all lights
                    exp.RemoveProperty("bEnabled");
                }

                // KNOWN BAD NAMES
                // Rename duplicate texture/material objects to be unique to avoid some issues with tooling
                if (exp.ClassName == "Texture2D")
                {
                    if (exp.InstancedFullPath == "BIOA_JUG80_T.JUG80_SAIL")
                    {
                        // Rename to match crossgen
                        exp.ObjectName = "JUG80_SAIL_CROSSGENFIX";
                    }
                    else if (exp.InstancedFullPath == "BIOA_ICE60_T.checker")
                    {
                        // Rename to match crossgen
                        exp.ObjectName = "BIOA_ICE60_T.checker_CROSSGENFIX";
                    }
                }
            }
        }

        private static void PruneUnusedProperties(ExportEntry exp)
        {
            // Lots of components are not used or don't exist and can't be imported in LE1
            // Get rid of them here
            PropertyCollection props = exp.GetProperties();

            // Might be better to enumerate all object properties and trim out ones that reference
            // known non-existent things
            if (exp.IsA("LightComponent"))
            {
                props.RemoveNamedProperty("PreviewInnerCone");
                props.RemoveNamedProperty("PreviewOuterCone");
                props.RemoveNamedProperty("PreviewLightRadius");
            }

            if (exp.IsA("NavigationPoint"))
            {
                props.RemoveNamedProperty("GoodSprite");
                props.RemoveNamedProperty("BadSprite");
            }

            if (exp.IsA("BioArtPlaceable"))
            {
                props.RemoveNamedProperty("CoverMesh"); // Property exists but is never set
            }

            if (exp.IsA("SoundNodeAttenuation"))
            {
                props.RemoveNamedProperty("LPFMinRadius");
                props.RemoveNamedProperty("LPFMaxRadius");
            }

            if (exp.IsA("BioAPCoverMeshComponent"))
            {
                exp.Archetype = null; // Remove the archetype. This is on BioDoor's and does nothing in practice, in ME1 there is nothing to copy from the archetype
            }

            if (exp.IsA("BioSquadCombat"))
            {
                props.RemoveNamedProperty("m_oSprite");
            }

            if (exp.IsA("CameraActor"))
            {
                props.RemoveNamedProperty("MeshComp"); // some actors have a camera mesh that was probably used to better visualize in-editor
            }

            exp.WriteProperties(props);
        }
        #endregion
    }
}
