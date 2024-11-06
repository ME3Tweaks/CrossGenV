using System.Collections.Generic;
using System.Diagnostics;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using System.Linq;

namespace CrossGenV.Classes
{
    /// <summary>
    /// New content for VTest
    /// </summary>
    public class VTestAdditionalContent
    {
        // List of victorious IFPs (standard)
        private static string[] victoryIFPs =
        [
            "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_SetBool_6", // Have not accepted Vidinos Challenge
            "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_SetBool_3", // We have completed Vidinos Challenge
            "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_SetBool_5", // You have taken all of them Shepard (Ocaren)
        ];

        private static string ahernVictoryIFP = "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_SetBool_4"; // Have completed Ahern's challenge - special
        private static string extraFirstPlaceIFP = "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_SetBool_7"; // New high score on map already completed

        public static void AddMissionCompletionExperience(IMEPackage le1File, VTestOptions options)
        {
            ExportEntry originalNode = null;
            var sequence = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin");
            foreach (var vic in victoryIFPs)
            {
                // 33% per first-first place
                originalNode = le1File.FindExport(vic);
                GenerateMissionCompletedXPEvent(sequence, 0.33f, originalNode, options);
            }

            // Ahern mission 100% (FULL LEVEL)
            originalNode = le1File.FindExport(ahernVictoryIFP);
            GenerateMissionCompletedXPEvent(sequence, 1.0f, originalNode, options);

            // Extra 1st places 15%
            originalNode = le1File.FindExport(extraFirstPlaceIFP);
            GenerateMissionCompletedXPEvent(sequence, 0.15f, originalNode, options);
        }

        private static void GenerateMissionCompletedXPEvent(ExportEntry sequence, float mult, ExportEntry originalNode, VTestOptions options)
        {
            var settings = SequenceObjectCreator.CreatePMCheckState(sequence, VTestPlot.CROSSGEN_PMB_INDEX_FIRSTPLACE_EXPERIENCE_ENABLED, options.cache);
            KismetHelper.InsertActionAfter(originalNode, "Out", settings, 0, "False");

            var grant = SequenceObjectCreator.CreateSequenceObject(sequence, "LEXSeqAct_GrantLevelBasedXPPercent", options.cache);
            KismetHelper.SetComment(grant, "Standard win: 0.5x of a level");
            var multiplier = SequenceObjectCreator.CreateFloat(sequence, 0.5f, options.cache);
            KismetHelper.CreateVariableLink(grant, "LevelPercent", multiplier);

            KismetHelper.CreateOutputLink(settings, "True", grant);
            KismetHelper.CreateOutputLink(grant, "Out", KismetHelper.GetOutputLinksOfNode(settings)[1][0].LinkedOp as ExportEntry);
        }

        public static void AddExtraEnemyTypes(IMEPackage le1File, VTestOptions options)
        {
            var fname = le1File.FileNameNoExtension.ToUpper();
            switch (fname)
            {
                case "BIOA_PRC2_CCTHAI_DSG":
                    SetupEnemyTypes(le1File, "TheWorld.PersistentLevel.Main_Sequence.SUR_Thai_Handler.SequenceReference_1", options);
                    SetupEnemyTypes(le1File, "TheWorld.PersistentLevel.Main_Sequence.VAM_Thai_Handler.SequenceReference_1", options);
                    SetupEnemyTypes(le1File, "TheWorld.PersistentLevel.Main_Sequence.CAH_Thai_Handler.SequenceReference_1", options);
                    SetupEnemyTypesTA(le1File, "TheWorld.PersistentLevel.Main_Sequence.TA_Thai_Handler.SequenceReference_1", options);
                    break;
                case "BIOA_PRC2_CCLAVA_DSG":
                    SetupEnemyTypes(le1File, "TheWorld.PersistentLevel.Main_Sequence.SUR_Lava_Handler.SequenceReference_1", options);
                    SetupEnemyTypes(le1File, "TheWorld.PersistentLevel.Main_Sequence.VAM_Lava_Handler.SequenceReference_1", options);
                    SetupEnemyTypes(le1File, "TheWorld.PersistentLevel.Main_Sequence.CAH_Lava_Handler.SequenceReference_1", options);
                    SetupEnemyTypesTA(le1File, "TheWorld.PersistentLevel.Main_Sequence.TA_Lava_Handler.SequenceReference_1", options);
                    break;
                case "BIOA_PRC2_CCCRATE_DSG":
                    SetupEnemyTypesTA(le1File, "TheWorld.PersistentLevel.Main_Sequence.TA_Crate_Handler.SequenceReference_1", options);
                    break;
                case "BIOA_PRC2_CCCAVE_DSG":
                    SetupEnemyTypes(le1File, "TheWorld.PersistentLevel.Main_Sequence.SUR_Cave_Handler.SequenceReference_1", options);
                    SetupEnemyTypes(le1File, "TheWorld.PersistentLevel.Main_Sequence.VAM_Cave_Handler.SequenceReference_1", options);
                    SetupEnemyTypes(le1File, "TheWorld.PersistentLevel.Main_Sequence.CAH_Cave_Handler.SequenceReference_1", options);
                    break;
            }
        }

