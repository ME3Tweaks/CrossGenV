using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;

namespace CrossGenV.Classes.Modes
{
    /// <summary>
    /// Changes for capture mode
    /// </summary>
    internal class VTestCapture
    {
        public static void AddCaptureEngagementSequencing(IMEPackage le1Package, VTestOptions options)
        {
            var chargeClass = EntryImporter.EnsureClassIsInFile(le1Package, "CrossgenAI_Charge", new RelinkerOptionsPackage() { PortExportsAsImportsWhenPossible = true, Cache = options.cache });
            foreach (var seq in le1Package.Exports.Where(x => x.ClassName == "Sequence").ToList())
            {
                var seqName = VTestKismet.GetSequenceName(seq);
                if (seqName == "Spawn_Single_Guy" && VTestKismet.IsContainedWithinSequenceNamed(seq, "CAH_Respawner"))
                {
                    InstallAIChanger(seq, chargeClass, options);
                    continue;
                }

                if (seqName == "Cap_And_Hold_Point")
                {
                    InstallCapSignals(seq, options);
                    continue;
                }
            }

            var packageName = le1Package.FileNameNoExtension;
            switch (packageName)
            {
                case "BIOA_PRR2_CCTHAI_DSG":
                {
                    // Disconnect the logic for disabling enemies on capture start as we have changed it
                        var deactivateDefenders = le1Package.FindExport("TheWorld.PersistentLevel.Main_Sequence.CAH_Thai_Handler.SeqAct_Gate_6");
                        le1Package.FindExport("TheWorld.PersistentLevel.Main_Sequence.CAH_Thai_Handler.SeqAct_Gate_6");
                    KismetHelper.RemoveFromSequence(deactivateDefenders, true);
                    break;
                }
            }
        }

        private static void InstallCapSignals(ExportEntry seq, VTestOptions options)
        {
            var disengageHook = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "BioSeqAct_ChangeStrategy", -1688, 3696);
            var engageHook = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "BioSeqAct_ChangeStrategy", -568, 3232);
            var externVolume = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqVar_Object", 272, 4248); // SeqVar_External
            var signalEngage = SequenceObjectCreator.CreateActivateRemoteEvent(seq, "ForceCaptureEngage", options.cache);
            KismetHelper.CreateVariableLink(signalEngage, "Instigator", externVolume); // Pass volume through which is used to determine if we should kill the pawn
            var signalDisenage = SequenceObjectCreator.CreateActivateRemoteEvent(seq, "CaptureDisengage", options.cache);

