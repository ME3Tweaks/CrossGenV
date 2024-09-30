using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossGenV.Classes
{
    public class VTestDebug
    {
        /// <summary>
        /// Unlocks the Ahern Mission early on Debug builds for testing purposes
        /// </summary>
        /// <param name="le1File"></param>
        /// <param name="vTestOptions"></param>
        public static void DebugUnlockAhernMission(IMEPackage le1File, VTestOptions vTestOptions)
        {
            if (vTestOptions.debugBuild && le1File.FindExport("prc2_ochren_D.prc2_ochren_dlg") is { } conversation)
            {
                var replies = conversation.GetProperty<ArrayProperty<StructProperty>>("m_ReplyList");
                replies[100].Properties.AddOrReplaceProp(new IntProperty(-1, "nConditionalFunc"));
                replies[108].Properties.AddOrReplaceProp(new IntProperty(-1, "nConditionalFunc"));
                Debug.WriteLine($"Unlocking Ahern Mission in Ochren Conversation in file {le1File.FileNameNoExtension}");
                conversation.WriteProperty(replies);
            }
        }

        public static void DebugConversationConsoleEvents(IMEPackage le1File, VTestOptions vTestOptions)
        {
            if (vTestOptions.debugBuild && le1File.FileNameNoExtension.ToUpper() == "BIOA_PRC2_CCMAIN_CONV")
            {
                // Ahern always talks
                le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqVar_Float_6").WriteProperty(new FloatProperty(0, "FloatValue")); // Change of not saying something
                le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqVar_Float_9").WriteProperty(new FloatProperty(0, "FloatValue")); // Change of not saying something

                // Console events to trigger post-cinematics

                var ahernWinRE = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqEvent_Console", vTestOptions.cache);
                var ahernLossRE = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqEvent_Console", vTestOptions.cache);
                var ahernLoss2RE = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqEvent_Console", vTestOptions.cache);
                var vidinos1RE = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqEvent_Console", vTestOptions.cache);
                var vidinos2RE = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqEvent_Console", vTestOptions.cache);
                var ocarenRE = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqEvent_Console", vTestOptions.cache);
                var ahernSpecialRE = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqEvent_Console", vTestOptions.cache);

                ahernWinRE.WriteProperty(new NameProperty("AhernWin", "ConsoleEventName"));
                ahernLossRE.WriteProperty(new NameProperty("AhernLoss1", "ConsoleEventName"));
                ahernLoss2RE.WriteProperty(new NameProperty("AhernLoss2", "ConsoleEventName"));
                vidinos1RE.WriteProperty(new NameProperty("Vidinos1", "ConsoleEventName")); // Dickhead to Bryant
                vidinos2RE.WriteProperty(new NameProperty("Vidinos2", "ConsoleEventName")); // Vidinos loses his challenge (win 8 missions)
                ocarenRE.WriteProperty(new NameProperty("Ocaren", "ConsoleEventName"));
                ahernSpecialRE.WriteProperty(new NameProperty("AhernSpecial", "ConsoleEventName"));


                KismetHelper.CreateOutputLink(ahernLossRE, "Out", le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_Teleport_19"));
                KismetHelper.CreateOutputLink(ahernLoss2RE, "Out", le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_Teleport_13"));
                KismetHelper.CreateOutputLink(ahernWinRE, "Out", le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_Teleport_5"));
                KismetHelper.CreateOutputLink(vidinos1RE, "Out", le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SequenceReference_0"));
                KismetHelper.CreateOutputLink(vidinos2RE, "Out", le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SequenceReference_1"));
                KismetHelper.CreateOutputLink(ocarenRE, "Out", le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_Teleport_6"));
                KismetHelper.CreateOutputLink(ahernSpecialRE, "Out", le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.BioSeqAct_BlackScreen_4"));


                KismetHelper.AddObjectsToSequence(le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin"), false, ahernWinRE, ahernLossRE, ahernLoss2RE, vidinos1RE, vidinos2RE, ocarenRE, ahernSpecialRE);
            }
        }
    }
}
