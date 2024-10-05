using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Gammtek.Extensions;
using LegendaryExplorerCore.Gammtek.Extensions.Collections.Generic;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Pathing;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using static LegendaryExplorerCore.Packages.CompressionHelper;
using static Microsoft.IO.RecyclableMemoryStreamManager;

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

        /// <summary>
        /// Where to hookup the handshake logic
        /// </summary>
        public string HookupIFP { get; set; }

        /// <summary>
        /// If we aren't using HookupIFP, we trigger on remote event. This is the prefix to the NPCName that will signal the incoming remote event.
        /// </summary>
        public string RemoteEventNamePrefix { get; set; }

        /// <summary>
        /// Comment to attach to the handshake in Kismet
        /// </summary>
        public string Comment { get; set; }
    }

    class FrameworkSignal
    {
        public string PackageFile { get; set; }
        public string HookupIFP { get; set; }
        public string EventName { get; set; }
        /// <summary>
        /// Name of the outlink to use on hookup
        /// </summary>
        public string OutLinkName { get; set; } = "Out";
    }

    /// <summary>
    /// Handles frameworking of NPCs
    /// </summary>
    public static class VTestFramework
    {
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

        private static readonly FrameworkSignal[] NeededSetupSignals =
        [
            new () {PackageFile = "BIOA_PRC2_CCLOBBY", EventName = "GuardpostRivalVidinos", HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.SeqEvent_LevelLoaded_1"},
            new () {PackageFile = "BIOA_PRC2_CCLOBBY", EventName = "GuardpostVegasTurianGuard3", HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.SeqEvent_LevelLoaded_1"}
        ];
        /// <summary>
        /// Triggers pawns to move around to their needed locations via RemoteEvent triggers
        /// </summary>
        /// <param name="options"></param>
        public static void AddPawnSetupSignaling(VTestOptions options)
        {
            foreach (var setupSignal in NeededSetupSignals)
            {
                options.SetStatusText($"Adding framework signal {setupSignal.EventName} in {setupSignal.PackageFile}");
                var packagePAth = Path.Combine(VTestPaths.VTest_FinalDestDir, $"{setupSignal.PackageFile}.pcc");
                var package = MEPackageHandler.OpenMEPackage(packagePAth);

                var hookup = package.FindExport(setupSignal.HookupIFP);
                var sequence = KismetHelper.GetParentSequence(hookup);
                var are = SequenceObjectCreator.CreateActivateRemoteEvent(sequence, setupSignal.EventName, options.cache);

                KismetHelper.InsertActionAfter(hookup, setupSignal.OutLinkName, are, 0, "Out");
                package.Save();
            }
        }

        private static readonly CaseInsensitiveDictionary<string> FindObjectByTagFixes = new()
        {
            {"BIOA_PRC2_TUR_Guard01", "NPC_VegasTurianGuard3"},
            {"prc2_tur_jerk_entry", "NPC_RivalVidinos"},
            {"prc2_ochren", "NPC_Ochren"},
            {"PRC2_TUR_OldWarrior", "NPC_Dahga"},
            {"PRC2_HUM_Ahern", "NPC_Ahern"},
        };

        public static readonly FrameworkPawn[] NPCsToFramework =
        [
            // We might as well just framework everyone.

            // Two turian guards that salute you on entry
            new() { LevelSource = "BIOA_PRC2_CCLOBBY", BioPawnName = "BioPawn_0", NPCName = "VegasTurianGuard1", HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.SeqEvent_LevelLoaded_1", Comment="Entry: Saluting guard"},
            new() { LevelSource = "BIOA_PRC2_CCLOBBY", BioPawnName = "BioPawn_10", NPCName = "VegasTurianGuard2", HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.SeqEvent_LevelLoaded_1", Comment="Entry: Saluting guard"},

            // Mid Room Crew
            new() { LevelSource = "BIOA_PRC2_CCMID", BioPawnName = "BioPawn_3", NPCName = "VegasHumanCrew1", HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Prefabs.SeqEvent_LevelLoaded_0"},
            new() { LevelSource = "BIOA_PRC2_CCMID", BioPawnName = "BioPawn_5", NPCName = "VegasHumanCrew2", HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Prefabs.SeqEvent_LevelLoaded_0" },
            new() { LevelSource = "BIOA_PRC2_CCMID", BioPawnName = "BioPawn_6", NPCName = "VegasHumanCrew3", HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Prefabs.SeqEvent_LevelLoaded_0" },
            new() { LevelSource = "BIOA_PRC2_CCMID", BioPawnName = "BioPawn_7", NPCName = "VegasHumanCrew4", HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Prefabs.SeqEvent_LevelLoaded_0" },

            // Competitors in the room at the tables
            // These are the actual names that appear on the scoreboard
            new() { LevelSource = "BIOA_PRC2_CCSIM01_DSG", BioPawnName = "BioPawn_0", NPCName = "RivalProchor", HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Prefabs.SeqEvent_LevelLoaded_2"},
            new() { LevelSource = "BIOA_PRC2_CCSIM01_DSG", BioPawnName = "BioPawn_1", NPCName = "RivalMinket", HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Prefabs.SeqEvent_LevelLoaded_2" },
            new() { LevelSource = "BIOA_PRC2_CCSIM01_DSG", BioPawnName = "BioPawn_2", NPCName = "RivalBryant", HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.SeqEvent_LevelLoaded_2"},
            new() { LevelSource = "BIOA_PRC2_CCSIM01_DSG", BioPawnName = "BioPawn_3", NPCName = "RivalCSell1", HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Prefabs.SeqEvent_LevelLoaded_2" },
            new() { LevelSource = "BIOA_PRC2_CCSIM01_DSG", BioPawnName = "BioPawn_5", NPCName = "RivalSophomin", HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Prefabs.SeqEvent_LevelLoaded_2" },
            new() { LevelSource = "BIOA_PRC2_CCSIM01_DSG", BioPawnName = "BioPawn_7", NPCName = "RivalCSell2", HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Prefabs.SeqEvent_LevelLoaded_2" }, // Looks like tag was never updated.
            new() { LevelSource = "BIOA_PRC2_CCSIM01_DSG", BioPawnName = "BioPawn_8", NPCName = "RivalChior", HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Prefabs.SeqEvent_LevelLoaded_2"},

            // Krogan doofus
            new() { LevelSource = "BIOA_PRC2_CCSIM02_DSG", BioPawnName = "BioPawn_3", NPCName = "RivalBurrum", HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.SeqEvent_LevelLoaded_0", Comment="Krogran Frat Boy"},

            new() { LevelSource = "BIOA_PRC2_CCSIM04_DSG", BioPawnName = "BioPawn_11", NPCName = "Ochren", HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Prefabs.SeqEvent_LevelLoaded_0", Comment="Salarian Joker" },

            new() {
                LevelSource = "BIOA_PRC2_CCMAIN_CONV",
                BioPawnName = "BioPawn_0",
                NPCName = "Dahga",
                HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.SeqAct_Log_2"
            },
            // Guardpost - Vidinos
            new() { 
                LevelSource = "BIOA_PRC2_CCMAIN_CONV", 
                BioPawnName = "BioPawn_1", 
                NPCName = "RivalVidinos", 
                // HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.SeqEvent_LevelLoaded_1", 
                RemoteEventNamePrefix = "Guardpost"
            },
            // Guardpost - Person across from Vidinos
            new() { LevelSource = "BIOA_PRC2_CCMAIN_CONV",
                BioPawnName = "BioPawn_2", NPCName = "VegasTurianGuard3",
                RemoteEventNamePrefix = "Guardpost",
                Comment = "Entry: Guard across from Vidinos"
            },
            
            new() { 
                LevelSource = "BIOA_PRC2_CCSIM04_DSG", 
                BioPawnName = "BioPawn_7", 
                NPCName = "Ahern", 
                GenerateBioNPC = false, 
                HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.BioSeqAct_BlackScreen_1", 
                Comment="Ahern: My Special Mission Introduction"
            },

            // Ahern - Observation Deck next to Ochren
            new()
            {
                LevelSource = "BIOA_PRC2_CCMAIN_CONV",
                BioPawnName = "BioPawn_3", 
                NPCName = "Ahern",
                GenerateBioNPC = false,
                // RemoteEventNamePrefix = "AhernPostMissionQuip",
                HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SeqEvent_RemoteEvent_0",
                Comment="Ahern: Post mission quip"

            },
            // Ahern - Standard?
            new() {
                LevelSource = "BIOA_PRC2_CCMAIN_CONV",
                BioPawnName = "BioPawn_4",
                NPCName = "Ahern",
                HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.SeqAct_Log_2",
                Comment = "Ahern: Main location"
            },
            // Vidinos - Near Ocaren
            new()
            {
                LevelSource = "BIOA_PRC2_CCMAIN_CONV",
                BioPawnName = "BioPawn_5",
                NPCName = "RivalVidinos",
                GenerateBioNPC = false,
                HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.Match_End_Cin.SequenceReference_0.Sequence_980.SeqEvent_SequenceActivated_0",
                Comment = "Vidinos: Post mission cutscenes"
            },
            // Bryant guy that we're supposed to care about for some reason
            new()
            {
                LevelSource = "BIOA_PRC2_CCMAIN_CONV", BioPawnName = "BioPawn_8", NPCName = "RivalBryant",
                GenerateBioNPC = false,
                HookupIFP = "TheWorld.PersistentLevel.Main_Sequence.SeqAct_Log_2"
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
            foreach (var npc in NPCsToFramework)
            {
                options.SetStatusText($"  {npc.NPCName} from {npc.LevelSource}");

                var sourcePath = Path.Combine(VTestPaths.VTest_FinalDestDir, $"{npc.LevelSource}.pcc");
                var package = MEPackageHandler.OpenMEPackage(sourcePath);

                FrameworkPackage(package, npc, options);

                package.Save();
            }

            // Step 3: Fix up the existing find object by tags
            foreach (var pName in PackagesToUpdateFindObjects)
            {
                options.SetStatusText($"  Fixing up ObjectFindByTag in {pName}");

                var sourcePath = Path.Combine(VTestPaths.VTest_FinalDestDir, $"{pName}.pcc");
                var package = MEPackageHandler.OpenMEPackage(sourcePath);

                FixupFindObjects(package, options);

                package.Save();
            }


            // Final step: Update trigger streams
            options.SetStatusText("Updating trigger streams");
            UpdateTriggerStreams(options);
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
            ExportEntry hookup = null;
            if (pawn.HookupIFP != null)
            {
                // Hook directly up to where we say
                hookup = package.FindExport(pawn.HookupIFP);
            }
            else
            {
                // Generate a remote event listener
                // Somewhere else in vtest will have to signal this.
                var mainseq = package.FindExport("TheWorld.PersistentLevel.Main_Sequence");
                hookup = SequenceObjectCreator.CreateSeqEventRemoteActivated(mainseq, $"{pawn.RemoteEventNamePrefix}{pawn.NPCName}", options.cache);
            }

            if (hookup == null)
                Debugger.Break();

            var seq = KismetHelper.GetParentSequence(hookup);
            ExportEntry pawnRef = null;

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

            // MITM the handshake
            var handshake = GenerateHandshakeSequence(seq, pawn, options);
            KismetHelper.InsertActionAfter(hookup, "Out", handshake, 0, "Ready");

            // Remove the actor we have now moved to another package
            hookup.FileRef.RemoveFromLevelActors(originalPawn);
            EntryPruner.TrashEntriesAndDescendants([originalPawn]);
        }

        /// <summary>
        /// Makes specific corrections to packages that need some logic changed
        /// </summary>
        /// <param name="package"></param>
        private static void PreCorrectPackage(IMEPackage package)
        {
            if (package.FileNameNoExtension == "BIOA_PRC2_CCSIM04_DSG")
            {
                // This breaks frameworking for ahern
                // Since this package has multiple frameworks we need to precorrect it only once.
                var levelLoaded = package.FindExport("TheWorld.PersistentLevel.Main_Sequence.SeqEvent_LevelLoaded_0");
                if (levelLoaded != null)
                {
                    KismetHelper.RemoveFromSequence(levelLoaded, true);
                }
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
                setLoc.WriteProperty(CommonStructs.RotatorProp(0,0,0, "RotationValue"));
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
            node.ObjectName = new NameReference("BioSeqVar_ObjectFindByTag", node.ObjectName.Number);
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