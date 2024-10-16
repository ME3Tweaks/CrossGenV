using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Gammtek.Extensions;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;

namespace CrossGenV.Classes
{
    public class FrameworkPawn
    {
        public StructProperty Location { get; set; }
        public StructProperty Rotation { get; set; }

        public string LevelSource { get; set; }

        /// <summary>
        /// Used as Tag, also set for filename.
        /// </summary>
        public string NPCName { get; set; }

        /// <summary>
        /// If this should be split out to separate file. Set to false if you are using a different version of this pawn. In that instance, use same NPCName.
        /// </summary>
        public bool GenerateBioNPC { get; set; } = true;

        /// <summary>
        /// Instanced object name of the pawn actor.
        /// </summary>
        public string BioPawnName { get; set; }

        ///// <summary>
        ///// Where to hookup the handshake logic
        ///// </summary>
        //public string HookupIFP { get; set; }

        /// <summary>
        /// If we aren't using HookupIFP, we trigger on remote event. This is the prefix to the NPCName that will signal the incoming remote event.
        /// </summary>
        public string RemoteEventNamePrefix { get; set; }

        /// <summary>
        /// Comment to attach to the handshake in Kismet
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// When this package becomes visible, it will have a level loaded event that sets up the pawn
        /// </summary>
        public string PackageToSignalOnVisibility { get; set; }

        /// <summary>
        /// If the visibility of the package should also set the pawn to active.
        /// </summary>
        public bool SetActiveOnVisibility { get; set; }


        /// <summary>
        /// Where to install handshakes - ensuring the pawn is loaded and ready for use
        /// </summary>
        public SequenceHookup[] Handshakes { get; set; } = [];

        /// <summary>
        /// Definitions of where to fire a signal to teleport the frameworked actor to the specified position. This will trigger handshake to make them active at that location.
        /// </summary>
        public SequenceHookup[] TeleportSignals { get; set; } = [];
    }

    public class SequenceHookup
    {
        /// <summary>
        /// What file the hookup is in.
        /// </summary>
        public string PackageFile { get; set; }

        /// <summary>
        /// Where to install the hookup.
        /// </summary>
        public string HookupIFP { get; set; }
        /// <summary>
        /// Name of the outlink to use on hookup
        /// </summary>
        public string OutLinkName { get; set; } = "Out";

        /// <summary>
        /// If a one frame delay should be added
        /// </summary>
        public bool TwoFrameDelay { get; set; }
    }

    /// <summary>
    /// Handles frameworking of NPCs
    /// </summary>
    public static class VTestFramework
    {
        /// <summary>
        /// Installs a level loaded event in the package that sets the pawn location up. This shouldn't require the handshake system due to how the levels load in order.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="pawn"></param>
        /// <param name="options"></param>
        public static void InstallPawnLocationSignal(IMEPackage package, FrameworkPawn pawn, bool setActive, VTestOptions options)
        {
            var mainSeq = package.FindExport("TheWorld.PersistentLevel.Main_Sequence");
            var levelLoaded = SequenceObjectCreator.CreateLevelLoaded(mainSeq, options.cache);
            var pawnRef = SequenceObjectCreator.CreateFindObject(mainSeq, $"NPC_{pawn.NPCName}", false, options.cache);
            var setLoc = SequenceObjectCreator.CreateSetLocation(mainSeq, pawnRef, cache: options.cache);
            var sActive = SequenceObjectCreator.CreateSetActive(mainSeq, pawnRef, SequenceObjectCreator.CreateBool(mainSeq, true, options.cache), cache: options.cache);
            if (pawn.Location != null)
            {
                pawn.Location.Name = "LocationValue";
                setLoc.WriteProperty(new BoolProperty(true, "bSetLocation"));
                setLoc.WriteProperty(pawn.Location);
            }

            setLoc.WriteProperty(new BoolProperty(true, "bSetRotation"));
            if (pawn.Rotation != null)
            {
                pawn.Rotation.Name = "RotationValue";
                setLoc.WriteProperty(pawn.Rotation);
            }
            else
            {
                // Force rotate back to zero
                setLoc.WriteProperty(CommonStructs.RotatorProp(0, 0, 0, "RotationValue"));
            }

            KismetHelper.CreateVariableLink(setLoc, "Target", pawnRef);
            KismetHelper.CreateOutputLink(levelLoaded, "Loaded and Visible", setLoc);
            KismetHelper.CreateOutputLink(setLoc, "Out", sActive);
        }

        /// <summary>
        /// Packages that need tags updated in the find object
        /// </summary>
        private static readonly string[] PackagesToUpdateFindObjects =
        [
            "BIOA_PRC2_CCLOBBY",
            "BIOA_PRC2_CCMAIN_CONV",
            "BIOA_PRC2_CCMID",
            "BIOA_PRC2_CCSIM04_DSG",
            "BIOA_PRC2_CCCrate_DSG",
            "BIOA_PRC2_CCCrate_DSG",
            "BIOA_PRC2_CCAhern_DSG",
            "BIOA_PRC2_CCLava_DSG",
            "BIOA_PRC2_CCCave_DSG",
            "BIOA_PRC2_CCThai_DSG",
        ];

        private static readonly CaseInsensitiveDictionary<string> FindObjectByTagFixes = new()
        {
            {"BIOA_PRC2_TUR_Guard01", "NPC_VegasTurianGuard3"},
            {"prc2_tur_jerk_entry", "NPC_RivalVidinos"},
            {"prc2_tur_jerk", "NPC_RivalVidinos"},
            {"prc2_ochren", "NPC_Ochren"},
            {"PRC2_TUR_OldWarrior", "NPC_Dahga"},
            {"PRC2_HUM_Ahern", "NPC_Ahern"},
            {"prc2_hmmyoungster", "NPC_RivalBryant"},
        };

