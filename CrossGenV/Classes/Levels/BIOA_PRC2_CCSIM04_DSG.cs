﻿using System.Collections.Generic;
using System.Linq;
using LegendaryExplorerCore.Gammtek.Extensions;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.ObjectInfo;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCSIM04_DSG : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PostPortingCorrection()
        {
            // 09/26/2024 - Install mod settings menu (via DropTheSquid)
            VTestKismet.InstallVTestHelperSequenceNoInput(le1File, "TheWorld.PersistentLevel.Main_Sequence", "HelperSequences.SimulatorSettingsLogic", vTestOptions);
            var artPlacable = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.SimulatorSettingsLogic.BioSeqEvt_ArtPlaceableUsed_0");
            artPlacable.WriteProperty(new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioInert_3"), "Originator"));

            InstallLoadScreenFix();
            StreamInHenchTextures();

            InstallLockers();
        }

        /// <summary>
        /// Installs armor lockers so the player can customize their squadmates
        /// </summary>
        private void InstallLockers()
        {
            var helperBase = vTestOptions.vTestHelperPackage.FindExport("CCSIM04_DSG");

            // Port the POIs and spawnpoint for hench
            var actorsToPort = helperBase.FileRef.Exports.Where(x => x.Parent == helperBase && x.IsA("Actor"));
            var portedActors = new List<ExportEntry>();
            foreach (var actor in actorsToPort)
            {
                EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, actor, le1File,
                    le1File.GetLevel(), true,
                    new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out var ported);

                portedActors.Add(ported as ExportEntry);
            }

            le1File.AddToLevelActorsIfNotThere(portedActors.ToArray());

            // Install the helper sequence
            VTestKismet.InstallVTestHelperSequenceNoInput(le1File, "TheWorld.PersistentLevel.Main_Sequence","CCSIM04_DSG.HenchmenLockers", vTestOptions);
        }

        private void InstallLoadScreenFix()
        {
            // Task 103 - using -RESUME causes first load screen to always be GLO
            // We don't want ASI or global file changes, just plug and play
            // Play a one frame load screen to trigger reset
            var hookup = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Load_And_Start_Scenario.BioSeqAct_ScalarMathUnit_1");
            var hookupDest = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Load_And_Start_Scenario.SeqAct_Switch_1");
            var seq = KismetHelper.GetParentSequence(hookup);
            var init = SequenceObjectCreator.CreateInitLoadingMovie(seq, "SingleFrameBlack.bik", vTestOptions.cache);
            var play = SequenceObjectCreator.CreatePlayLoadingMovie(seq, vTestOptions.cache);
            var stop = SequenceObjectCreator.CreateStopLoadingMovie(seq, vTestOptions.cache);
            var delay = SequenceObjectCreator.CreateDelay(seq, 0.016f, vTestOptions.cache);

            KismetHelper.CreateOutputLink(init, "Done", play);
            KismetHelper.CreateOutputLink(play, "Out", delay);
            KismetHelper.CreateOutputLink(delay, "Finished", stop);

            var hasShownLoadOnce = SequenceObjectCreator.CreateScopeNamed(seq, "SeqVar_Bool", "HasShownLoadScreenOnce", vTestOptions.cache);
            var trueBool = SequenceObjectCreator.CreateBool(seq, true, vTestOptions.cache);
            var testBool = SequenceObjectCreator.CreateCompareBool(seq, hasShownLoadOnce, vTestOptions.cache);
            var setBool = SequenceObjectCreator.CreateSetBool(seq, hasShownLoadOnce, trueBool, vTestOptions.cache);

            // Has loaded once?
            KismetHelper.SetComment(testBool, "Crossgen: See if we've had a load screen before\nIf we haven't, using -RESUME to start game will show wrong load screen");
            KismetHelper.ChangeOutputLink(hookup, 0, 0, testBool.UIndex);
            KismetHelper.CreateOutputLink(testBool, "False", init);
            KismetHelper.CreateOutputLink(testBool, "True", hookupDest);
            KismetHelper.CreateOutputLink(stop, "Done", setBool);
            KismetHelper.CreateOutputLink(setBool, "Out", hookupDest);

        }

        private void StreamInHenchTextures()
        {
            // streams in hench textures so they are less likely to LOD when the load screen comes down
            var hookup = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Load_And_Start_Scenario.SequenceReference_0.Sequence_1537.SequenceReference_11");
            var dest = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Load_And_Start_Scenario.SequenceReference_0.Sequence_1537.SeqAct_FinishSequence_1");
            var hench1 = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Load_And_Start_Scenario.SequenceReference_0.Sequence_1537.SeqVar_Object_2");
            var hench2 = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Load_And_Start_Scenario.SequenceReference_0.Sequence_1537.SeqVar_Object_3");

            var seq = KismetHelper.GetParentSequence(hookup);

            var streamIn = SequenceObjectCreator.CreateSequenceObject(seq, "LEXSeqAct_StreamInActorTextures", vTestOptions.cache);
            KismetHelper.SetComment(streamIn, "Crossgen: Stream in hench textures to reduce LOD pop-in");
            KismetHelper.ChangeOutputLink(hookup, 0, 0, streamIn.UIndex);
            KismetHelper.CreateOutputLink(streamIn, "Out", dest);
            KismetHelper.CreateVariableLink(streamIn, "Target", hench1);
            KismetHelper.CreateVariableLink(streamIn, "Target", hench2);
            KismetHelper.CreateVariableLink(streamIn, "Time", SequenceObjectCreator.CreateFloat(seq, 10f, vTestOptions.cache));
        }
    }
}
