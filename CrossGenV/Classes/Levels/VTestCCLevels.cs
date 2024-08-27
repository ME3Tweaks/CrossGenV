using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            if (true || !vTestOptions.debugBuild)
            {
                var seq = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence");
                var levelLoaded = SequenceObjectCreator.CreateLevelLoaded(seq, vTestOptions.cache);
                var preventSave = SequenceObjectCreator.CreateToggleSave(seq, SequenceObjectCreator.CreateBool(seq, false), vTestOptions.cache);
                KismetHelper.CreateOutputLink(levelLoaded, "Loaded and Visible", preventSave);
            }
        }
    }
}
