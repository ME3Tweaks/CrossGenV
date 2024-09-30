using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.UnrealScript;

namespace CrossGenV.Classes.Levels
{
    internal class CCMainConv
    {
        public static void PostPortingCorrections(IMEPackage me1File, IMEPackage le1File, VTestOptions vTestOptions)
        {
            FixFirstSeeShepardCinematic(le1File);


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
        }

        public static void FixFirstSeeShepardCinematic(IMEPackage le1File)
        {
            var fileLib = new FileLib(le1File);
            var usop = new UnrealScriptOptionsPackage() { Cache = new PackageCache() };
            var flOk = fileLib.Initialize(usop);

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
    }
}
