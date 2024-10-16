﻿using CrossGenV.Classes.Modes;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using System;
using System.Diagnostics;
using System.Linq;

namespace CrossGenV.Classes.Levels
{
    /// <summary>
    /// BIOA_PRC2_CCTHAI_SND, BIOA_PRC2_CCCAVE_SND, BIOA_PRC2_CCLAVA_SND, BIOA_PRC2_CCCRATE_SND, NOT AHERN
    /// </summary>
    internal class BIOA_PRC2_CC_DSG : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        private static string[] assetsToEnsureReferencedInSim = new[]
{
            "BIOG_GTH_TRO_NKD_R.NKDa.GTH_TRO_NKDa_MDL", // Geth Trooper
            "BIOG_GTH_STP_NKD_R.NKDa.GTH_STP_NKDa_MDL", // Geth Prime
        };

        public void PostPortingCorrection()
        {
            var upperFName = le1File.FileNameNoExtension.ToUpper();

            // Kill streak voice line sequence
            VTestKismet.InstallVTestHelperSequenceNoInput(le1File, "TheWorld.PersistentLevel.Main_Sequence", "HelperSequences.KillStreakVoiceLine", vTestOptions);

            // 09/24/2024 - Add additional enemies logic
            VTestAdditionalContent.AddExtraEnemyTypes(le1File, vTestOptions);

            PreventSavingOnSimLoad(le1File, vTestOptions);
            ResetRamping(le1File, vTestOptions);

            VTestAudio.SetupMusicIntensity(le1File, upperFName, vTestOptions);
            VTestAudio.SetupKillStreakVO(le1File, vTestOptions);

            // Force the pawns that will spawn to have their meshes in memory
            // They are not referenced directly
            var assetsToReference = le1File.Exports.Where(x => assetsToEnsureReferencedInSim.Contains(x.InstancedFullPath)).ToArray();
            VTestUtility.AddWorldReferencedObjects(le1File, assetsToReference);

            VTestCapture.AddCaptureEngagementSequencing(le1File, vTestOptions);

            foreach (var exp in le1File.Exports.Where(x => x.ClassName == "Sequence").ToList())
            {
                var seqName = exp.GetProperty<StrProperty>("ObjName")?.Value;

                #region Skip broken SeqAct_ActorFactory for bit explosion | Increase Surival Mode Engagement

                if (seqName == "Spawn_Single_Guy")
                {
                    RemoveBitExplosionEffect(exp);
                    FixGethFlashlights(exp, vTestOptions);
                    // Increase survival mode engagement by forcing the player to engage with enemies that charge the player.
                    // This prevents them from camping and getting free survival time
                    if (VTestKismet.IsContainedWithinSequenceNamed(exp, "SUR_Respawner") && !VTestKismet.IsContainedWithinSequenceNamed(exp, "CAH_Respawner")) // Might force on CAH too since it's not that engaging.
                    {
                        // Sequence objects + add to sequence
                        var crustAttach = VTestKismet.FindSequenceObjectByClassAndPosition(exp, "BioSeqAct_AttachCrustEffect", 5920, 1672);
                        var currentPawn = VTestKismet.FindSequenceObjectByClassAndPosition(exp, "SeqVar_Object", 4536, 2016);

                        // AI change
                        var delay = SequenceObjectCreator.CreateSequenceObject(le1File, "BioSeqAct_Delay", vTestOptions.cache);
                        var delayDuration = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_RandomFloat", vTestOptions.cache);
                        var aiChoiceRand = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_RandomFloat", vTestOptions.cache);
                        var aiChoiceComp = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqCond_CompareFloat", vTestOptions.cache);
                        var aiChoiceAssaultThreshold = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_Float", vTestOptions.cache);
                        var changeAiCharge = SequenceObjectCreator.CreateSequenceObject(le1File, "BioSeqAct_ChangeAI", vTestOptions.cache);
                        var changeAiAssault = SequenceObjectCreator.CreateSequenceObject(le1File, "BioSeqAct_ChangeAI", vTestOptions.cache);
                        var chargeAiLog = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_Log", vTestOptions.cache);
                        var assaultAiLog = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_Log", vTestOptions.cache);

                        // Toxic and Phasic
                        var setWeaponAttributes = SequenceObjectCreator.CreateSequenceObject(le1File, "LEXSeqAct_SetWeaponAttribute", vTestOptions.cache);
                        var toxicFactor = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_Float", vTestOptions.cache);
                        var phasicFactor = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_Float", vTestOptions.cache);
                        //var addToxicFactor = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_Float", vTestOptions.cache);
                        //var addPhasicFactor = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_Float", vTestOptions.cache);
                        //var respawnCount = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_Float", vTestOptions.cache);

                        KismetHelper.AddObjectsToSequence(exp, false, delay, delayDuration, aiChoiceRand, aiChoiceComp, aiChoiceAssaultThreshold, changeAiCharge, changeAiAssault, assaultAiLog, chargeAiLog, setWeaponAttributes, toxicFactor, phasicFactor);

                        // Configure sequence object properties
                        delayDuration.WriteProperty(new FloatProperty(11, "Min"));
                        delayDuration.WriteProperty(new FloatProperty(20, "Max"));
                        KismetHelper.SetComment(toxicFactor, "Toxic to counter player regen");
                        KismetHelper.SetComment(phasicFactor, "Phasic to counter player powers");
                        toxicFactor.WriteProperty(new FloatProperty(1, "FloatValue"));
                        phasicFactor.WriteProperty(new FloatProperty(1, "FloatValue"));


                        // CHARGE AI BRANCH
                        var chargeAiClass = EntryImporter.EnsureClassIsInFile(le1File, "BioAI_Charge", new RelinkerOptionsPackage() { Cache = vTestOptions.cache });
                        changeAiCharge.WriteProperty(new ObjectProperty(chargeAiClass, "ControllerClass"));
                        KismetHelper.SetComment(chargeAiLog, "CROSSGEN: Engaging player with BioAI_Charge");

                        // ASSAULT AI BRANCH
                        var assaultAiClass = EntryImporter.EnsureClassIsInFile(le1File, "BioAI_Assault", new RelinkerOptionsPackage() { Cache = vTestOptions.cache });
                        changeAiAssault.WriteProperty(new ObjectProperty(assaultAiClass, "ControllerClass"));
                        KismetHelper.SetComment(assaultAiLog, "CROSSGEN: Relaxing player engagement with BioAI_Assault");

                        // ASSAULT CHANCE - 1 in 4 chance
                        aiChoiceRand.WriteProperty(new FloatProperty(0, "Min"));
                        aiChoiceRand.WriteProperty(new FloatProperty(4, "Max"));
                        aiChoiceAssaultThreshold.WriteProperty(new FloatProperty(3f, "FloatValue")); // The generated random number must be above this to change to assault. 

                        // Connect sequence objects - Delay and branch pick
                        KismetHelper.CreateOutputLink(crustAttach, "Done", delay);
                        KismetHelper.CreateVariableLink(delay, "Duration", delayDuration);
                        KismetHelper.CreateOutputLink(delay, "Finished", aiChoiceComp);
                        KismetHelper.CreateVariableLink(aiChoiceComp, "A", aiChoiceRand);
                        KismetHelper.CreateVariableLink(aiChoiceComp, "B", aiChoiceAssaultThreshold);

                        // Connect sequence objects - CHARGE BRANCH
                        KismetHelper.CreateOutputLink(aiChoiceComp, "A < B", changeAiCharge);
                        KismetHelper.CreateOutputLink(changeAiCharge, "Out", chargeAiLog);
                        KismetHelper.CreateVariableLink(changeAiCharge, "Pawn", currentPawn);
                        KismetHelper.CreateVariableLink(chargeAiLog, "Object", currentPawn);

                        // Connect sequence objects - ASSAULT BRANCH
                        KismetHelper.CreateOutputLink(aiChoiceComp, "A >= B", changeAiAssault);
                        KismetHelper.CreateOutputLink(changeAiAssault, "Out", assaultAiLog);
                        KismetHelper.CreateVariableLink(changeAiAssault, "Pawn", currentPawn);
                        KismetHelper.CreateVariableLink(assaultAiLog, "Object", currentPawn);

                        // Stop timer on any event in this sequence 
                        var events = KismetHelper.GetAllSequenceElements(exp).OfType<ExportEntry>().Where(x => x.IsA("SeqEvent")).ToList();
                        foreach (var seqEvent in events)
                        {
                            KismetHelper.CreateOutputLink(seqEvent, "Out", delay, 1); // Cancel the delay as spawn stopped or changed (or restarted)
                        }

                        // Connect sequence object - toxic / phasic (Needs gated to only activate later!)
                        KismetHelper.CreateOutputLink(crustAttach, "Done", setWeaponAttributes);
                        KismetHelper.CreateVariableLink(setWeaponAttributes, "Pawn", currentPawn);
                        KismetHelper.CreateVariableLink(setWeaponAttributes, "Toxic Factor", toxicFactor);
                        KismetHelper.CreateVariableLink(setWeaponAttributes, "Phasic Factor", phasicFactor);

                        exp.WriteProperty(new StrProperty("Spawn_Single_Guy_SUR", "ObjName"));
                        VTestAdditionalContent.InstallTalentRamping(crustAttach, "Done", vTestOptions);
                        VTestAdditionalContent.InstallPowerRamping(crustAttach, "Done", vTestOptions);
                    }
                }

                #endregion

                #region Fix dual-finishing sequence. Someone thought they were being clever, bet they didn't know it'd cause an annurysm 12 years later

                else if (seqName == "Hench_Take_Damage")
                {
                    var sequenceObjects = exp.GetProperty<ArrayProperty<ObjectProperty>>("SequenceObjects").Select(x => x.ResolveToEntry(le1File) as ExportEntry).ToList();
                    var attachEvents = sequenceObjects.Where(x => x.ClassName == "SeqAct_AttachToEvent").ToList(); // We will route one of these to the other
                    var starting = sequenceObjects.First(x => x.ClassName == "SeqEvent_SequenceActivated");
                    //var ending = sequenceObjects.First(x => x.ClassName == "SeqEvent_FinishSequence");
                    KismetHelper.RemoveOutputLinks(starting); // prevent dual outs
                    KismetHelper.CreateOutputLink(starting, "Out", attachEvents[0]);
                    KismetHelper.RemoveOutputLinks(attachEvents[0]);
                    KismetHelper.CreateOutputLink(attachEvents[0], "Out", attachEvents[1]); // Make it serial
                }
                #endregion

                #region Issue Rally Command at map start to ensure squadmates don't split up, blackscreen off should be fade in not turn off
                else if (seqName == "TA_V3_Gametype_Handler")
                {
                    // Time Trial

                    // Auto draw weapon (hookup)
                    var startObj = VTestKismet.FindSequenceObjectByClassAndPosition(exp, "BioSeqAct_SetActionState"/*, 712, 2256*/);

                    // RALLY
                    var rallyObj = SequenceObjectCreator.CreateSequenceObject(le1File, "LEXSeqAct_SquadCommand", vTestOptions.cache);
                    KismetHelper.AddObjectToSequence(rallyObj, exp);
                    KismetHelper.CreateOutputLink(startObj, "Out", rallyObj);

                    // Time to beat notification
                    var topScore = VTestKismet.InstallVTestHelperSequenceNoInput(le1File, exp.InstancedFullPath, "HelperSequences.GetTopScore", vTestOptions);
                    var showMessage = SequenceObjectCreator.CreateSequenceObject(exp, "LEXSeqAct_ShowMessageEx", vTestOptions.cache);
                    var scoreString = SequenceObjectCreator.CreateString(exp, "<Unset>", vTestOptions.cache);
                    KismetHelper.CreateVariableLink(topScore, "ScoreString", scoreString);
                    KismetHelper.CreateVariableLink(showMessage, "Message", scoreString);
                    KismetHelper.CreateVariableLink(showMessage, "DisplayTime", SequenceObjectCreator.CreateFloat(exp, 2.5f, vTestOptions.cache));
                    KismetHelper.CreateOutputLink(topScore, "Out", showMessage);
                    KismetHelper.CreateOutputLink(rallyObj, "Out", topScore);
                    BIOA_PRC2_CC_DSG.FixSimMapTextureLoading(VTestKismet.FindSequenceObjectByClassAndPosition(exp, "BioSeqAct_Delay", 72, 1736), vTestOptions);
                }
                else if (seqName == "Check_Capping_Completion")
                {
                    // Survival uses this as game mode?
                    // Capture...?
                    // Both use this same named item because why not
                    var startObj = VTestKismet.FindSequenceObjectByClassAndPosition(exp, "BioSeqAct_SetActionState"/*, 584, 2200*/);

                    // RALLY
                    var rallyObj = SequenceObjectCreator.CreateSequenceObject(le1File, "LEXSeqAct_SquadCommand", vTestOptions.cache);
                    KismetHelper.AddObjectToSequence(rallyObj, exp);
                    KismetHelper.CreateOutputLink(startObj, "Out", rallyObj); // RALLY
                    BIOA_PRC2_CC_DSG.FixSimMapTextureLoading(VTestKismet.FindSequenceObjectByClassAndPosition(exp, "BioSeqAct_Delay"/*, -152, 1768*/), vTestOptions);


                    // Time to beat notification
                    var topScore = VTestKismet.InstallVTestHelperSequenceNoInput(le1File, exp.InstancedFullPath, "HelperSequences.GetTopScore", vTestOptions);
                    var showMessage = SequenceObjectCreator.CreateSequenceObject(exp, "LEXSeqAct_ShowMessageEx", vTestOptions.cache);
                    var scoreString = SequenceObjectCreator.CreateString(exp, "<Unset>", vTestOptions.cache);
                    KismetHelper.CreateVariableLink(topScore, "ScoreString", scoreString);
                    KismetHelper.CreateVariableLink(showMessage, "Message", scoreString);
                    KismetHelper.CreateVariableLink(showMessage, "DisplayTime", SequenceObjectCreator.CreateFloat(exp, 2.5f, vTestOptions.cache));
                    KismetHelper.CreateOutputLink(topScore, "Out", showMessage);
                    KismetHelper.CreateOutputLink(rallyObj, "Out", topScore);


                    var surDecayStart = VTestKismet.FindSequenceObjectByClassAndPosition(exp, "BioSeqAct_DUISetTextStringRef", 3544, 2472);
                    if (surDecayStart != null)
                    {
                        // It's survival

                        // 09/25/2024 - In OT the map started pretty much immediately, in crossgen we give the player a few seconds to prepare for combat
                        // We are moving the timer start signaling to after the delay so they don't get free time for survival
                        // We remove the events from here; we will fire them after the delay in the parent sequence
                        KismetHelper.RemoveOutputLinks(surDecayStart);

                        // Add signal to decay start
                        var surDecaySignal = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_ActivateRemoteEvent", vTestOptions.cache);
                        KismetHelper.AddObjectToSequence(surDecaySignal, exp);
                        surDecaySignal.WriteProperty(new NameProperty("CROSSGEN_START_SUR_HEALTHGATE_DECAY", "EventName"));
                        KismetHelper.CreateOutputLink(surDecayStart, "Out", surDecaySignal);
                    }
                }
                else if (seqName == "Vampire_Mode_Handler")
                {
                    // Hunt
                    var startObj = VTestKismet.FindSequenceObjectByClassAndPosition(exp, "BioSeqAct_SetActionState" /*, 1040, 2304*/);

                    // RALLY
                    var rallyObj = SequenceObjectCreator.CreateSequenceObject(le1File, "LEXSeqAct_SquadCommand", vTestOptions.cache);
                    KismetHelper.AddObjectToSequence(rallyObj, exp);
                    KismetHelper.CreateOutputLink(startObj, "Out", rallyObj); // RALLY

                    // Score to beat notification
                    var topScore = VTestKismet.InstallVTestHelperSequenceNoInput(le1File, exp.InstancedFullPath, "HelperSequences.GetTopScore", vTestOptions);
                    var showMessage = SequenceObjectCreator.CreateSequenceObject(exp, "LEXSeqAct_ShowMessageEx", vTestOptions.cache);
                    var scoreString = SequenceObjectCreator.CreateString(exp, "<Unset>", vTestOptions.cache);
                    KismetHelper.CreateVariableLink(topScore, "ScoreString", scoreString);
                    KismetHelper.CreateVariableLink(showMessage, "Message", scoreString);
                    KismetHelper.CreateVariableLink(showMessage, "DisplayTime", SequenceObjectCreator.CreateFloat(exp, 2.5f, vTestOptions.cache));
                    KismetHelper.CreateOutputLink(topScore, "Out", showMessage);
                    KismetHelper.CreateOutputLink(rallyObj, "Out", topScore);
                    BIOA_PRC2_CC_DSG.FixSimMapTextureLoading(VTestKismet.FindSequenceObjectByClassAndPosition(exp, "BioSeqAct_Delay", 304, 1952), vTestOptions);
                }
                else if (seqName is "Play_Ahern_Quip_For_TA_Intro" or "Play_Ahern_Quip_For_SUR_Intro" or "Play_Ahern_Quip_For_VAM_Intro" or "Play_Ahern_Quip_For_CAH_Intro")
                {
                    // Install music remote event at the end
                    var setBool = KismetHelper.GetSequenceObjects(exp).OfType<ExportEntry>().First(x => x.ClassName == "SeqAct_SetBool" && x.GetProperty<ArrayProperty<StructProperty>>("OutputLinks")[0].GetProp<ArrayProperty<StructProperty>>("Links").Count == 0);
                    var remoteEvent = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_ActivateRemoteEvent", vTestOptions.cache);
                    KismetHelper.AddObjectToSequence(remoteEvent, exp);
                    KismetHelper.CreateOutputLink(setBool, "Out", remoteEvent);
                    remoteEvent.WriteProperty(new NameProperty("StartSimMusic", "EventName"));
                }

                // SURVIVAL RAMPING - Survival Thai, Cave, Lava (Crate doesn't have one, no point doing it for Ahern's)
                else if (seqName is "SUR_Thai_Handler" or "SUR_Cave_Handler" or "SUR_Lava_Handler")
                {
                    var startTimerSignal = SequenceObjectCreator.CreateActivateRemoteEvent(exp, "START_TIMER");
                    var delay = KismetHelper.GetSequenceObjects(exp).OfType<ExportEntry>().FirstOrDefault(x => x.ClassName == "BioSeqAct_Delay"); // First one is the one we care about
                    KismetHelper.InsertActionAfter(delay, "Finished", startTimerSignal, 0, "Out");

                    VTestSurvival.InstallSurvivalRamping(startTimerSignal, exp, vTestOptions);
                }
                else if (seqName is "CAH_Cave_Handler" or "CAH_Thai_Handler" or "CAH_Lava_Handler")
                {
                    VTestCapture.InstallCaptureRamping(exp, vTestOptions);
                }
                //else if (seqName == "Cap_And_Hold_Point")
                //{
                //    // Capture
                //    var startObj = FindSequenceObjectByPosition(exp, 584, 2200, "BioSeqAct_SetActionState");
                //    var newObj = SequenceObjectCreator.CreateSequenceObject(le1File, "LEXSeqAct_SquadCommand", MEGame.LE1, vTestOptions.cache);
                //    KismetHelper.AddObjectToSequence(newObj, exp);
                //    KismetHelper.CreateOutputLink(startObj, "Out", newObj); // RALLY
                //}

                #region Black Screen Fade In instead of just turning off
                if (seqName is "Vampire_Mode_Handler" or "Check_Capping_Completion" or "TA_V3_Gametype_Handler")
                {
                    var sequenceObjects = exp.GetProperty<ArrayProperty<ObjectProperty>>("SequenceObjects").Select(x => x.ResolveToEntry(le1File) as ExportEntry).ToList();
                    var fadeFromBlacks = sequenceObjects.Where(x => x.ClassName == "BioSeqAct_BlackScreen").ToList(); // We will route one of these to the other
                    if (fadeFromBlacks.Count != 1)
                        Debugger.Break();
                    foreach (var ffb in fadeFromBlacks)
                    {
                        ffb.WriteProperty(new EnumProperty("BlackScreenAction_FadeFromBlack", "BlackScreenActionSet", MEGame.LE1, "m_eBlackScreenAction"));
                    }
                }
                #endregion

                #endregion

                #region Black screen on scoreboard
                else if (seqName == "OL_Size")
                {
                    // Fadein is handled by scoreboard DSG
                    var compareBool = VTestKismet.FindSequenceObjectByClassAndPosition(exp, "SeqCond_CompareBool", 8064, 3672);
                    KismetHelper.SkipSequenceElement(compareBool, "True"); // Skip out to true

                    // Add signal to stop decay and restore healthgate
                    if (VTestKismet.IsContainedWithinSequenceNamed(exp, "SUR_Lava_Handler", true) ||
                        VTestKismet.IsContainedWithinSequenceNamed(exp, "SUR_Thai_Handler", true) ||
                        VTestKismet.IsContainedWithinSequenceNamed(exp, "SUR_Crate_Handler", true) ||
                        VTestKismet.IsContainedWithinSequenceNamed(exp, "SUR_Cave_Handler", true))
                    {
                        // It's survival, we should signal to restore the healthgate
                        var surRestoreHealthgateSignal = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_ActivateRemoteEvent", vTestOptions.cache);
                        KismetHelper.AddObjectToSequence(surRestoreHealthgateSignal, exp);
                        surRestoreHealthgateSignal.WriteProperty(new NameProperty("CROSSGEN_RESTORE_SUR_HEALTHGATE", "EventName"));
                        KismetHelper.CreateOutputLink(VTestKismet.FindSequenceObjectByClassAndPosition(exp, "SeqEvent_SequenceActivated"), "Out", surRestoreHealthgateSignal);
                    }

                }
                #endregion

                else if (seqName == "SUR_Lava_Handler" || seqName == "SUR_Thai_Handler" || seqName == "SUR_Crate_Handler" || seqName == "SUR_Cave_Handler")
                {
                    VTestKismet.InstallVTestHelperSequenceNoInput(le1File, exp.InstancedFullPath, "HelperSequences.SurvivalHealthGateCurve", vTestOptions);
                }
            }

            if (upperFName == "BIOA_PRC2_CCLAVA_DSG")
            {
                PostPortingCorrection_CCLava_DSG();
            }
        }

