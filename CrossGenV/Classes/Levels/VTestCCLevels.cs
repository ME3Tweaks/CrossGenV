using System.Linq;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;

namespace CrossGenV.Classes.Levels
{
    public class VTestCCLevels
    {
        /// <summary>
        /// Disables saving the game while in the simulator
        /// </summary>
        /// <param name="le1File"></param>
        /// <param name="vTestOptions"></param>
        public static void PreventSavingOnSimLoad(IMEPackage le1File, VTestOptions vTestOptions)
        {
            // Debug builds allow you to save the game RIGHT before you draw your weapon. This makes it much easier to debug levels
            // Todo: Remove this true block once all levels are verified save preventing and then save-enabled once map exit
            if (!vTestOptions.debugBuild)
            {
                var seq = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence");
                var levelLoaded = SequenceObjectCreator.CreateLevelLoaded(seq, vTestOptions.cache);
                var preventSave = SequenceObjectCreator.CreateToggleSave(seq, SequenceObjectCreator.CreateBool(seq, false), vTestOptions.cache);
                KismetHelper.CreateOutputLink(levelLoaded, "Loaded and Visible", preventSave);
            }
        }

        /// <summary>
        /// Calls the reset ramping remote event
        /// </summary>
        /// <param name="le1File"></param>
        /// <param name="vTestOptions"></param>
        public static void ResetRamping(IMEPackage le1File, VTestOptions vTestOptions)
        {
            var seq = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence");
            var loaded = KismetHelper.GetAllSequenceElements(seq).OfType<ExportEntry>().FirstOrDefault(x => x.ClassName == "SeqEvent_LevelLoaded");
            loaded ??= SequenceObjectCreator.CreateLevelLoaded(seq, vTestOptions.cache);
            var re = SequenceObjectCreator.CreateActivateRemoteEvent(seq, "CG_RESET_RAMPING", vTestOptions.cache);

            // We do both Loaded and Visible and Out because the first LevelLoaded we find may be either, since OT used Out and LE uses Loaded and Visible - both seem to work, as they are on outlink 0.
            KismetHelper.CreateOutputLink(loaded, "Loaded and Visible", re);
            KismetHelper.CreateOutputLink(loaded, "Out", re);

        }
    }
}
