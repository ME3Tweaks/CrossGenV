using System.Collections.Generic;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Gammtek.Extensions;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;

namespace CrossGenV.Classes
{
    /// <summary>
    /// Rips doors out of the files and puts them into their own standalone PLC files.
    /// </summary>
    internal class VTestDoors
    {
        private static string[] NearbyLevelNames =
        [
            "BIOA_PRC2_CCSIM",
            "BIOA_PRC2_CCMID",
            "BIOA_PRC2_CCLOBBY",
            "BIOA_PRC2_CCAIRLOCK"
        ];

        public static void RipoutDoors(VTestOptions options)
        {
            var doorPackageName = "BIOA_PRC2_CCDoors";
            string[] doorFiles =
            [
                "BIOA_PRC2",
                "BIOA_PRC2_CCAirlock",
                "BIOA_PRC2_CCMID02_LAY",
                "BIOA_PRC2_CCSIM03_LAY"
            ];

            // door master is precomputed
            var doorMasterF = Path.Combine(VTestPaths.VTest_FinalDestDir, $"{doorPackageName}.pcc");
            var doorMaster = MEPackageHandler.OpenMEPackage(doorMasterF);

            IMEPackage bioaprc2 = null;
            foreach (var df in doorFiles)
            {
                var ripoutPath = Path.Combine(VTestPaths.VTest_FinalDestDir, $"{df}.pcc");
                var doorPackage = MEPackageHandler.OpenMEPackage(ripoutPath);

                foreach (var door in doorPackage.Exports.Where(x => x.ClassName == "BioDoor" && !x.IsArchetype).ToList())
                {
                    doorPackage.RemoveFromLevelActors(door);
                    // Door should get knocked out when we resynthesize package.

                }

                if (df == "BIOA_PRC2")
                {
                    bioaprc2 = doorPackage;
                    // Remove all prefab instances and biouseables as the are also for doors
                    foreach (var door in doorPackage.Exports.Where(x => x.ClassName is "PrefabInstance" or "BioUseable"))
                    {
                        doorPackage.RemoveFromLevelActors(door);
                        // Door should get knocked out when we resynthesize package.
                    }

                    KismetHelper.RemoveFromSequence(doorPackage.FindExport("TheWorld.PersistentLevel.Main_Sequences.Prefabs"), false);
                }
                else
                {
                    doorPackage.Save();
                }
            }


            var lsk = ExportCreator.CreateExport(bioaprc2, "LevelStreamingKismet", "LevelStreamingKismet", bioaprc2.GetLevel(), cache: options.cache);
            lsk.WriteProperty(new NameProperty(doorPackageName, "PackageName"));
            VTestUtility.RebuildStreamingLevels(bioaprc2);

            UpdateStreaming(bioaprc2);

            bioaprc2.Save();
        }

