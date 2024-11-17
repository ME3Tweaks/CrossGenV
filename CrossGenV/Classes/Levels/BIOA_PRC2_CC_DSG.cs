using System;
using CrossGenV.Classes.Modes;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using System.Diagnostics;
using System.Linq;
using LegendaryExplorerCore.Gammtek.Extensions;

namespace CrossGenV.Classes.Levels
{
    /// <summary>
    /// BIOA_PRC2_CCTHAI_DSG, BIOA_PRC2_CCCAVE_DSG, BIOA_PRC2_CCLAVA_DSG, BIOA_PRC2_CCCRATE_DSG, NOT AHERN
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

        public virtual void PostPortingCorrection()
        {
            SharedPostPortingCorrection();
        }

        public void FixSoftlockWhenRagdollOnGameEnd()
        {
            foreach (var teleport in le1File.Exports.Where(x => x.ClassName == "SeqAct_Teleport" && VTestKismet.IsContainedWithinSequenceNamed(x, "OL_Size")).ToList())
            {
                var seq = KismetHelper.GetParentSequence(teleport);
                var objs = KismetHelper.GetSequenceObjects(seq).OfType<ExportEntry>().ToList();
                var connections = KismetHelper.FindOutputConnectionsToNode(teleport, objs);

                var forceTeleport = SequenceObjectCreator.CreateSequenceObject(seq, "LEXSeqAct_ForceTeleport", vTestOptions.cache);
                foreach (var con in connections)
                {
                    var oVars = KismetHelper.GetVariableLinksOfNode(teleport);
                    KismetHelper.WriteVariableLinksToNode(forceTeleport, oVars);
                    KismetHelper.ChangeOutputLink(con, 0, 0, forceTeleport.UIndex);
                    KismetHelper.WriteOutputLinksToNode(forceTeleport, KismetHelper.GetOutputLinksOfNode(teleport));

                    // KismetHelper.RemoveFromSequence(teleport, true);
                    KismetHelper.RemoveAllLinks(teleport); // Disconnect from nodes
                }

            }
        }