        /// <summary>
        /// Level specific corrections for CCLava_DSG
        /// </summary>
        private void PostPortingCorrection_CCLava_DSG()
        {
            // SeqAct_ChangeCollision changed and requires an additional property otherwise it doesn't work.
            string[] collisionsToTurnOff =
            [
                // Hut doors and kill volumes
                "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_1",
                "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_3",
                "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_5",
                "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_6",
            ];

            foreach (var cto in collisionsToTurnOff)
            {
                var exp = le1File.FindExport(cto);
                exp.WriteProperty(new EnumProperty("COLLIDE_NoCollision", "ECollisionType", MEGame.LE1, "CollisionType"));
            }

            string[] collisionsToTurnOn =
            [
                // Hut doors and kill volumes
                "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_0",
                "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_9",
                "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_2",
                "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_4",
            ];

            foreach (var cto in collisionsToTurnOn)
            {
                var exp = le1File.FindExport(cto);
                exp.WriteProperty(new EnumProperty("COLLIDE_BlockAll", "ECollisionType", MEGame.LE1, "CollisionType"));
            }

            // Add code to disable reachspecs when turning the doors on so enemies do not try to use these areas
            var hutSeq = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility");
            var cgSource = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_4");
            var disableReachSpecs = SequenceObjectCreator.CreateSequenceObject(le1File, "LEXSeqAct_ToggleReachSpec", vTestOptions.cache);

            KismetHelper.AddObjectToSequence(disableReachSpecs, hutSeq);
            KismetHelper.CreateOutputLink(cgSource, "Out", disableReachSpecs, 2);

            string[] reachSpecsToDisable =
            [
                // NORTH ROOM
                "TheWorld.PersistentLevel.ReachSpec_1941", // CoverLink to PathNode
                "TheWorld.PersistentLevel.ReachSpec_1937", // CoverLink to CoverLink
                "TheWorld.PersistentLevel.ReachSpec_2529", // PathNode to PathNode

                // SOUTH ROOM
                "TheWorld.PersistentLevel.ReachSpec_1856", // CoverLink to PathNode
                "TheWorld.PersistentLevel.ReachSpec_1849", // CoverLink to CoverLink
            ];

            foreach (var rs in reachSpecsToDisable)
            {
                var obj = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_Object", vTestOptions.cache);
                KismetHelper.AddObjectToSequence(obj, hutSeq);
                obj.WriteProperty(new ObjectProperty(le1File.FindExport(rs), "ObjValue"));
                KismetHelper.CreateVariableLink(disableReachSpecs, "ReachSpecs", obj);
            }
        }

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

