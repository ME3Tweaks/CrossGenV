using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.UnrealScript;

namespace CrossGenV.Classes.Levels
{
    public class BIOA_PRC2_CCMAIN_CONV : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PostPortingCorrection()
        {
            DebugConversationConsoleEvents();

            FixFirstSeeShepardCinematic();

            FixLoadingPostScoreboard();

            TightenPostMissionBlackScreens();
            // Ahern's post-mission dialogue. This installs the streaming textures event
            // 11/03/2024 - No longer used due to frameworking these pawns
            // var sequence = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin");
            //var remoteEvent = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqEvent_RemoteEvent", vTestOptions.cache);
            //var streamInTextures = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_StreamInTextures", vTestOptions.cache);
            //KismetHelper.AddObjectsToSequence(sequence, false, remoteEvent, streamInTextures);

            //KismetHelper.CreateOutputLink(remoteEvent, "Out", streamInTextures);
            //KismetHelper.CreateVariableLink(streamInTextures, "Location", le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqVar_Object_19")); //Location: Ahern Teleport Point

            ////remoteEvent.WriteProperty(new NameProperty("PrimeTexturesAhern", "EventName"));
            ////var materials = new ArrayProperty<ObjectProperty>("ForceMaterials")
            ////{
            ////    // AHERN
            ////    new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_4.BioMaterialInstanceConstant_103")),
            ////    new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_4.BioMaterialInstanceConstant_104")),
            ////    new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_4.BioMaterialInstanceConstant_105")),
            ////    new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_4.BioMaterialInstanceConstant_106")),
            ////    new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_4.BioMaterialInstanceConstant_107")),

            ////    // VIDINOS
            ////    new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_5.BioMaterialInstanceConstant_120")), //armor
            ////    new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_5.BioMaterialInstanceConstant_121")), //head
            ////    new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_5.BioMaterialInstanceConstant_122")), // eye

            ////    // BRYANT
            ////    new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_8.BioMaterialInstanceConstant_97")), // armor
            ////    new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_8.BioMaterialInstanceConstant_101")), //head
            ////    new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_8.BioMaterialInstanceConstant_98")), //hair
            ////    new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_8.BioMaterialInstanceConstant_102")), //scalp
            ////    new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_8.BioMaterialInstanceConstant_100")), // eye
            ////    new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_8.BioMaterialInstanceConstant_99")) //lash
            ////};

            // Give Ahern my-special-mission a moment to settle once his pawn becomes visible but before we show him. He seems to bounce down.
            var togglePL = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.Play_Ahern_Offers_Final_Mission_0.SeqAct_Toggle_0");
            var delay = SequenceObjectCreator.CreateDelay(KismetHelper.GetParentSequence(togglePL), 0.3f, vTestOptions.cache);
            KismetHelper.InsertActionAfter(togglePL, "Out", delay, 0, "Finished");


            //streamInTextures.WriteProperty(materials);
            //streamInTextures.WriteProperty(new FloatProperty(12f, "Seconds")); // How long to force stream. We set this to 12 to ensure blackscreen and any delays between fully finish

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
        }

        /// <summary>
        /// Tightens up delays for post-mission items
        /// </summary>
        private void TightenPostMissionBlackScreens()
        {
            var playerQuit = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.BioSeqAct_Delay_1");
            playerQuit.WriteProperty(new FloatProperty(0.1f, "Duration"));

        }

        /// <summary>
        /// Installs a custom WaitForBackgroundStreaming class 
        /// </summary>
        private void FixLoadingPostScoreboard()
        {
            var hookup = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.BioSeqAct_SetStreamingState_0");
            var seq = KismetHelper.GetParentSequence(hookup);
            var waitForStreaming = SequenceObjectCreator.CreateSequenceObject(seq, "LEXSeqAct_WaitForBackgroundStreaming", vTestOptions.cache);
            KismetHelper.InsertActionAfter(hookup, "Out", waitForStreaming, 0, "Finished");
        }