        /// <summary>
        /// Sets up the capture ring changes that changes ring over capture time
        /// </summary>
        /// <param name="mapName"></param>
        public void SetCaptureRingChanger(string mapName)
        {
            foreach (var seq in le1File.Exports.Where(x => x.ClassName == "Sequence").ToList())
            {
                var seqName = VTestKismet.GetSequenceName(seq);
                if (seqName == "Cap_And_Hold_Point" && VTestKismet.IsContainedWithinSequenceNamed(seq, $"CAH_{mapName}_Handler"))
                {
                    var initHook = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqAct_SetMaterial", -1424, 2872);

                    var percentCalc = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "BioSeqAct_ScalarMathUnit", 2832, 3216);
                    var percent = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqVar_Float", 2920, 3304);
                    var ring = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqVar_External", 1000, 4104);

                    var capping = SequenceObjectCreator.CreateSequenceObject(seq, "LEXSeqAct_RingCapping", vTestOptions.cache);

                    // Reset (on initial gametype start)
                    KismetHelper.CreateOutputLink(initHook, "Out", capping);
                    // Reset (on capture starting)
                    KismetHelper.CreateOutputLink(initHook, "Out", capping);

                    // Update
                    // For percent calc, we MUST be the first outlink, or it will break when we do a further calculation in this frame for completed
                    var outLinks = KismetHelper.GetOutputLinksOfNode(percentCalc);
                    outLinks[0].Insert(0, new OutputLink() { LinkedOp = capping, InputLinkIdx = 1 }); // Update
                    KismetHelper.WriteOutputLinksToNode(percentCalc, outLinks);

                    KismetHelper.CreateVariableLink(capping, "Ring", ring);
                    KismetHelper.CreateVariableLink(capping, "Completion", percent);



                    // SetMaterial for completed
                    // This material is referenced in RingCapping properties so it will be pulled in out of VTestHelper automatically
                    var setCompletedPointMat = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqAct_SetMaterial", 3536, 3232);
                    var setCompletedRingMat = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqAct_SetMaterial", 3680, 3264);
                    // setCompletedMat.WriteProperty(new ObjectProperty(seq.FileRef.FindExport("CROSSGENV.StrategicRing_Cap_MAT_Finished_matInst"), "NewMaterial"));
                    KismetHelper.SkipSequenceElement(setCompletedRingMat, "Out");
                    KismetHelper.RemoveFromSequence(setCompletedRingMat, false);
                    KismetHelper.CreateOutputLink(setCompletedPointMat, "Out", capping, 2); // Complete

                    // SetMaterial for initial touch
                    var cappingCompletedCheck = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqCond_CompareBool", 360, 3216);
                    var capturingMaterial = le1File.FindExport("CROSSGENV.StrategicRing_Cap_MAT_NEW_matInst"); // Ring capturing material
                    var capPointObject = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqVar_External", 1000, 4264);
                    var setCapturingMaterialOnPoint = SequenceObjectCreator.CreateSetMaterial(seq, capPointObject, capturingMaterial, vTestOptions.cache);
                    KismetHelper.SetComment(setCapturingMaterialOnPoint, "Not capped yet - set to ring capturing material so they share same mat");
                    KismetHelper.CreateOutputLink(cappingCompletedCheck, "True", capping, 2); // Complete

                    // Set initial material state BEFORE the delay demiurge used. No idea why. Even they commented asking why it was so late
                    var finishedCappingQ = SequenceObjectCreator.CreateCompareBool(seq, SequenceObjectCreator.CreateScopeNamed(seq, "SeqVar_Bool", "Capped_Yet", vTestOptions.cache));
                    KismetHelper.SetComment(finishedCappingQ, "Crossgen: Setup material on same frame as when we begin capping instead after later delay");
                    KismetHelper.CreateOutputLink(finishedCappingQ, "True", capping, 2); // Finished
                    KismetHelper.CreateOutputLink(finishedCappingQ, "False", setCapturingMaterialOnPoint);
                    KismetHelper.CreateOutputLink(setCapturingMaterialOnPoint, "Out", capping, 0); // Reset

                    // We re-order the outlinks of cah mode check so that our material update runs first to ensure proper execution order
                    var isCahMode = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqCond_CompareBool", -1936, 3224);
                    outLinks = KismetHelper.GetOutputLinksOfNode(isCahMode);
                    outLinks[0].Insert(0, new OutputLink() { LinkedOp = finishedCappingQ });
                    KismetHelper.WriteOutputLinksToNode(isCahMode, outLinks);


                    // Untouch point during capture - reset point material as it will remain visible
                    var uncappedMat = le1File.FindExport("BIOA_PRC2_MatFX.StrategicPoint_Cap_MAT");
                    var untouchSound = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqAct_PlaySound", -1912, 3696);
                    var setToBlinkingPointMat = SequenceObjectCreator.CreateSetMaterial(seq, capPointObject, uncappedMat, vTestOptions.cache);
                    KismetHelper.SetComment(setToBlinkingPointMat, "Reset back to uncapped material");
                    KismetHelper.InsertActionAfter(untouchSound, "Out", setToBlinkingPointMat, 0, "Out");

                    // Finished Capping: Change to Completed look
                    EntryExporter.ExportExportToPackage(vTestOptions.vTestHelperPackage.FindExport("CROSSGENV.StrategicRing_Cap_MAT_Finished_matInst"), le1File, out var portedFinishedMat, vTestOptions.cache);
                    var completedSet = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqAct_SetMaterial", 3536, 3232);
                    completedSet.WriteProperty(new ObjectProperty(portedFinishedMat, "NewMaterial"));

                    continue;
                }
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

        public static void RemoveBitExplosionEffect(ExportEntry exp, VTestOptions options)
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
            // Above is 2021 code, before we had helper classes to find things.

            // 11/09/2024 - Remove all crusts from pawn before setting holowipe crust
            // Also 11/09/2024 - For some reason removing crusts also removes player ability to turn for indeterminate amounts of time
            //var deathSetObj = VTestKismet.FindSequenceObjectByClassAndPosition(exp, "SeqAct_SetObject", 5568, 2008);
            //var currentPawn = VTestKismet.FindSequenceObjectByClassAndPosition(exp, "SeqVar_Object", 4536, 2016);
            //var removeCrusts = SequenceObjectCreator.CreateSequenceObject(exp, "LEXSeqAct_RemoveAllCrustEffects", options.cache);
            //KismetHelper.CreateVariableLink(removeCrusts, "Target", currentPawn);
            //KismetHelper.InsertActionAfter(deathSetObj, "Out", removeCrusts, 0, "Out");

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

            // 11/15/2024 - Add block for texture
            var blockForTextureStreaming = SequenceObjectCreator.CreateBlockForTextureStreaming(sequence, vTestOptions.cache);
            KismetHelper.CreateOutputLink(streamInDelay, "Finished", blockForTextureStreaming); // Loading Screen Delay to block for any remaining streaming
            KismetHelper.CreateOutputLink(blockForTextureStreaming, "Finished", stopLoadingMovie); // Block to Stop Loading Movie

        }