        public static void FixGethFlashlights(ExportEntry sequence, VTestOptions vTestOptions)
        {
            // Custom class by Kinkojiro to add and un-add the flashlight VFX
            var actorFactoryWithOwner = VTestKismet.FindSequenceObjectByClassAndPosition(sequence, "SeqAct_ActorFactoryWithOwner");
            var attachFL = SequenceObjectCreator.CreateSequenceObject(sequence.FileRef, "LEXSeqAct_AttachGethFlashLight", vTestOptions.cache);
            var deattachFL = SequenceObjectCreator.CreateSequenceObject(sequence.FileRef, "LEXSeqAct_AttachGethFlashLight", vTestOptions.cache);
            KismetHelper.AddObjectsToSequence(sequence, false, attachFL, deattachFL);

            // ATTACH FLASHLIGHT
            {
                var outLinksFactory = KismetHelper.GetOutputLinksOfNode(actorFactoryWithOwner);
                var originalOutlink = outLinksFactory[0][2].LinkedOp;
                outLinksFactory[0][2].LinkedOp = attachFL; // repoint to attachFL
                KismetHelper.WriteOutputLinksToNode(actorFactoryWithOwner, outLinksFactory);
                KismetHelper.CreateOutputLink(attachFL, "Done", originalOutlink as ExportEntry);
                var currentPawn = VTestKismet.FindSequenceObjectByClassAndPosition(sequence, "SeqVar_Object", 4536, 2016);
                KismetHelper.CreateVariableLink(attachFL, "Target", currentPawn);
            }
            // DETACH FLASHLIGHT
            {
                var attachCrustEffect = VTestKismet.FindSequenceObjectByClassAndPosition(sequence, "BioSeqAct_AttachCrustEffect", 5752, 2000);
                var attachOutlinks = KismetHelper.GetOutputLinksOfNode(attachCrustEffect);
                var originalOutlink = attachOutlinks[0][1].LinkedOp;
                attachOutlinks[0][1].LinkedOp = deattachFL; // repoint to deattachFL
                attachOutlinks[0][1].InputLinkIdx = 1; // Detach
                KismetHelper.WriteOutputLinksToNode(attachCrustEffect, attachOutlinks);
                KismetHelper.CreateOutputLink(deattachFL, "Done", originalOutlink as ExportEntry);

                var cachedPawn = VTestKismet.FindSequenceObjectByClassAndPosition(sequence, "SeqVar_Object", 5640, 2128);
                KismetHelper.CreateVariableLink(deattachFL, "Target", cachedPawn);
            }
        }

