using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;

namespace CrossGenV.Classes.Levels
{
    internal class CCMainConv
    {
        public static void PostPortingCorrections(IMEPackage me1File, IMEPackage le1File, VTestOptions vTestOptions)
        {
            // Ahern's post-mission dialogue. This installs the streaming textures event
            var sequence = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin");
            var remoteEvent = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqEvent_RemoteEvent", vTestOptions.cache);
            var streamInTextures = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_StreamInTextures", vTestOptions.cache);
            KismetHelper.AddObjectsToSequence(sequence, false, remoteEvent, streamInTextures);

            KismetHelper.CreateOutputLink(remoteEvent, "Out", streamInTextures);
            KismetHelper.CreateVariableLink(streamInTextures, "Location", le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqVar_Object_19")); //Location: Ahern Teleport Point

            remoteEvent.WriteProperty(new NameProperty("PrimeTexturesAhern", "EventName"));
            var materials = new ArrayProperty<ObjectProperty>("ForceMaterials");

            // AHERN
            materials.Add(new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_4.BioMaterialInstanceConstant_103")));
            materials.Add(new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_4.BioMaterialInstanceConstant_104")));
            materials.Add(new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_4.BioMaterialInstanceConstant_105")));
            materials.Add(new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_4.BioMaterialInstanceConstant_106")));
            materials.Add(new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_4.BioMaterialInstanceConstant_107")));

            // VIDINOS
            materials.Add(new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_5.BioMaterialInstanceConstant_120"))); //armor
            materials.Add(new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_5.BioMaterialInstanceConstant_121"))); //head
            materials.Add(new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_5.BioMaterialInstanceConstant_122"))); // eye

            // BRYANT
            materials.Add(new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_8.BioMaterialInstanceConstant_97"))); // armor
            materials.Add(new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_8.BioMaterialInstanceConstant_101"))); //head
            materials.Add(new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_8.BioMaterialInstanceConstant_98"))); //hair
            materials.Add(new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_8.BioMaterialInstanceConstant_102"))); //scalp
            materials.Add(new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_8.BioMaterialInstanceConstant_100"))); // eye
            materials.Add(new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_8.BioMaterialInstanceConstant_99"))); //lash

            streamInTextures.WriteProperty(materials);
            streamInTextures.WriteProperty(new FloatProperty(12f, "Seconds")); // How long to force stream. We set this to 12 to ensure blackscreen and any delays between fully finish

            // Install classes not in ME1
            var bftsItems = new[]
            {
                            ("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.BioSeqAct_Delay_5", "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_Interp_2","TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin", "Finished"),
                            ("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.BioSeqAct_Delay_6", "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.BioSeqAct_BlackScreen_5","TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin", "Finished"),
                            ("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.BioSeqAct_Delay_2", "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_Interp_7","TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin", "Finished"),
                            ("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.BioSeqAct_Delay_3", "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_Interp_11","TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin", "Finished"),
                            ("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SequenceReference_0.Sequence_980.BioSeqAct_Delay_4","TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SequenceReference_0.Sequence_980.BioSeqAct_BlackScreen_2", "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SequenceReference_0.Sequence_980", "Finished"),

                            ("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SequenceReference_2.Sequence_982.BioSeqAct_ModifyPropertyPawn_0","TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SequenceReference_2.Sequence_982.BioSeqAct_BlackScreen_3", "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SequenceReference_2.Sequence_982","Out"),
                            ("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SequenceReference_1.Sequence_981.SeqAct_Delay_1","TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SequenceReference_1.Sequence_981.BioSeqAct_BlackScreen_4", "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SequenceReference_1.Sequence_981", "Finished")

                        };

            foreach (var bfts in bftsItems)
            {
                var startNode = le1File.FindExport(bfts.Item1);
                var endNode = le1File.FindExport(bfts.Item2);
                var bsequence = le1File.FindExport(bfts.Item3);
                var streaming = SequenceObjectCreator.CreateSequenceObject(le1File, "BioSeqAct_BlockForTextureStreaming", vTestOptions.cache);
                KismetHelper.AddObjectToSequence(streaming, bsequence);
                KismetHelper.CreateOutputLink(startNode, bfts.Item4, streaming);
                KismetHelper.CreateOutputLink(streaming, "Finished", endNode);
            }

            // 08/26/2024 - Add completion experience
            VTestAdditionalContent.AddMissionCompletionExperience(le1File, vTestOptions);

            if (vTestOptions.debugBuild)
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