        public void SharedPostPortingCorrection()
        {
            var upperFName = le1File.FileNameNoExtension.ToUpper();

            // Kill streak voice line sequence
            VTestKismet.InstallVTestHelperSequenceNoInput(le1File, "TheWorld.PersistentLevel.Main_Sequence", "HelperSequences.KillStreakVoiceLine", vTestOptions);

            PreventSavingOnSimLoad(le1File, vTestOptions);
            ResetRamping(le1File, vTestOptions);

            // 10/22/2024 - NaNuke - Custom capture ring shader
            VTestPostCorrections.AddCustomShader(le1File, "CROSSGENV.StrategicRing_Cap_MAT_NEW");

            // 2024: Disable running until sim wipe starts which helps prevents touching capture points too soon.
            VTestPostCorrections.DisallowRunningUntilModeStarts(le1File, vTestOptions);

            VTestAudio.SetupMusicIntensity(le1File, upperFName, vTestOptions);
            VTestAudio.SetupKillStreakVO(le1File, vTestOptions);

            // 11/14/2024 - Note on this - This is for default enemies (not wavelist, but originals), this prevents a blocking load on first spawn
            // as demiurge's pre-caching doesn't work.
            // 2021: Force the pawns that will spawn to have their meshes in memory
            // They are not referenced directly
            var assetsToReference = le1File.Exports.Where(x => assetsToEnsureReferencedInSim.Contains(x.InstancedFullPath)).ToArray();
            VTestUtility.AddWorldReferencedObjects(le1File, assetsToReference);

            // depends on SpawnSingleGuy sequence name.
            VTestCapture.AddCaptureEngagementSequencing(le1File, vTestOptions);

            // PASS 1
            foreach (var seq in le1File.Exports.Where(x => x.ClassName == "Sequence").ToList())
            {
                var seqName = VTestKismet.GetSequenceName(seq);

                #region Skip broken SeqAct_ActorFactory for bit explosion | Increase Surival Mode Engagement

                if (seqName == "Spawn_Single_Guy")
                {
                    RemoveBitExplosionEffect(seq, vTestOptions);
                    FixGethFlashlights(seq, vTestOptions);

                    // Spawn_Single_Guy is also updated in a second pass since we clone sequences and this loop will miss that.
                }

                #endregion

                #region Fix dual-finishing sequence. Someone thought they were being clever, bet they didn't know it'd cause an annurysm 12 years later

                else if (seqName == "Hench_Take_Damage")
                {
                    var sequenceObjects = seq.GetProperty<ArrayProperty<ObjectProperty>>("SequenceObjects").Select(x => x.ResolveToEntry(le1File) as ExportEntry).ToList();
                    var attachEvents = sequenceObjects.Where(x => x.ClassName == "SeqAct_AttachToEvent").ToList(); // We will route one of these to the other
                    var starting = sequenceObjects.First(x => x.ClassName == "SeqEvent_SequenceActivated");
                    //var ending = sequenceObjects.First(x => x.ClassName == "SeqEvent_FinishSequence");
                    KismetHelper.RemoveOutputLinks(starting); // prevent dual outs
                    KismetHelper.CreateOutputLink(starting, "Out", attachEvents[0]);
                    KismetHelper.RemoveOutputLinks(attachEvents[0]);
                    KismetHelper.CreateOutputLink(attachEvents[0], "Out", attachEvents[1]); // Make it serial
                }
                #endregion

                #region Issue Rally Command at map start to ensure squadmates don't split up, blackscreen off should be fade in not turn off, PLAYER_QUIT streaming
                else if (seqName == "TA_V3_Gametype_Handler")
                {
                    // Time Trial

                    // Auto draw weapon (hookup)
                    var startObj = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "BioSeqAct_SetActionState"/*, 712, 2256*/);

                    // RALLY
                    var rallyObj = SequenceObjectCreator.CreateSequenceObject(le1File, "LEXSeqAct_SquadCommand", vTestOptions.cache);
                    KismetHelper.AddObjectToSequence(rallyObj, seq);
                    KismetHelper.CreateOutputLink(startObj, "Out", rallyObj);

                    // Time to beat notification
                    var topScore = VTestKismet.InstallVTestHelperSequenceNoInput(le1File, seq.InstancedFullPath, "HelperSequences.GetTopScore", vTestOptions);
                    var showMessage = SequenceObjectCreator.CreateSequenceObject(seq, "LEXSeqAct_ShowMessageEx", vTestOptions.cache);
                    var scoreString = SequenceObjectCreator.CreateString(seq, "<Unset>", vTestOptions.cache);
                    KismetHelper.CreateVariableLink(topScore, "ScoreString", scoreString);
                    KismetHelper.CreateVariableLink(showMessage, "Message", scoreString);
                    KismetHelper.CreateVariableLink(showMessage, "DisplayTime", SequenceObjectCreator.CreateFloat(seq, 2.5f, vTestOptions.cache));
                    KismetHelper.CreateOutputLink(topScore, "Out", showMessage);
                    KismetHelper.CreateOutputLink(rallyObj, "Out", topScore);
                    BIOA_PRC2_CC_DSG.FixSimMapTextureLoading(VTestKismet.FindSequenceObjectByClassAndPosition(seq, "BioSeqAct_Delay", 72, 1736), vTestOptions);

                    PlayerQuitStreamingFix(VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqAct_SetBool", -904, 3544), "Out");
                }
                else if (seqName == "Check_Capping_Completion")
                {
                    // Survival uses this as game mode?
                    // Capture...?
                    // Both use this same named item because why not
                    var startObj = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "BioSeqAct_SetActionState"/*, 584, 2200*/);

                    // RALLY
                    var rallyObj = SequenceObjectCreator.CreateSequenceObject(le1File, "LEXSeqAct_SquadCommand", vTestOptions.cache);
                    KismetHelper.AddObjectToSequence(rallyObj, seq);
                    KismetHelper.CreateOutputLink(startObj, "Out", rallyObj); // RALLY
                    BIOA_PRC2_CC_DSG.FixSimMapTextureLoading(VTestKismet.FindSequenceObjectByClassAndPosition(seq, "BioSeqAct_Delay"/*, -152, 1768*/), vTestOptions);


                    // Time to beat notification
                    var topScore = VTestKismet.InstallVTestHelperSequenceNoInput(le1File, seq.InstancedFullPath, "HelperSequences.GetTopScore", vTestOptions);
                    var showMessage = SequenceObjectCreator.CreateSequenceObject(seq, "LEXSeqAct_ShowMessageEx", vTestOptions.cache);
                    var scoreString = SequenceObjectCreator.CreateString(seq, "<Unset>", vTestOptions.cache);
                    KismetHelper.CreateVariableLink(topScore, "ScoreString", scoreString);
                    KismetHelper.CreateVariableLink(showMessage, "Message", scoreString);
                    KismetHelper.CreateVariableLink(showMessage, "DisplayTime", SequenceObjectCreator.CreateFloat(seq, 2.5f, vTestOptions.cache));
                    KismetHelper.CreateOutputLink(topScore, "Out", showMessage);
                    KismetHelper.CreateOutputLink(rallyObj, "Out", topScore);


                    var surDecayStart = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "BioSeqAct_DUISetTextStringRef", 3544, 2472);
                    if (surDecayStart != null)
                    {
                        // It's survival

                        // 09/25/2024 - In OT the map started pretty much immediately, in crossgen we give the player a few seconds to prepare for combat
                        // We are moving the timer start signaling to after the delay so they don't get free time for survival
                        // We remove the events from here; we will fire them after the delay in the parent sequence
                        KismetHelper.RemoveOutputLinks(surDecayStart);

                        // Add signal to decay start
                        var surDecaySignal = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_ActivateRemoteEvent", vTestOptions.cache);
                        KismetHelper.AddObjectToSequence(surDecaySignal, seq);
                        surDecaySignal.WriteProperty(new NameProperty("CROSSGEN_START_SUR_HEALTHGATE_DECAY", "EventName"));
                        KismetHelper.CreateOutputLink(surDecayStart, "Out", surDecaySignal);
                    }

                    // Same sequence names, different positions.
                    var playerQuitSetBool = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqAct_SetBool", 144, 4136);
                    if (playerQuitSetBool == null)
                    {
                        playerQuitSetBool = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqAct_SetBool", -568, 3408);
                    }
                    PlayerQuitStreamingFix(playerQuitSetBool, "Out");
                }
                else if (seqName == "Vampire_Mode_Handler")
                {
                    // Hunt
                    var startObj = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "BioSeqAct_SetActionState" /*, 1040, 2304*/);

