using System;
using System.Collections.Generic;
using System.Linq;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;

namespace CrossGenV.Classes.Modes
{
    internal class VTestSurvival
    {
        // We can spawn up to this many additional enemies.
        private static readonly int MaxEnemyRampCount = 10;

        /// <summary>
        /// SURVIVAL ONLY - Clones respawners, activating them over time if the option is on. Also increases talent and weapon mod chances.
        /// </summary>
        /// <param name="seq"></param>
        /// <param name="vTestOptions"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void InstallSurvivalRamping(ExportEntry startTimerObj, ExportEntry seq, VTestOptions vTestOptions)
        {

            var updateEnemies = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqAct_Gate", -3474, 1102); // SUR LAVA
            updateEnemies ??= VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqAct_Gate", 89, 1075); // SUR THAI
            updateEnemies ??= VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqAct_Log", 1214, 865); // SUR CAVE


            var respawners = VTestKismet.GetSequenceObjectReferences(seq, "SUR_Respawner");
            List<ExportEntry> newRespawners = new List<ExportEntry>();
            if (respawners.Any())
            {
                var respawnerSeqRef = respawners[0];

                int currentEnemyNum = respawners.Count + 1;

                var currentRamp = 0;
                while (currentRamp < MaxEnemyRampCount)
                {
                    var links = KismetHelper.GetVariableLinksOfNode(respawnerSeqRef);
                    var newRespawner = KismetHelper.CloneObject(respawnerSeqRef, keepPositioning: true); //, seq, cloneChildren: true);
                    var objName = VTestKismet.GetSequenceName(newRespawner);
                    if (objName == respawnerSeqRef.ObjectName.Instanced)
                    {
                        newRespawner.WriteProperty(new StrProperty(newRespawner.ObjectName.Instanced, "ObjName"));
                    }

                    Console.WriteLine($">> Cloned - {VTestKismet.GetSequenceFullPath(respawnerSeqRef)} to {VTestKismet.GetSequenceFullPath(newRespawner)}");
                    var spawnList = KismetHelper.CloneObject(links[0].LinkedNodes[0] as ExportEntry); // Clone the spawnlist so crossgen wavelist distribution is more varied
                    newRespawners.Add(newRespawner);
                    var enemyNum = SequenceObjectCreator.CreateInt(seq, currentEnemyNum, vTestOptions.cache);
                    links[0].LinkedNodes[0] = spawnList; // Set new spawn list
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

            // Todo: Make this dynamic for different levels that have different survival times
            float startTime = 45f;
            var currentTime = SequenceObjectCreator.CreateScopeNamed(seq, "SeqVar_Float", "OFFICIAL_TIME", vTestOptions.cache);

            // Subdivide into ramp chunks
            // We +1 to ensure it's never zero, as well as not having a guaranteed 100% all the time.
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


            ExportEntry previousCompareFloat = null;

            int third = (int)Math.Ceiling(MaxEnemyRampCount / 3.0f);
            int current = 0;
            foreach (var respawner in newRespawners)
            {
                current++;
                var gate = SequenceObjectCreator.CreateGate(seq, vTestOptions.cache);
                var pmCheck = SequenceObjectCreator.CreatePMCheckState(seq, VTestPlot.CROSSGEN_PMB_INDEX_RAMPING_SPAWNCOUNT_ENABLED, vTestOptions.cache); // We put this behind gate as we use this for ramping difficulty
                var unlockLog = SequenceObjectCreator.CreateLog(seq, "Activating a new ramping respawner", cache: vTestOptions.cache);
                KismetHelper.CreateOutputLink(gate, "Out", pmCheck, 0); // Gate to Initialize
                KismetHelper.CreateOutputLink(gate, "Out", gate, 2); // Close gate


                KismetHelper.CreateOutputLink(pmCheck, "True", unlockLog); // PMCheck to Log
                KismetHelper.CreateOutputLink(unlockLog, "Out", respawner, 1); // Log to Initialize
                KismetHelper.CreateOutputLink(updateEnemies, "Out" , respawner, 0); // Update guys goes to Activate it seems...

                var time = SequenceObjectCreator.CreateFloat(seq, startTime);
                var compare = SequenceObjectCreator.CreateCompareFloat(seq, currentTime, time, vTestOptions.cache);

                KismetHelper.CreateOutputLink(compare, "A >= B", gate);
                if (previousCompareFloat != null)
                {
                    // Link to next compare
                    KismetHelper.CreateOutputLink(previousCompareFloat, "A >= B", compare);
                }
                else
                {
                    var tickEvent = SequenceObjectCreator.CreateSeqEventRemoteActivated(seq, "TimerTick", vTestOptions.cache);
                    var logTime = SequenceObjectCreator.CreateLog(seq, "Seconds ticked: ", cache: vTestOptions.cache);
                    KismetHelper.CreateVariableLink(logTime, "Float", currentTime);
                    KismetHelper.CreateOutputLink(tickEvent, "Out", logTime);
                    KismetHelper.CreateOutputLink(logTime, "Out", compare);
                }

                // Weapon, Talent chances increase every ramp
                KismetHelper.CreateOutputLink(gate, "Out", addFloatMC);
                KismetHelper.CreateOutputLink(gate, "Out", addFloatTC);

                // Increment counts on 1/3 and 2/3 of full ramp
                if (current % third == 0 && current != MaxEnemyRampCount)
                {
                    KismetHelper.CreateOutputLink(gate, "Out", addIntMC);
                }
                // Offset ramp by 1 for talents to start
                if ((current + 1) % third == 0 && current != MaxEnemyRampCount)
                {
                    KismetHelper.CreateOutputLink(gate, "Out", addIntTC);
                }

                previousCompareFloat = compare;
                startTime += 30; // How many seconds between enemy ramping
            }

            {
                // 11/14/2024 - Mob Player
                startTime += 7; // 7 more seconds after we start to really crank up the difficulty (20 since last extra spawn)

                var time = SequenceObjectCreator.CreateFloat(seq, startTime);
                var compare = SequenceObjectCreator.CreateCompareFloat(seq, currentTime, time, vTestOptions.cache);
                var gate = SequenceObjectCreator.CreateGate(seq, vTestOptions.cache);
                var pmCheck = SequenceObjectCreator.CreatePMCheckState(seq, VTestPlot.CROSSGEN_PMB_INDEX_RAMPING_SPAWNCOUNT_ENABLED, vTestOptions.cache); // We put this behind gate as we use this for ramping difficulty
                var berserk = SequenceObjectCreator.CreateScopeNamed(seq, "SeqVar_Bool", "BerserkMode", vTestOptions.cache);
                var setBerserk = SequenceObjectCreator.CreateSetBool(seq, berserk, SequenceObjectCreator.CreateBool(seq, true, vTestOptions.cache));
                var goBerserk = SequenceObjectCreator.CreateActivateRemoteEvent(seq, "GoBerserk", vTestOptions.cache);
                KismetHelper.SetComment(setBerserk, "Pawns will force-target the player until they get in close-range, then AI switches back to the original");
                var goneBerserkLog = SequenceObjectCreator.CreateLog(seq, "Berserk mode ACTIVATED", cache: vTestOptions.cache);

                KismetHelper.CreateOutputLink(previousCompareFloat, "A >= B", compare, 0); // Previous compare to ours
                KismetHelper.CreateOutputLink(compare, "A >= B", gate); // Current time to gate
                KismetHelper.CreateOutputLink(gate, "Out", gate, 2); // Close gate
                KismetHelper.CreateOutputLink(gate, "Out", pmCheck); // Gate to feature check
                KismetHelper.CreateOutputLink(pmCheck, "True", setBerserk); // PMCheck to setbool
                KismetHelper.CreateOutputLink(setBerserk, "Out", goBerserk); // setBerserk to signal
                KismetHelper.CreateOutputLink(goBerserk, "Out", goneBerserkLog);
            }
        }
    }
}