            KismetHelper.InsertActionAfter(engageHook, "Out", signalEngage, 0, "Out");
            KismetHelper.InsertActionAfter(disengageHook, "Out", signalDisenage, 0, "Out");
        }

        private static void InstallAIChanger(ExportEntry seq, IEntry chargeClass, VTestOptions options)
        {
            var dist = SequenceObjectCreator.CreateFloat(seq, 0, options.cache);
            var player = SequenceObjectCreator.CreatePlayerObject(seq, true, options.cache);

            var deathListener = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqAct_AttachToEvent", 5680, 1672);
            var currentEnemy = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqVar_Object", 4536, 2016);
            var disablePawnHook = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "Sequence", 5424, 1672);
            var changeAiOnSignal = SequenceObjectCreator.CreateChangeAI(seq, chargeClass, currentEnemy, options.cache);
            var changeAiOnSpawn = SequenceObjectCreator.CreateChangeAI(seq, chargeClass, currentEnemy, options.cache);


            var forceEngage = SequenceObjectCreator.CreateSeqEventRemoteActivated(seq, "ForceCaptureEngage", options.cache);
            var captureDisengage = SequenceObjectCreator.CreateSeqEventRemoteActivated(seq, "CaptureDisengage", options.cache);
            var randEngageSw = SequenceObjectCreator.CreateRandSwitch(seq, 2, options.cache);
            KismetHelper.SetComment(randEngageSw, "Force target to player");

            var unlockTarget = SequenceObjectCreator.CreateSequenceObject(seq, "BioSeqAct_UnLockTarget", options.cache);
            var setTarget = SequenceObjectCreator.CreateSequenceObject(seq, "BioSeqAct_LockTarget", options.cache);

            KismetHelper.CreateVariableLink(unlockTarget, "Pawn", currentEnemy);

            //var setTarget = SequenceObjectCreator.CreateSequenceObject(seq, "LEXSeqAct_ForceCombatTarget", options.cache);
            KismetHelper.CreateVariableLink(setTarget, "Pawn", currentEnemy);
            KismetHelper.CreateVariableLink(setTarget, "Target", player);
            // KismetHelper.CreateVariableLink(setTarget, "LockTarget", SequenceObjectCreator.CreateBool(seq, true, options.cache));

            var currentState = SequenceObjectCreator.CreateBool(seq, false, options.cache);
            var trueState = SequenceObjectCreator.CreateBool(seq, true, options.cache);
            var falseState = SequenceObjectCreator.CreateBool(seq, false, options.cache);

            var setBoolStateTrue = SequenceObjectCreator.CreateSetBool(seq, currentState, trueState, options.cache);
            var setBoolStateFalse = SequenceObjectCreator.CreateSetBool(seq, currentState, falseState, options.cache);

            var compareBool = SequenceObjectCreator.CreateCompareBool(seq, currentState, options.cache);

            var getDist = SequenceObjectCreator.CreateSequenceObject(seq, "LEXSeqAct_GetDistance2D", options.cache);
            KismetHelper.CreateVariableLink(getDist, "A", player);
            KismetHelper.CreateVariableLink(getDist, "B", currentEnemy);
            KismetHelper.CreateVariableLink(getDist, "Distance", dist);
            var respawnDist = SequenceObjectCreator.CreateFloat(seq, 27000, options.cache); // It's using Size() instead of Distance(). We may want to consider just making a custom class and returning .Distance().
            var compare = SequenceObjectCreator.CreateCompareFloat(seq, dist, respawnDist, options.cache);
            var doDamage = SequenceObjectCreator.CreateCauseDamage(seq, currentEnemy, null, 9999, options.cache); // Do a ton of damage. The instigator is not the player because that triggers killstreaks
            var ksvoDelay = SequenceObjectCreator.CreateDelay(seq, 1, options.cache);
            KismetHelper.SetComment(ksvoDelay, "Slight delay to allow enemies to be killed");
            var reDisableKSVO = SequenceObjectCreator.CreateActivateRemoteEvent(seq, "DisableKSVO", options.cache);
            var reEnableKSVO = SequenceObjectCreator.CreateActivateRemoteEvent(seq, "EnableKSVO", options.cache);


            // AI Targets Player
            KismetHelper.CreateOutputLink(changeAiOnSignal, "Out", randEngageSw);
            KismetHelper.CreateOutputLink(changeAiOnSpawn, "Out", randEngageSw);
            KismetHelper.CreateOutputLink(randEngageSw, "Link 1", unlockTarget);
            KismetHelper.CreateOutputLink(randEngageSw, "Link 2", unlockTarget); // TEST: 100%

            KismetHelper.CreateOutputLink(unlockTarget, "Success", setTarget);


            // Engage
            KismetHelper.CreateOutputLink(forceEngage, "Out", setBoolStateTrue);
            KismetHelper.CreateOutputLink(setBoolStateTrue, "Out", getDist);
            KismetHelper.CreateOutputLink(getDist, "Out", compare);

            KismetHelper.CreateOutputLink(compare, "A >= B", reDisableKSVO);
            KismetHelper.CreateOutputLink(reDisableKSVO, "Out", doDamage);
            KismetHelper.CreateOutputLink(doDamage, "Out", ksvoDelay);
            KismetHelper.CreateOutputLink(ksvoDelay, "Finished", reEnableKSVO);
            KismetHelper.CreateOutputLink(compare, "A < B", changeAiOnSignal);

            // Disengage
            KismetHelper.CreateOutputLink(captureDisengage, "Out", setBoolStateFalse);

            // Original logic flows out on False
            KismetHelper.InsertActionAfter(disablePawnHook, "Out", compareBool, 0, "False");

            // Change AI on spawn
            KismetHelper.CreateOutputLink(compareBool, "True", changeAiOnSpawn);
            KismetHelper.CreateOutputLink(changeAiOnSpawn, "Out", deathListener);

