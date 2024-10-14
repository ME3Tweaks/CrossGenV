using System;
using System.Collections.Generic;
using System.Linq;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;

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
            var respawners = VTestKismet.GetSequenceObjectReferences(seq, "SUR_Respawner");


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

            // Todo: Make this dynamic for different levels that have different survival times
            float startTime = 35f;
            var currentTime = SequenceObjectCreator.CreateScopeNamed(seq, "SeqVar_Float", "OFFICIAL_TIME", vTestOptions.cache);
            var gameHandler = VTestKismet.GetSequenceObjectReferences(seq, "Check_Capping_Completion");

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
                KismetHelper.CreateOutputLink(gate, "Out", pmCheck, 0); // Gate to Initialize
                KismetHelper.CreateOutputLink(gate, "Out", gate, 2); // Close gate

                KismetHelper.CreateOutputLink(pmCheck, "True", respawner, 1); // PMCheck to Initialize

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
                    KismetHelper.CreateOutputLink(gameHandler[0], "Update_Guys", compare);
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
                startTime += 13; // How many seconds between enemy ramping
            }
        }
    }
}