        private static void SetupEnemyTypes(IMEPackage le1File, string hookupIFP, VTestOptions options)
        {
            var pawnLoad = le1File.FindExport(hookupIFP);
            var sequence = KismetHelper.GetParentSequence(pawnLoad);

            // Get all object lists in the sequence that have object types of BioPawnChallengeScaledType objects in them.
            var objectListsLists = KismetHelper.GetSequenceObjects(sequence).OfType<ExportEntry>().Where(x =>
                    x.ClassName == "SeqVar_ObjectList"
                    && x.GetProperty<ArrayProperty<ObjectProperty>>("ObjList") is var objList
                    && objList.Count > 0)
                .ToList();


            List<ExportEntry> spawnLists = new List<ExportEntry>();
            foreach (var objList in objectListsLists)
            {
                var list = objList.GetProperty<ArrayProperty<ObjectProperty>>("ObjList");
                var hasChallengeScaledType = list.Any(x =>
                    x.Value > 0 && le1File.GetUExport(x.Value) is ExportEntry bpcst
                                && bpcst.ClassName == "BioPawnChallengeScaledType");
                if (!hasChallengeScaledType)
                    continue;

                spawnLists.Add(objList);
            }

            // All spawnlists contain the types data, 
            // But we want to make sure we don't include the original load types

            // ToArray to make dupe
            var seqElements = KismetHelper.GetAllSequenceElements(sequence).OfType<ExportEntry>().ToList();
            foreach (var spawnList in spawnLists.ToArray())
            {
                var preloadObjs = KismetHelper.FindVariableConnectionsToNode(spawnList, seqElements, ["Pawn type list"]);
                foreach (var preloadObj in preloadObjs)
                {
                    spawnLists.Remove(preloadObj);
                }
            }

            var spawnChanger = VTestKismet.InstallVTestHelperSequenceNoInput(le1File, sequence.InstancedFullPath,
                "HelperSequences.CrossgenUpdateSpawnlists", options);

            KismetHelper.InsertActionAfter(pawnLoad, "Cache pawns created", spawnChanger, 0, "Cached");
            KismetHelper.CreateOutputLink(pawnLoad, "Cache pawns deleted", spawnChanger, 1);

            foreach (var sl in spawnLists)
            {
                KismetHelper.CreateVariableLink(spawnChanger, "SpawnLists", sl);
            }
        }