        public static void RemoveBitExplosionEffect(ExportEntry exp)
        {
            // These sequences need the 'bit explosion' effect removed because BioWare changed something in SeqAct_ActorFactory and completely broke it
            // We are just going to use the crust effect instead
            var sequenceObjects = exp.GetProperty<ArrayProperty<ObjectProperty>>("SequenceObjects");
            foreach (var seqObjProp in sequenceObjects.ToList()) // ToList as we're going to modify it so we need a copy
            {
                if (!sequenceObjects.Contains(seqObjProp))
                    continue; // it's already been removed

                var seqObj = seqObjProp.ResolveToEntry(exp.FileRef) as ExportEntry;
                if (seqObj != null)
                {
                    if (seqObj.ClassName == "SeqAct_ActorFactory")
                    {
                        var outLinks = KismetHelper.GetOutputLinksOfNode(seqObj);
                        outLinks[0].RemoveAt(0); // Remove the first outlink, which goes to Delay
                        KismetHelper.WriteOutputLinksToNode(seqObj, outLinks); // remove the link so we don't try to connect to it when skipping
                        KismetHelper.SkipSequenceElement(seqObj, "Finished");
                        sequenceObjects.Remove(seqObjProp);
                    }
                    else if (seqObj.ClassName == "BioSeqAct_Delay")
                    {
                        // We can ID these by the position data since they are built from a template and thus always have the same positions
                        // It also references destroying the spawned particle system
                        var props = seqObj.GetProperties();
                        if (props.GetProp<IntProperty>("ObjPosX")?.Value == 4440 &&
                            props.GetProp<IntProperty>("ObjPosY")?.Value == 2672)
                        {
                            // This needs removed too
                            var nextNodes = KismetHelper.GetOutputLinksOfNode(seqObj);
                            var nextNode = nextNodes[0][0].LinkedOp as ExportEntry;
                            var subNodeOfNext = KismetHelper.GetVariableLinksOfNode(nextNode)[0].LinkedNodes[0] as ExportEntry;

                            // Remove all of them from the sequence
                            sequenceObjects.Remove(seqObjProp); // Delay
                            sequenceObjects.Remove(new ObjectProperty(subNodeOfNext.UIndex)); // Destroy
                            sequenceObjects.Remove(new ObjectProperty(nextNode.UIndex)); // SeqVar_Object
                        }
                    }
                }
            }

            exp.WriteProperty(sequenceObjects);
        }