        /// <summary>
        /// Updates streaming states since we stripped out door streaming state handling as it's very bug prone.
        /// </summary>
        /// <param name="bioaprc2"></param>
        private static void UpdateStreamingManual(IMEPackage bioaprc2)
        {
            // Make Lobby visible by moving loaded to visible
            var airlockBTS = bioaprc2.FindExport("TheWorld.PersistentLevel.BioTriggerStream_32");
            var streaming = BioTriggerStreaming.FromExport(airlockBTS);
            streaming.StreamingStates[0].VisibleChunkNames.AddRange(streaming.StreamingStates[0].LoadChunkNames);
            streaming.StreamingStates[0].VisibleChunkNames = streaming.StreamingStates[0].VisibleChunkNames.Distinct().ToList();
            streaming.StreamingStates[0].LoadChunkNames.Clear(); // Airlock should have it and next area visible
            streaming.StreamingStates[0].LoadChunkNames.Add("bioa_prc2_ccmid"); // We preload a bit of mid so its less likely to require streaming 
            streaming.StreamingStates[0].LoadChunkNames.Add("bioa_prc2_ccmid01"); // We preload a bit of mid so its less likely to require streaming 
            streaming.StreamingStates[0].LoadChunkNames.Add("bioa_prc2_ccmid02"); // We preload a bit of mid so its less likely to require streaming 
            streaming.StreamingStates[0].LoadChunkNames.Add("bioa_prc2_ccmid03"); // We preload a bit of mid so its less likely to require streaming 
            streaming.StreamingStates[0].LoadChunkNames.Add("bioa_prc2_ccmid04"); // We preload a bit of mid so its less likely to require streaming 
            streaming.StreamingStates[0].StateName = "None"; // Remove state name
            streaming.StreamingStates.RemoveAt(1); // Remove door open state as it is no longer used.
            streaming.WriteStreamingStates(airlockBTS);

            // Make main room area and airlock visible in the lobby area as player can go to either.
            var lobby = bioaprc2.FindExport("TheWorld.PersistentLevel.BioTriggerStream_33");
            streaming = BioTriggerStreaming.FromExport(lobby);
            streaming.StreamingStates[0].VisibleChunkNames.AddRange(streaming.StreamingStates[0].LoadChunkNames);

            streaming.StreamingStates[0].StateName = "None"; // Remove state name
            streaming.StreamingStates.RemoveAt(2); // Remove LobbyDoorOpened state as it is no longer used.
            streaming.StreamingStates.RemoveAt(1); // Remove Airlock_Lobby_Visible state as it is no longer used.
            streaming.WriteStreamingStates(lobby);

            // MID - Right - Make Lobby visible instead of loaded
            var midRight = bioaprc2.FindExport("TheWorld.PersistentLevel.BioTriggerStream_34");
            streaming = BioTriggerStreaming.FromExport(midRight);
            streaming.StreamingStates[0].VisibleChunkNames.Add("bioa_prc2_cclobby");
            streaming.StreamingStates[0].VisibleChunkNames.Add("bioa_prc2_cclobby01");
            streaming.StreamingStates[0].VisibleChunkNames.Add("bioa_prc2_cclobby02");


            streaming.StreamingStates[0].LoadChunkNames.Remove("bioa_prc2_cclobby");
            streaming.StreamingStates[0].LoadChunkNames.Remove("bioa_prc2_cclobby01");
            streaming.StreamingStates[0].LoadChunkNames.Remove("bioa_prc2_cclobby02");

            streaming.StreamingStates[0].StateName = "None"; // Remove state name
            streaming.StreamingStates.RemoveAt(1); // Remove LobbyDoorOpened state as it is no longer used.
            streaming.WriteStreamingStates(midRight);


            // MID - Left - Make Sim room visible, begin preloading lobby
            var midLeft = bioaprc2.FindExport("TheWorld.PersistentLevel.BioTriggerStream_7");
            streaming = BioTriggerStreaming.FromExport(midLeft);
            streaming.StreamingStates[0].VisibleChunkNames.AddRange(streaming.StreamingStates[0].LoadChunkNames);
            streaming.StreamingStates[0].StateName = "None"; // Remove state name
            streaming.StreamingStates.RemoveAt(1); // Remove SimDoorOpened state as it is no longer used.
            streaming.WriteStreamingStates(midLeft);
        }

