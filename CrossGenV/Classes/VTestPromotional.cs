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
            }
        }
    }
}