        private void FixFirstSeeShepardCinematic()
        {
            var fileLib = new FileLib(le1File);
            var usop = new UnrealScriptOptionsPackage() { Cache = new PackageCache() };
            var flOk = fileLib.Initialize(usop);
            if (!flOk) return;

            // Update interp length and fade out timing
            le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.InterpData_0").WriteProperty(new FloatProperty(12.75f, "InterpLength"));
            var fadeCurve = @"properties
{
    FloatTrack = {
                  Points = ({InVal = 0.00566500006, OutVal = 1.01869094, ArriveTangent = 0.0, LeaveTangent = 0.0, InterpMode = EInterpCurveMode.CIM_CurveAuto}, 
                            {InVal = 1.84915781, OutVal = 0.0, ArriveTangent = -0.482986927, LeaveTangent = -0.482986927, InterpMode = EInterpCurveMode.CIM_CurveAuto}, 
                            {InVal = 2.11481309, OutVal = 0.0, ArriveTangent = 1.64963126, LeaveTangent = 1.64963126, InterpMode = EInterpCurveMode.CIM_CurveAuto}, 
                            {InVal = 2.47485948, OutVal = 1.03217697, ArriveTangent = 0.0, LeaveTangent = 0.0, InterpMode = EInterpCurveMode.CIM_CurveAuto}, 
                            {InVal = 2.95016456, OutVal = 0.0, ArriveTangent = -0.118679896, LeaveTangent = -0.118679896, InterpMode = EInterpCurveMode.CIM_CurveAuto}, 
                            {InVal = 12.3999996, OutVal = 0.00551004661, ArriveTangent = 0.498885512, LeaveTangent = 0.498885512, InterpMode = EInterpCurveMode.CIM_CurveAuto}, 
                            {InVal = 12.75, OutVal = 0.997771025, ArriveTangent = -0.00430613197, LeaveTangent = -0.00430613197, InterpMode = EInterpCurveMode.CIM_CurveAuto}, 
                            {InVal = 13.75, OutVal = 0.0, ArriveTangent = 0.0, LeaveTangent = 0.0, InterpMode = EInterpCurveMode.CIM_CurveAuto}
                           )
                 }
}";
            UnrealScriptCompiler.CompileDefaultProperties(le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.InterpData_0.InterpGroupDirector_0.InterpTrackFade_0"), fadeCurve, fileLib, usop);

        }

        private void DebugConversationConsoleEvents()
        {
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

                var ocarenTransition = SequenceObjectCreator.CreateSequenceObject(le1File, "BioSeqAct_PMExecuteTransition", vTestOptions.cache);
                ocarenTransition.WriteProperty(new IntProperty(6269, "m_nIndex"));

                var vidinos2Transition = SequenceObjectCreator.CreateSequenceObject(le1File, "BioSeqAct_PMExecuteTransition", vTestOptions.cache);
                vidinos2Transition.WriteProperty(new IntProperty(6275, "m_nIndex"));


                KismetHelper.CreateOutputLink(ahernLossRE, "Out", le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_Teleport_19"));
                KismetHelper.CreateOutputLink(ahernLoss2RE, "Out", le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_Teleport_13"));
                KismetHelper.CreateOutputLink(ahernWinRE, "Out", le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_Teleport_5"));
                KismetHelper.CreateOutputLink(vidinos1RE, "Out", le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SequenceReference_0"));
                KismetHelper.CreateOutputLink(vidinos2RE, "Out", vidinos2Transition);
                KismetHelper.CreateOutputLink(vidinos2Transition, "Out", le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SequenceReference_1"));
                KismetHelper.CreateOutputLink(ocarenRE, "Out", ocarenTransition);
                KismetHelper.CreateOutputLink(ocarenTransition, "Out", le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_Teleport_6"));
                KismetHelper.CreateOutputLink(ahernSpecialRE, "Out", le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.BioSeqAct_BlackScreen_4"));


                KismetHelper.AddObjectsToSequence(le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin"), false, ahernWinRE, ahernLossRE, ahernLoss2RE, vidinos1RE, vidinos2RE, ocarenRE, ahernSpecialRE, ocarenTransition, vidinos2Transition);
            }
        }
    }
}
