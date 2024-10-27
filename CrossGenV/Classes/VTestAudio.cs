using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Pathing;
using LegendaryExplorerCore.Unreal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CrossGenV.Classes
{
    /// <summary>
    /// Things related to VTest's audio/music
    /// </summary>
    public class VTestAudio
    {
        public static void SetupKillStreakVO(IMEPackage le1File, VTestOptions vTestOptions)
        {
            foreach (var sequence in le1File.Exports.Where(x => x.ClassName == "Sequence").ToList())
            {
                var seqName = sequence.GetProperty<StrProperty>("ObjName")?.Value;
                if (seqName == "TA_V3_Gametype_Handler")
                {
                    var spawnerDeath = VTestKismet.FindSequenceObjectByClassAndPosition(sequence, "SeqEvent_SequenceActivated", -1392, 2824);
                    if (spawnerDeath != null)
                    {
                        VTestKismet.InstallRemoteEventSignal(le1File, spawnerDeath.InstancedFullPath, "EnemyKilled", vTestOptions);
                    }
                }
                else if (seqName == "CAH_Respawner")
                {
                    var spawner = VTestKismet.FindSequenceObjectByClassAndPosition(sequence, "SequenceReference", 1920, 2512);
                    if (spawner != null)
                    {
                        VTestKismet.InstallRemoteEventSignal(le1File, spawner.InstancedFullPath, "EnemyKilled", vTestOptions, "Enemy Killed");
                    }
                }
                else if (seqName == "Check_Capping_Completion")
                {
                    // SUR
                    var spawner = VTestKismet.FindSequenceObjectByClassAndPosition(sequence, "SeqEvent_SequenceActivated", -204, 2984);
                    if (spawner != null)
                    {
                        VTestKismet.InstallRemoteEventSignal(le1File, spawner.InstancedFullPath, "EnemyKilled", vTestOptions, "Enemy Killed");
                    }
                }
                else if (seqName == "Vampire_Mode_Handler")
                {
                    // SUR
                    var spawner = VTestKismet.FindSequenceObjectByClassAndPosition(sequence, "SeqEvent_SequenceActivated", -2728, 2976);
                    if (spawner != null)
                    {
                        VTestKismet.InstallRemoteEventSignal(le1File, spawner.InstancedFullPath, "EnemyKilled", vTestOptions, "Enemy Killed");
                    }
                }
            }
        }

        public static void SetupMusicIntensity(IMEPackage le1File, string upperFName, VTestOptions vTestOptions)
        {
            foreach (var sequence in le1File.Exports.Where(x => x.ClassName == "Sequence").ToList())
            {
                var seqName = sequence.GetProperty<StrProperty>("ObjName")?.Value;
                if (seqName == "TA_V3_Gametype_Handler")
                {
                    var spawnerDeath = VTestKismet.FindSequenceObjectByClassAndPosition(sequence, "SeqEvent_SequenceActivated", -1392, 2824);
                    if (spawnerDeath != null)
                    {
                        VTestKismet.InstallVTestHelperSequenceViaEvent(le1File, spawnerDeath.InstancedFullPath, "HelperSequences.MusicIntensityTA", vTestOptions);
                    }
                }
                else if (seqName == "Check_Capping_Completion")
                {
                    // Capping and Survival both have same-named sequence (thanks demiurge!)

                    // CAH
                    var finishedCappingCAH = VTestKismet.FindSequenceObjectByClassAndPosition(sequence, "SeqEvent_SequenceActivated", -1024, 2632);
                    if (finishedCappingCAH != null)
                    {
                        VTestKismet.InstallVTestHelperSequenceViaEvent(le1File, finishedCappingCAH.InstancedFullPath, "HelperSequences.MusicIntensityCAH", vTestOptions);
                    }


                    // SUR
                    var finishedCappingSUR = VTestKismet.FindSequenceObjectByClassAndPosition(sequence, "SeqAct_Log", 2456, 2760);
                    if (finishedCappingSUR != null)
                    {
                        var musicSUR = VTestKismet.InstallVTestHelperSequenceViaEvent(le1File, finishedCappingSUR.InstancedFullPath, "HelperSequences.MusicIntensitySUR", vTestOptions);
                        var intensifyWaveIdx = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_Int", vTestOptions.cache);
                        switch (upperFName)
                        {
                            // Values here are 1 indexed! So add one to your preferred starting wave
                            case "BIOA_PRC2_CCCAVE_DSG":
                                intensifyWaveIdx.WriteProperty(new IntProperty(4, "IntValue"));
                                break;
                            case "BIOA_PRC2_CCLAVA_DSG":
                                intensifyWaveIdx.WriteProperty(new IntProperty(5, "IntValue"));
                                break;
                            case "BIOA_PRC2_CCTHAI_DSG":
                                intensifyWaveIdx.WriteProperty(new IntProperty(5, "IntValue"));
                                break;
                        }
                        KismetHelper.AddObjectToSequence(intensifyWaveIdx, sequence);
                        KismetHelper.CreateVariableLink(musicSUR, "IntensifyWaveNum", intensifyWaveIdx);
                    }
                }
                else if (seqName == "Vampire_Mode_Handler")
                {
                    var updateWave = VTestKismet.FindSequenceObjectByClassAndPosition(sequence, "SeqEvent_SequenceActivated", -2728, 2976);
                    if (updateWave != null)
                    {
                        VTestKismet.InstallVTestHelperSequenceViaEvent(le1File, updateWave.InstancedFullPath, "HelperSequences.MusicIntensityVAM", vTestOptions);
                    }
                }
            }
        }


        public static void InstallMusicVolume(IMEPackage le1File, VTestOptions vTestOptions)
        {
            var pl = le1File.FindExport("TheWorld.PersistentLevel");
            var helperMusicVol = vTestOptions.vTestHelperPackage.FindExport("CCMaps.BioMusicVolume_0");
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, helperMusicVol, le1File, pl, true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out var musicVolEntry);

            var musicVol = musicVolEntry as ExportEntry;
            var fileName = Path.GetFileNameWithoutExtension(le1File.FilePath).ToUpper();
            int soundState = 0; // The column in the 2DA to use (for the soundque)
            bool hasIntensity2 = false;
            bool hasIntensity3 = false; // Left in case we ever decide to use it. Maybe on ahern?
            switch (fileName)
            {
                case "BIOA_PRC2_CCTHAI_SND":
                    musicVol.WriteProperty(new NameProperty("CrossGen_Mus_Thai", "MusicID")); // Virmire Ride
                    LevelTools.SetLocation(musicVol, 1040, -28200, -2000);
                    hasIntensity2 = true;
                    break;
                case "BIOA_PRC2_CCLAVA_SND":
                    musicVol.WriteProperty(new NameProperty("CrossGen_Mus_Lava", "MusicID")); // 
                    LevelTools.SetLocation(musicVol, 28420, -26932, -26858);
                    hasIntensity2 = true;
                    break;
                case "BIOA_PRC2_CCCRATE_SND":
                    musicVol.WriteProperty(new NameProperty("CrossGen_Mus_Crate", "MusicID")); // 
                    LevelTools.SetLocation(musicVol, 15783, -27067, -5491);
                    // No intensity 2 as this map is < 2 minutes in length
                    break;
                case "BIOA_PRC2_CCCAVE_SND":
                    musicVol.WriteProperty(new NameProperty("CrossGen_Mus_Cave", "MusicID")); // 
                    LevelTools.SetLocation(musicVol, -16480, -28456, -2614);
                    hasIntensity2 = true;
                    break;
                case "BIOA_PRC2_CCAHERN_SND":
                    musicVol.WriteProperty(new NameProperty("CrossGen_Mus_Ahern", "MusicID")); // Saren Peniultimate
                    LevelTools.SetLocation(musicVol, -41129, -27013, -2679);
                    hasIntensity2 = true;
                    break;
            }

            // Install model references
            var model = le1File.Exports.First(x => x.ClassName == "Model"); // Every level file will have this in porting
            musicVol.WriteProperty(new ObjectProperty(model, "Brush"));
            le1File.FindExport("TheWorld.PersistentLevel.BioMusicVolume_0.BrushComponent_9").WriteProperty(new ObjectProperty(model, "Brush"));


            // Install sequencing to turn music on and off
            // Plot check?

            var sequence = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence");

            var startMusicEvt = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqEvent_RemoteEvent", vTestOptions.cache);
            var stopMusicEvt = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqEvent_RemoteEvent", vTestOptions.cache);
            var plotCheck = SequenceObjectCreator.CreateSequenceObject(le1File, "BioSeqAct_PMCheckState", vTestOptions.cache);
            var musOn = SequenceObjectCreator.CreateSequenceObject(le1File, "BioSeqAct_MusicVolumeEnable", vTestOptions.cache);
            var musOff = SequenceObjectCreator.CreateSequenceObject(le1File, "BioSeqAct_MusicVolumeDisable", vTestOptions.cache);
            var musVolSeqObj = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_Object", vTestOptions.cache);

            var stateBeingSet = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_Int", vTestOptions.cache);
            var musicStatePlotInt = SequenceObjectCreator.CreateSequenceObject(le1File, "BioSeqVar_StoryManagerInt", vTestOptions.cache);
            var setInt = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_SetInt", vTestOptions.cache);

            startMusicEvt.WriteProperty(new NameProperty("StartSimMusic", "EventName"));
            stopMusicEvt.WriteProperty(new NameProperty("StopSimMusic", "EventName"));
            musVolSeqObj.WriteProperty(new ObjectProperty(musicVol, "ObjValue"));

            // Sequencing
            KismetHelper.AddObjectsToSequence(sequence, false, startMusicEvt, stopMusicEvt, plotCheck, musOn, musOff, musVolSeqObj, stateBeingSet, musicStatePlotInt, setInt);

            KismetHelper.CreateOutputLink(startMusicEvt, "Out", plotCheck);
            KismetHelper.CreateOutputLink(plotCheck, "False", setInt); // CHANGE TO musOff IN FINAL BUILD
            KismetHelper.CreateOutputLink(setInt, "Out", musOn);
            KismetHelper.CreateOutputLink(stopMusicEvt, "Out", musOff);


            KismetHelper.CreateVariableLink(musOn, "Music Volume", musVolSeqObj);
            KismetHelper.CreateVariableLink(musOff, "Music Volume", musVolSeqObj);

            KismetHelper.CreateVariableLink(setInt, "Target", musicStatePlotInt);
            KismetHelper.CreateVariableLink(setInt, "Value", stateBeingSet);

            // Music bool
            KismetHelper.SetComment(plotCheck, "Music is disabled?");
            plotCheck.WriteProperty(new IntProperty(7657, "m_nIndex"));

            // Setup SetInt values
            stateBeingSet.WriteProperty(new IntProperty(soundState, "IntValue"));
            musicStatePlotInt.WriteProperty(new IntProperty(74, "m_nIndex")); // Global Soundstate (2DA columns)
            musicStatePlotInt.WriteProperty(new StrProperty("CurrentMusicState", "m_sRefName"));
            musicStatePlotInt.WriteProperty(new EnumProperty("None", "EBioRegionAutoSet", MEGame.LE1, "Region"));

            // Intensities
            var intensity2 = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_Int", vTestOptions.cache);
            var intensity3 = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_Int", vTestOptions.cache);

            intensity2.WriteProperty(new IntProperty(1, "IntValue")); // 0 indexed
            intensity3.WriteProperty(new IntProperty(2, "IntValue")); // 0 indexed

            var evtIntensity2 = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqEvent_RemoteEvent", vTestOptions.cache);
            var evtIntensity3 = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqEvent_RemoteEvent", vTestOptions.cache);
            evtIntensity2.WriteProperty(new NameProperty("MusicIntensity2", "EventName"));
            evtIntensity3.WriteProperty(new NameProperty("MusicIntensity3", "EventName"));

            var setInt2 = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_SetInt", vTestOptions.cache);
            var setInt3 = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_SetInt", vTestOptions.cache);

            KismetHelper.AddObjectsToSequence(sequence, false, intensity2, intensity3, evtIntensity2, evtIntensity3, setInt2, setInt3);
            if (hasIntensity2)
            {
                KismetHelper.CreateOutputLink(evtIntensity2, "Out", setInt2);
            }

            if (hasIntensity3)
            {
                KismetHelper.CreateOutputLink(evtIntensity3, "Out", setInt3);
            }

            KismetHelper.CreateVariableLink(setInt2, "Target", musicStatePlotInt);
            KismetHelper.CreateVariableLink(setInt3, "Target", musicStatePlotInt);

            KismetHelper.CreateVariableLink(setInt2, "Value", intensity2);
            KismetHelper.CreateVariableLink(setInt3, "Value", intensity3);

            // DEBUG
            if (vTestOptions.debugBuild)
            {
                var touch = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqEvent_Touch", vTestOptions.cache);
                KismetHelper.AddObjectToSequence(touch, sequence);
                touch.WriteProperty(new ObjectProperty(musicVol, "Originator"));

                var touchLog = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_Log", vTestOptions.cache);
                KismetHelper.AddObjectToSequence(touchLog, sequence);
                KismetHelper.SetComment(touchLog, "Touched Music Volume");

                var untouchLog = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_Log", vTestOptions.cache);
                KismetHelper.AddObjectToSequence(untouchLog, sequence);
                KismetHelper.SetComment(untouchLog, "UnTouched Music Volume");

                KismetHelper.CreateOutputLink(touch, "Touched", touchLog);
                KismetHelper.CreateOutputLink(touch, "UnTouched", untouchLog);
            }
        }

        public static void FixAudioLengths(IMEPackage le1File, VTestOptions vTestOptions)
        {
            var fname = Path.GetFileNameWithoutExtension(le1File.FilePath);
            foreach (var exp in le1File.Exports.Where(x => x.ClassName == "BioSeqEvt_ConvNode").ToList())
            {
                switch (le1File.Localization)
                {
                    case MELocalization.None: // English also is None it seems (for things like Ocaren)
                    case MELocalization.INT:
                        FixINTAudioLengths(exp, vTestOptions);
                        break;
                }
            }
        }


        private static void FixINTAudioLengths(ExportEntry export, VTestOptions vTestOptions)
        {
            var ifp = export.InstancedFullPath;
            switch (ifp)
            {
                case "prc2_ahern_N.Node_Data_Sequence.BioSeqEvt_ConvNode_7":
                    SetGenderSpecificLength(export, 0.5f, 0.5f, vTestOptions); // "I lost a lot of good friends in the first contact war..."  AFFECTS BOTH
                    break;
                case "prc2_ahern_N.Node_Data_Sequence.BioSeqEvt_ConvNode_111":
                    SetGenderSpecificLength(export, 0, 1, vTestOptions); // The scores are tallied, and the winners appear... . Maleshep cuts off at 'Know'
                    break;
                case "prc2_ahern_N.Node_Data_Sequence.BioSeqEvt_ConvNode_10":
                    SetGenderSpecificLength(export, 0.5f, 0.5f, vTestOptions); // I never thought I'd see the day. Good work, Shepard. Really good work
                    break;
                case "prc2_ahern_N.Node_Data_Sequence.BioSeqEvt_ConvNode_217":
                    SetGenderSpecificLength(export, 0.5f, 0f, vTestOptions); // I got a brochure from ExoGeni and they dropped a prefab down on Intai'sae for me, here in the Argus Rho cluster.
                    break;
                case "prc2_ochren_N.Node_Data_Sequence.BioSeqEvt_ConvNode_67":
                    SetGenderSpecificLength(export, 0, 1.1f, vTestOptions); // So you must be the famous commander shepard
                    break;
                case "prc2_ochren_N.Node_Data_Sequence.BioSeqEvt_ConvNode_68":
                    SetGenderSpecificLength(export, 0, 0.8f, vTestOptions); // "Do you need something? I'm sure I have a few minutes before someone forgets their password"
                    break;
                case "prc2_ochren_N.Node_Data_Sequence.BioSeqEvt_ConvNode_12":
                    // This line is not cut off, but it ends exactly as ocaren stops talking. This doesn't mesh well with the sarcasm and the following sigh
                    // so we add a bit of a pause for dramatic effect on this excellent delivery he has for this line
                    SetGenderSpecificLength(export, 0, 0.8f, vTestOptions); // "No, our agents train in a simulator killing real, actual people"
                    break;
                case "prc2_ochren_N.Node_Data_Sequence.BioSeqEvt_ConvNode_142":
                    SetGenderSpecificLength(export, 0, 0.8f, vTestOptions); //"Really? Thank me? Well, I guess I'll redouble my efforts."
                    break;
                case "prc2_ochren_N.Node_Data_Sequence.BioSeqEvt_ConvNode_195":
                    SetGenderSpecificLength(export, 0, 0.5f, vTestOptions); // What's my objective in capture mode?
                    break;
                case "prc2_ochren_N.Node_Data_Sequence.BioSeqEvt_ConvNode_125":
                    SetGenderSpecificLength(export, 0, 0.6f, vTestOptions); // (2nd) You have your choices between volcanic and tropical courses
                    break;
                case "prc2_ochren_N.Node_Data_Sequence.BioSeqEvt_ConvNode_192":
                    SetGenderSpecificLength(export, 0, 0.7f, vTestOptions); // [NEW] Boot up the Subterranean level (Capture mode)
                    break;
                case "prc2_ochren_N.Node_Data_Sequence.BioSeqEvt_ConvNode_206":
                    SetGenderSpecificLength(export, 0, 1.5f, vTestOptions); // I don't say this very often... but good luck (only when doing ahern's mission the first time) | THIS LINE IS WAY CUT OFFin 
                    break;
                case "prc2_ochren_N.Node_Data_Sequence.BioSeqEvt_ConvNode_35":
                    SetGenderSpecificLength(export, 0.5f, 0f, vTestOptions); // Survival FIRST TIME [NEW] I'll go with Survival Mode
                    break;

                case "prc2_jealous_jerk_N.Node_Data_Sequence.BioSeqEvt_ConvNode_36":
                    SetGenderSpecificLength(export, 0.5f, 0.5f, vTestOptions); // I look forward to the challenge (vidinos, first win)
                    break;
                case "prc2_jealous_jerk_N.Node_Data_Sequence.BioSeqEvt_ConvNode_32":
                    SetGenderSpecificLength(export, 2f, 0.0f, vTestOptions); // What? Oh yes... the weapon... Let no one say vidinos is not a man of his word | This line is REALLY bad with the startup time. It's like 2 whole seconds
                    break;
                case "prc2_jealous_jerk_N.Node_Data_Sequence.BioSeqEvt_ConvNode_30":
                    SetGenderSpecificLength(export, 0.8f, 0.0f, vTestOptions); // Skill at cheating the system, maybe. I'll get to the bottom of this soon enough
                    break;
                case "prc2_jealous_jerk_N.Node_Data_Sequence.BioSeqEvt_ConvNode_31":
                    SetGenderSpecificLength(export, 0.8f, 0.0f, vTestOptions); // I'll find out how you rigged the simulator. Your "record" won't stand. Heads will roll
                    break;
                case "prc2_jealous_jerk_N.Node_Data_Sequence.BioSeqEvt_ConvNode_12":
                    SetGenderSpecificLength(export, -0.8f, 0.0f, vTestOptions); // That wasn't luck. It was skill. | Femshep's dialogue is too long here
                    break;
            }
        }


        /// <summary>
        /// This code is a hack to work around some crazy issue in LE1 where the conversation length for each gender is different.
        /// This occurs in OT but to a lesser degree but whatever the issue is, it's somewhere we can't see in native.
        /// This code installs a gender check and directs flow to a different interp for each, which has the length adjusted
        /// by an offset.
        /// </summary>
        /// <param name="export">The conversation start event node that connects to the interp</param>
        /// <param name="femaleOffset">The interplength duration delta for females.</param>
        /// <param name="femaleOffset">The interplength duration delta for males.</param>
        private static void SetGenderSpecificLength(ExportEntry export, float femaleOffset, float maleOffset, VTestOptions vTestOptions)
        {
            // Split the nodes
            var sequence = KismetHelper.GetParentSequence(export);
            var outbound = KismetHelper.GetOutputLinksOfNode(export);
            var interpFemale = outbound[0][0].LinkedOp as ExportEntry;
            var interpDataFemale = KismetHelper.GetVariableLinksOfNode(interpFemale)[0].LinkedNodes[0] as ExportEntry;

            var interpMale = EntryCloner.CloneTree(interpFemale);
            var interpDataMale = EntryCloner.CloneTree(interpDataFemale);

            var pmCheckFemale = SequenceObjectCreator.CreateSequenceObject(export.FileRef, "BioSeqAct_PMCheckConditional", vTestOptions.cache);
            pmCheckFemale.WriteProperty(new IntProperty(144, "m_nIndex")); // PlayerIsFemale
            pmCheckFemale.RemoveProperty("VariableLinks"); // These are not necessary.

            KismetHelper.AddObjectsToSequence(sequence, false, interpMale, interpDataMale, pmCheckFemale);

            // Link everything up.
            outbound[0][0].LinkedOp = pmCheckFemale;
            KismetHelper.WriteOutputLinksToNode(export, outbound); // Start Event to Conditional check
            KismetHelper.CreateOutputLink(pmCheckFemale, "True", interpFemale); // Player Female -> Female Interp
            KismetHelper.CreateOutputLink(pmCheckFemale, "False", interpMale); // Player Male -> Male Interp
            KismetHelper.RemoveVariableLinks(interpMale); // Remove existing interp data
            KismetHelper.CreateVariableLink(interpMale, "Data", interpDataMale); // Hook up the male interp data instead


            // Update the lengths by the specified amounts.
            var femaleLen = interpDataMale.GetProperty<FloatProperty>("InterpLength");
            femaleLen.Value += femaleOffset;
            interpDataFemale.WriteProperty(femaleLen);

            var maleLen = interpDataMale.GetProperty<FloatProperty>("InterpLength");
            maleLen.Value += maleOffset;
            interpDataMale.WriteProperty(maleLen);
        }
    }
}