#if DEBUG
            //KismetHelper.CreateOutputLink(,"",SequenceObjectCreator.CreateLog(seq, "Forcing capture engagement", options.cache));
            //SequenceObjectCreator.CreateLog(seq, "Disengaging capture", options.cache);
#endif
        }

        // Capture supports up to 3 waves as there are max 4 spawn points and only the 4th one will matter
        private static readonly int MaxEnemyRampCount = 6; // 1 + 2 + 3

        /// <summary>
        /// CAPTURE ONLY - Clones respawners, activating one at a time on capture completion if the option is turned on.
        /// </summary>
        /// <param name="seq"></param>
        /// <param name="vTestOptions"></param>
        public static void InstallCaptureRamping(ExportEntry seq, VTestOptions vTestOptions)
        {
            var levelName = seq.FileRef.FileNameNoExtension;

            var newRespawners = InstallNewRespawners(seq, vTestOptions);
            InstallRespawnerActivations(seq, newRespawners, vTestOptions);
            InstallTalentAndModRamping(seq, vTestOptions);
        }

        private static void InstallTalentAndModRamping(ExportEntry seq, VTestOptions vTestOptions)
        {
            var capAndHolds = VTestKismet.GetSequenceObjectReferences(seq, "Cap_And_Hold_Point");
            foreach (var capAndHold in capAndHolds)
            {
                var outLinks = KismetHelper.GetOutputLinksOfNode(capAndHold);
                outLinks[2].Clear(); // Remove 'Deactivate_Defenders' outputs
                KismetHelper.WriteOutputLinksToNode(capAndHold, outLinks);
            }

            // Subdivide into ramp chunks
            var chanceInc = SequenceObjectCreator.CreateFloat(seq, 1.0f / (MaxEnemyRampCount + 1), vTestOptions.cache);
            KismetHelper.SetComment(chanceInc, "Chance increment");

            var countInc = SequenceObjectCreator.CreateInt(seq, 1, vTestOptions.cache);


            var modChance = SequenceObjectCreator.CreateScopeNamed(seq, "SeqVar_Float", "CG_RAMP_WEAPONMOD_CHANCE", vTestOptions.cache);
            var talentChance = SequenceObjectCreator.CreateScopeNamed(seq, "SeqVar_Float", "CG_RAMP_TALENT_CHANCE", vTestOptions.cache);

            // MAX IS TWO
            var modCount = SequenceObjectCreator.CreateScopeNamed(seq, "SeqVar_Int", "CG_RAMP_WEAPONMODS_COUNT", vTestOptions.cache);
            var talentCount = SequenceObjectCreator.CreateScopeNamed(seq, "SeqVar_Int", "CG_RAMP_TALENTS_COUNT", vTestOptions.cache);

            var addFloatMC = SequenceObjectCreator.CreateAddFloat(seq, modChance, chanceInc, modChance);
            KismetHelper.SetComment(addFloatMC, "Increase chance of getting a weapon mod");
            var addFloatTC = SequenceObjectCreator.CreateAddFloat(seq, talentChance, chanceInc, talentChance);
            KismetHelper.SetComment(addFloatTC, "Increase chance of getting a talent");
            var addIntMC = SequenceObjectCreator.CreateAddInt(seq, modCount, countInc, modCount);
            KismetHelper.SetComment(addIntMC, "Increase max amount of additional weapon mods allowed");
            var addIntTC = SequenceObjectCreator.CreateAddInt(seq, talentCount, countInc, talentCount);
            KismetHelper.SetComment(addIntTC, "Increase max amount of additional talents allowed");

            // Log for debugging
            var logWMChance = SequenceObjectCreator.CreateLog(seq, "** Weapon Mod Chance **");
            var logWMCount = SequenceObjectCreator.CreateLog(seq, "** Weapon Mod Count **");
            var logTChance = SequenceObjectCreator.CreateLog(seq, "** Talent Chance **");
            var logTCount = SequenceObjectCreator.CreateLog(seq, "** Talent Count **");

            VTestKismet.HookupLog(logWMChance, "Weapon Mod Chance: ", floatVal: modChance);
            VTestKismet.HookupLog(logTChance, "Talent Chance: ", floatVal: talentChance);
            VTestKismet.HookupLog(logWMCount, "Weapon Mod Chance: ", intVal: modCount);
            VTestKismet.HookupLog(logTCount, "Talent Count: ", intVal: talentCount);

            KismetHelper.CreateOutputLink(addFloatMC, "Out", logWMChance);
            KismetHelper.CreateOutputLink(addFloatTC, "Out", logTChance);
            KismetHelper.CreateOutputLink(addIntMC, "Out", logWMCount);
            KismetHelper.CreateOutputLink(addIntTC, "Out", logTCount);



            // INCREMENTING GATES
            var cappingCompletion = VTestKismet.GetSequenceObjectReferences(seq, "Check_Capping_Completion").FirstOrDefault();

            var pmCheckTalents = SequenceObjectCreator.CreatePMCheckState(seq, VTestPlot.CROSSGEN_PMB_INDEX_RAMPING_TALENTS_ENABLED, vTestOptions.cache); // We put this behind gate as we use this for ramping difficulty
            var pmCheckMods = SequenceObjectCreator.CreatePMCheckState(seq, VTestPlot.CROSSGEN_PMB_INDEX_RAMPING_WEAPONMODS_ENABLED, vTestOptions.cache); // We put this behind gate as we use this for ramping difficulty

            KismetHelper.SetComment(pmCheckTalents, "Crossgen: Talent ramping enabled?");
            KismetHelper.SetComment(pmCheckMods, "Crossgen: Weapon mod ramping enabled?");

            KismetHelper.CreateOutputLink(cappingCompletion, "Keep_Trying_Champo", pmCheckTalents);
            KismetHelper.CreateOutputLink(cappingCompletion, "Keep_Trying_Champo", pmCheckMods);

            KismetHelper.CreateOutputLink(pmCheckTalents, "True", addFloatTC);
            KismetHelper.CreateOutputLink(pmCheckTalents, "True", addIntTC);

            KismetHelper.CreateOutputLink(pmCheckMods, "True", addFloatMC);
            KismetHelper.CreateOutputLink(pmCheckMods, "True", addIntMC);

            //    KismetHelper.CreateOutputLink(pmCheck, "True", respawner, 1); // PMCheck to Initialize


            //    // Weapon, Talent chances increase every ramp
            //    KismetHelper.CreateOutputLink(pmCheck, "True", addFloatMC);
            //    KismetHelper.CreateOutputLink(pmCheck, "True", addFloatTC);

            //    // Increment counts on 1/3 and 2/3 of full ramp
            //    KismetHelper.CreateOutputLink(pmCheck, "True", addIntMC);

            //    KismetHelper.CreateOutputLink(respawner, "DoneInitializing", addIntTC);
            //    KismetHelper.CreateOutputLink(respawner, "DoneInitializing", addFloatMC);
            //    KismetHelper.CreateOutputLink(respawner, "DoneInitializing", addFloatTC);

            //ExportEntry previousCompareFloat = null;

            //int current = 0;
            //foreach (var respawner in newRespawners)
            //{
            //    current++;
            //    
            //}
        }

        private static List<ExportEntry> InstallNewRespawners(ExportEntry seq, VTestOptions vTestOptions)
        {
            var respawners = VTestKismet.GetSequenceObjectReferences(seq, "CAH_Respawner");
            List<ExportEntry> newRespawners = new List<ExportEntry>();
            if (respawners.Any())
            {
                int currentEnemyNum = respawners.Count + 1;

                var currentRamp = 0;
                while (currentRamp < MaxEnemyRampCount)
                {
                    var links = KismetHelper.GetVariableLinksOfNode(respawners[0]);
                    var newRespawner = KismetHelper.CloneObject(respawners[0], seq, cloneChildren: true);
                    newRespawners.Add(newRespawner);
                    var enemyNum = SequenceObjectCreator.CreateInt(seq, currentEnemyNum, vTestOptions.cache);
                    links[2].LinkedNodes[0] = enemyNum; // Repoint to our new enemy number

                    KismetHelper.WriteVariableLinksToNode(newRespawner, links);


                    var outLinks = KismetHelper.GetOutputLinksOfNode(respawners[0]);
                    outLinks[1].Clear(); // Remove 'DoneInitializing'
                    KismetHelper.WriteOutputLinksToNode(newRespawner, outLinks);



                    // DoneInitializing -> Activate on itself to start the spawn
                    KismetHelper.CreateOutputLink(newRespawner, "DoneInitializing", newRespawner, 0);

                    currentEnemyNum++;
                    currentRamp++;
                }
            }

            return newRespawners;
        }

        private static void InstallCaptureRespawnDivisor(ExportEntry cappingCompletion, VTestOptions vTestOptions)
        {
            var seq = KismetHelper.GetParentSequence(cappingCompletion);

            List<ExportEntry> respawnTimes = new List<ExportEntry>();
            respawnTimes.Add(SequenceObjectCreator.CreateScopeNamed(seq, "SeqVar_Float", "Path_Guy_Respawn_Time", vTestOptions.cache));
            respawnTimes.Add(SequenceObjectCreator.CreateScopeNamed(seq, "SeqVar_Float", "Defender_Respawn_Time", vTestOptions.cache));

            foreach (var rt in respawnTimes)
            {
                var divide = SequenceObjectCreator.CreateDivideFloat(seq, rt, SequenceObjectCreator.CreateFloat(seq, 2f, vTestOptions.cache), rt, cache: vTestOptions.cache);
                KismetHelper.CreateOutputLink(cappingCompletion, "Keep_Trying_Champo", divide);
                KismetHelper.SetComment(divide, "Divide respawn time in half to make enemies populate faster");
            }
        }

        private static void InstallRespawnerActivations(ExportEntry seq, List<ExportEntry> newRespawners, VTestOptions vTestOptions)
        {
            var cappingCompletion = VTestKismet.GetSequenceObjectReferences(seq, "Check_Capping_Completion").FirstOrDefault();

            InstallCaptureRespawnDivisor(cappingCompletion, vTestOptions);

            var pmCheck = SequenceObjectCreator.CreatePMCheckState(seq, VTestPlot.CROSSGEN_PMB_INDEX_RAMPING_SPAWNCOUNT_ENABLED, vTestOptions.cache); // Gate behind ramping setting
            KismetHelper.SetComment(pmCheck, "Crossgen: Spawn ramping enabled?");

            KismetHelper.CreateOutputLink(cappingCompletion, "Keep_Trying_Champo", pmCheck);

            ExportEntry lastGate = pmCheck;
            string lastOutlink = "True";

            var amountInWave = 1;
            var linkThroughIndex = 0;
            for (int i = 0; i < newRespawners.Count; i += amountInWave - 1) // -1 to account for the +1 we do at the end
            {
                Debug.WriteLine($"Wave: {amountInWave}");
                var gateBool = SequenceObjectCreator.CreateBool(seq, false, vTestOptions.cache);
                var checkBool = SequenceObjectCreator.CreateCompareBool(seq, gateBool, vTestOptions.cache);
                KismetHelper.SetComment(checkBool, $"Has been activated (Ramping wave {amountInWave})?");
                var setBool = SequenceObjectCreator.CreateSetBool(seq, gateBool, SequenceObjectCreator.CreateBool(seq, true, vTestOptions.cache), vTestOptions.cache);
                KismetHelper.SetComment(setBool, $"Set has been activated true (Ramping wave {amountInWave})");

                KismetHelper.CreateOutputLink(lastGate, lastOutlink, checkBool); // Link last into ours
                KismetHelper.CreateOutputLink(checkBool, "False", setBool); // Link logic up for this activation

                for (var numDone = 0; numDone < amountInWave; numDone++)
                {
                    var respawner = newRespawners[numDone + i];
                    KismetHelper.CreateOutputLink(setBool, "Out", respawner, 1); // Link logic up for this activation
                    KismetHelper.CreateOutputLink(cappingCompletion, "Completed", respawner, 2); // Deactivate on completion
                }

                lastGate = checkBool;
                lastOutlink = "True";
                amountInWave++;
            }
        }
    }
}