                    // RALLY
                    var rallyObj = SequenceObjectCreator.CreateSequenceObject(le1File, "LEXSeqAct_SquadCommand", vTestOptions.cache);
                    KismetHelper.AddObjectToSequence(rallyObj, seq);
                    KismetHelper.CreateOutputLink(startObj, "Out", rallyObj); // RALLY

                    // Score to beat notification
                    var topScore = VTestKismet.InstallVTestHelperSequenceNoInput(le1File, seq.InstancedFullPath, "HelperSequences.GetTopScore", vTestOptions);
                    var showMessage = SequenceObjectCreator.CreateSequenceObject(seq, "LEXSeqAct_ShowMessageEx", vTestOptions.cache);
                    var scoreString = SequenceObjectCreator.CreateString(seq, "<Unset>", vTestOptions.cache);
                    KismetHelper.CreateVariableLink(topScore, "ScoreString", scoreString);
                    KismetHelper.CreateVariableLink(showMessage, "Message", scoreString);
                    KismetHelper.CreateVariableLink(showMessage, "DisplayTime", SequenceObjectCreator.CreateFloat(seq, 2.5f, vTestOptions.cache));
                    KismetHelper.CreateOutputLink(topScore, "Out", showMessage);
                    KismetHelper.CreateOutputLink(rallyObj, "Out", topScore);
                    BIOA_PRC2_CC_DSG.FixSimMapTextureLoading(VTestKismet.FindSequenceObjectByClassAndPosition(seq, "BioSeqAct_Delay", 304, 1952), vTestOptions);
                }
                else if (seqName is "Play_Ahern_Quip_For_TA_Intro" or "Play_Ahern_Quip_For_SUR_Intro" or "Play_Ahern_Quip_For_VAM_Intro" or "Play_Ahern_Quip_For_CAH_Intro")
                {
                    // Install music remote event at the end
                    var setBool = KismetHelper.GetSequenceObjects(seq).OfType<ExportEntry>().First(x => x.ClassName == "SeqAct_SetBool" && x.GetProperty<ArrayProperty<StructProperty>>("OutputLinks")[0].GetProp<ArrayProperty<StructProperty>>("Links").Count == 0);
                    var remoteEvent = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_ActivateRemoteEvent", vTestOptions.cache);
                    KismetHelper.AddObjectToSequence(remoteEvent, seq);
                    KismetHelper.CreateOutputLink(setBool, "Out", remoteEvent);
                    remoteEvent.WriteProperty(new NameProperty("StartSimMusic", "EventName"));
                }

                // SURVIVAL RAMPING - Survival Thai, Cave, Lava (Crate doesn't have one, no point doing it for Ahern's)
                else if (seqName is "SUR_Thai_Handler" or "SUR_Cave_Handler" or "SUR_Lava_Handler")
                {
                    var startTimerSignal = SequenceObjectCreator.CreateActivateRemoteEvent(seq, "START_TIMER");
                    var delay = KismetHelper.GetSequenceObjects(seq).OfType<ExportEntry>().FirstOrDefault(x => x.ClassName == "BioSeqAct_Delay"); // First one is the one we care about
                    KismetHelper.InsertActionAfter(delay, "Finished", startTimerSignal, 0, "Out");

                    VTestKismet.InstallVTestHelperSequenceNoInput(le1File, seq.InstancedFullPath, "HelperSequences.SurvivalHealthGateCurve", vTestOptions);

                    // We install survival ramping in another pass as our loop on sequence objects will not detect the changes
                    VTestSurvival.InstallSurvivalRamping(startTimerSignal, seq, vTestOptions);
                }
                else if (seqName is "CAH_Cave_Handler" or "CAH_Thai_Handler" or "CAH_Lava_Handler")
                {
                    VTestCapture.InstallCaptureRamping(seq, vTestOptions);
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
                    var sequenceObjects = seq.GetProperty<ArrayProperty<ObjectProperty>>("SequenceObjects").Select(x => x.ResolveToEntry(le1File) as ExportEntry).ToList();
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
                    var compareBool = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqCond_CompareBool", 8064, 3672);
                    KismetHelper.SkipSequenceElement(compareBool, "True"); // Skip out to true

                    // Add signal to stop decay and restore healthgate
                    if (VTestKismet.IsContainedWithinSequenceNamed(seq, "SUR_Lava_Handler", true) ||
                        VTestKismet.IsContainedWithinSequenceNamed(seq, "SUR_Thai_Handler", true) ||
                        //VTestKismet.IsContainedWithinSequenceNamed(seq, "SUR_Crate_Handler", true) ||
                        VTestKismet.IsContainedWithinSequenceNamed(seq, "SUR_Cave_Handler", true))
                    {
                        // It's survival, we should signal to restore the healthgate
                        var surRestoreHealthgateSignal = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_ActivateRemoteEvent", vTestOptions.cache);
                        KismetHelper.AddObjectToSequence(surRestoreHealthgateSignal, seq);
                        surRestoreHealthgateSignal.WriteProperty(new NameProperty("CROSSGEN_RESTORE_SUR_HEALTHGATE", "EventName"));
                        KismetHelper.CreateOutputLink(VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqEvent_SequenceActivated"), "Out", surRestoreHealthgateSignal);
                    }

                }
                #endregion
            }

            // PASS 2
            foreach (var seq in le1File.Exports.Where(x => x.ClassName == "Sequence").ToList())
            {
                // Increase survival mode engagement by forcing the player to engage with enemies that charge the player.
                // This prevents them from camping and getting free survival time
                // This also covers CAH as it uses SUR_Respawner sequence.
                // 11/14/2024 - Filter out VAM_Respawner
                if (VTestKismet.GetSequenceName(seq) == "Spawn_Single_Guy" && VTestKismet.IsContainedWithinSequenceNamed(seq, "SUR_Respawner")
                                                                           && !VTestKismet.IsContainedWithinSequenceNamed(seq, "VAM_Respawner") && !VTestKismet.IsContainedWithinSequenceNamed(seq, "TA_Respawner"))
                {
                    Console.WriteLine($">> Spawn_Single_Guy SUR {seq.UIndex} - {VTestKismet.GetSequenceFullPath(seq)}");

                    InstallRespawnerChanges(seq, vTestOptions);
                }
            }


            // 09/24/2024 - Add additional enemies logic
            // 11/15/2024 - Must go after pass 2 so respawn cloning sets this up propperly
            VTestAdditionalContent.AddExtraEnemyTypes(le1File, vTestOptions);

            FixSoftlockWhenRagdollOnGameEnd();
        }

        public static void InstallRespawnerChanges(ExportEntry seq, VTestOptions vTestOptions)
        {
            // seq.FileRef.Save();
            var seqName = VTestKismet.GetSequenceFullPath(seq);
            var berserkAi = EntryImporter.EnsureClassIsInFile(seq.FileRef, "CrossgenAI_MobPlayer", new RelinkerOptionsPackage() { Cache = vTestOptions.cache });

            // Sequence objects + add to sequence
            var crustAttach = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "BioSeqAct_AttachCrustEffect", 5920, 1672);
            var currentPawn = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqVar_Object", 4536, 2016);
            var death = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqEvent_Death", 1664, 2608);

            // AI change
            var delay = SequenceObjectCreator.CreateSequenceObject(seq.FileRef, "BioSeqAct_Delay", vTestOptions.cache);
            var delayDuration = SequenceObjectCreator.CreateSequenceObject(seq.FileRef, "SeqVar_RandomFloat", vTestOptions.cache);
            var aiChoiceRand = SequenceObjectCreator.CreateSequenceObject(seq.FileRef, "SeqVar_RandomFloat", vTestOptions.cache);
            var aiChoiceComp = SequenceObjectCreator.CreateSequenceObject(seq.FileRef, "SeqCond_CompareFloat", vTestOptions.cache);
            var aiChoiceAssaultThreshold = SequenceObjectCreator.CreateSequenceObject(seq.FileRef, "SeqVar_Float", vTestOptions.cache);
            var changeAiCharge = SequenceObjectCreator.CreateSequenceObject(seq.FileRef, "BioSeqAct_ChangeAI", vTestOptions.cache);
            var changeAiAssault = SequenceObjectCreator.CreateSequenceObject(seq.FileRef, "BioSeqAct_ChangeAI", vTestOptions.cache);
            var chargeAiLog = SequenceObjectCreator.CreateSequenceObject(seq.FileRef, "SeqAct_Log", vTestOptions.cache);
            var assaultAiLog = SequenceObjectCreator.CreateSequenceObject(seq.FileRef, "SeqAct_Log", vTestOptions.cache);

            //11/14/2024 - Berserk mode
            var berserkCheck = SequenceObjectCreator.CreateCompareBool(seq, SequenceObjectCreator.CreateScopeNamed(seq, "SeqVar_Bool", "BerserkMode", vTestOptions.cache), vTestOptions.cache);
            var changeAiBerserk = SequenceObjectCreator.CreateChangeAI(seq, berserkAi, currentPawn, true, vTestOptions.cache);
            var berserkGate = SequenceObjectCreator.CreateGate(seq, vTestOptions.cache);
            var berserkSignal = SequenceObjectCreator.CreateSeqEventRemoteActivated(seq, "GoBerserk", vTestOptions.cache);
            var berserkStaggerDelay = SequenceObjectCreator.CreateRandomDelay(seq, 1, 4, vTestOptions.cache); // stagger a bit so it's not all at once
            KismetHelper.CreateOutputLink(berserkSignal, "Out", berserkGate);
            KismetHelper.CreateOutputLink(berserkGate, "Out", berserkGate, 2); // Close the gate
            KismetHelper.CreateOutputLink(berserkGate, "Out", berserkStaggerDelay);
            KismetHelper.CreateOutputLink(berserkStaggerDelay, "Finished", changeAiBerserk); // Go berserk
            var goneBerserkLog = SequenceObjectCreator.CreateLog(seq, "Pawn has gone berserk", true, vTestOptions.cache);
            KismetHelper.CreateOutputLink(changeAiBerserk, "Out", goneBerserkLog);

            // Toxic and Phasic
            var setWeaponAttributes = SequenceObjectCreator.CreateSequenceObject(seq.FileRef, "LEXSeqAct_SetWeaponAttribute", vTestOptions.cache);
            var toxicFactor = SequenceObjectCreator.CreateSequenceObject(seq.FileRef, "SeqVar_Float", vTestOptions.cache);
            var phasicFactor = SequenceObjectCreator.CreateSequenceObject(seq.FileRef, "SeqVar_Float", vTestOptions.cache);
            //var addToxicFactor = SequenceObjectCreator.CreateSequenceObject(seq.FileRef, "SeqVar_Float", vTestOptions.cache);
            //var addPhasicFactor = SequenceObjectCreator.CreateSequenceObject(seq.FileRef, "SeqVar_Float", vTestOptions.cache);
            //var respawnCount = SequenceObjectCreator.CreateSequenceObject(seq.FileRef, "SeqVar_Float", vTestOptions.cache);

            KismetHelper.AddObjectsToSequence(seq, false, delay, delayDuration, aiChoiceRand, aiChoiceComp, aiChoiceAssaultThreshold, changeAiCharge, changeAiAssault, assaultAiLog, chargeAiLog, setWeaponAttributes, toxicFactor, phasicFactor);

            // Configure sequence object properties
            delayDuration.WriteProperty(new FloatProperty(11, "Min"));
            delayDuration.WriteProperty(new FloatProperty(20, "Max"));
            KismetHelper.SetComment(toxicFactor, "Toxic to counter player regen");
            KismetHelper.SetComment(phasicFactor, "Phasic to counter player powers");
            toxicFactor.WriteProperty(new FloatProperty(1, "FloatValue"));
            phasicFactor.WriteProperty(new FloatProperty(1, "FloatValue"));


            // CHARGE AI BRANCH
            var chargeAiClass = EntryImporter.EnsureClassIsInFile(seq.FileRef, "CrossgenAI_Charge", new RelinkerOptionsPackage() { PortExportsAsImportsWhenPossible = true, Cache = vTestOptions.cache });
            changeAiCharge.WriteProperty(new ObjectProperty(chargeAiClass, "ControllerClass"));
            KismetHelper.SetComment(chargeAiLog, "CROSSGEN: Engaging player with CrossgenAI_Charge");

            // ASSAULT AI BRANCH
            var assaultAiClass = EntryImporter.EnsureClassIsInFile(seq.FileRef, "CrossgenAI_Assault", new RelinkerOptionsPackage() { PortExportsAsImportsWhenPossible = true, Cache = vTestOptions.cache });
            changeAiAssault.WriteProperty(new ObjectProperty(assaultAiClass, "ControllerClass"));
            KismetHelper.SetComment(assaultAiLog, "CROSSGEN: Relaxing player engagement with CrossgenAI_Assault");

            // ASSAULT CHANCE - 1 in 4 chance
            aiChoiceRand.WriteProperty(new FloatProperty(0, "Min"));
            aiChoiceRand.WriteProperty(new FloatProperty(4, "Max"));
            aiChoiceAssaultThreshold.WriteProperty(new FloatProperty(3f, "FloatValue")); // The generated random number must be above this to change to assault. 

            // Connect sequence objects - Stop AI change timer when pawn dies 10/26/2024
            KismetHelper.CreateOutputLink(death, "Out", delay, 1); // Stop

            // Connect sequence objects - Delay and branch pick
            KismetHelper.CreateOutputLink(crustAttach, "Done", berserkCheck);
            // -- Berserk
            KismetHelper.CreateOutputLink(berserkCheck, "True", changeAiBerserk);
            // -- Not berserk
            KismetHelper.CreateOutputLink(berserkCheck, "False", delay);
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
            var events = KismetHelper.GetAllSequenceElements(seq).OfType<ExportEntry>().Where(x => x.IsA("SeqEvent")).ToList();
            foreach (var seqEvent in events)
            {
                KismetHelper.CreateOutputLink(seqEvent, "Out", delay, 1); // Cancel the delay as spawn stopped or changed (or restarted)
            }

            // Connect sequence object - toxic / phasic (Needs gated to only activate later!)
            KismetHelper.CreateOutputLink(crustAttach, "Done", setWeaponAttributes);
            KismetHelper.CreateVariableLink(setWeaponAttributes, "Pawn", currentPawn);
            KismetHelper.CreateVariableLink(setWeaponAttributes, "Toxic Factor", toxicFactor);
            KismetHelper.CreateVariableLink(setWeaponAttributes, "Phasic Factor", phasicFactor);

            // Uniquely name sequences to make it easier to tell what a sequence is actually doing...
            if (VTestKismet.IsContainedWithinSequenceNamed(seq, "CAH_Respawner"))
            {
                seq.WriteProperty(new StrProperty("Spawn_Single_Guy_CAH", "ObjName"));
            }
            else if (VTestKismet.IsContainedWithinSequenceNamed(seq, "SUR_Respawner"))
            {
                seq.WriteProperty(new StrProperty("Spawn_Single_Guy_SUR", "ObjName"));
            }
            else
            {
                // VAM
            }
            VTestAdditionalContent.InstallTalentRamping(crustAttach, "Done", vTestOptions);
            VTestAdditionalContent.InstallPowerRamping(crustAttach, "Done", vTestOptions);
        }

        private void PlayerQuitStreamingFix(ExportEntry hookup, string outlinkName)
        {
            // Streams in CCSIM04 in loaded state so it's ready to show sooner
            var seq = KismetHelper.GetParentSequence(hookup);
            var sss = SequenceObjectCreator.CreateSetStreamingState(seq, SequenceObjectCreator.CreateName(seq, "Load_Post_Scenario_Scoreboard", vTestOptions.cache), SequenceObjectCreator.CreateBool(seq, true, vTestOptions.cache));
            KismetHelper.InsertActionAfter(hookup, outlinkName, sss, 0, "Out");
        }

        public virtual void PrePortingCorrection()
        {
            // 11/11/2024 - Strip references to level-baked pawns; we want to load them dynamically instead so we have consistency in all files
            var mainSeq = me1File.FindExport("TheWorld.PersistentLevel.Main_Sequence");
            var mainSeqObjs = KismetHelper.GetSequenceObjects(mainSeq).OfType<ExportEntry>().ToList();
            var destroyObj = mainSeqObjs.FirstOrDefault(x => x.ClassName == "SeqAct_Destroy");
            if (destroyObj != null)
            {
                var incomingRef = KismetHelper.FindOutputConnectionsToNode(destroyObj, mainSeqObjs);
                var variables = KismetHelper.GetVariableLinksOfNode(destroyObj);

                foreach (var ir in incomingRef)
                {
                    KismetHelper.RemoveFromSequence(ir, true);
                }
                foreach (var vl in variables)
                {
                    foreach (var vn in vl.LinkedNodes.OfType<ExportEntry>())
                    {
                        KismetHelper.RemoveFromSequence(vn, true);
                    }
                }
                KismetHelper.RemoveFromSequence(destroyObj, true);
            }

            var levelActors = me1File.GetLevelActors();
            foreach (var actor in levelActors.Where(x => x.ClassName == "BioPawn"))
            {
                me1File.RemoveFromLevelActors(actor);
            }

            // Change strategic ring material name so it picks up donor instead
            var strategicRing = me1File.FindExport("BIOA_PRC2_MatFX.Material.StrategicRing_Cap_MAT");
            if (strategicRing != null)
            {
                var cgv = ExportCreator.CreatePackageExport(me1File, "CROSSGENV");
                strategicRing.idxLink = cgv.UIndex;
                strategicRing.ObjectName = "StrategicRing_Cap_MAT_NEW_matInst";
                strategicRing.Class = EntryImporter.EnsureClassIsInFile(me1File, "MaterialInstanceConstant", new RelinkerOptionsPackage()); // Change class so donor is properly picked up?
            }
        }
    }
}
