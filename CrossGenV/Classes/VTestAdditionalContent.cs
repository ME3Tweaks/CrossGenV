using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace CrossGenV.Classes
{
    /// <summary>
    /// New content for VTest
    /// </summary>
    public class VTestAdditionalContent
    {
        // List of victorious IFPs (standard)
        private static string[] victoryIFPs =
        [
            "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_SetBool_6", // Have not accepted Vidinos Challenge
            "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_SetBool_3", // We have completed Vidinos Challenge
            "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_SetBool_5", // You have taken all of them Shepard (Ocaren)
        ];

        private static string ahernVictoryIFP = "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_SetBool_4"; // Have completed Ahern's challenge - special
        private static string extraFirstPlaceIFP = "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_SetBool_7"; // New high score on map already completed

        public static void AddMissionCompletionExperience(IMEPackage le1File, VTestOptions options)
        {
            ExportEntry originalNode = null;
            var sequence = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin");
            foreach (var vic in victoryIFPs)
            {
                // 33% per first-first place
                originalNode = le1File.FindExport(vic);
                GenerateMissionCompletedXPEvent(sequence, 0.33f, originalNode, options);
            }

            // Ahern mission 100% (FULL LEVEL)
            originalNode = le1File.FindExport(ahernVictoryIFP);
            GenerateMissionCompletedXPEvent(sequence, 1.0f, originalNode, options);

            // Extra 1st places 15%
            originalNode = le1File.FindExport(extraFirstPlaceIFP);
            GenerateMissionCompletedXPEvent(sequence, 0.15f, originalNode, options);
        }

        private static void GenerateMissionCompletedXPEvent(ExportEntry sequence, float mult, ExportEntry originalNode, VTestOptions options)
        {
            // Todo: Add mod settings menu control for this before each node fires.

            var grant = SequenceObjectCreator.CreateSequenceObject(sequence, "LEXSeqAct_GrantLevelBasedXPPercent", options.cache);
            KismetHelper.SetComment(grant, "Standard win: 0.5x of a level");
            var multiplier = SequenceObjectCreator.CreateFloat(sequence, 0.5f, options.cache);
            KismetHelper.CreateVariableLink(grant, "LevelPercent", multiplier);

            KismetHelper.InsertActionAfter(originalNode, "Out", grant, 0, "Out");
        }
    }
}