        public static readonly FrameworkPawn[] NPCsToFramework =
        [
            // We might as well just framework everyone.

            // Two turian guards that salute you on entry
            new() {
                LevelSource = "BIOA_PRC2_CCLOBBY",
                BioPawnName = "BioPawn_0",
                NPCName = "VegasTurianGuard1",
                Handshakes = [
                    new ()
                    {
                        HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.SeqEvent_LevelLoaded_1"
                    }
                ],
                Comment="Entry: Saluting guard"
            },
            new() {
                LevelSource = "BIOA_PRC2_CCLOBBY",
                BioPawnName = "BioPawn_10",
                NPCName = "VegasTurianGuard2",
                Handshakes = [
                    new ()
                    {
                        HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.SeqEvent_LevelLoaded_1"
                    }
                ],
                Comment="Entry: Saluting guard"
            },

            // Mid Room Crew
            new() {
                LevelSource = "BIOA_PRC2_CCMID",
                BioPawnName = "BioPawn_3",
                NPCName = "VegasHumanCrew1",
                Handshakes = [
                    new ()
                    {
                        HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Prefabs.SeqEvent_LevelLoaded_0"
                    }
                ],
                Comment = "Human Male: Standing at console"
            },
            new() {
                LevelSource = "BIOA_PRC2_CCMID",
                BioPawnName = "BioPawn_5",
                NPCName = "VegasHumanCrew2",
                Handshakes = [
                    new ()
                    {
                        HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Prefabs.SeqEvent_LevelLoaded_0"
                    }
                ],
                Comment = "Human Female: Leftmost cockpit console"
            },
            new() {
                LevelSource = "BIOA_PRC2_CCMID",
                BioPawnName = "BioPawn_6",
                NPCName = "VegasHumanCrew3",
                Handshakes = [
                    new ()
                    {
                        HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Prefabs.SeqEvent_LevelLoaded_0"
                    }
                ],
                Comment = "Human Female: Left cockpit row, right side (position 2)"
            },
            new() {
                LevelSource = "BIOA_PRC2_CCMID",
                BioPawnName = "BioPawn_7",
                NPCName = "VegasHumanCrew4",
                Handshakes = [
                    new ()
                    {
                        HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Prefabs.SeqEvent_LevelLoaded_0"
                    }
                ],
                Comment = "Human male: Right cockpit row, left side (position 3)"
            },

            // Competitors in the room at the tables
            // These are the actual names that appear on the scoreboard
            new() {
                LevelSource = "BIOA_PRC2_CCSIM01_DSG",
                BioPawnName = "BioPawn_0",
                NPCName = "RivalProchor",
                Handshakes = [
                    new ()
                    {
                        HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Prefabs.SeqEvent_LevelLoaded_2"
                    }
                ]
            },
            new() {
                LevelSource = "BIOA_PRC2_CCSIM01_DSG",
                BioPawnName = "BioPawn_1",
                NPCName = "RivalMinket",
                Handshakes = [
                    new ()
                    {
                        HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Prefabs.SeqEvent_LevelLoaded_2"
                    }
                ]
            },
            // Standard Bryant
            new() {
                LevelSource = "BIOA_PRC2_CCSIM01_DSG",
                BioPawnName = "BioPawn_2",
                NPCName = "RivalBryant",
                RemoteEventNamePrefix = "Console",
                Handshakes = [
                    // Determining visibility
                    new ()
                    {
                        HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.SeqAct_Log_2"
                    },
                ],
                TeleportSignals = [
                    // Post vidinos conversation, signal this
                    new ()
                    {
                        PackageFile = "BIOA_PRC2_CCMAIN_CONV",
                        HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SequenceReference_0.Sequence_980.BioSeqAct_ModifyPropertyPawn_0"
                    }
                ]
            },
            new() {
                LevelSource = "BIOA_PRC2_CCSIM01_DSG",
                BioPawnName = "BioPawn_3",
                NPCName = "RivalCSell1",
                Handshakes = [
                    new ()
                    {
                        HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Prefabs.SeqEvent_LevelLoaded_2"
                    }
                ]
            },
            new() { LevelSource = "BIOA_PRC2_CCSIM01_DSG", BioPawnName = "BioPawn_5", NPCName = "RivalSophomin",
                Handshakes =
                [
                    new ()
                    {
                        HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Prefabs.SeqEvent_LevelLoaded_2"
                    }
                ]
            },
            new() {
                // Looks like tag was never updated on this pawn
                LevelSource = "BIOA_PRC2_CCSIM01_DSG",
                BioPawnName = "BioPawn_7",
                NPCName = "RivalCSell2",
                Handshakes = [
                    new () {
                        HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Prefabs.SeqEvent_LevelLoaded_2"
                    }
                ]
            },
            new() {
                LevelSource = "BIOA_PRC2_CCSIM01_DSG",
                BioPawnName = "BioPawn_8",
                NPCName = "RivalChior",
                Handshakes = [
                    new () { HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Prefabs.SeqEvent_LevelLoaded_2"}
                ]
            },

            // Burrum - 'Krogan Frat Boy'
            new() { LevelSource = "BIOA_PRC2_CCSIM02_DSG",
                BioPawnName = "BioPawn_3",
                NPCName = "RivalBurrum",
                Handshakes = [
                    new () {
                        HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.SeqEvent_LevelLoaded_0"
                    }
                ],
                Comment="Krogran Frat Boy"
            },
            
            // Ochren
            new () {
                LevelSource = "BIOA_PRC2_CCSIM04_DSG",
                BioPawnName = "BioPawn_11",
                NPCName = "Ochren",
                Handshakes = [
                    new () { HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Prefabs.SeqEvent_LevelLoaded_0"}
                ],
                Comment = "Salarian Joker"
            },

            // Dahga
            new()
            {
                LevelSource = "BIOA_PRC2_CCMAIN_CONV",
                BioPawnName = "BioPawn_0",
                NPCName = "Dahga",
                Handshakes = [
                    new () { HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.SeqAct_Log_2"}
                ]
            },
            // Guardpost - Vidinos
            new() {
                LevelSource = "BIOA_PRC2_CCMAIN_CONV",
                BioPawnName = "BioPawn_1",
                NPCName = "RivalVidinos", 
                // HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.SeqEvent_LevelLoaded_1", 
                RemoteEventNamePrefix = "Guardpost",
                PackageToSignalOnVisibility = "BIOA_PRC2_CCLOBBY",
                SetActiveOnVisibility = true,
                TeleportSignals = [
                    new ()
                    {
                        // Post Vidinos conversation, teleport back to home
                        PackageFile = "BIOA_PRC2_CCMAIN_CONV",
                        HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SequenceReference_0.Sequence_980.BioSeqAct_ModifyPropertyPawn_0",
                    }
                ]
            },
            // Guardpost - Person across from Vidinos
            new() {
                LevelSource = "BIOA_PRC2_CCMAIN_CONV",
                BioPawnName = "BioPawn_2", NPCName = "VegasTurianGuard3",
                RemoteEventNamePrefix = "Guardpost",
                Comment = "Entry: Guard across from Vidinos",
                PackageToSignalOnVisibility = "BIOA_PRC2_CCLOBBY",
                SetActiveOnVisibility = true
            },

            new() {
                LevelSource = "BIOA_PRC2_CCSIM04_DSG",
                BioPawnName = "BioPawn_7",
                NPCName = "Ahern",
                GenerateBioNPC = false,
                Comment="Ahern: My Special Mission Introduction",
                RemoteEventNamePrefix = "MySpecialMission",
                TeleportSignals = [
                    // May consider this as it may let textures warm up, but this is before ochren talks.
                    // TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.BioSeqAct_PMExecuteTransition_15
                    new() { PackageFile = "BIOA_PRC2_CCMAIN_CONV", HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.Play_Ahern_Offers_Final_Mission_0.SeqEvent_SequenceActivated_0"}
                    ]
                // We don't signal visibility here
            },

            // Ahern - Observation Deck next to Ochren
            new()
            {
                LevelSource = "BIOA_PRC2_CCMAIN_CONV",
                BioPawnName = "BioPawn_3",
                NPCName = "Ahern",
                GenerateBioNPC = false,
                // RemoteEventNamePrefix = "AhernPostMissionQuip",
                // HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_Teleport_18",
                Comment="Ahern: Post mission quip",
                RemoteEventNamePrefix = "PostMissionQuip",
                TeleportSignals = [
                    new()
                    {
                        // Success
                        PackageFile = "BIOA_PRC2_CCMAIN_CONV",
                        HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_Teleport_13",
                        TwoFrameDelay = true,
                    },
                    new()
                    {
                        // Failure - fires after delay cause maybe some other level loaded is hiding ahern
                        PackageFile = "BIOA_PRC2_CCMAIN_CONV",
                        HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_Teleport_19",
                        TwoFrameDelay = true,
                    }
                ]
                // We use remote event for visibility
            },
            // Ahern - Standard post
            new() {
                LevelSource = "BIOA_PRC2_CCMAIN_CONV",
                BioPawnName = "BioPawn_4",
                NPCName = "Ahern",
                Comment = "Ahern: Main location",
                // LEVEL VISIBILITY ==================
                // When CCMid becomes visible, always teleport Ahern to his post
                PackageToSignalOnVisibility = "BIOA_PRC2_CCMID",
                SetActiveOnVisibility = true,

                // HANDSHAKES ========================
                Handshakes = [
                    // new (){ HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.SeqAct_Log_2"}
                ],
                // TELEPORT SIGNALING ================
                RemoteEventNamePrefix = "Standard",
                TeleportSignals = [
                    // End of impressive work conversation (unlock ahern mission) -> teleport out of that location
                    new() { PackageFile = "BIOA_PRC2_CCMAIN_CONV", HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.Play_Ahern_Offers_Final_Mission_0.SeqAct_Toggle_1"},
                    // End of match end cine
                    new () { PackageFile = "BIOA_PRC2_CCMAIN_CONV", HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqAct_ToggleInput_1"}
                ]
            },
            // Vidinos - Near Ocaren
            new()
            {
                LevelSource = "BIOA_PRC2_CCMAIN_CONV",
                BioPawnName = "BioPawn_5",
                NPCName = "RivalVidinos",
                GenerateBioNPC = false,
                Handshakes= [
                    new() { HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SequenceReference_0.Sequence_980.SeqEvent_SequenceActivated_0"}
                ],
                Comment = "Vidinos: Post mission cutscenes",
            },
            // Bryant - Cutscene with Vidinos
            new()
            {
                LevelSource = "BIOA_PRC2_CCMAIN_CONV",
                BioPawnName = "BioPawn_8",
                NPCName = "RivalBryant",
                GenerateBioNPC = false,
                RemoteEventNamePrefix = "Cutscene",
                Handshakes =
                [
                    new ()
                    {
                        // Start of Vidinos conversation
                        PackageFile = "BIOA_PRC2_CCMAIN_CONV",
                        HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SequenceReference_0.Sequence_980.SeqEvent_SequenceActivated_0",
                    },
                ],
                Comment = "Bryant: Post-mission cutscene"
            },
        ];

        public static void FrameworkNPCs(VTestOptions options)
        {
            // We can drop the cache contents, since we don't need old stuff anymore
            options.cache.ReleasePackages();

            // Step 1: Inventory and generate NPC files
            options.SetStatusText("Generating framework NPC files");
            foreach (var npc in NPCsToFramework)
            {
                var sourcePath = Path.Combine(VTestPaths.VTest_FinalDestDir, $"{npc.LevelSource}.pcc");
                if (!File.Exists(sourcePath))
                {
#if !DEBUG
throw new Exception("This must be fixed for release!");
#else
                    options.SetStatusText($"   Could not find package {npc.LevelSource}, skipping");
                    continue;
#endif
                }

                var package = MEPackageHandler.OpenMEPackage(sourcePath);

                var npcActor = package.FindExport($"TheWorld.PersistentLevel.{npc.BioPawnName}");

                // Cache location and rotation of this instance of the pawn so when we do the handshake we can set these
                npc.Location = npcActor.GetProperty<StructProperty>("Location");
                npc.Rotation = npcActor.GetProperty<StructProperty>("Rotation");

                if (npc.GenerateBioNPC)
                {
                    options.SetStatusText($"  BIONPC_{npc.NPCName}");
                    GenerateFrameworkNPC(npcActor, npc.NPCName, options);
                }
            }

            // Step 2: Modify packages and generate framework changes
            options.SetStatusText("Stripping hardcoded NPCs");
            IMEPackage frameworkingPackage = null;
            foreach (var npc in NPCsToFramework.OrderBy(x => x.LevelSource))
            {
                options.SetStatusText($"  {npc.NPCName} from {npc.LevelSource}");

                var currentPackageFname = frameworkingPackage?.FileNameNoExtension;
                if (currentPackageFname != npc.LevelSource)
                {
                    // Package is changing

                    // Save current package, if any
                    frameworkingPackage?.Save();

                    // Load new package
                    var sourcePath = Path.Combine(VTestPaths.VTest_FinalDestDir, $"{npc.LevelSource}.pcc");
#if DEBUG
                    if (!File.Exists(sourcePath))
                    {
                        options.SetStatusText($"Skipping missing file: {npc.LevelSource}");
                        continue;
                    }
#endif
                    frameworkingPackage = MEPackageHandler.OpenMEPackage(sourcePath);
                }

                FrameworkPackage(frameworkingPackage, npc, options);
            }

            // Final save for step 2
            frameworkingPackage?.Save();

            // Step 3: Modify packages and generate framework changes
            options.SetStatusText("Setting up level visibility triggers");
            foreach (var npc in NPCsToFramework.Where(x => x.PackageToSignalOnVisibility != null).OrderBy(x => x.PackageToSignalOnVisibility))
            {
                options.SetStatusText($"  {npc.NPCName} in {npc.PackageToSignalOnVisibility}");

                var currentPackageFname = frameworkingPackage?.FileNameNoExtension;
                if (currentPackageFname != npc.PackageToSignalOnVisibility)
                {
                    // Package is changing

                    // Save current package, if any
                    frameworkingPackage?.Save();

                    // Load new package
                    var sourcePath = Path.Combine(VTestPaths.VTest_FinalDestDir, $"{npc.PackageToSignalOnVisibility}.pcc");
#if DEBUG
                    if (!File.Exists(sourcePath))
                    {
                        options.SetStatusText($"Skipping missing file: {npc.LevelSource}");
                        continue;
                    }
#endif
                    frameworkingPackage = MEPackageHandler.OpenMEPackage(sourcePath);
                }

                InstallPawnLocationSignal(frameworkingPackage, npc, npc.SetActiveOnVisibility, options);
            }

            // Final save for step 3
            frameworkingPackage?.Save();

            // Step 4: Fix up the existing find object by tags
            options.SetStatusText($"Fixing Tags");

            foreach (var pName in PackagesToUpdateFindObjects)
            {
                options.SetStatusText($"  Fixing up ObjectFindByTag in {pName}");

                var sourcePath = Path.Combine(VTestPaths.VTest_FinalDestDir, $"{pName}.pcc");
#if DEBUG
                if (!File.Exists(sourcePath))
                {
                    options.SetStatusText($"Skipping missing file: {pName}");
                    continue;
                }
#endif
                var package = MEPackageHandler.OpenMEPackage(sourcePath);

                FixupFindObjects(package, options);
                FixupConversationSpeakers(package, options);
                package.Save();
            }

            // Step 5: Install remote event activators to teleport things around where necessary
            frameworkingPackage = null;
            foreach (var npc in NPCsToFramework.Where(x => x.TeleportSignals != null && x.TeleportSignals.Length > 0))
            {
                options.SetStatusText($"Installing signals for {npc.NPCName}");

                foreach (var signal in npc.TeleportSignals)
                {
                    var currentPackageFname = frameworkingPackage?.FileNameNoExtension;
                    if (currentPackageFname != signal.PackageFile)
                    {
                        // Package is changing

                        // Save current package, if any
                        frameworkingPackage?.Save();

                        // Load new package
                        var sourcePath = Path.Combine(VTestPaths.VTest_FinalDestDir, $"{signal.PackageFile}.pcc");
#if DEBUG
                        if (!File.Exists(sourcePath))
                        {
                            options.SetStatusText($"Skipping missing file: {signal.PackageFile}");
                            continue;
                        }
#endif
                        frameworkingPackage = MEPackageHandler.OpenMEPackage(sourcePath);
                    }

                    InstallRemoteEventSignal(npc, signal, frameworkingPackage, options);
                }

                frameworkingPackage?.Save();
            }


            // Final step: Update trigger streams
            options.SetStatusText("Updating trigger streams");
            UpdateTriggerStreams(options);
        }

        private static void FixupConversationSpeakers(IMEPackage package, VTestOptions options)
        {
            foreach (var exp in package.Exports.Where(x => x.ClassName == "BioConversation"))
            {
                var speakerList = exp.GetProperty<ArrayProperty<StructProperty>>("m_SpeakerList");
                if (speakerList == null)
                    continue;
                foreach (var speaker in speakerList)
                {
                    var tag = speaker.GetProp<NameProperty>("sSpeakerTag").Value;
                    if (FindObjectByTagFixes.TryGetValue(tag, out var newTag))
                    {
                        speaker.Properties.AddOrReplaceProp(new NameProperty(newTag, "sSpeakerTag"));
                    }
                }
                exp.WriteProperty(speakerList);
            }
        }

        /// <summary>
        /// Installs an ActiveRemoteEvent after the hookup to set the pawn up for use
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="signal"></param>
        /// <param name="frameworkingPackage"></param>
        /// <param name="options"></param>
        private static void InstallRemoteEventSignal(FrameworkPawn npc, SequenceHookup signal, IMEPackage frameworkingPackage, VTestOptions options)
        {
            var reName = $"{npc.RemoteEventNamePrefix}{npc.NPCName}";
            var hookup = frameworkingPackage.FindExport(signal.HookupIFP);
            var seq = KismetHelper.GetParentSequence(hookup);
            var re = SequenceObjectCreator.CreateActivateRemoteEvent(seq, reName, options.cache);
            KismetHelper.InsertActionAfter(hookup, signal.OutLinkName, re, 0, "Out");

            if (signal.TwoFrameDelay)
            {
                // Insert a two frame delay to allow kismet to finish execution before continuing. doing a 0.001f didn't seem to be enough.
                var delay = SequenceObjectCreator.CreateDelay(seq, 0.32f, options.cache);
                KismetHelper.InsertActionAfter(re, "Out", delay, 0, "Finished");
            }
        }

        private static void FixupFindObjects(IMEPackage package, VTestOptions options)
        {
            foreach (var ofbt in package.Exports.Where(x => x.ClassName is "BioSeqVar_ObjectFindByTag" or "BioSeqVar_ObjectListFindByTag" or "BioEvtSysTrackGesture"))
            {
                switch (ofbt.ClassName)
                {
                    case "BioSeqVar_ObjectFindByTag":
                    case "BioSeqVar_ObjectListFindByTag":
                        {
                            var tagToFind = ofbt.GetProperty<StrProperty>("m_sObjectTagToFind");
                            if (tagToFind == null)
                                continue;

                            if (FindObjectByTagFixes.TryGetValue(tagToFind.Value, out var newValue))
                            {
                                tagToFind.Value = newValue;
                                ofbt.WriteProperty(tagToFind);
                            }

                            break;
                        }
                    case "BioEvtSysTrackGesture":
                        {
                            var tagToFind = ofbt.GetProperty<StrProperty>("sActorTag");
                            if (tagToFind == null)
                                continue;

                            if (FindObjectByTagFixes.TryGetValue(tagToFind.Value, out var newValue))
                            {
                                tagToFind.Value = newValue;
                                ofbt.WriteProperty(tagToFind);
                            }
                            break;
                        }
                }
            }
        }


        /// <summary>
        /// Generates a framework NPC package based on the given pawn. The package will be modified for clean removal - the package should not be used for other usages.
        /// </summary>
        /// <param name="pawnActor">The actor to modify</param>
        private static void GenerateFrameworkNPC(ExportEntry pawnActor, string npcVarName, VTestOptions options)
        {
            var outPath = Path.Combine(VTestPaths.VTest_FinalDestDir, "Framework");
            Directory.CreateDirectory(outPath);
            var outFilePath = Path.Combine(outPath, $"BIONPC_{npcVarName}.pcc");

            MEPackageHandler.CreateEmptyLevel(outFilePath, pawnActor.Game);
            var npcPackage = MEPackageHandler.OpenMEPackage(outFilePath);


            var level = npcPackage.FindExport("TheWorld.PersistentLevel");
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, pawnActor,
                npcPackage, level, true,
                new RelinkerOptionsPackage() { Cache = options.cache, PortImportsMemorySafe = true },
                out var portedPawnEntry);

            var portedActor = portedPawnEntry as ExportEntry;
            portedActor.WriteProperty(new NameProperty($"NPC_{npcVarName}", "Tag"));
            var behavior = portedActor.GetProperty<ObjectProperty>("m_oBehavior")
                .ResolveToExport(pawnActor.FileRef, options.cache);
            behavior.WriteProperty(new BoolProperty(false,
                "bActive")); // Make inactive, level-side handshake will activate them

            GenerateHandshake(npcPackage, npcVarName, portedActor, options);

            npcPackage.AddToLevelActorsIfNotThere(portedActor);

            npcPackage.Save();
        }

        private static void GenerateHandshake(IMEPackage npcPackage, string npcVarName, ExportEntry actor,
            VTestOptions options)
        {
            // Generates the LevelLoaded and Poll events that provide a callback for listeners to know this NPC has loaded and is visible in the level.
            var sequence = npcPackage.FindExport("TheWorld.PersistentLevel.Main_Sequence");
            var levelLoaded = SequenceObjectCreator.CreateLevelLoaded(sequence, options.cache);
            var re = SequenceObjectCreator.CreateSeqEventRemoteActivated(sequence, $"Poll_NPC_{npcVarName}",
                options.cache);
            var are = SequenceObjectCreator.CreateActivateRemoteEvent(sequence, $"Live_NPC_{npcVarName}",
                options.cache);

            // Crossgen: We SetObject the pawn that loads to set the variable in PRC2 so we can just reference that instead of doing a ton of invasive sequence changes
            //var pawnObj = SequenceObjectCreator.CreateObject(sequence, actor, options.cache);
            //var varObj = SequenceObjectCreator.CreateScopeNamed(sequence, "SeqVar_Object", $"NPC_{npcVarName}", options.cache);
            //var setObject = SequenceObjectCreator.CreateSetObject(sequence, varObj, pawnObj, options.cache);
            //KismetHelper.CreateOutputLink(levelLoaded, "Loaded and Visible", setObject);
            //KismetHelper.CreateOutputLink(setObject, "Out", setObject);

            KismetHelper.CreateOutputLink(levelLoaded, "Loaded and Visible", are);
            KismetHelper.CreateOutputLink(re, "Out", are);
        }

        /// <summary>
        /// Removes pawn from package and installs the handshake to set the pawn up for use.
        /// </summary>
        /// <param name="package"></param>
        private static void FrameworkPackage(IMEPackage package, FrameworkPawn pawn, VTestOptions options)
        {
            PreCorrectPackage(package);

            // Convert references
            var originalPawn = package.FindExport($"TheWorld.PersistentLevel.{pawn.BioPawnName}");
            var referencingEntries = originalPawn.GetEntriesThatReferenceThisOne();

            foreach (var reference in referencingEntries)
            {
                if (reference.Key is ExportEntry referencingExp)
                {
                    if (referencingExp.ClassName == "SeqVar_Object")
                    {
                        ConvertHardReferenceToSoft(referencingExp, $"NPC_{pawn.NPCName}", options);
                    }
                    else if (referencingExp.ClassName == "BioSeqEvt_OnPlayerActivate")
                    {
                        AttachNPCToEvent(pawn, referencingExp, options);
                    }
                    else if (referencingExp.ClassName == "Level")
                    {
                        // Don't care
                    }
                    else if (!referencingExp.InstancedFullPath.StartsWith(originalPawn.InstancedFullPath))
                    {
                        // It's not a child
                        options.SetStatusText($"Out of tree reference: {referencingExp.InstancedFullPath}");
                    }
                }
            }

            // Install handshakes
            InstallHandshakes(package, pawn, options);


            // Remove the actor we have now moved to another package
            package.RemoveFromLevelActors(originalPawn);
            EntryPruner.TrashEntriesAndDescendants([originalPawn]);
        }

        private static void InstallHandshakes(IMEPackage package, FrameworkPawn pawn, VTestOptions options)
        {

            if (pawn.RemoteEventNamePrefix != null)
            {
                // Generate a remote event listener
                // Somewhere else in vtest will have to signal this.
                var mainseq = package.FindExport("TheWorld.PersistentLevel.Main_Sequence");
                var hookup = SequenceObjectCreator.CreateSeqEventRemoteActivated(mainseq, $"{pawn.RemoteEventNamePrefix}{pawn.NPCName}", options.cache);

                // Install standalone
                var handshakeSeq = GenerateHandshakeSequence(mainseq, pawn, options);
                KismetHelper.InsertActionAfter(hookup, "Out", handshakeSeq, 0, "Ready");
                KismetHelper.RemoveOutputLinks(handshakeSeq, true); // Completely remove
            }

            // Install all handshake MITM nodes
            foreach (var handshake in pawn.Handshakes)
            {
                ExportEntry hookup = package.FindExport(handshake.HookupIFP);
                if (hookup == null)
                    Debugger.Break();

                var seq = KismetHelper.GetParentSequence(hookup);

                // MITM the handshake
                var handshakeSeq = GenerateHandshakeSequence(seq, pawn, options);
                KismetHelper.InsertActionAfter(hookup, handshake.OutLinkName, handshakeSeq, 0, "Ready");
            }
        }

        /// <summary>
        /// Makes specific corrections to packages that need some logic changed
        /// </summary>
        /// <param name="package"></param>
        private static void PreCorrectPackage(IMEPackage package)
        {
            if (package.FileNameNoExtension == "BIOA_PRC2_CCSIM01_DSG")
            {
                // Change ordering of Bryant's gun being removed
                var levelLoaded = package.FindExport("TheWorld.PersistentLevel.Main_Sequence.SeqEvent_LevelLoaded_2");
                if (levelLoaded != null)
                {
                    KismetHelper.RemoveFromSequence(levelLoaded, true);
                }

                // This moves it after level loaded / handshake and fires only when the pawn is actually being activated
                var modifyActivate = package.FindExport("TheWorld.PersistentLevel.Main_Sequence.BioSeqAct_ModifyPropertyPawn_3");
                var setWeapon = package.FindExport("TheWorld.PersistentLevel.Main_Sequence.BioSeqAct_SetWeapon_0");
                var pmCheckState = package.FindExport("TheWorld.PersistentLevel.Main_Sequence.BioSeqAct_PMCheckState_6");
                KismetHelper.ChangeOutputLink(modifyActivate, 0, 0, setWeapon.UIndex);
                KismetHelper.CreateOutputLink(setWeapon, "Success", pmCheckState);
                KismetHelper.CreateOutputLink(setWeapon, "Failed", pmCheckState);

            }
            if (package.FileNameNoExtension == "BIOA_PRC2_CCSIM04_DSG")
            {
                // This breaks frameworking for ahern
                // Since this package has multiple frameworks we need to precorrect it only once.
                var levelLoaded = package.FindExport("TheWorld.PersistentLevel.Main_Sequence.SeqEvent_LevelLoaded_0");
                if (levelLoaded != null)
                {
                    KismetHelper.RemoveFromSequence(levelLoaded, true);
                }

                return;
            }

            if (package.FileNameNoExtension == "BIOA_PRC2_CCMAIN_CONV")
            {
                // Level Loaded hides Ahern
                KismetHelper.RemoveFromSequence(package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SequenceReference_2.Sequence_982.SeqEvent_LevelLoaded_0"), true);
                KismetHelper.RemoveFromSequence(package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SequenceReference_2.Sequence_982.BioSeqAct_ModifyPropertyPawn_4"), true);
                KismetHelper.RemoveFromSequence(package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SequenceReference_2.Sequence_982.SeqVar_Bool_4"), true);

            }
        }

        private static ExportEntry GenerateHandshakeSequence(ExportEntry parentSequence, FrameworkPawn pawn,
            VTestOptions options)
        {
            var seq = SequenceObjectCreator.CreateSequence(parentSequence, $"Handshake_{pawn.NPCName}", options.cache);
            var seqIn = SequenceDesigner.CreateInput(seq, "In", options.cache);
            var readyOut = SequenceDesigner.CreateOutput(seq, "Ready", options.cache);
            var pawnRef = SequenceObjectCreator.CreateFindObject(seq, $"NPC_{pawn.NPCName}", cache: options.cache);

            var poll = SequenceObjectCreator.CreateActivateRemoteEvent(seq, $"Poll_NPC_{pawn.NPCName}", options.cache);
            var live = SequenceObjectCreator.CreateSeqEventRemoteActivated(seq, $"Live_NPC_{pawn.NPCName}",
                options.cache);
            var gate = SequenceObjectCreator.CreateGate(seq, options.cache);
            gate.WriteProperty(new BoolProperty(false, "bOpen"));

            var setLoc = SequenceObjectCreator.CreateSetLocation(seq, pawnRef, cache: options.cache);
            if (pawn.Location != null)
            {
                pawn.Location.Name = "LocationValue";
                setLoc.WriteProperty(new BoolProperty(true, "bSetLocation"));
                setLoc.WriteProperty(pawn.Location);
            }

            setLoc.WriteProperty(new BoolProperty(true, "bSetRotation"));
            if (pawn.Rotation != null)
            {
                pawn.Rotation.Name = "RotationValue";
                setLoc.WriteProperty(pawn.Rotation);
            }
            else
            {
                // Force rotate back to zero
                setLoc.WriteProperty(CommonStructs.RotatorProp(0, 0, 0, "RotationValue"));
            }

            var modifyPawn =
                VTestKismet.AddHelperObjectToSequence(seq, "VTestHelper.BioSeqAct_ModifyPropertyPawn_0", options);
            KismetHelper.CreateVariableLink(modifyPawn, "Target", pawnRef);
            KismetHelper.CreateVariableLink(modifyPawn, "Active",
                SequenceObjectCreator.CreateBool(seq, true, options.cache));
            // We don't want to edit the tag, but we're going to just leave this commented here
            //KismetHelper.CreateVariableLink(modifyPawn, "Tag", SequenceObjectCreator.CreateName(seq, $"NPC_{pawn.NPCName}", options.cache));


            // Logic
            KismetHelper.CreateOutputLink(seqIn, "Out", poll); // Sequence activation -> Poll
            KismetHelper.CreateOutputLink(poll, "Out", gate, 1); // Activate Remote Event -> Gate Open
            KismetHelper.CreateOutputLink(live, "Out", gate); // Remote Event -> Gate In
            KismetHelper.CreateOutputLink(gate, "Out", gate, 2); // Close self once passed through
            KismetHelper.CreateOutputLink(gate, "Out", setLoc); // Gate output -> SetLocation
            KismetHelper.CreateOutputLink(setLoc, "Out", modifyPawn); // SetLocation -> ModifyPropertyPawn
            KismetHelper.CreateOutputLink(modifyPawn, "Out", readyOut); // SetLocation -> ModifyPropertyPawn

            if (pawn.Comment != null)
            {
                KismetHelper.SetComment(seq, pawn.Comment);
            }

            return seq;
        }

        /// <summary>
        /// Converts a SeqVar_Object reference to a FindObjectByTag.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="tagToFind"></param>
        /// <param name="options"></param>
        private static void ConvertHardReferenceToSoft(ExportEntry node, string tagToFind, VTestOptions options)
        {
            var num = node.ObjectName.Number;
            var name = new NameReference("BioSeqVar_ObjectFindByTag", num);
            var baseIFP = node.ParentInstancedFullPath;

            while (node.FileRef.FindExport($"{baseIFP}.{name.Instanced}") != null)
            {
                name = new NameReference("BioSeqVar_ObjectFindByTag", num++);
            }

            node.ObjectName = name;
            node.Class = EntryImporter.EnsureClassIsInFile(node.FileRef, "BioSeqVar_ObjectFindByTag",
                new RelinkerOptionsPackage() { Cache = options.cache });
            node.RemoveProperty("ObjValue");
            node.WriteProperty(new StrProperty(tagToFind, "m_sObjectTagToFind"));
        }

        private static void UpdateTriggerStreams(VTestOptions options)
        {
            var sourcePath = Path.Combine(VTestPaths.VTest_FinalDestDir, $"BIOA_PRC2.pcc");
            if (!File.Exists(sourcePath))
            {
#if !DEBUG
throw new Exception("This must be fixed for release!");
#else
                options.SetStatusText($"   Could not find package BIOA_PRC2, skipping trigger stream update");
                return;
#endif
            }

            var package = MEPackageHandler.OpenMEPackage(sourcePath);


            // Build map of each level, and if it appears in the list, the value of other things to add.
            var npcLevels = new CaseInsensitiveDictionary<List<string>>();
            foreach (var npc in NPCsToFramework)
            {
                if (npc.GenerateBioNPC)
                {
                    var lsk = ExportCreator.CreateExport(package, "LevelStreamingKismet", "LevelStreamingKismet", package.GetLevel(), cache: options.cache);
                    lsk.WriteProperty(new NameProperty($"BIONPC_{npc.NPCName}", "PackageName"));
                }

                var levelSource = npc.LevelSource;
                if (levelSource.EndsWith("_dsg", StringComparison.OrdinalIgnoreCase))
                {
                    // Remove _dsg as game loads these automatically it seems, we have to use the base name
                    levelSource = levelSource[..^4];
                }

                if (!npcLevels.TryGetValue(levelSource, out var list))
                {
                    list = new List<string>();
                    npcLevels[levelSource] = list;
                }

                list.Add($"BIONPC_{npc.NPCName}");
            }


            foreach (var bts in package.Exports.Where(x => x.ClassName == "BioTriggerStream").ToList())
            {
                if (bts.UIndex == 323)
                {

                }

                var streamingStates = bts.GetProperty<ArrayProperty<StructProperty>>("StreamingStates");
                foreach (var state in streamingStates)
                {
                    var visibleChunkNames = state.GetProp<ArrayProperty<NameProperty>>("VisibleChunkNames");
                    var loadChunkNames = state.GetProp<ArrayProperty<NameProperty>>("LoadChunkNames");
                    var inChunkName = state.GetProp<NameProperty>("InChunkName");
                    FrameworkChunks(visibleChunkNames, npcLevels, inChunkName?.Value);
                    FrameworkChunks(loadChunkNames, npcLevels, null, visibleChunkNames);
                }

                bts.WriteProperty(streamingStates);
            }

            // We updated LSKs, now add them
            VTestUtility.RebuildStreamingLevels(package);

            package.Save();
        }

        private static void FrameworkChunks(ArrayProperty<NameProperty> chunkList,
            CaseInsensitiveDictionary<List<string>> npcLevels, NameReference? inChunkName,
            ArrayProperty<NameProperty> chunksToNotAdd = null)
        {
            List<NameReference> extraNames = new List<NameReference>();

            foreach (var chunk in chunkList.ToList())
            {
                if (npcLevels.TryGetValue(chunk.Value, out var list))
                {
                    // Add the NPC packages
                    extraNames.AddRange(list.Select(x => new NameReference(x)));
                }
            }

            // Also add the ones for 'InChunkName', because that appears to be the current visible chunk you are in... or on... not really sure what this means
            if (inChunkName.HasValue && npcLevels.TryGetValue(inChunkName.Value, out var list2))
            {
                extraNames.AddRange(list2.Select(x => new NameReference(x)));
            }

            // Filter out the second list if passed in (we don't want items both in Visible and Loaded at the same time)
            if (chunksToNotAdd != null)
            {
                var namesToNoAdd = chunksToNotAdd.Select(x => x.Value).ToList();
                extraNames = extraNames.Except(namesToNoAdd).ToList();
            }

            // Filter out duplicates
            extraNames = extraNames.Distinct().ToList();
            chunkList.InsertRange(0, extraNames.Select(x => new NameProperty(x)));
        }

        private static void AttachNPCToEvent(FrameworkPawn pawn, ExportEntry seqEvent, VTestOptions options)
        {
            var sequence = KismetHelper.GetParentSequence(seqEvent);
            var levelLoaded = SequenceObjectCreator.CreateLevelLoaded(sequence, options.cache);
            var liveNpc = SequenceObjectCreator.CreateSeqEventRemoteActivated(sequence, $"Live_NPC_{pawn.NPCName}", options.cache);
            var gate = SequenceObjectCreator.CreateGate(sequence, options.cache);
            var findObj = SequenceObjectCreator.CreateFindObject(sequence, $"NPC_{pawn.NPCName}", cache: options.cache);
            var attachToEvent = SequenceObjectCreator.CreateAttachToEvent(sequence, findObj, seqEvent, options.cache);

            KismetHelper.CreateOutputLink(levelLoaded, "Loaded and Visible", gate, 1); // Whenever level becomes visible, open the gate, as going to loaded state seems to wipe out the attachment
            KismetHelper.CreateOutputLink(liveNpc, "Out", gate);
            KismetHelper.CreateOutputLink(gate, "Out", gate, 2); // Close self
            KismetHelper.CreateOutputLink(gate, "Out", attachToEvent);

            // Remove this property; we will use our own instead
            seqEvent.RemoveProperty("Originator");
        }
    }
}