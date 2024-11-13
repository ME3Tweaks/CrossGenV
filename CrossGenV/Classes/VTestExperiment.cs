using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

                        InventoryPackage(vTestOptions.vTestHelperPackage, vTestOptions);
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


            // goto Checks;

            vTestOptions.SetStatusText("Clearing mod folder");
            // Clear out dest dir
            VTestUtility.DeleteFilesAndFoldersRecursively(VTestPaths.VTest_FinalDestDir, deleteDirectoryItself: false);

            // Copy in precomputed files
            vTestOptions.SetStatusText("Copying precomputed files");
            foreach (var f in Directory.GetFiles(VTestPaths.VTest_PrecomputedDir, "*.*", SearchOption.AllDirectories))
            {
                var destDir = VTestPaths.VTest_FinalDestDir;

                // Precomputed copy will flatten source directory
                var relativePath = Path.GetRelativePath(VTestPaths.VTest_PrecomputedDir, f);
                if (relativePath.Contains(Path.DirectorySeparatorChar))
                {
                    // It's subfolder. We only support one layer deep for CrossgenV
                    var subDirName = Path.GetDirectoryName(relativePath);
                    destDir = Path.Combine(destDir, subDirName);
                    Directory.CreateDirectory(destDir);
                }
                File.Copy(f, Path.Combine(destDir, Path.GetFileName(f)), true);
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
            var levelCache = rootCache.ChainNewCache();
            foreach (var vTestLevel in vTestOptions.vTestLevels)
            {
                levelCache = rootCache.ChainNewCache();
                vTestOptions.cache = levelCache.ChainNewCache();
                var levelFiles = Directory.GetFiles(Path.Combine(VTestPaths.VTest_SourceDir, vTestLevel)).ToList();

                // Port LOC first, it will be super commonly accessed. We will stuff it in the cache
                var locInt = Path.Combine(VTestPaths.VTest_SourceDir, vTestLevel, $"BIOA_{vTestLevel}_LOC_INT.SFM");
                if (File.Exists(locInt))
                {
                    vTestOptions.cache = levelCache.ChainNewCache();
                    PortFile(locInt, vTestLevel, true, vTestOptions); // Master file is first in the list.
                    levelFiles.Remove(locInt);
                    levelCache.InsertIntoCache(MEPackageHandler.OpenMEPackage(Path.Combine(VTestPaths.VTest_FinalDestDir, $"BIOA_{vTestLevel}_LOC_INT.pcc")));
                }

                PortFile(levelFiles[0], vTestLevel, true, vTestOptions); // Master file is first in the list.
                levelFiles.RemoveAt(0);
                levelCache.InsertIntoCache(MEPackageHandler.OpenMEPackage(Path.Combine(VTestPaths.VTest_FinalDestDir, $"BIOA_{vTestLevel}.pcc")));


                vTestOptions.cache = levelCache.ChainNewCache();
                // Port LOC files first so that import resolution of localized assets is correct when doing the main files
                Parallel.ForEach(levelFiles, new ParallelOptions() { MaxDegreeOfParallelism = (vTestOptions.parallelizeLevelBuild ? 6 : 1) }, f =>
                {
                    // Uncomment to filter for iteration
                    //if (!f.Contains("lava_dsg", StringComparison.OrdinalIgnoreCase))
                    //    return;

                    if (f.GetUnrealLocalization() == MELocalization.None)
                        return; // Do not port

                    vTestOptions.cache = rootCache.ChainNewCache();
                    PortFile(f, vTestLevel, false, vTestOptions);
                });

                // Port non LOC files next, after LOC files have been generated
                Parallel.ForEach(levelFiles, new ParallelOptions() { MaxDegreeOfParallelism = (vTestOptions.parallelizeLevelBuild ? 6 : 1) }, f =>
                {
                    // Uncomment to filter for iteration
                    //if (!f.Contains("lava_dsg", StringComparison.OrdinalIgnoreCase))
                    //    return;

                    if (f.GetUnrealLocalization() != MELocalization.None)
                        return; // Do not port

                    vTestOptions.cache = rootCache.ChainNewCache();
                    PortFile(f, vTestLevel, false, vTestOptions);
                });
            }

            vTestOptions.cache = rootCache.ChainNewCache();

            // 10/26/2024 - Convert all AI classes in the mod to Crossgen versions for simulator
            // Only convert if not for static lighting and we are building PRC2
            if (!vTestOptions.isBuildForStaticLightingBake && vTestOptions.vTestLevels.Contains("PRC2"))
            {
                VTestAI.ConvertAIToCrossgen(vTestOptions);
            }


            // TLKS ARE DONE POST ONLY
            VTestTLK.PostUpdateTLKs(vTestOptions);

            // 10/02/2024 - Framework all NPCs
            VTestFramework.FrameworkNPCs(vTestOptions);

            // 10/19/2024 - Stream in all materials when map load signal occurs
            // 10/27/2024 - This must be done after frameworking as we trash things in frameworking
            VTestTextures.InstallAllPrepTextureSignals(vTestOptions);

            // 10/09/2024 - Compute reachspecs
            VTestPathing.ComputeReachspecs(vTestOptions);

            // 10/02/2024 - Lightmap textures don't stream in fast enough for this to be worth doing, just
            // save them in the package.
            // 10/31/2024 - Streamed lighting mode is used to render the loading screens
            // 08/23/2024 - Externalize lightmap textures. There are not enough new textures to
            // make it worth doing non-lighting (also annoying bugs I don't want to fix in TFC Compactor)
            if (vTestOptions.useStreamedLighting)
            {
                VTestTextures.MoveTexturesToTFC("Lighting", "DLC_MOD_Vegas", true, vTestOptions);
            }

            // 11/12/2024 - Ensure referencing in packages before we resynthesize them
            VTestReferencer.EnsureReferencesInDecooked(vTestOptions);
            VTestReferencer.EnsureReferencesInWavelists(vTestOptions);
            VTestReferencer.EnsureReferencesInBase(vTestOptions);

            // 08/23/2024 - Add package resynthesis for cleaner output
            if (vTestOptions.resynthesizePackages)
            {
                foreach (var packagePath in Directory.GetFiles(VTestPaths.VTest_FinalDestDir, "*.pcc", SearchOption.AllDirectories))
                {
                    var package = MEPackageHandler.OpenMEPackage(packagePath);
                    vTestOptions.SetStatusText($"Resynthesizing package {package.FileNameNoExtension}");

                    var newPackage = PackageResynthesizer.ResynthesizePackage(package, vTestOptions.cache, true);
                    newPackage.Save();
                }
            }

        // VTest post QA
        Checks:
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

                // 11/11/2024 - Lighting rebake auto exports to UDK
                var masters = Directory.GetFiles(VTestPaths.VTest_FinalDestDir, "BioP*.pcc", SearchOption.AllDirectories);
                foreach (var master in masters)
                {
                    var package = MEPackageHandler.OpenMEPackage(master);
                    vTestOptions.SetStatusText($"Converting to UDK map: {package.FileNameNoExtension}");
                    var udkMaster = ConvertToUDK.GenerateUDKFileForLevel(package);

                    var levelFiles = new List<string>();
                    foreach (var additionalPackage in ((MEPackage)package).AdditionalPackagesToCook)
                    {
                        var subLevel = Path.Combine(VTestPaths.VTest_FinalDestDir, $"{additionalPackage}.pcc");
                        if (File.Exists(subLevel))
                        {
                            var subPackage = MEPackageHandler.OpenMEPackage(subLevel);
                            vTestOptions.SetStatusText($"  Converting to UDK map: {subPackage.FileNameNoExtension}");
                            levelFiles.Add(ConvertToUDK.GenerateUDKFileForLevel(subPackage));
                        }
                    }

                    // Set up master sublevels
                    using IMEPackage persistentUDK = MEPackageHandler.OpenUDKPackage(udkMaster);
                    IEntry levStreamingClass = persistentUDK.GetEntryOrAddImport("Engine.LevelStreamingAlwaysLoaded", "Class");
                    IEntry theWorld = persistentUDK.Exports.First(exp => exp.ClassName == "World");
                    int i = 1;
                    int firstLevStream = persistentUDK.ExportCount;
                    foreach (string fileName in levelFiles.Select(Path.GetFileNameWithoutExtension))
                    {
                        persistentUDK.AddExport(new ExportEntry(persistentUDK, theWorld, new NameReference("LevelStreamingAlwaysLoaded", i), properties:
                        [
                            new NameProperty(fileName, "PackageName"),
                            CommonStructs.ColorProp(System.Drawing.Color.FromArgb(255, (byte)(i % 256), (byte)((255 - i) % 256), (byte)(i * 7 % 256)), "DrawColor")
                        ])
                        {
                            Class = levStreamingClass
                        });
                        i++;
                    }

                    var streamingLevelsProp = new ArrayProperty<ObjectProperty>("StreamingLevels");
                    for (int j = firstLevStream; j < persistentUDK.ExportCount; j++)
                    {
                        streamingLevelsProp.Add(new ObjectProperty(j));
                    }

                    persistentUDK.Exports.First(exp => exp.ClassName == "WorldInfo").WriteProperty(streamingLevelsProp);
                    persistentUDK.Save();

                }
            }
        }

        private static void TestResynth()
        {
            var outPath = Path.Combine(VTestPaths.VTest_FinalDestDir, "SeekFreeShaderTest.pcc");
            MEPackageHandler.CreateEmptyLevel(outPath, MEGame.LE1);
            var newPackage = MEPackageHandler.OpenMEPackage(outPath);

            var sourcePackageF = Path.Combine(VTestPaths.VTest_FinalDestDir, "BIOA_PRC2_CCCAVE_DSG.pcc");
            var sourcePackage = MEPackageHandler.OpenMEPackage(sourcePackageF);

            // Initial port should be OK
            Console.WriteLine(">> INITIAL PORT IN");
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies,
                sourcePackage.FindExport("SeekFreeShaderCache"), newPackage, null,
                true, new RelinkerOptionsPackage(), out var portedSFCache);

            Console.WriteLine(">> PACKAGE SAVE");
            newPackage.Save();

            // Replacement will fail now
            Console.WriteLine(">> REPLACEMENT");
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.ReplaceSingularWithRelink,
                sourcePackage.FindExport("SeekFreeShaderCache"), newPackage, newPackage.FindExport("SeekFreeShaderCache"),
                true, new RelinkerOptionsPackage(), out portedSFCache);

            Console.WriteLine("Done.");
            Environment.Exit(0);

        }

        /// <summary>
        /// Inventories classes in a package and puts them into the class database
        /// </summary>
        /// <param name="package"></param>
        /// <param name="options"></param>
        public static void InventoryPackage(IMEPackage package, VTestOptions options)
        {
            // Inventory the classes from vtest helper to ensure they can be created without having to be in the 
            // code for LEC
            var vTestSeqObjClasses = package.Exports.Where(x => x.IsClass).ToList();
            foreach (var e in vTestSeqObjClasses)
            {
                var classInfo = GlobalUnrealObjectInfo.generateClassInfo(e);
                options.SetStatusText($@"  Inventorying class {e.InstancedFullPath}");
                if (e.InheritsFrom("SequenceObject"))
                {
                    var defaults = e.GetDefaults();
                    GlobalUnrealObjectInfo.GenerateSequenceObjectInfoForClassDefaults(defaults);
                }

                GlobalUnrealObjectInfo.InstallCustomClassInfo(e.ObjectName, classInfo, e.Game);
            }
        }

        private static void PortFile(string levelFileName, string masterMapName, bool forcePort, VTestOptions vTestOptions)
        {
            var levelName = Path.GetFileNameWithoutExtension(levelFileName).ToUpper();

            //if (!forcePort && !levelFileName.Contains("CCCAVE_DSG", StringComparison.OrdinalIgnoreCase))
            //    return;

            if (levelFileName.Contains("_LOC_", StringComparison.OrdinalIgnoreCase))
            {
#if DEBUG
                if (levelFileName.GetUnrealLocalization() == MELocalization.INT)
#endif
                    PortLOCFile(levelFileName, vTestOptions);
            }
            else
            {
                //if (levelFileName.Contains("ccsim", StringComparison.OrdinalIgnoreCase))
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

            // ME1 PS3 version changed names of base files due to engine version change
            me1File.ReplaceName("BIOC_Base", "SFXGame");
            me1File.ReplaceName("BIOG_StrategicAI", "SFXStrategicAI");

            CorrectFileForLEXMapFileDefaults(me1File, le1File, vTestOptions);
            VTestPreCorrections.PrePortingCorrections(me1File, le1File, vTestOptions);

            var itemsToPort = new List<ExportEntry>();

            var me1PL = me1File.FindExport(@"TheWorld.PersistentLevel");
            var me1PersistentLevel = ObjectBinary.From<Level>(me1PL);

            itemsToPort.AddRange(me1PersistentLevel.Actors.Where(x => x != 0) // Skip blanks
                .Select(x => me1File.GetUExport(x))
                .Where(x => ClassesToVTestPort.Contains(x.ClassName) || (syncBioWorldInfo && ClassesToVTestPortMasterOnly.Contains(x.ClassName))
            // Allow porting terrain if doing static lighting build because we don't care about the collision data.
            // Disabled 11/11/2024 - We also don't care about the lighting either cause the lightmaps for it are worthless and it speeds up lightmap baking.
            //|| (vTestOptions.isBuildForStaticLightingBake && x.ClassName == "Terrain")
            ));

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
            VTestPostCorrections.PostPortingCorrections(me1File, le1File, vTestOptions);

            if (vTestOptions.debugBuild)
            {
                vTestOptions.SetStatusText($"Enabling debug features");
                VTest_EnableDebugOptionsOnPackage(le1File, vTestOptions);
            }

            // 08/21/2024 - Add static lighting import
            // PRC2AA lightmaps look awful
            // 11/10/2024 - Verified and tested in UDK, it looks awful, it looks like ME1 lightmaps were done in Maya somehow?
            // 11/12/2024 - Okay fine we'll do static lighting. Sheesh. Did manual tweaking of lighting for lightmap bake
            //if (!levelName.Contains("PRC2AA", StringComparison.OrdinalIgnoreCase))
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

            vTestOptions.SetStatusText($"RCP CHECK for {le1File.FileNameNoExtension}");

            Debug.WriteLine($"RCP CHECK FOR {le1File.FileNameNoExtension} -------------------------");
            var sw = Stopwatch.StartNew();
            ReferenceCheckPackage rcp = new ReferenceCheckPackage();
            EntryChecker.CheckReferences(rcp, le1File, LECLocalizationShim.NonLocalizedStringConverter);
            sw.Stop();
            Debug.WriteLine($"RCP CHECK TIME {le1File.FileNameNoExtension}: {sw.ElapsedMilliseconds}ms");


            foreach (var err in rcp.GetBlockingErrors())
            {
                vTestOptions.SetStatusText($"RCP: [ERROR] {err.Entry.InstancedFullPath} {err.Message}");
            }

            foreach (var err in rcp.GetSignificantIssues())
            {
                vTestOptions.SetStatusText($"RCP: [WARN] {err.Entry.InstancedFullPath} {err.Message}");
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
                    RelinkAllowDifferingClassesInRelink = false // Still allows swapping Material and MaterialInstanceConstants / BioSWF and GFxMovieInfo
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
            VTestPreCorrections.PrePortingCorrections(sourcePackage, package, vTestOptions);

            var bcBaseIdx = sourcePackage.findName("BIOC_Base");
            if (bcBaseIdx >= 0)
            {
                sourcePackage.replaceName(bcBaseIdx, "SFXGame");
            }

            // Port packages
            var objReferencer = sourcePackage.Exports.First(x => x.idxLink == 0 && x.ClassName == "ObjectReferencer");
            var objs = objReferencer.GetProperty<ArrayProperty<ObjectProperty>>("ReferencedObjects");
            objs.RemoveAll(x => x.ResolveToEntry(sourcePackage).IsA("MaterialExpression")); // These will be referenced as needed. Don't port the originals. If these aren't removed we will get debug logger output.

            // 11/11/2024 - Do not port unused DistributionFloatUniform objects - they are unused and pull in import references that do not exist
            objs.RemoveAll(x => x.ResolveToEntry(sourcePackage).IsA("DistributionFloatUniform"));

            objReferencer.WriteProperty(objs);

            var rop = new RelinkerOptionsPackage()
            {
                Cache = vTestOptions.cache.ChainNewCache(),
                TargetGameDonorDB = vTestOptions.objectDB,
                PortExportsAsImportsWhenPossible = true,
                GenerateImportsForGlobalFiles = true,
            };

            Stopwatch sw = Stopwatch.StartNew();
            var results = EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, objReferencer,
                package, null, true, rop, out var newEntry);

            var step1 = sw.ElapsedMilliseconds;

            foreach (var e in sourcePackage.Exports.Where(x => x.ClassName == "BioMorphFace"))
            {
                rop = new RelinkerOptionsPackage()
                {
                    IsCrossGame = true,
                    ImportExportDependencies = true,
                    Cache = vTestOptions.cache.ChainNewCache(),
                    TargetGameDonorDB = vTestOptions.objectDB
                };
                var link = e.Parent != null ? package.FindEntry(e.ParentFullPath) : null;
                var report = EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, e, package, link, true, rop, out _);
                if (report.Any())
                {
                    //Debugger.Break();
                }
            }

            sw.Stop();
            var step2 = sw.ElapsedMilliseconds;

            vTestOptions.SetStatusText($"LOC Porting times for {package.FileNameNoExtension}: {step1}ms ObjectReferencer, {step2}ms BioMorphFace");


            //CorrectSequences(package, vTestOptions);
            var postPortingSW = Stopwatch.StartNew();
            VTestPostCorrections.PostPortingCorrections(sourcePackage, package, vTestOptions);
            postPortingSW.Stop();
            vTestOptions.SetStatusText($"PPC time: {postPortingSW.ElapsedMilliseconds}ms");
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
    }
}