        private static void UpdateStreaming(IMEPackage bioaprc2)
        {
            // Do not write these - they are just for reading vanilla streaming
            BioTriggerStreaming[] stationStreams =
            [
                BioTriggerStreaming.FromExport(bioaprc2.FindExport("TheWorld.PersistentLevel.BioTriggerStream_4")), // Sim Loader
                BioTriggerStreaming.FromExport(bioaprc2.FindExport("TheWorld.PersistentLevel.BioTriggerStream_36")), // Sim Room
                BioTriggerStreaming.FromExport(bioaprc2.FindExport("TheWorld.PersistentLevel.BioTriggerStream_7")), // Mid Left
                BioTriggerStreaming.FromExport(bioaprc2.FindExport("TheWorld.PersistentLevel.BioTriggerStream_34")), // Mid Right
                BioTriggerStreaming.FromExport(bioaprc2.FindExport("TheWorld.PersistentLevel.BioTriggerStream_33")), // Lobby
                BioTriggerStreaming.FromExport(bioaprc2.FindExport("TheWorld.PersistentLevel.BioTriggerStream_32")) // Airlock
            ];

            // Merge visibility from old door states.
            for (int i = 1; i < stationStreams.Length; i++)
            {
                var ss = stationStreams[i];
                var defState = ss.StreamingStates[0];
                foreach (var extraState in ss.StreamingStates)
                {
                    if (defState == extraState)
                        continue;

                    defState.VisibleChunkNames.AddRange(extraState.VisibleChunkNames);
                }
                defState.VisibleChunkNames.Add("BIOA_PRC2_CCDoors");
                defState.VisibleChunkNames = defState.VisibleChunkNames.Distinct().ToList();
            }

            //List<NameReference> stationLevels = new List<NameReference>();

            //foreach (var ss in stationStreams)
            //{
            //    BioTriggerStreaming bts = BioTriggerStreaming.FromExport(bioaprc2.FindExport(ss));
            //    stationLevels.AddRange(bts.StreamingStates[0].VisibleChunkNames);
            //}

            //stationLevels = stationLevels.Distinct().ToList();

            // ADJACENT STREAMING

            // We go left to right on the map.
            for (int i = 0; i < stationStreams.Length; i++)
            {
                // Get our stream
                var vanillaStreaming = stationStreams[i];
                var streaming = BioTriggerStreaming.FromExport(bioaprc2.FindExport(vanillaStreaming.Export.InstancedFullPath));

                BioTriggerStreaming leftBTS = i > 0 ? stationStreams[i - 1] : null;
                BioTriggerStreaming leftLoadBTS = i > 1 ? stationStreams[i - 2] : null;
                BioTriggerStreaming rightBTS = i < 5 ? stationStreams[i + 1] : null;
                BioTriggerStreaming rightLoadBTS = i < 4 ? stationStreams[i + 2] : null;

                var defState = streaming.StreamingStates[0];

                // Don't reset states on loader
                if (i > 0)
                {
                    streaming.StreamingStates.Clear();
                    streaming.StreamingStates.Add(defState);
                    defState.StateName = "None";
                }

                defState.VisibleChunkNames = vanillaStreaming.StreamingStates[0].VisibleChunkNames.ToList(); // Make copy
                if (rightBTS != null)
                {
                    defState.VisibleChunkNames.AddRange(rightBTS.StreamingStates[0].VisibleChunkNames);
                }
                if (leftBTS != null)
                {
                    defState.VisibleChunkNames.AddRange(leftBTS.StreamingStates[0].VisibleChunkNames);
                }

                defState.VisibleChunkNames = defState.VisibleChunkNames.Distinct().Where(x => x != defState.InChunkName).OrderBy(x => x.Instanced).ToList();

                defState.LoadChunkNames.Clear();
                if (rightLoadBTS != null)
                {
                    defState.LoadChunkNames.AddRange(rightLoadBTS.StreamingStates[0].VisibleChunkNames);
                }
                if (leftLoadBTS != null)
                {
                    defState.LoadChunkNames.AddRange(leftLoadBTS.StreamingStates[0].VisibleChunkNames);
                }

                defState.LoadChunkNames = defState.LoadChunkNames.Distinct().Where(x => x != defState.InChunkName).OrderBy(x => x.Instanced).ToList();
                FixSpecificStreaming(defState);
                streaming.WriteStreamingStates(vanillaStreaming.Export);
            }

        }

        private static void FixSpecificStreaming(StreamingState defState)
        {
            if (defState.InChunkName == "BIOA_PRC2_CCMid")
            {
                // Remove extra vista stuff from airlock
                defState.VisibleChunkNames.Remove("bioa_prc2_ccspace03");
            }
            else if (defState.InChunkName == "BIOA_PRC2_CCAirlock")
            {
                // Remove mid vista stuff
                defState.VisibleChunkNames.Remove("bioa_prc2_ccspace02");
            }
        }
    }
}