        /// <summary>
        /// Changes sequencing a bit to install a force-load of mips plus a delay
        /// </summary>
        public static void FixSimMapTextureLoading(ExportEntry startDelay, VTestOptions vTestOptions, ExportEntry streamingLocation = null)
        {
            var sequence = KismetHelper.GetParentSequence(startDelay);
            var stopLoadingMovie = VTestKismet.FindSequenceObjectByClassAndPosition(sequence, "BioSeqAct_StopLoadingMovie");
            KismetHelper.RemoveOutputLinks(startDelay);

            var streamInTextures = SequenceObjectCreator.CreateSequenceObject(startDelay.FileRef, "SeqAct_StreamInTextures", vTestOptions.cache);
            var streamInDelay = SequenceObjectCreator.CreateSequenceObject(startDelay.FileRef, "SeqAct_Delay", vTestOptions.cache);
            var remoteEventStreamIn = SequenceObjectCreator.CreateSequenceObject(startDelay.FileRef, "SeqAct_ActivateRemoteEvent", vTestOptions.cache);

            KismetHelper.AddObjectToSequence(remoteEventStreamIn, sequence);
            KismetHelper.AddObjectToSequence(streamInTextures, sequence);
            KismetHelper.AddObjectToSequence(streamInDelay, sequence);

            streamInDelay.WriteProperty(new FloatProperty(4f, "Duration")); // Load screen will be 4s
            streamInTextures.WriteProperty(new FloatProperty(8f, "Seconds")); // Force textures to stream in at full res for a bit over the load screen time
            remoteEventStreamIn.WriteProperty(new NameProperty("CROSSGEN_PrepTextures", "EventName")); // This is used to signal other listeners that they should also stream in textures

            streamingLocation ??= KismetHelper.GetSequenceObjects(sequence).OfType<ExportEntry>().First(x => x.ClassName == "SeqVar_External" && x.GetProperty<StrProperty>("VariableLabel")?.Value == "Scenario_Start_Location");
            KismetHelper.CreateVariableLink(streamInTextures, "Location", streamingLocation);

            KismetHelper.CreateOutputLink(startDelay, "Finished", remoteEventStreamIn); // Initial 1 frame delay to event signal
            KismetHelper.CreateOutputLink(remoteEventStreamIn, "Out", streamInTextures); // Event Signal to StreamInTextures
            KismetHelper.CreateOutputLink(remoteEventStreamIn, "Out", streamInDelay); // Event Signal to Loading Screen Delay
            KismetHelper.CreateOutputLink(streamInDelay, "Finished", stopLoadingMovie); // Loading Screen Delay to Stop Loading Movie
        }
    }
}
