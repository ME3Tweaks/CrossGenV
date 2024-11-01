using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Gammtek.Extensions;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;

namespace CrossGenV.Classes
{
    /// <summary>
    /// Code for promotional materials goes here. Trailer, images, etc
    /// </summary>
    internal class VTestPromotional
    {
        public static void AddTrailerCameras(IMEPackage le1Package, VTestOptions options)
        {
            // Port in the camera actor and add it to the level
            var cameraActor = options.vTestHelperPackage.FindExport("TrailerV2.CameraActor_24");
            var cameraBoom = options.vTestHelperPackage.FindExport("TrailerV2.InterpActor_24");
            var pl = le1Package.GetLevel();
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, cameraActor, pl.FileRef, pl, true, new RelinkerOptionsPackage(), out var portedCameraEntry);
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, cameraBoom, pl.FileRef, pl, true, new RelinkerOptionsPackage(), out var portedCameraBoomEntry);
            var portedCamera = portedCameraEntry as ExportEntry;
            var portedBoom = portedCameraBoomEntry as ExportEntry;
            portedBoom.WriteProperty(new ArrayProperty<ObjectProperty>([portedCamera], "Attached"));
            portedCamera.WriteProperty(new BoolProperty(true, "bHardAttach"));
            portedCamera.WriteProperty(new ObjectProperty(portedBoom, "Base"));
            le1Package.AddToLevelActorsIfNotThere(portedCamera, portedBoom);

