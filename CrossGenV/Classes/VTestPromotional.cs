using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Gammtek.Extensions;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;

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
            var pl = le1Package.GetLevel();
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, cameraActor,
                pl.FileRef, pl, true, new RelinkerOptionsPackage(), out var portedCameraEntry);
            var portedCamera = portedCameraEntry as ExportEntry;
            le1Package.AddToLevelActorsIfNotThere(portedCamera);

            // Port the camera sequence
            VTestKismet.InstallVTestHelperSequenceNoInput(le1Package, "TheWorld.PersistentLevel.Main_Sequence", "HelperSequences.TrailerCameras", options);
        }
    }
}