        private static void SetupEnemyTypesTA(IMEPackage le1File, string hookupIFP, VTestOptions options)
        {
            // ObjectList -> Object
            // Different 
            var pawnLoad = le1File.FindExport(hookupIFP);
            if (pawnLoad == null)
            {
                le1File.Save();
                Debugger.Break();
            }
            var sequence = KismetHelper.GetParentSequence(pawnLoad);

            // Get all object lists in the sequence that have object types of BioPawnChallengeScaledType objects in them.
            var spawnBPCST = KismetHelper.GetSequenceObjects(sequence).OfType<ExportEntry>().Where(x =>
                    x.ClassName == "SeqVar_Object"
                    && x.GetProperty<ObjectProperty>("ObjValue") is var objValue
                    && objValue.Value > 0
                    && le1File.GetUExport(objValue.Value) is ExportEntry bpcst
                    && bpcst.ClassName == "BioPawnChallengeScaledType")
                .ToList();

            var spawnChanger = VTestKismet.InstallVTestHelperSequenceNoInput(le1File, sequence.InstancedFullPath,
                "HelperSequences.CrossgenUpdateSpawnsTA", options);

            KismetHelper.InsertActionAfter(pawnLoad, "Cache pawns created", spawnChanger, 0, "Cached");
            KismetHelper.CreateOutputLink(pawnLoad, "Cache pawns deleted", spawnChanger, 1);

            foreach (var sl in spawnBPCST)
            {
                KismetHelper.CreateVariableLink(spawnChanger, "Spawns", sl);
            }
        }

        public static void InstallTalentRamping(ExportEntry hookup, string outName, VTestOptions options)
        {
            var seq = KismetHelper.GetParentSequence(hookup);
            var currentPawn = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqVar_Object", 4536, 2016);
            if (currentPawn == null)
            {

            }
            var pmCheckState = SequenceObjectCreator.CreatePMCheckState(seq, VTestPlot.CROSSGEN_PMB_INDEX_RAMPING_WEAPONMODS_ENABLED, options.cache);
            var addTalents = SequenceObjectCreator.CreateSequenceObject(seq, "LEXSeqAct_AddWeaponMods", options.cache);
            KismetHelper.CreateOutputLink(pmCheckState, "True", addTalents);

            var chance = SequenceObjectCreator.CreateScopeNamed(seq, "SeqVar_Float", "CG_RAMP_WEAPONMOD_CHANCE", options.cache);
            var count = SequenceObjectCreator.CreateScopeNamed(seq, "SeqVar_Int", "CG_RAMP_WEAPONMODS_COUNT", options.cache);

            KismetHelper.CreateVariableLink(addTalents, "Pawn", currentPawn);
            KismetHelper.CreateVariableLink(addTalents, "ModCount", count);
            KismetHelper.CreateVariableLink(addTalents, "Chance", chance);

            KismetHelper.CreateOutputLink(hookup, outName, pmCheckState);
        }

        public static void InstallPowerRamping(ExportEntry hookup, string outName, VTestOptions options)
        {
            var seq = KismetHelper.GetParentSequence(hookup);
            var currentPawn = VTestKismet.FindSequenceObjectByClassAndPosition(seq, "SeqVar_Object", 4536, 2016);
            if (currentPawn == null)
            {

            }
            var pmCheckState = SequenceObjectCreator.CreatePMCheckState(seq, VTestPlot.CROSSGEN_PMB_INDEX_RAMPING_TALENTS_ENABLED, options.cache);
            var addTalents = SequenceObjectCreator.CreateSequenceObject(seq, "LEXSeqAct_AddTalents", options.cache);
            KismetHelper.CreateOutputLink(pmCheckState, "True", addTalents);

            var chance = SequenceObjectCreator.CreateScopeNamed(seq, "SeqVar_Float", "CG_RAMP_TALENT_CHANCE", options.cache);
            var count = SequenceObjectCreator.CreateScopeNamed(seq, "SeqVar_Int", "CG_RAMP_TALENTS_COUNT", options.cache);

            KismetHelper.CreateVariableLink(addTalents, "Pawn", currentPawn);
            KismetHelper.CreateVariableLink(addTalents, "TalentCount", count);
            KismetHelper.CreateVariableLink(addTalents, "Chance", chance);

            KismetHelper.CreateOutputLink(hookup, outName, pmCheckState);
        }
    }
}