            // Port the blank containing camera sequence
            var seq = VTestKismet.InstallVTestHelperSequenceNoInput(le1Package, "TheWorld.PersistentLevel.Main_Sequence", "HelperSequences.TrailerCameras", options);
            GenerateTrailerEvents(seq, options);
        }

        private static void GenerateTrailerEvents(ExportEntry seq, VTestOptions options)
        {
            // For each file we will pull its interp and make an event for it
            var camPaths = Path.Combine(VTestPaths.VTest_DonorsDir, "..", "CamPaths");
            var files = Directory.GetFiles(camPaths, "*.pcc");

            foreach (var camPathF in files)
            {
                using var camPathP = MEPackageHandler.OpenMEPackage(camPathF);

                var isLoadCam = camPathP.FileNameNoExtension.Contains("Load", StringComparison.CurrentCultureIgnoreCase);

                var singleSeq = VTestKismet.InstallVTestHelperSequenceNoInput(seq.FileRef, seq.InstancedFullPath, "HelperSequences.SingleTrailerCamera", options);
                singleSeq.ObjectName = camPathP.FileNameNoExtension;

                var startRe = SequenceObjectCreator.CreateSeqEventConsole(singleSeq, camPathP.FileNameNoExtension, options.cache);
                var stopRe = SequenceObjectCreator.CreateSeqEventConsole(singleSeq, "NoTrailer", options.cache);
                var interp = seq.FileRef.FindExport($"{singleSeq.InstancedFullPath}.SeqAct_Interp_0");

                // Source
                var interpDataSource = camPathP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CamPathSeq.InterpData_0");
                var interpDataMoveSource = camPathP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CamPathSeq.InterpData_0.InterpGroup_0.InterpTrackMove_0");
                var interpDataFOVSource = camPathP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CamPathSeq.InterpData_0.InterpGroup_1.InterpTrackFloatProp_0");

                // Dest
                var interpData = seq.FileRef.FindExport($"{singleSeq.InstancedFullPath}.InterpData_0");
                var interpDataMove = seq.FileRef.FindExport($"{singleSeq.InstancedFullPath}.InterpData_0.InterpGroup_0.InterpTrackMove_0");
                var interpDataFOV = seq.FileRef.FindExport($"{singleSeq.InstancedFullPath}.InterpData_0.InterpGroup_1.InterpTrackFloatProp_0");

                interpData.WriteProperty(interpDataSource.GetProperty<FloatProperty>("InterpLength"));
                EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.ReplaceSingularWithRelink, interpDataMoveSource, seq.FileRef, interpDataMove, true, new RelinkerOptionsPackage(), out _);
                EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.ReplaceSingularWithRelink, interpDataFOVSource, seq.FileRef, interpDataFOV, true, new RelinkerOptionsPackage(), out _);

                KismetHelper.CreateOutputLink(startRe, "Out", interp, 0); // Start
                KismetHelper.CreateOutputLink(stopRe, "Out", interp, 2); // Stop


                var levels = GetVisibleLevels(camPathP.FileNameNoExtension.Substring(0, camPathP.FileNameNoExtension.IndexOf("_")));
                if (levels.Length > 0)
                {
                    // If this is a load cam and we have levels to set visible/invisible for load effect
                    // we must change logic to trigger lightmap textures to load low LOD first
                    if (isLoadCam)
                    {
                        // We don't directly loop it
                        interp.RemoveProperty("bLooping");
                        var gate = SequenceObjectCreator.CreateGate(singleSeq);
                        KismetHelper.CreateOutputLink(startRe, "Out", gate, 1); // Open
                        KismetHelper.CreateOutputLink(stopRe, "Out", gate, 2); // Close
                        var gc = SequenceObjectCreator.CreateSequenceObject(singleSeq, "SeqAct_ForceGarbageCollection", options.cache);
                        var mlv = SequenceObjectCreator.CreateSequenceObject(singleSeq, "LEXSeqAct_MultiLevelVisibility", options.cache);
                        mlv.WriteProperty(new ArrayProperty<NameProperty>(levels.Select(x=> new NameProperty(x)), "Levels"));

                        var delay = SequenceObjectCreator.CreateDelay(singleSeq, 1, options.cache);
                        KismetHelper.CreateOutputLink(startRe, "Out", mlv); // Initial load
                        KismetHelper.CreateOutputLink(interp, "Completed", delay);
                        KismetHelper.CreateOutputLink(interp, "Completed", mlv, 2); // Unload
                        KismetHelper.CreateOutputLink(delay, "Finished", gc);
                        KismetHelper.CreateOutputLink(gc, "Finished", gate);
                        KismetHelper.CreateOutputLink(gate, "Out", mlv); // Visible
                        KismetHelper.CreateOutputLink(gate, "Out", interp); // Visible

                    }
                }
            }
        }

        /// <summary>
        /// Gets list of all visible levels for a given level, for forcing streamed in or out
        /// </summary>
        /// <param name="baseLevel">Base level map (simulator name like lava)</param>
        /// <returns></returns>
        private static NameReference[] GetVisibleLevels(string baseLevel)
        {
            switch (baseLevel.ToLower())
            {
                case "lava":
                    return
                    [
                        "BIOA_PRC2_CCLAVA",
                        "BIOA_PRC2_CCLAVA_CROSSGEN",
                        "BIOA_PRC2_CCLAVA_DSG",
                        "BIOA_PRC2_CCLAVA_L",
                        "BIOA_PRC2_CCLAVA_SND",
                    ];
                case "cave":
                    return
                    [
                        "BIOA_PRC2_CCCAVE",
                        "BIOA_PRC2_CCCAVE01_LAY",
                        "BIOA_PRC2_CCCAVE02_LAY",
                        "BIOA_PRC2_CCCAVE03_LAY",
                        "BIOA_PRC2_CCCAVE04_LAY",
                        "BIOA_PRC2_CCCAVE_DSG",
                        "BIOA_PRC2_CCCAVE_L",
                        "BIOA_PRC2_CCCAVE_SND",
                    ];
                case "crate":
                    return
                    [
                        "BIOA_PRC2_CCCRATE",
                        "BIOA_PRC2_CCCRATE01_LAY",
                        "BIOA_PRC2_CCCRATE02_LAY",
                        "BIOA_PRC2_CCCRATE_DSG",
                        "BIOA_PRC2_CCCRATE_L",
                        "BIOA_PRC2_CCCRATE_SND",
                    ];
                case "thai":
                    return
                    [
                        "BIOA_PRC2_CCTHAI",
                        "BIOA_PRC2_CCTHAI01_LAY",
                        "BIOA_PRC2_CCTHAI02_LAY",
                        "BIOA_PRC2_CCTHAI03_LAY",
                        "BIOA_PRC2_CCTHAI04_LAY",
                        "BIOA_PRC2_CCTHAI05_LAY",
                        "BIOA_PRC2_CCTHAI06_LAY",
                        "BIOA_PRC2_CCTHAI_DSG",
                        "BIOA_PRC2_CCTHAI_L",
                        "BIOA_PRC2_CCTHAI_SND"
                    ];
                case "ahern":
                    return
                    [
                        "BIOA_PRC2_CCAHERN",
                        "BIOA_PRC2_CCAHERN_ART",
                        "BIOA_PRC2_CCAHERN_DSG",
                        "BIOA_PRC2_CCAHERN_SND",
                    ];
                default:
                    return [];
            }

        }
    }
}
