﻿using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Pathing;
using LegendaryExplorerCore.Unreal;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2 : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PostPortingCorrection()
        {
            // PRC1 BioSoundNodeWaveStreamingData:
            // This is hack to port things over in ModdedSource. The streaming data was referenced by an object that doesn't actually
            // use this (game will die if it tries). We remove this reference and set up our own.
            // This is a total hack, but it works for less code.
            le1File.Save();
            le1File.FindExport("TheWorld.PersistentLevel.AmbientSound_20").RemoveProperty("Base");
            VTestUtility.AddWorldReferencedObjects(le1File, le1File.FindExport("DVDStreamingAudioData.PC.snd_prc1_music")); // This must stay in memory for the music 2DA to work for PRC1 audio

            FixGethPulseGunVFX();
            
            FixBlockingLevelLoads();

            // Level Load Blocking Texture Streaming
            VTestKismet.InstallVTestHelperSequenceNoInput(le1File, "TheWorld.PersistentLevel.Main_Sequence", "HelperSequences.LevelLoadTextureStreaming", vTestOptions);
            // The original logic is removed in the ModdedSource file

            FixLoadingLagNearOchren();

            // 10/05/2024 - Remove mod level extensions. No mods ever used these according to the Nexus File DB
            // Devs can use sideloader framework instead these days.
            // VTestUtility.AddModLevelExtensions(le1File, "BIOA_PRC2");

            // Blocking Volumes for shep to stand on post-mission
            int[] sourceTriggerStreams = new int[]
            {
                            10, 11, 12, 13, 18 // 18 is technically not required (ahern) but left in event of future changes. These are the scoreboard triggerstreams
            };

            var sourceAsset = le1File.FindExport(@"TheWorld.PersistentLevel.BlockingVolume_15");

            // Move trigger streams to make next playable area start streaming in before player regains control
            // to prevent blocking load
            foreach (var sts in sourceTriggerStreams)
            {
                var newBlockingVolume = EntryCloner.CloneTree(sourceAsset);
                //EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneTreeAsChild, sourceAsset, le1File, destLevel, true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out var newBlockingVolume);

                var tsExport = le1File.FindExport(@"TheWorld.PersistentLevel.BioTriggerStream_" + sts);
                var loc = LevelTools.GetLocation(tsExport);
                LevelTools.SetLocation(newBlockingVolume as ExportEntry, loc.X, loc.Y, loc.Z - 256f);
            }

            AddGlobalVariables();
        }

        private void FixGethPulseGunVFX()
        {
            // This is a new material that adds HoloWipe VFX to the geth pulse gun, that is only referenced from our 2DA
            // Ordinarily this would be in a startup file (such as BIOC_Materials for basegame) but that doesn't seem to work so we have it in this file instead

            // Clone BIOG_WPN_ tree
            var gethPulseVfxRoot = vTestOptions.vTestHelperPackage.FindExport("BIOG_WPN_ALL_MASTER_L");
            var gethPulseVfxIFP = "BIOG_WPN_ALL_MASTER_L.Appearance.Geth.VTEST_WPN_GTH_Appr";
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneTreeAsChild, gethPulseVfxRoot, le1File, null,
                true, new RelinkerOptionsPackage(), out var _);

            // Effectively clone/relink all references for VTEST_WPN_GTH_Appr
            var gethPulseVfx = le1File.FindExport(gethPulseVfxIFP);
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.ReplaceSingularWithRelink, vTestOptions.vTestHelperPackage.FindExport(gethPulseVfxIFP),
                le1File, gethPulseVfx, true, new RelinkerOptionsPackage(), out var _);
            VTestUtility.AddWorldReferencedObjects(le1File, gethPulseVfx);
        }

        private void FixBlockingLevelLoads()
        {
            // Adjust the triggerstreams to pre-stream in some files to prevent a full blocking load from occurring.
            // They all have the same state name

            string[] levelsToAdd = new[]
            {
                    "BIOA_PRC2_CCMain_Conv", // This will trigger blocking load as it goes directly to visible on the next change
                    "BIOA_PRC2_CCMain_SND", // This will trigger blocking load as it goes directly to visible on the next change

                    // These ones are here just to pre-load things into memory in the event the player just mashes their way through
                    "BIOA_PRC2_CCSim",
                    "BIOA_PRC2_CCSim_ART",
                    "BIOA_PRC2_CCSim_DSG",
                };

            foreach (var export in le1File.Exports.Where(x => x.ClassName == "BioTriggerStream"))
            {
                var ss = export.GetProperty<ArrayProperty<StructProperty>>("StreamingStates");

                if (ss != null && ss.Count == 2)
                {
                    // State idx 1 is the one we want.
                    var state = ss[1];
                    if (state.GetProp<NameProperty>("StateName")?.Value.Name == "Load_Post_Scenario_Scoreboard")
                    {
                        Debug.WriteLine($"Updating streaming state for more preload: BIOA_PRC2 {export.ObjectName.Instanced}");
                        var loadChunkNames = state.GetProp<ArrayProperty<NameProperty>>("LoadChunkNames");
                        foreach (var lta in levelsToAdd)
                        {
                            loadChunkNames.Add(new NameProperty(lta));
                        }

                        export.WriteProperty(ss);
                    }
                }
            }
        }

        /// <summary>
        /// Makes the area around ochren not have different loaded levels as player will cross boundary quite often
        /// </summary>
        private void FixLoadingLagNearOchren()
        {
            var nearOchren = le1File.FindExport("TheWorld.PersistentLevel.BioTriggerStream_4");

            var streamingStates = nearOchren.GetProperty<ArrayProperty<StructProperty>>("StreamingStates");
            var visible = streamingStates[0];
            var loadChunks = visible.GetProp<ArrayProperty<NameProperty>>("LoadChunkNames");
            loadChunks.Clear(); // We're just going to reorder the whole thing
            loadChunks.Add(new NameProperty("BIOA_PRC2_CCMid"));
            loadChunks.Add(new NameProperty("bioa_prc2_ccmid01"));
            loadChunks.Add(new NameProperty("bioa_prc2_ccmid02"));
            loadChunks.Add(new NameProperty("bioa_prc2_ccmid03"));
            loadChunks.Add(new NameProperty("bioa_prc2_ccmid04"));
            loadChunks.Add(new NameProperty("bioa_prc2_ccmid04"));
            loadChunks.Add(new NameProperty("bioa_prc2_ccspace02"));
            loadChunks.Add(new NameProperty("bioa_prc2_ccsim05_dsg"));

            nearOchren.WriteProperty(streamingStates);
        }

        private void AddGlobalVariables()
        {
            var entries = new List<ExportEntry>();
            var seq = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence");

            // Ramping variables
            var wmChance = SequenceObjectCreator.CreateFloat(seq, 0, vTestOptions.cache);
            wmChance.WriteProperty(new NameProperty("CG_RAMP_WEAPONMOD_CHANCE", "VarName"));
            var tChance = SequenceObjectCreator.CreateFloat(seq, 0, vTestOptions.cache);
            tChance.WriteProperty(new NameProperty("CG_RAMP_TALENT_CHANCE", "VarName"));

            var wmCount = SequenceObjectCreator.CreateInt(seq, 0, vTestOptions.cache);
            wmCount.WriteProperty(new NameProperty("CG_RAMP_WEAPONMODS_COUNT", "VarName"));
            var tCount = SequenceObjectCreator.CreateInt(seq, 0, vTestOptions.cache);
            tCount.WriteProperty(new NameProperty("CG_RAMP_TALENTS_COUNT", "VarName"));

            var resetRamping = SequenceObjectCreator.CreateSeqEventRemoteActivated(seq, "CG_RESET_RAMPING");
            var zeroFloat = SequenceObjectCreator.CreateFloat(seq, 0, vTestOptions.cache);
            var zeroInt = SequenceObjectCreator.CreateInt(seq, 0, vTestOptions.cache);
            entries.Add(SequenceObjectCreator.CreateSetFloat(seq, wmChance, zeroFloat, vTestOptions.cache));
            entries.Add(SequenceObjectCreator.CreateSetFloat(seq, tChance, zeroFloat, vTestOptions.cache));
            entries.Add(SequenceObjectCreator.CreateSetInt(seq, wmCount, zeroInt, vTestOptions.cache));
            entries.Add(SequenceObjectCreator.CreateSetInt(seq, tCount, zeroInt, vTestOptions.cache));

            ExportEntry previous = resetRamping;
            foreach (var entry in entries)
            {
                KismetHelper.CreateOutputLink(previous, "Out", entry);
                previous = entry;
            }
        }
    }
}
