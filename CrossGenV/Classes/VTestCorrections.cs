using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using LegendaryExplorerCore.Unreal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Pathing;
using System.IO;
using CrossGenV.Classes.Levels;
using LegendaryExplorerCore.Gammtek.Extensions.Collections.Generic;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.GameFilesystem;

namespace CrossGenV.Classes
{
    /// <summary>
    /// Standalone file corrections for VTest
    /// </summary>
    public class VTestCorrections
    {
        // ME1 -> LE1 Prefab's Sequence class was changed to a subclass. No different props though.
        private static void CorrectPrefabSequenceClass(IMEPackage le1File)
        {
            foreach (var le1Exp in le1File.Exports)
            {
                if (le1Exp.IsA("Prefab"))
                {
                    var prefabSeqObj = le1Exp.GetProperty<ObjectProperty>("PrefabSequence");
                    if (prefabSeqObj != null && prefabSeqObj.ResolveToEntry(le1File) is ExportEntry export)
                    {
                        var prefabSeqClass = le1File.FindImport("Engine.PrefabSequence");
                        if (prefabSeqClass == null)
                        {
                            var seqClass = le1File.FindImport("Engine.Sequence");
                            prefabSeqClass = new ImportEntry(le1File, seqClass.Parent?.UIndex ?? 0, "PrefabSequence") { PackageFile = seqClass.PackageFile, ClassName = "Class" };
                            le1File.AddImport(prefabSeqClass);
                        }

                        Debug.WriteLine($"Corrected Sequence -> PrefabSequence class type for {le1Exp.InstancedFullPath}");
                        export.Class = prefabSeqClass;
                    }
                }
                else if (le1Exp.IsA("PrefabInstance"))
                {
                    var seq = le1Exp.GetProperty<ObjectProperty>("SequenceInstance")?.ResolveToEntry(le1File) as ExportEntry;
                    if (seq != null && seq.ClassName == "Sequence")
                    {
                        var prefabSeqClass = le1File.FindImport("Engine.PrefabSequence");
                        if (prefabSeqClass == null)
                        {
                            var seqClass = le1File.FindImport("Engine.Sequence");
                            prefabSeqClass = new ImportEntry(le1File, seqClass.Parent?.UIndex ?? 0, "PrefabSequence") { PackageFile = seqClass.PackageFile, ClassName = "Class" };
                            le1File.AddImport(prefabSeqClass);
                        }

                        Debug.WriteLine($"Corrected Sequence -> PrefabSequence class type for {le1Exp.InstancedFullPath}");
                        seq.Class = prefabSeqClass;
                    }
                }
            }
        }

        private static void CorrectPathfindingNetwork(IMEPackage me1File, IMEPackage le1File, VTestOptions vTestOptions)
        {
            var le1PL = le1File.FindExport("TheWorld.PersistentLevel");
            if (le1PL == null)
                return; // This file doesn't have a level
            Level me1L = ObjectBinary.From<Level>(me1File.FindExport("TheWorld.PersistentLevel"));
            Level le1L = ObjectBinary.From<Level>(le1PL);

            //PropertyCollection mcs = new PropertyCollection();
            //mcs.AddOrReplaceProp(new FloatProperty(400, "Radius"));
            //mcs.AddOrReplaceProp(new FloatProperty(400, "Height"));
            //StructProperty maxPathSize = new StructProperty("Cylinder", mcs, "MaxPathSize");

            // NavList Chain start and end
            if (me1L.NavListEnd != 0 && le1File.FindExport(me1File.GetUExport(me1L.NavListEnd).InstancedFullPath) is { } matchingNavEnd)
            {
                le1L.NavListEnd = matchingNavEnd.UIndex;

                if (me1L.NavListStart != 0 && le1File.FindExport(me1File.GetUExport(me1L.NavListStart).InstancedFullPath) is { } matchingNavStart)
                {
                    le1L.NavListStart = matchingNavStart.UIndex;
                    while (matchingNavStart != null)
                    {
                        int uindex = matchingNavStart.UIndex;
                        var props = matchingNavStart.GetProperties();
                        //props.AddOrReplaceProp(maxPathSize);
                        var next = props.GetProp<ObjectProperty>("nextNavigationPoint");
                        //matchingNavStart.WriteProperties(props);
                        matchingNavStart = next?.ResolveToEntry(le1File) as ExportEntry;
                        if (matchingNavStart == null && uindex != matchingNavEnd.UIndex)
                        {
                            Debugger.Break();
                        }
                    }
                }
            }

            // CoverList Chain start and end
            if (me1L.CoverListEnd != 0 && le1File.FindExport(me1File.GetUExport(me1L.CoverListEnd).InstancedFullPath) is { } matchingCoverEnd)
            {
                le1L.CoverListEnd = matchingCoverEnd.UIndex;

                if (me1L.CoverListStart != 0 && le1File.FindExport(me1File.GetUExport(me1L.CoverListStart).InstancedFullPath) is { } matchingCoverStart)
                {
                    le1L.CoverListStart = matchingCoverStart.UIndex;
                    while (matchingCoverStart != null)
                    {
                        int uindex = matchingCoverStart.UIndex;
                        var props = matchingCoverStart.GetProperties();
                        //props.AddOrReplaceProp(maxPathSize);
                        var next = props.GetProp<ObjectProperty>("NextCoverLink");
                        //matchingNavStart.WriteProperties(props);
                        matchingCoverStart = next?.ResolveToEntry(le1File) as ExportEntry;
                        if (matchingCoverStart == null && uindex != matchingCoverEnd.UIndex)
                        {
                            Debugger.Break();
                        }
                    }
                }
            }

            // Cross level actors
            foreach (var exportIdx in me1L.CrossLevelActors)
            {
                var me1E = me1File.GetUExport(exportIdx);
                if (le1File.FindExport(me1E.InstancedFullPath) is { } crossLevelActor)
                {
                    le1L.CrossLevelActors.Add(crossLevelActor.UIndex);
                }
            }

            // Regenerate the 'End' struct cause it will have ported wrong
            CorrectReachSpecs(me1File, le1File);
            CorrectBioWaypointSet(me1File, le1File, vTestOptions); // Has NavReference -> ActorReference
            le1PL.WriteBinary(le1L);
        }

        /// <summary>
        /// Corrects UDK/ME1/ME2 reachspec system to ME3 / LE
        /// </summary>
        /// <param name="me1File"></param>
        /// <param name="le1File"></param>
        public static void CorrectReachSpecs(IMEPackage me1File, IMEPackage le1File)
        {

            // Have to do LE1 -> ME1 for references as not all reachspecs may have been ported
            foreach (var le1Exp in le1File.Exports.Where(x => x.IsA("ReachSpec")))
            {
                var le1End = le1Exp.GetProperty<StructProperty>("End");
                if (le1End != null)
                {
                    var me1Exp = me1File.FindExport(le1Exp.InstancedFullPath);
                    var me1End = me1Exp.GetProperty<StructProperty>("End");
                    var le1Props = le1Exp.GetProperties();
                    le1Props.RemoveNamedProperty("End");

                    PropertyCollection newEnd = new PropertyCollection();
                    newEnd.Add(me1End.GetProp<StructProperty>("Guid"));

                    var me1EndEntry = me1End.GetProp<ObjectProperty>("Nav");
                    me1EndEntry ??= me1End.GetProp<ObjectProperty>("Actor"); // UDK uses 'Actor' but it's in wrong position
                    if (me1EndEntry != null)
                    {
                        newEnd.Add(new ObjectProperty(le1File.FindExport(me1File.GetUExport(me1EndEntry.Value).InstancedFullPath).UIndex, "Actor"));
                    }
                    else
                    {
                        newEnd.Add(new ObjectProperty(0, "Actor")); // This is probably cross level or end of chain
                    }

                    StructProperty nes = new StructProperty("ActorReference", newEnd, "End", true);
                    le1Props.AddOrReplaceProp(nes);
                    le1Exp.WriteProperties(le1Props);
                    le1Exp.WriteBinary(new byte[0]); // When porting from UDK there's some binary data. This removes it

                    // Test properties
                    le1Exp.GetProperties();
                }
            }
        }

        public static void CorrectBioWaypointSet(IMEPackage me1File, IMEPackage le1File, VTestOptions vTestOptions)
        {
            foreach (var lbwps in le1File.Exports.Where(x => x.ClassName == "BioWaypointSet"))
            {
                var matchingMe1 = me1File.FindExport(lbwps.InstancedFullPath);
                var mWaypointRefs = matchingMe1.GetProperty<ArrayProperty<StructProperty>>("WaypointReferences");
                var lWaypointRefs = lbwps.GetProperty<ArrayProperty<StructProperty>>("WaypointReferences");
                lWaypointRefs.Clear(); // We're going to reconstruct these

                foreach (var mWay in mWaypointRefs)
                {
                    var le1Props = GlobalUnrealObjectInfo.getDefaultStructValue(MEGame.LE1, "ActorReference", true, le1File, packageCache: vTestOptions.cache);
                    VTestCover.ConvertNavRefToActorRef(mWay, le1Props, me1File, le1File, vTestOptions);
                    lWaypointRefs.Add(new StructProperty("ActorReference", le1Props, isImmutable: true));
                }
                lbwps.WriteProperty(lWaypointRefs);
            }
        }

        /// <summary>
        /// Changes weapon references in Geth 2DAs to use our new weapon entries that include HoloWipe VFX. Needs proper 2DA to work.
        /// </summary>
        /// <param name="le1File"></param>
        /// <param name="vTestOptions"></param>
        private static void CorrectGethEquipment2DAs(IMEPackage le1File, VTestOptions vTestOptions)
        {
            var tables = le1File.Exports.Where(e =>
                e.ClassName == "Bio2DANumberedRows" && e.ParentName == "Geth" &&
                e.ObjectName.ToString().Contains("_Equipment"));

            foreach (var t in tables)
            {
                var binary = ObjectBinary.From<Bio2DABinary>(t);
                var initial = binary.Cells[0].IntValue;
                binary.Cells[0].IntValue = initial switch
                {
                    22 => 630,
                    23 => 631,
                    423 => 632,
                    519 => 633,
                    _ => initial
                };
                t.WriteBinary(binary);
                t.ObjectName = "VTEST_" + t.ObjectName;
            }
        }

        private static void CorrectSequenceObjects(ExportEntry seq, VTestOptions vTestOptions)
        {
            // Set ObjInstanceVersions to LE value
            if (seq.IsA("SequenceObject"))
            {
                if (LE1UnrealObjectInfo.ObjectInfo.SequenceObjects.TryGetValue(seq.ClassName, out var soi))
                {
                    seq.WriteProperty(new IntProperty(soi.ObjInstanceVersion, "ObjInstanceVersion"));
                }
                else
                {
                    Debug.WriteLine($"SequenceCorrection: Didn't correct {seq.UIndex} {seq.ObjectName}, not in LE1 ObjectInfo SequenceObjects");
                }

                var children = seq.GetChildren();
                foreach (var child in children)
                {
                    if (child is ExportEntry chExp)
                    {
                        CorrectSequenceObjects(chExp, vTestOptions);
                    }
                }
            }

            // Fix extra four bytes after SeqAct_Interp
            if (seq.ClassName == "SeqAct_Interp")
            {
                seq.WriteBinary(Array.Empty<byte>());
            }

            if (seq.ClassName == "SeqAct_SetInt")
            {
                seq.WriteProperty(new BoolProperty(true, "bIsUpdated"));
            }


            // Fix missing PropertyNames on VariableLinks
            if (seq.IsA("SequenceOp"))
            {
                var varLinks = seq.GetProperty<ArrayProperty<StructProperty>>("VariableLinks");
                if (varLinks is null) return;
                foreach (var t in varLinks.Values)
                {
                    string desc = t.GetProp<StrProperty>("LinkDesc").Value;

                    if (desc == "Target" && seq.ClassName == "SeqAct_SetBool")
                    {
                        t.Properties.AddOrReplaceProp(new NameProperty("Target", "PropertyName"));
                    }

                    if (desc == "Value" && seq.ClassName == "SeqAct_SetInt")
                    {
                        t.Properties.AddOrReplaceProp(new NameProperty("Values", "PropertyName"));
                    }
                }

                seq.WriteProperty(varLinks);
            }
        }

        private static void CorrectSequences(IMEPackage le1File, VTestOptions vTestOptions)
        {
            // Find sequences that aren't in other sequences
            foreach (var seq in le1File.Exports.Where(e => e is { ClassName: "Sequence" } && !e.Parent.IsA("SequenceObject")))
            {
                CorrectSequenceObjects(seq, vTestOptions);
            }
        }

        private static void FixPinkVisorMaterial(IMEPackage package)
        {
            ExportEntry visorMatInstance = package.FindExport(@"BIOG_HMM_HGR_HVY_R.BRT.HMM_BRT_HVYa_MAT_1a");
            if (visorMatInstance is not null)
            {
                var vectorParameterValues = visorMatInstance.GetProperty<ArrayProperty<StructProperty>>("VectorParameterValues");
                foreach (var param in vectorParameterValues.Values)
                {
                    var name = param.GetProp<NameProperty>("ParameterName").Value;
                    if (name == "HGR_Colour_01")
                    {

                        param.Properties.AddOrReplaceProp(CommonStructs.LinearColorProp(0.07058824f, 0.08235294f, 0.09019608f, 0, "ParameterValue"));
                    }
                    else if (name == "HGR_Colour_02")
                    {
                        param.Properties.AddOrReplaceProp(CommonStructs.LinearColorProp(0.05882353f, 0.07058824f, 0.08235294f, 0, "ParameterValue"));
                    }
                }
                visorMatInstance.WriteProperty(vectorParameterValues);
            }
        }


        private static void CorrectTextures(IMEPackage package)
        {
            foreach (var exp in package.Exports.Where(x => x.IsTexture()))
            {
                var props = exp.GetProperties();
                var texinfo = ObjectBinary.From(exp) as UTexture2D;
                var numMips = texinfo.Mips.Count;
                var ns = props.GetProp<BoolProperty>("NeverStream");
                int lowMipCount = 0;
                for (int i = numMips - 1; i >= 0; i--)
                {
                    if (lowMipCount > 6 && (ns == null || ns.Value == false) && texinfo.Mips[i].IsLocallyStored && texinfo.Mips[i].StorageType != StorageTypes.empty)
                    {
                        exp.WriteProperty(new BoolProperty(true, "NeverStream"));
                        lowMipCount = -100; // This prevents this block from running again
                    }

                    if (texinfo.Mips[i].StorageType == StorageTypes.empty && exp.Parent.ClassName != "TextureCube")
                    {
                        // Strip this empty mip
                        //Debug.WriteLine($"Dropping empty mip {i} in {exp.InstancedFullPath}");
                        //texinfo.Mips.RemoveAt(i);
                    }

                    lowMipCount++;
                }
                exp.WriteBinary(texinfo);

                // Correct the MipTailBaseIdx. It's an indexer thing so it starts at 0
                //exp.WriteProperty(new IntProperty(texinfo.Mips.Count - 1, "MipTailBaseIdx"));

                // Correct the size. Is this required?

            }
        }

        private static string[] assetsToEnsureReferencedInSim = new[]
        {
            "BIOG_GTH_TRO_NKD_R.NKDa.GTH_TRO_NKDa_MDL", // Geth Trooper
            "BIOG_GTH_STP_NKD_R.NKDa.GTH_STP_NKDa_MDL", // Geth Prime
        };

        private static void LevelSpecificPostCorrections(string fName, IMEPackage me1File, IMEPackage le1File, VTestOptions vTestOptions)
        {
            var upperFName = fName.ToUpper();

            // 09/24/2024 - Add additional enemies logic
            VTestAdditionalContent.AddExtraEnemyTypes(le1File, vTestOptions);

            // Semi-global
            switch (upperFName)
            {
                case "BIOA_PRC2_CCAHERN_DSG":
                    {

                        foreach (var exp in le1File.Exports.Where(x => x.ClassName == "Sequence").ToList())
                        {
                            var seqName = exp.GetProperty<StrProperty>("ObjName")?.Value;
                            if (seqName == "SUR_Ahern_Handler")
                            {
                                // Ahern music start right as message plays
                                {
                                    var delay = VTestKismet.FindSequenceObjectByClassAndPosition(exp, "SeqAct_Delay", -7232, -1224);
                                    var remoteEvent = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_ActivateRemoteEvent", vTestOptions.cache);
                                    KismetHelper.AddObjectToSequence(remoteEvent, exp);
                                    KismetHelper.CreateOutputLink(delay, "Finished", remoteEvent);
                                    remoteEvent.WriteProperty(new NameProperty("StartSimMusic", "EventName"));
                                }

                                // Setup intensity increase 1
                                {
                                    var delay = VTestKismet.FindSequenceObjectByClassAndPosition(exp, "SeqAct_Gate", -2072, -1152);
                                    var remoteEvent = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_ActivateRemoteEvent", vTestOptions.cache);
                                    KismetHelper.AddObjectToSequence(remoteEvent, exp);
                                    KismetHelper.CreateOutputLink(delay, "Out", remoteEvent);
                                    remoteEvent.WriteProperty(new NameProperty("MusicIntensity2", "EventName"));
                                }

                                // Setup texture loading
                                var streamInLocNode = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.SUR_Ahern_Handler.SeqVar_Object_33");
                                VTestKismet.FixSimMapTextureLoading(VTestKismet.FindSequenceObjectByClassAndPosition(exp, "BioSeqAct_Delay", -8501, -1086), vTestOptions, streamInLocNode);

                                // Fix the UNCMineralSurvey to use the LE version instead of the OT version, which doesn't work well

                                var artPlacableUsed = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.SUR_Ahern_Handler.BioSeqEvt_ArtPlaceableUsed_3");
                                KismetHelper.RemoveOutputLinks(artPlacableUsed);
                                var leMineralSurvey = VTestKismet.InstallVTestHelperSequenceViaEvent(le1File, artPlacableUsed.InstancedFullPath, "HelperSequences.REF_UNCMineralSurvey", vTestOptions);
                                KismetHelper.CreateVariableLink(leMineralSurvey, "nMiniGameID", le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.SUR_Ahern_Handler.SeqVar_Int_1"));

                                KismetHelper.CreateOutputLink(leMineralSurvey, "Succeeded", le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.SUR_Ahern_Handler.BioSeqAct_SetRadarDisplay_0"));
                                KismetHelper.CreateOutputLink(leMineralSurvey, "Succeeded", le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.SUR_Ahern_Handler.BioSeqAct_ModifyPropertyArtPlaceable_2"));
                            }
                            else if (seqName == "Spawn_Single_Guy")
                            {
                                RemoveBitExplosionEffect(exp);
                                FixGethFlashlights(exp, vTestOptions); // This is just for consistency
                            }
                            else if (seqName == "OL_Size")
                            {
                                // Fadein is handled by scoreboard DSG
                                var compareBool = VTestKismet.FindSequenceObjectByClassAndPosition(exp, "SeqCond_CompareBool", 8064, 3672);
                                KismetHelper.SkipSequenceElement(compareBool, "True"); // Skip out to true
                            }
                        }

                        // Fix the AI to actually charge. For some reason they don't, maybe AI changed internally when going to LE1
                        var root = le1File.FindExport("BIOA_PRC2_SIM_C.Mercenary");
                        var chargeAi = EntryImporter.EnsureClassIsInFile(le1File, "BioAI_Charge", new RelinkerOptionsPackage() { Cache = vTestOptions.cache });
                        foreach (var exp in le1File.Exports.Where(x => x.idxLink == root.UIndex))
                        {
                            if (!exp.ObjectName.Name.Contains("Sniper"))
                            {
                                exp.WriteProperty(new ObjectProperty(chargeAi, "AIController"));
                            }
                        }
                        // Make memory unique
                        root.ObjectName = "Mercenary_Ahern_Crossgen";

                        // Do not turn off fade to black on map finish
                    }
                    break;
                case "BIOA_PRC2_CCCAVE_L":
                    ReduceDirectionalLights(le1File, 0.1f);
                    break;
                case "BIOA_PRC2_CCLAVA_L":
                    ReduceDirectionalLights(le1File, 0.15f);
                    break;
                case "BIOA_PRC2_CCCRATE_L":
                    ReduceDirectionalLights(le1File, 0.25f);
                    break;
                case "BIOA_PRC2_CCAHERN_ART":
                    ReduceDirectionalLights(le1File, 0.1f);
                    break;
                case "BIOA_PRC2_CCTHAI_L":
                    ReduceDirectionalLights(le1File, 0.6f);
                    break;
                case "BIOA_PRC2_CCCAVE_DSG":
                case "BIOA_PRC2_CCLAVA_DSG":
                case "BIOA_PRC2_CCCRATE_DSG":
                case "BIOA_PRC2_CCTHAI_DSG":
                    // Might need Aherns
                    {
                        VTestCCLevels.PreventSavingOnSimLoad(le1File, vTestOptions);
                        VTestAudio.SetupMusicIntensity(le1File, upperFName, vTestOptions);
                        VTestAudio.SetupKillStreakVO(le1File, vTestOptions);
                        // Force the pawns that will spawn to have their meshes in memory
                        // They are not referenced directly

                        var assetsToReference = le1File.Exports.Where(x => assetsToEnsureReferencedInSim.Contains(x.InstancedFullPath)).ToArray();
                        VTestUtility.AddWorldReferencedObjects(le1File, assetsToReference);

                        foreach (var exp in le1File.Exports.Where(x => x.ClassName == "Sequence").ToList())
                        {
                            var seqName = exp.GetProperty<StrProperty>("ObjName")?.Value;

                            #region Skip broken SeqAct_ActorFactory for bit explosion | Increase Surival Mode Engagement

                            if (seqName == "Spawn_Single_Guy")
                            {
                                RemoveBitExplosionEffect(exp);
                                FixGethFlashlights(exp, vTestOptions);
                                // Increase survival mode engagement by forcing the player to engage with enemies that charge the player.
                                // This prevents them from camping and getting free survival time
                                if (VTestKismet.IsContainedWithinSequenceNamed(exp, "SUR_Respawner") && !VTestKismet.IsContainedWithinSequenceNamed(exp, "CAH_Respawner")) // Might force on CAH too since it's not that engaging.
                                {
                                    // Sequence objects + add to sequence
                                    var crustAttach = VTestKismet.FindSequenceObjectByClassAndPosition(exp, "BioSeqAct_AttachCrustEffect", 5920, 1672);
                                    var currentPawn = VTestKismet.FindSequenceObjectByClassAndPosition(exp, "SeqVar_Object", 4536, 2016);

                                    // AI change
                                    var delay = SequenceObjectCreator.CreateSequenceObject(le1File, "BioSeqAct_Delay", vTestOptions.cache);
                                    var delayDuration = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_RandomFloat", vTestOptions.cache);
                                    var aiChoiceRand = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_RandomFloat", vTestOptions.cache);
                                    var aiChoiceComp = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqCond_CompareFloat", vTestOptions.cache);
                                    var aiChoiceAssaultThreshold = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_Float", vTestOptions.cache);
                                    var changeAiCharge = SequenceObjectCreator.CreateSequenceObject(le1File, "BioSeqAct_ChangeAI", vTestOptions.cache);
                                    var changeAiAssault = SequenceObjectCreator.CreateSequenceObject(le1File, "BioSeqAct_ChangeAI", vTestOptions.cache);
                                    var chargeAiLog = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_Log", vTestOptions.cache);
                                    var assaultAiLog = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_Log", vTestOptions.cache);

                                    // Toxic and Phasic
                                    var setWeaponAttributes = SequenceObjectCreator.CreateSequenceObject(le1File, "LEXSeqAct_SetWeaponAttribute", vTestOptions.cache);
                                    var toxicFactor = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_Float", vTestOptions.cache);
                                    var phasicFactor = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_Float", vTestOptions.cache);
                                    //var addToxicFactor = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_Float", vTestOptions.cache);
                                    //var addPhasicFactor = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_Float", vTestOptions.cache);
                                    //var respawnCount = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_Float", vTestOptions.cache);

                                    KismetHelper.AddObjectsToSequence(exp, false, delay, delayDuration, aiChoiceRand, aiChoiceComp, aiChoiceAssaultThreshold, changeAiCharge, changeAiAssault, assaultAiLog, chargeAiLog, setWeaponAttributes, toxicFactor, phasicFactor);

                                    // Configure sequence object properties
                                    delayDuration.WriteProperty(new FloatProperty(11, "Min"));
                                    delayDuration.WriteProperty(new FloatProperty(20, "Max"));
                                    KismetHelper.SetComment(toxicFactor, "Toxic to counter player regen");
                                    KismetHelper.SetComment(phasicFactor, "Phasic to counter player powers");
                                    toxicFactor.WriteProperty(new FloatProperty(1, "FloatValue"));
                                    phasicFactor.WriteProperty(new FloatProperty(1, "FloatValue"));


                                    // CHARGE AI BRANCH
                                    var chargeAiClass = EntryImporter.EnsureClassIsInFile(le1File, "BioAI_Charge", new RelinkerOptionsPackage() { Cache = vTestOptions.cache });
                                    changeAiCharge.WriteProperty(new ObjectProperty(chargeAiClass, "ControllerClass"));
                                    KismetHelper.SetComment(chargeAiLog, "CROSSGEN: Engaging player with BioAI_Charge");

                                    // ASSAULT AI BRANCH
                                    var assaultAiClass = EntryImporter.EnsureClassIsInFile(le1File, "BioAI_Assault", new RelinkerOptionsPackage() { Cache = vTestOptions.cache });
                                    changeAiAssault.WriteProperty(new ObjectProperty(assaultAiClass, "ControllerClass"));
                                    KismetHelper.SetComment(chargeAiLog, "CROSSGEN: Engaging player with BioAI_Assault");

                                    // ASSAULT CHANCE - 1 in 4 chance
                                    aiChoiceRand.WriteProperty(new FloatProperty(0, "Min"));
                                    aiChoiceRand.WriteProperty(new FloatProperty(4, "Max"));
                                    aiChoiceAssaultThreshold.WriteProperty(new FloatProperty(3f, "FloatValue")); // The generated random number must be above this to change to assault. 

                                    // Connect sequence objects - Delay and branch pick
                                    KismetHelper.CreateOutputLink(crustAttach, "Done", delay);
                                    KismetHelper.CreateVariableLink(delay, "Duration", delayDuration);
                                    KismetHelper.CreateOutputLink(delay, "Finished", aiChoiceComp);
                                    KismetHelper.CreateVariableLink(aiChoiceComp, "A", aiChoiceRand);
                                    KismetHelper.CreateVariableLink(aiChoiceComp, "B", aiChoiceAssaultThreshold);

                                    // Connect sequence objects - CHARGE BRANCH
                                    KismetHelper.CreateOutputLink(aiChoiceComp, "A < B", changeAiCharge);
                                    KismetHelper.CreateOutputLink(changeAiCharge, "Out", chargeAiLog);
                                    KismetHelper.CreateVariableLink(changeAiCharge, "Pawn", currentPawn);
                                    KismetHelper.CreateVariableLink(chargeAiLog, "Object", currentPawn);

                                    // Connect sequence objects - CHARGE BRANCH
                                    KismetHelper.CreateOutputLink(aiChoiceComp, "A >= B", changeAiAssault);
                                    KismetHelper.CreateOutputLink(changeAiAssault, "Out", assaultAiLog);
                                    KismetHelper.CreateVariableLink(changeAiAssault, "Pawn", currentPawn);
                                    KismetHelper.CreateVariableLink(assaultAiLog, "Object", currentPawn);

                                    // Stop timer on any event in this sequence 
                                    var events = KismetHelper.GetAllSequenceElements(exp).OfType<ExportEntry>().Where(x => x.IsA("SeqEvent")).ToList();
                                    foreach (var seqEvent in events)
                                    {
                                        KismetHelper.CreateOutputLink(seqEvent, "Out", delay, 1); // Cancel the delay as spawn stopped or changed (or restarted)
                                    }

                                    // Connect sequence object - toxic / phasic (Needs gated to only activate later!)
                                    KismetHelper.CreateOutputLink(crustAttach, "Done", setWeaponAttributes);
                                    KismetHelper.CreateVariableLink(setWeaponAttributes, "Pawn", currentPawn);
                                    KismetHelper.CreateVariableLink(setWeaponAttributes, "Toxic Factor", toxicFactor);
                                    KismetHelper.CreateVariableLink(setWeaponAttributes, "Phasic Factor", phasicFactor);

                                    exp.WriteProperty(new StrProperty("Spawn_Single_Guy_SUR", "ObjName"));
                                }
                            }

                            #endregion

                            #region Fix dual-finishing sequence. Someone thought they were being clever, bet they didn't know it'd cause an annurysm 12 years later

                            else if (seqName == "Hench_Take_Damage")
                            {
                                var sequenceObjects = exp.GetProperty<ArrayProperty<ObjectProperty>>("SequenceObjects").Select(x => x.ResolveToEntry(le1File) as ExportEntry).ToList();
                                var attachEvents = sequenceObjects.Where(x => x.ClassName == "SeqAct_AttachToEvent").ToList(); // We will route one of these to the other
                                var starting = sequenceObjects.First(x => x.ClassName == "SeqEvent_SequenceActivated");
                                //var ending = sequenceObjects.First(x => x.ClassName == "SeqEvent_FinishSequence");
                                KismetHelper.RemoveOutputLinks(starting); // prevent dual outs
                                KismetHelper.CreateOutputLink(starting, "Out", attachEvents[0]);
                                KismetHelper.RemoveOutputLinks(attachEvents[0]);
                                KismetHelper.CreateOutputLink(attachEvents[0], "Out", attachEvents[1]); // Make it serial
                            }
                            #endregion

                            #region Issue Rally Command at map start to ensure squadmates don't split up, blackscreen off should be fade in not turn off
                            else if (seqName == "TA_V3_Gametype_Handler")
                            {
                                // Time Trial
                                var startObj = VTestKismet.FindSequenceObjectByClassAndPosition(exp, "BioSeqAct_SetActionState"/*, 712, 2256*/);
                                var newObj = SequenceObjectCreator.CreateSequenceObject(le1File, "LEXSeqAct_SquadCommand", vTestOptions.cache);
                                KismetHelper.AddObjectToSequence(newObj, exp);
                                KismetHelper.CreateOutputLink(startObj, "Out", newObj); // RALLY
                                VTestKismet.FixSimMapTextureLoading(VTestKismet.FindSequenceObjectByClassAndPosition(exp, "BioSeqAct_Delay", 72, 1736), vTestOptions);
                            }
                            else if (seqName == "Check_Capping_Completion")
                            {
                                // Survival uses this as game mode?
                                // Capture...?
                                // Both use this same named item because why not
                                var startObj = VTestKismet.FindSequenceObjectByClassAndPosition(exp, "BioSeqAct_SetActionState"/*, 584, 2200*/);
                                var newObj = SequenceObjectCreator.CreateSequenceObject(le1File, "LEXSeqAct_SquadCommand", vTestOptions.cache);
                                KismetHelper.AddObjectToSequence(newObj, exp);
                                KismetHelper.CreateOutputLink(startObj, "Out", newObj); // RALLY
                                VTestKismet.FixSimMapTextureLoading(VTestKismet.FindSequenceObjectByClassAndPosition(exp, "BioSeqAct_Delay"/*, -152, 1768*/), vTestOptions);

                                var surDecayStart = VTestKismet.FindSequenceObjectByClassAndPosition(exp, "BioSeqAct_DUISetTextStringRef", 3544, 2472);
                                if (surDecayStart != null)
                                {
                                    // It's survival

                                    // 09/25/2024 - In OT the map started pretty much immediately, in crossgen we give the player a few seconds to prepare for combat
                                    // We are moving the timer start signaling to after the delay so they don't get free time for survival
                                    // We remove the events from here; we will fire them after the delay in the parent sequence
                                    KismetHelper.RemoveOutputLinks(surDecayStart);

                                    // Add signal to decay start
                                    var surDecaySignal = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_ActivateRemoteEvent", vTestOptions.cache);
                                    KismetHelper.AddObjectToSequence(surDecaySignal, exp);
                                    surDecaySignal.WriteProperty(new NameProperty("CROSSGEN_START_SUR_HEALTHGATE_DECAY", "EventName"));
                                    KismetHelper.CreateOutputLink(surDecayStart, "Out", surDecaySignal);
                                }
                            }
                            else if (seqName == "Vampire_Mode_Handler")
                            {
                                // Hunt
                                var startObj = VTestKismet.FindSequenceObjectByClassAndPosition(exp, "BioSeqAct_SetActionState" /*, 1040, 2304*/);
                                var newObj = SequenceObjectCreator.CreateSequenceObject(le1File, "LEXSeqAct_SquadCommand", vTestOptions.cache);
                                KismetHelper.AddObjectToSequence(newObj, exp);
                                KismetHelper.CreateOutputLink(startObj, "Out", newObj); // RALLY

                                VTestKismet.FixSimMapTextureLoading(VTestKismet.FindSequenceObjectByClassAndPosition(exp, "BioSeqAct_Delay", 304, 1952), vTestOptions);
                            }
                            else if (seqName is "Play_Ahern_Quip_For_TA_Intro" or "Play_Ahern_Quip_For_SUR_Intro" or "Play_Ahern_Quip_For_VAM_Intro" or "Play_Ahern_Quip_For_CAH_Intro")
                            {
                                // Install music remote event at the end
                                var setBool = KismetHelper.GetSequenceObjects(exp).OfType<ExportEntry>().First(x => x.ClassName == "SeqAct_SetBool" && x.GetProperty<ArrayProperty<StructProperty>>("OutputLinks")[0].GetProp<ArrayProperty<StructProperty>>("Links").Count == 0);
                                var remoteEvent = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_ActivateRemoteEvent", vTestOptions.cache);
                                KismetHelper.AddObjectToSequence(remoteEvent, exp);
                                KismetHelper.CreateOutputLink(setBool, "Out", remoteEvent);
                                remoteEvent.WriteProperty(new NameProperty("StartSimMusic", "EventName"));
                            }

                            // Enemy ramping - Survival Thai, Cave, Lava (Crate doesn't have one, no point doing it for Ahern's)
                            else if (seqName is "SUR_Thai_Handler" or "SUR_Cave_Handler" or "SUR_Lava_Handler")
                            {
                                var startTimerSignal = SequenceObjectCreator.CreateActivateRemoteEvent(exp, "START_TIMER");
                                var delay = KismetHelper.GetSequenceObjects(exp).OfType<ExportEntry>().FirstOrDefault(x => x.ClassName == "BioSeqAct_Delay"); // First one is the one we care about
                                KismetHelper.InsertActionAfter(delay, "Finished", startTimerSignal, 0, "Out");

                                VTestKismet.InstallEnemyCountRamp(startTimerSignal, exp, vTestOptions);
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
                                var sequenceObjects = exp.GetProperty<ArrayProperty<ObjectProperty>>("SequenceObjects").Select(x => x.ResolveToEntry(le1File) as ExportEntry).ToList();
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
                                var compareBool = VTestKismet.FindSequenceObjectByClassAndPosition(exp, "SeqCond_CompareBool", 8064, 3672);
                                KismetHelper.SkipSequenceElement(compareBool, "True"); // Skip out to true

                                // Add signal to stop decay and restore healthgate
                                if (VTestKismet.IsContainedWithinSequenceNamed(exp, "SUR_Lava_Handler", true) ||
                                    VTestKismet.IsContainedWithinSequenceNamed(exp, "SUR_Thai_Handler", true) ||
                                    VTestKismet.IsContainedWithinSequenceNamed(exp, "SUR_Crate_Handler", true) ||
                                    VTestKismet.IsContainedWithinSequenceNamed(exp, "SUR_Cave_Handler", true))
                                {
                                    // It's survival, we should signal to restore the healthgate
                                    var surRestoreHealthgateSignal = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_ActivateRemoteEvent", vTestOptions.cache);
                                    KismetHelper.AddObjectToSequence(surRestoreHealthgateSignal, exp);
                                    surRestoreHealthgateSignal.WriteProperty(new NameProperty("CROSSGEN_RESTORE_SUR_HEALTHGATE", "EventName"));
                                    KismetHelper.CreateOutputLink(VTestKismet.FindSequenceObjectByClassAndPosition(exp, "SeqEvent_SequenceActivated"), "Out", surRestoreHealthgateSignal);
                                }

                            }
                            #endregion
                            else if (seqName == "SUR_Lava_Handler" || seqName == "SUR_Thai_Handler" || seqName == "SUR_Crate_Handler" || seqName == "SUR_Cave_Handler")
                            {
                                VTestKismet.InstallVTestHelperSequenceNoInput(le1File, exp.InstancedFullPath, "HelperSequences.SurvivalHealthGateCurve", vTestOptions);
                            }
                        }
                    }
                    break;
                case "BIOA_PRC2_CCTHAI_SND":
                case "BIOA_PRC2_CCCAVE_SND":
                case "BIOA_PRC2_CCLAVA_SND":
                case "BIOA_PRC2_CCCRATE_SND":
                case "BIOA_PRC2_CCAHERN_SND":
                    VTestAudio.InstallMusicVolume(le1File, vTestOptions);
                    break;
                case "BIOA_PRC2AA_00_LAY":
                    // Need to set 'bCanStepUpOn' = false for certain static meshes as the collision is mega jank
                    {
                        var meshesToFix = new[]
                        {
                            "TheWorld.PersistentLevel.StaticMeshActor_0",
                            "TheWorld.PersistentLevel.StaticMeshActor_1",
                            "TheWorld.PersistentLevel.StaticMeshActor_2",
                            "TheWorld.PersistentLevel.StaticMeshActor_3",
                        };

                        foreach (var m in meshesToFix)
                        {
                            var exp = le1File.FindExport(m);
                            exp.WriteProperty(new BoolProperty(false, "bCanStepUpOn"));
                        }
                    }
                    break;
                case "BIOA_PRC2AA_00_DSG":
                    {
                        // Swap order of Has_Used_Courier and Can_Afford_Courier objects to prevent journal from completing if player can't afford
                        var executeTransition = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Courier_Box.BioSeqAct_PMExecuteTransition_1");
                        var linkFrom = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Courier_Box.BioSeqAct_PMCheckConditional_0");
                        KismetHelper.SkipSequenceElement(executeTransition, outboundLinkIdx: 0);
                        var linkFromLinks = KismetHelper.GetOutputLinksOfNode(linkFrom);
                        KismetHelper.WriteOutputLinksToNode(executeTransition, new List<List<OutputLink>> { linkFromLinks[0] });
                        linkFromLinks[0] = new List<OutputLink> { OutputLink.FromTargetExport(executeTransition, 0) };
                        KismetHelper.WriteOutputLinksToNode(linkFrom, linkFromLinks);
                        KismetHelper.SetComment(executeTransition, "VTEST: Swapped order to prevent journal completion with low credits");
                    }
                    break;
            }

            // Individual
            switch (upperFName)
            {
                case "BIOA_PRC2_CCAHERN_DSG":
                    {
                        VTestCCLevels.PreventSavingOnSimLoad(le1File, vTestOptions);

                        // Rally - This is not a template so it's done manually on this level
                        foreach (var exp in le1File.Exports.Where(x => x.ClassName == "Sequence").ToList())
                        {
                            var seqName = exp.GetProperty<StrProperty>("ObjName")?.Value;

                            if (seqName == "SUR_Ahern_Handler")
                            {
                                // Ahern's mission
                                var startObj = VTestKismet.FindSequenceObjectByClassAndPosition(exp, "SeqAct_Teleport", -8784, -1080); // Attach to Teleport since once loading movie is gone we'll hear it
                                var newObj = SequenceObjectCreator.CreateSequenceObject(le1File, "LEXSeqAct_SquadCommand", vTestOptions.cache);
                                KismetHelper.AddObjectToSequence(newObj, exp);
                                KismetHelper.CreateOutputLink(startObj, "Out", newObj); // RALLY
                            }
                        }

                        InstallAhernAntiCheese(le1File);
                        break;
                    }
                case "BIOA_PRC2_CCSIM_04_DSG":
                    {
                        // Install texture streaming for Ocaren
                        var sequence = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence");
                        var remoteEvent = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqEvent_RemoteEvent", vTestOptions.cache);
                        var streamInTextures = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_StreamInTextures", vTestOptions.cache);
                        KismetHelper.AddObjectsToSequence(sequence, false, remoteEvent, streamInTextures);
                        KismetHelper.CreateOutputLink(remoteEvent, "Out", streamInTextures);

                        remoteEvent.WriteProperty(new NameProperty("PrimeTexturesAhern", "EventName"));
                        var materials = new ArrayProperty<ObjectProperty>("ForceMaterials");

                        // OCAREN
                        materials.Add(new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_11.BioMaterialInstanceConstant_151")));
                        materials.Add(new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_11.BioMaterialInstanceConstant_152")));
                        materials.Add(new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_11.BioMaterialInstanceConstant_153")));

                        streamInTextures.WriteProperty(materials);
                        streamInTextures.WriteProperty(new FloatProperty(12f, "Seconds")); // How long to force stream. We set this to 12 to ensure blackscreen and any delays between fully finish
                    }
                    break;
                case "BIOA_PRC2_CCMAIN_CONV":
                    {
                        CCMainConv.PostPortingCorrections(me1File, le1File, vTestOptions);
                        break;
                    }
                case "BIOA_PRC2_CCLAVA_DSG":
                    {
                        // SeqAct_ChangeCollision changed and requires an additional property otherwise it doesn't work.
                        string[] collisionsToTurnOff = new[]
                        {
                            // Hut doors and kill volumes
                            "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_1",
                            "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_3",
                            "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_5",
                            "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_6",
                        };

                        foreach (var cto in collisionsToTurnOff)
                        {
                            var exp = le1File.FindExport(cto);
                            exp.WriteProperty(new EnumProperty("COLLIDE_NoCollision", "ECollisionType", MEGame.LE1, "CollisionType"));
                        }

                        string[] collisionsToTurnOn = new[]
                        {
                            // Hut doors and kill volumes
                            "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_0",
                            "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_9",
                            "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_2",
                            "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_4",
                        };

                        foreach (var cto in collisionsToTurnOn)
                        {
                            var exp = le1File.FindExport(cto);
                            exp.WriteProperty(new EnumProperty("COLLIDE_BlockAll", "ECollisionType", MEGame.LE1, "CollisionType"));
                        }

                        // Add code to disable reachspecs when turning the doors on so enemies do not try to use these areas
                        var hutSeq = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility");
                        var cgSource = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_4");
                        var disableReachSpecs = SequenceObjectCreator.CreateSequenceObject(le1File, "LEXSeqAct_ToggleReachSpec", vTestOptions.cache);

                        KismetHelper.AddObjectToSequence(disableReachSpecs, hutSeq);
                        KismetHelper.CreateOutputLink(cgSource, "Out", disableReachSpecs, 2);

                        string[] reachSpecsToDisable = new[]
                        {
                            // NORTH ROOM
                            "TheWorld.PersistentLevel.ReachSpec_1941", // CoverLink to PathNode
                            "TheWorld.PersistentLevel.ReachSpec_1937", // CoverLink to CoverLink
                            "TheWorld.PersistentLevel.ReachSpec_2529", // PathNode to PathNode

                            // SOUTH ROOM
                            "TheWorld.PersistentLevel.ReachSpec_1856", // CoverLink to PathNode
                            "TheWorld.PersistentLevel.ReachSpec_1849", // CoverLink to CoverLink
                        };

                        foreach (var rs in reachSpecsToDisable)
                        {
                            var obj = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_Object", vTestOptions.cache);
                            KismetHelper.AddObjectToSequence(obj, hutSeq);
                            obj.WriteProperty(new ObjectProperty(le1File.FindExport(rs), "ObjValue"));
                            KismetHelper.CreateVariableLink(disableReachSpecs, "ReachSpecs", obj);
                        }
                    }
                    break;
                case "BIOA_PRC2_CCSIM03_LAY":
                    {
                        // The door lighting channels needs fixed up.
                        //08/24/2024 - Disabled lighting changes due to static lighting bake
                        /*
                        var door = le1File.FindExport(@"TheWorld.PersistentLevel.BioDoor_1.SkeletalMeshComponent_1");
                        var channels = door.GetProperty<StructProperty>("LightingChannels");
                        channels.Properties.AddOrReplaceProp(new BoolProperty(false, "Static"));
                        channels.Properties.AddOrReplaceProp(new BoolProperty(false, "Dynamic"));
                        channels.Properties.AddOrReplaceProp(new BoolProperty(false, "CompositeDynamic"));
                        door.WriteProperty(channels); */

                    }
                    break;
                case "BIOA_PRC2_CCSIM_ART":
                    {
                        // Lights near the door need fixed up.

                        //08/24/2024 - Disabled lighting changes due to static lighting bake
                        /*
                        var doorPL = le1File.FindExport(@"TheWorld.PersistentLevel.StaticLightCollectionActor_11.PointLight_0_LC");
                        var lc = doorPL.GetProperty<StructProperty>("LightColor");
                        lc.GetProp<ByteProperty>("B").Value = 158;
                        lc.GetProp<ByteProperty>("G").Value = 194;
                        lc.GetProp<ByteProperty>("R").Value = 143;
                        doorPL.WriteProperty(lc);

                        var doorSL = le1File.FindExport(@"TheWorld.PersistentLevel.StaticLightCollectionActor_11.SpotLight_7_LC");
                        lc = doorSL.GetProperty<StructProperty>("LightColor");
                        lc.GetProp<ByteProperty>("B").Value = 215;
                        lc.GetProp<ByteProperty>("G").Value = 203;
                        lc.GetProp<ByteProperty>("R").Value = 195;
                        doorSL.WriteProperty(lc); */
                    }
                    break;
                case "BIOA_PRC2_CCSPACE02_DSG":
                    {
                        // Fixes for particle system
                        // Port in a new DominantLight
                        var sourceLight = vTestOptions.vTestHelperPackage.FindExport(@"CCSPACE02_DSG.DominantDirectionalLight_1");
                        var destLevel = le1File.FindExport("TheWorld.PersistentLevel");
                        EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, sourceLight, le1File, destLevel, true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out _);

                        // Correct some lighting channels
                        string[] unlitPSCs = new[]
                        {
                            "BIOA_PRC2_S.Prefab.PRC2_Skybox_Vista.PRC2_Skybox_Vista_Arc17.ParticleSystemComponent0",
                            "BIOA_PRC2_S.Prefab.PRC2_Skybox_Vista.PRC2_Skybox_Vista_Arc18.ParticleSystemComponent0",
                            "BIOA_PRC2_S.Prefab.PRC2_Skybox_Vista.PRC2_Skybox_Vista_Arc19.ParticleSystemComponent0"
                        };

                        foreach (var unlitPSC in unlitPSCs)
                        {
                            var exp = le1File.FindExport(unlitPSC);
                            var lightingChannels = exp.GetProperty<StructProperty>("LightingChannels");
                            lightingChannels.Properties.Clear();
                            lightingChannels.Properties.Add(new BoolProperty(true, "bInitialized"));
                            lightingChannels.Properties.Add(new BoolProperty(true, new NameReference("Cinematic", 4)));
                            exp.WriteProperty(lightingChannels);
                        }

                    }
                    break;
                case "BIOA_PRC2_CCCRATE":
                    {
                        // needs something to fill framebuffer
                        var sourceAsset = vTestOptions.vTestHelperPackage.FindExport(@"CROSSGENV.StaticMeshActor_32000");
                        var destLevel = le1File.FindExport("TheWorld.PersistentLevel");
                        EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, sourceAsset, le1File, destLevel, true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out var mesh);
                        LevelTools.SetLocation(mesh as ExportEntry, 15864, -25928, -5490);
                    }
                    break;
                case "BIOA_PRC2_CCCAVE":
                    {
                        // needs something to fill framebuffer
                        var sourceAsset = vTestOptions.vTestHelperPackage.FindExport(@"CROSSGENV.StaticMeshActor_32000");
                        var destLevel = le1File.FindExport("TheWorld.PersistentLevel");
                        EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, sourceAsset, le1File, destLevel, true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out var mesh);
                        LevelTools.SetLocation(mesh as ExportEntry, -16430, -28799, -2580);

                        // Needs a second one to hide the top too
                        var fb2 = EntryCloner.CloneTree(mesh) as ExportEntry;
                        var rotation = fb2.GetProperty<StructProperty>("Rotation");
                        rotation.Properties.AddOrReplaceProp(new FloatProperty(0f, "Pitch"));
                        fb2.WriteProperty(rotation);
                    }
                    break;
                case "BIOA_PRC2_CCSIM": // this this be in CCSIM05_DSG instead?
                    {
                        // SCOREBOARD FRAMEBUFFER
                        // This is handled by porting model from CCSIM03_LAY
                        // needs something to fill framebuffer
                        //var sourceAsset = vTestOptions.vTestHelperPackage.FindExport(@"CROSSGENV.StaticMeshActor_32000");
                        //var destLevel = le1File.FindExport("TheWorld.PersistentLevel");
                        //EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, sourceAsset, le1File, destLevel, true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out var mesh);
                        //LevelTools.SetLocation(mesh as ExportEntry, -3750, -1624, -487);
                        //LevelTools.SetDrawScale3D(mesh as ExportEntry, 3, 3, 3);
                    }
                    break;
                case "BIOA_PRC2":
                    {
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
                    }
                    break;
            }
        }

        private static void CorrectTerrainSetup(IMEPackage me1File, IMEPackage le1File, VTestOptions vTestOptions)
        {
            if (vTestOptions.isBuildForStaticLightingBake)
                return; // Do not make corrections.

            // Correct AlphaMaps to match the original
            var mTerrain = me1File.Exports.First(x => x.ClassName == "Terrain");
            var me1Terrain = ObjectBinary.From<Terrain>(mTerrain);
            var lTerrain = le1File.Exports.First(x => x.ClassName == "Terrain");
            var le1Terrain = ObjectBinary.From<Terrain>(lTerrain);

            var properties = lTerrain.GetProperties();

            var fName = le1File.FileNameNoExtension;
            var alphamaps = new List<AlphaMap>();

            if (fName == "BIOA_PRC2_CCLAVA")
            {
                // 5 maps (vs 4 in the source)
                alphamaps.Add(me1Terrain.AlphaMaps[0]); // Default
                alphamaps.Add(me1Terrain.AlphaMaps[1]); // RiverOverride
                alphamaps.Add(new AlphaMap() { Data = new byte[me1Terrain.Heights.Length] }); // Water Rock Override (NOT USED)
                alphamaps.Add(me1Terrain.AlphaMaps[2]); // Rock02 Override (?)
                alphamaps.Add(new AlphaMap() { Data = new byte[me1Terrain.Heights.Length] }); // BLANK (NOT USED)

                CorrectTerrainMaterialsAndSlopes(mTerrain, lTerrain, false, vTestOptions); // Needs changes to avoid patches
            }
            else if (fName == "BIOA_PRC2AA_00_LAY")
            {
                // THERE ARE NO ALPHAMAPS
                CorrectTerrainMaterialsAndSlopes(mTerrain, lTerrain, true, vTestOptions); // Needs changes to avoid patches
            }
            else if (fName == "BIOA_PRC2_CCAHERN")
            {
                // The only layer used is Rock02. The other terrain layer is not used
                // We must correct the AlphaMapIndexes
                alphamaps.Add(me1Terrain.AlphaMaps[0]);
                var layers = properties.GetProp<ArrayProperty<StructProperty>>("Layers");
                for (int i = 0; i < layers.Count; i++)
                {
                    var layer = layers[i];
                    if (i == 3)
                    {
                        // ROCK02
                        layer.Properties.AddOrReplaceProp(new IntProperty(0, "AlphaMapIndex"));
                    }
                    else
                    {
                        layer.Properties.AddOrReplaceProp(new IntProperty(-1, "AlphaMapIndex"));
                    }
                }
            }

            le1Terrain.AlphaMaps = alphamaps.ToArray();
            lTerrain.WritePropertiesAndBinary(properties, le1Terrain);
        }

        private static void CorrectVFX(IMEPackage me1File, IMEPackage le1File, VTestOptions vTestOptions)
        {
            // Needs a fadein
            var glitchRandom = le1File.FindExport("BIOA_PRC2_MatFX.VFX.Glitch_Random");
            if (glitchRandom != null)
            {
                var props = glitchRandom.GetProperties();
                props.AddOrReplaceProp(new NameProperty("EMT_HoloWipe", "m_nmEffectsMaterial"));
                props.AddOrReplaceProp(new BoolProperty(true, "m_bIgnorePooling"));
                props.AddOrReplaceProp(new EnumProperty("BIO_VFX_PRIORITY_ALWAYS", "EBioVFXPriority", MEGame.LE1, "ePriority"));
                glitchRandom.WriteProperties(props);
            }

            // Fixed to look fadeout
            var glitchedToDeath = le1File.FindExport("BIOA_PRC2_MatFX.DeathEffects.GlitchedToDeath");
            if (glitchedToDeath != null)
            {
                var props = glitchedToDeath.GetProperties();
                props.AddOrReplaceProp(new NameProperty("EMT_HoloWipe", "m_nmEffectsMaterial"));
                props.AddOrReplaceProp(new BoolProperty(true, "m_bIgnorePooling"));
                props.AddOrReplaceProp(new EnumProperty("BIO_VFX_PRIORITY_ALWAYS", "EBioVFXPriority", MEGame.LE1, "ePriority"));
                glitchedToDeath.WriteProperties(props);
            }

            // Correct missing Geth Holowipe
            // This is kind of a hack. Doing it properly would require renaming tons of objects which breaks the dynamic load system LE1 has

            var le1Rvr = le1File.FindExport(@"EffectsMaterials.Users.GTH_TNT_MASTER_MAT_USER.RvrMaterialMultiplexor_16");
            if (le1Rvr != null)
            {
                Debug.WriteLine("Correct Geth Holo VFX");
                var replacement = vTestOptions.vTestHelperPackage.FindExport(@"EffectsMaterials.Users.GTH_TNT_MASTER_MAT_USER.RvrMaterialMultiplexor_16");
                EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.ReplaceSingularWithRelink, replacement, le1File, le1Rvr, true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out _);
            }
        }

        public static void PostPortingCorrections(IMEPackage me1File, IMEPackage le1File, VTestOptions vTestOptions)
        {
            // Corrections to run AFTER porting is done

            // Copy over the AdditionalPackagesToCook
            var fName = Path.GetFileNameWithoutExtension(le1File.FilePath);
            if (fName.CaseInsensitiveEquals("BIOA_PRC2") || fName.CaseInsensitiveEquals("BIOA_PRC2AA"))
            {

                if (me1File is MEPackage me1FileP && le1File is MEPackage le1FileP)
                {
                    // This will be used for lighting build step (has to be done manually)
                    var files = Directory.GetFiles(Directory.GetParent(me1File.FilePath).FullName, "*.sfm",
                        SearchOption.TopDirectoryOnly).Where(x => Path.GetFileNameWithoutExtension(x).StartsWith(fName, StringComparison.InvariantCultureIgnoreCase) && Path.GetFileNameWithoutExtension(x) != fName && x.GetUnrealLocalization() == MELocalization.None).ToList();

                    le1FileP.AdditionalPackagesToCook.ReplaceAll(files.Select(x => Path.GetFileNameWithoutExtension(x)));
                }
            }

            vTestOptions.SetStatusText($"PPC (CoverSlots)");
            VTestCover.ReinstateCoverSlots(me1File, le1File, vTestOptions);
            vTestOptions.SetStatusText($"PPC (FireLinks)");
            VTestCover.GenerateFireLinkItemsForFile(me1File, le1File, vTestOptions); // This must run after cover slots are reinstated. This might need to run after all files are generated if there are cross levels
            vTestOptions.SetStatusText($"PPC (Textures)");
            CorrectTextures(le1File);
            vTestOptions.SetStatusText($"PPC (PrefabSequences)");
            CorrectPrefabSequenceClass(le1File);
            vTestOptions.SetStatusText($"PPC (Sequences)");
            CorrectSequences(le1File, vTestOptions);
            vTestOptions.SetStatusText($"PPC (Pathfinding)");
            CorrectPathfindingNetwork(me1File, le1File, vTestOptions);
            vTestOptions.SetStatusText($"PPC (MaterialInstanceConstants)");
            PostCorrectMaterialsToInstanceConstants(me1File, le1File, vTestOptions);
            vTestOptions.SetStatusText($"PPC (VFX)");
            CorrectVFX(me1File, le1File, vTestOptions);
            vTestOptions.SetStatusText($"PPC (Pink Visor)");
            FixPinkVisorMaterial(le1File);
            vTestOptions.SetStatusText($"PPC (Unlock Ahern Mission)");
            VTestDebug.DebugUnlockAhernMission(le1File, vTestOptions);
            vTestOptions.SetStatusText($"PPC (2DAs)");
            CorrectGethEquipment2DAs(le1File, vTestOptions);
            vTestOptions.SetStatusText($"PPC (Audio Lengths)");
            VTestAudio.FixAudioLengths(le1File, vTestOptions);
            vTestOptions.SetStatusText($"PPC (Planters)");
            VTestCorrections.FixPlanters(le1File, vTestOptions);
            // Disabled in static build
            //vTestOptions.SetStatusText($"PPC (Lighting)");
            //FixLighting(le1File, vTestOptions);
            vTestOptions.SetStatusText($"PPC (Ahern Conversation)");
            FixAhernConversation(le1File, vTestOptions);

            // Disabled 08/18/2024 - Do not use assets for optimization as we now rebake lighting
            //vTestOptions.SetStatusText($"PPC (Optimization)");
            //PortME1OptimizationAssets(me1File, le1File, vTestOptions);
            vTestOptions.SetStatusText($"PPC (LEVEL SPECIFIC)");

            // Port in the collision-corrected terrain
            if (fName.CaseInsensitiveEquals("BIOA_PRC2_CCLava"))
            {
                // Only port in corrected terrain if we are not doing a build for static lighting.
                if (!vTestOptions.isBuildForStaticLightingBake)
                {
                    VTestTerrain.PortInCorrectedTerrain(me1File, le1File, "CCLava.Terrain_1", "BIOA_LAV60_00_LAY.pcc", vTestOptions);
                    CorrectTerrainSetup(me1File, le1File, vTestOptions);

                    // We don't use the lightmap from UDK as it doesn't work properly for some reason
                    // Correct the DirectionalMaxComponent scalar
                    var terrainComponents = le1File.Exports.Where(x => x.ClassName == "TerrainComponent");
                    foreach (var tc in terrainComponents)
                    {
                        var tcBin = ObjectBinary.From<TerrainComponent>(tc);
                        var lm = tcBin.LightMap as LightMap_2D;
                        lm.ScaleVector2.X *= vTestOptions.LavaLightmapScalar;
                        lm.ScaleVector2.Y *= vTestOptions.LavaLightmapScalar;
                        lm.ScaleVector2.Z *= vTestOptions.LavaLightmapScalar;
                        tc.WriteBinary(tcBin);
                    }

                    VTestKismet.CreateSignaledTextureStreaming(le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence"), VTestMaterial.cclavaTextureStreamingMaterials, vTestOptions);
                }
            }
            else if (fName.CaseInsensitiveEquals("BIOA_PRC2_CCSIM04_DSG"))
            {
                // 09/26/2024 - Install mod settings menu (via DropTheSquid)
                VTestKismet.InstallVTestHelperSequenceNoInput(le1File, "TheWorld.PersistentLevel.Main_Sequence", "HelperSequences.SimulatorSettingsLogic", vTestOptions);
                var artPlacable = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.SimulatorSettingsLogic.BioSeqEvt_ArtPlaceableUsed_0");
                artPlacable.WriteProperty(new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioInert_3"),"Originator"));
            }
            else if (fName.CaseInsensitiveEquals("BIOA_PRC2AA_00_LAY"))
            {
                if (!vTestOptions.isBuildForStaticLightingBake)
                {
                    VTestTerrain.PortInCorrectedTerrain(me1File, le1File, "PRC2AA.Terrain_1", "BIOA_UNC20_00_LAY.pcc", vTestOptions);
                    CorrectTerrainSetup(me1File, le1File, vTestOptions);
                }
            }
            else if (fName.CaseInsensitiveEquals("BIOA_PRC2_CCAHERN"))
            {
                if (!vTestOptions.isBuildForStaticLightingBake)
                {
                    VTestTerrain.PortInCorrectedTerrain(me1File, le1File, "CCAHERN.Terrain_1", "BIOA_LAV60_00_LAY.pcc", vTestOptions);
                    CorrectTerrainSetup(me1File, le1File, vTestOptions);
                }

                VTestCCLevels.PreventSavingOnSimLoad(le1File, vTestOptions);
            }
            else if (fName.CaseInsensitiveEquals("BIOA_PRC2_CCMID_ART"))
            {
                // Brighten up the corners that are dead ends
                string[] exports =
                [
                    "TheWorld.PersistentLevel.StaticLightCollectionActor_10.PointLight_24_LC",
                    "TheWorld.PersistentLevel.StaticLightCollectionActor_10.PointLight_27_LC"
                ];
                foreach (var lightIFP in exports)
                {
                    var cornerLight = le1File.FindExport(lightIFP);
                    var props = cornerLight.GetProperties();
                    props.AddOrReplaceProp(new FloatProperty(0.7f, "Brightness")); // Up from 0.3
                    props.GetProp<StructProperty>("LightingChannels").Properties.AddOrReplaceProp(new BoolProperty(true, "CompositeDynamic"));
                    cornerLight.WriteProperties(props);
                }

            }
            else if (fName.CaseInsensitiveEquals("BIOA_PRC2_CCSIM05_DSG"))
            {
                // Port in the custom sequence used for switching UIs
                VTestKismet.InstallVTestHelperSequenceViaOut(le1File, "TheWorld.PersistentLevel.Main_Sequence.Play_Central_Scoreboard_Matinee.SeqEvent_RemoteEvent_0", "ScoreboardSequence.UISwitcherLogic", false, vTestOptions, out _);

                // Port in the keybinding sequences
                VTestKismet.InstallVTestHelperSequenceViaOut(le1File, "TheWorld.PersistentLevel.Main_Sequence.Play_Central_Scoreboard_Matinee.SeqEvent_RemoteEvent_0", "ScoreboardSequence.KeybindsInstaller", true, vTestOptions, out var gate);
                VTestKismet.InstallVTestHelperSequenceViaOut(le1File, "TheWorld.PersistentLevel.Main_Sequence.Play_Central_Scoreboard_Matinee.SeqAct_Gate_3", "ScoreboardSequence.KeybindsUninstaller", false, vTestOptions, out _);

                var scoreboardSeq = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Play_Central_Scoreboard_Matinee");
                var existingScoreboard = VTestKismet.FindSequenceObjectByClassAndPosition(scoreboardSeq, "BioSeqAct_BioToggleCinematicMode", 3552, 808);
                KismetHelper.CreateOutputLink(existingScoreboard, "Out", gate, 1); // Open the gate again.

            }
            else if (fName.CaseInsensitiveEquals("BIOA_PRC2_CCSCOREBOARD_DSG"))
            {
                // Porting in ANY of these crashes the game. Why??

                // Port in the UI switching and keybinding for PC
                // Port in the custom sequence used for switching UIs. Should only run if not skipping the scoreboard
                VTestKismet.InstallVTestHelperSequenceViaOut(le1File, "TheWorld.PersistentLevel.Main_Sequence.Play_Post_Scenario_Scoreboard_Matinee.UIAction_PlaySound_0", "ScoreboardSequence.UISwitcherLogic", false, vTestOptions, out _);

                // Port in the keybinding sequences
                VTestKismet.InstallVTestHelperSequenceViaOut(le1File, "TheWorld.PersistentLevel.Main_Sequence.Play_Post_Scenario_Scoreboard_Matinee.BioSeqAct_MiniGame_1", "ScoreboardSequence.KeybindsInstaller", true, vTestOptions, out var gate);
                VTestKismet.InstallVTestHelperSequenceViaOut(le1File, "TheWorld.PersistentLevel.Main_Sequence.Play_Post_Scenario_Scoreboard_Matinee.UIAction_PlaySound_1", "ScoreboardSequence.KeybindsUninstaller", false, vTestOptions, out _);
            }
            else if (fName.CaseInsensitiveEquals("BIOA_PRC2"))
            {
                #region PRC1 BioSoundNodeWaveStreamingData
                // This is hack to port things over in ModdedSource. The streaming data was referenced by an object that doesn't actually
                // use this (game will die if it tries). We remove this reference and set up our own.
                // This is a total hack, but it works for less code.
                le1File.Save();
                le1File.FindExport("TheWorld.PersistentLevel.AmbientSound_20").RemoveProperty("Base");
                VTestUtility.AddWorldReferencedObjects(le1File, le1File.FindExport("DVDStreamingAudioData.PC.snd_prc1_music")); // This must stay in memory for the music 2DA to work for PRC1 audio
                #endregion

                #region Geth Pulse Gun Crust VFX Fix
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
                #endregion

                #region Full Blocking Load Fix
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

                #endregion

                #region Level Load Blocking Texture Streaming
                VTestKismet.InstallVTestHelperSequenceNoInput(le1File, "TheWorld.PersistentLevel.Main_Sequence", "HelperSequences.LevelLoadTextureStreaming", vTestOptions);
                // The original logic is removed in the ModdedSource file
                #endregion

                VTestUtility.AddModLevelExtensions(le1File, "BIOA_PRC2");
            }
            else if (fName.CaseInsensitiveEquals("BIOA_PRC2AA"))
            {
                // Improved loader
                VTestKismet.InstallVTestHelperSequenceNoInput(le1File, "TheWorld.PersistentLevel.Main_Sequence", "HelperSequences.LevelLoadTextureStreaming", vTestOptions);
            }

            // Not an else statement as this is level generic
            if (!vTestOptions.isBuildForStaticLightingBake)
            {
                if (fName.StartsWith("BIOA_PRC2AA"))
                {
                    // Lights are way overblown for this map. This value is pretty close to the original game
                    foreach (var pl in le1File.Exports.Where(x => x.IsA("LightComponent")))
                    {
                        var brightness = pl.GetProperty<FloatProperty>("Brightness")?.Value ?? 1;
                        pl.WriteProperty(new FloatProperty(brightness * .1f, "Brightness"));
                    }
                }

                if (fName.StartsWith("BIOA_PRC2") && !fName.StartsWith("BIOA_PRC2AA"))
                {
                    // Lights are way overblown for this map. This value is pretty close to the original game
                    foreach (var pl in le1File.Exports.Where(x => x.IsA("LightComponent")))
                    {
                        if (pl.InstancedFullPath ==
                            "TheWorld.PersistentLevel.PointLightToggleable_10.PointLightComponent_1341242")
                            continue; // Pointlight that fixes ahern conversation. Do not change this

                        var brightness = pl.GetProperty<FloatProperty>("Brightness")?.Value ?? 1;
                        pl.WriteProperty(new FloatProperty(brightness * .4f, "Brightness"));
                    }
                }
            }

            // Kill streak voice line sequence
            switch (fName)
            {
                case "BIOA_PRC2_CCAHERN_DSG":
                case "BIOA_PRC2_CCTHAI_DSG":
                case "BIOA_PRC2_CCLAVA_DSG":
                case "BIOA_PRC2_CCCAVE_DSG":
                case "BIOA_PRC2_CCCRATE_DSG":
                    {
                        VTestKismet.InstallVTestHelperSequenceNoInput(le1File, "TheWorld.PersistentLevel.Main_Sequence", "HelperSequences.KillStreakVoiceLine", vTestOptions);
                        break;
                    }
            }

            LevelSpecificPostCorrections(fName, me1File, le1File, vTestOptions);


            var level = le1File.FindExport("TheWorld.PersistentLevel");
            if (level != null)
            {
                LevelTools.RebuildPersistentLevelChildren(level);
            }
            //CorrectTriggerStreamsMaybe(me1File, le1File);
        }

        public static void PreCorrectBioTriggerStream(ExportEntry triggerStream)
        {
            // Older games (ME1 at least) can reference levels that don't exist. This didn't break game. Later games this does break. Maybe. IDK.

            var streamingStates = triggerStream.GetProperty<ArrayProperty<StructProperty>>("StreamingStates");
            if (streamingStates != null)
            {
                foreach (var ss in streamingStates)
                {
                    var inChunkName = ss.GetProp<NameProperty>("InChunkName").Value.Name.ToLower();

                    if (inChunkName != "none" && VTest_NonExistentBTSFiles.Contains(inChunkName))
                        Debugger.Break(); // Hmm....

                    var visibleChunks = ss.GetProp<ArrayProperty<NameProperty>>("VisibleChunkNames");
                    for (int i = visibleChunks.Count - 1; i >= 0; i--)
                    {
                        if (VTest_NonExistentBTSFiles.Contains(visibleChunks[i].Value.Name.ToLower()))
                        {
                            Debug.WriteLine($"PreCorrect: VS Remove BTS level {visibleChunks[i].Value}");
                            //visibleChunks.RemoveAt(i);
                        }
                    }

                    var loadChunks = ss.GetProp<ArrayProperty<NameProperty>>("LoadChunkNames");
                    for (int i = loadChunks.Count - 1; i >= 0; i--)
                    {
                        if (VTest_NonExistentBTSFiles.Contains(loadChunks[i].Value.Name.ToLower()))
                        {
                            Debug.WriteLine($"PreCorrect: LC Remove BTS level {loadChunks[i].Value}");
                            //loadChunks.RemoveAt(i);
                        }
                    }
                }

                triggerStream.WriteProperty(streamingStates);
            }
            else
            {
                //yDebug.WriteLine($"{triggerStream.InstancedFullPath} in {triggerStream} has NO StreamingStates!!");
            }
        }

        private static void CorrectTerrainMaterialsAndSlopes(ExportEntry mTerrain, ExportEntry lTerrain, bool prc2aa, VTestOptions vTestOptions)
        {
            var le1File = lTerrain.FileRef;
            var me1File = mTerrain.FileRef;

            var mLayers = mTerrain.GetProperty<ArrayProperty<StructProperty>>("Layers");
            var lLayers = lTerrain.GetProperty<ArrayProperty<StructProperty>>("Layers");


            foreach (var lLayer in lLayers)
            {
                // Find matching mLayer
                var lSetup = lLayer.GetProp<ObjectProperty>("Setup").ResolveToEntry(le1File) as ExportEntry;
                ExportEntry mSetup = null;
                foreach (var mSetupStruct in mLayers)
                {
                    var mSetupTmp = mSetupStruct.GetProp<ObjectProperty>("Setup").ResolveToEntry(me1File) as ExportEntry;
                    if (mSetupTmp != null && mSetupTmp.InstancedFullPath == lSetup.InstancedFullPath)
                    {
                        mSetup = mSetupTmp;
                        break;
                    }
                }

                if (mSetup == null)
                    continue; // Don't update this

                var mMaterials = mSetup.GetProperty<ArrayProperty<StructProperty>>("Materials");
                var lMaterials = lSetup.GetProperty<ArrayProperty<StructProperty>>("Materials");

                if (prc2aa && mMaterials.Count == lMaterials.Count)
                {
                    // ONLY PRC2AA WILL RUN THIS CODE
                    for (int i = 0; i < mMaterials.Count; i++)
                    {
                        var mMat = mMaterials[i];
                        var lMat = lMaterials[i];

                        foreach (var prop in mMat.Properties)
                        {
                            if (prop is ObjectProperty)
                                continue; // Do not change

                            lMat.Properties.AddOrReplaceProp(prop);
                        }
                    }
                }

                // CORRECTIONS
                var setupName = mSetup.ObjectName.Name;
                switch (setupName)
                {
                    case "UNC20_TLSetup_lessDispl": // PRC2AA_00_LAY
                        {
                            // Memory Unique
                            var cgv = le1File.FindExport("CROSSGENV");
                            if (cgv == null)
                            {
                                cgv = ExportCreator.CreatePackageExport(lTerrain.FileRef, "CROSSGENV", null);
                                cgv.indexValue = 0;
                            }

                            lSetup.Parent = cgv;
                            lSetup.ObjectName = "CROSSGENV_PRC2AA_TerrainLayerSetup";

                            // Slope Changes
                            lMaterials[0].GetProp<StructProperty>("MinSlope").GetProp<FloatProperty>("Base").Value = 2; // 90 degrees never
                            lMaterials[1].GetProp<StructProperty>("MinSlope").GetProp<FloatProperty>("Base").Value = 0; // 0 degrees always
                        }

                        break;
                    case "lav60_RIVER01_terrain_setup": // PRC2_CCLAVA
                        {
                            if (!vTestOptions.isBuildForStaticLightingBake)
                            {
                                // Memory Unique
                                var cgv = le1File.FindExport("CROSSGENV");
                                if (cgv == null)
                                {
                                    cgv = ExportCreator.CreatePackageExport(lTerrain.FileRef, "CROSSGENV", null);
                                    cgv.indexValue = 0;
                                }

                                lSetup.Parent = cgv;
                                lSetup.ObjectName = "CROSSGENV_PRC2_CCLAVA_TerrainLayerSetup";

                                // No idea why this fixes it tbh but it does
                                lMaterials[0].GetProp<FloatProperty>("Alpha").Value = 0;
                                lMaterials[1].GetProp<FloatProperty>("Alpha").Value = 0;
                            }
                        }
                        break;
                }


                lSetup.WriteProperty(lMaterials);

            }
        }

        // This is list of materials to run a conversion to a MaterialInstanceConstant
        // List is not long cause not a lot of materials support this...
        private static string[] vtest_DonorMaterials = new[]
        {
            "BIOA_MAR10_T.UNC_HORIZON_MAT_Dup",
        };

        private static void PostCorrectMaterialsToInstanceConstants(IMEPackage me1Package, IMEPackage le1Package, VTestOptions vTestOptions)
        {
            // Oh lordy this is gonna suck

            // Donor materials need tweaks to behave like the originals
            // So we make a new MaterialInstanceConstant, copy in the relevant(?) values,
            // and then repoint all incoming references to the Material to use this MaterialInstanceConstant instead.
            // This is going to be slow and ugly code
            // Technically this could be done in the relinker but I don't want to stuff
            // something this ugly in there
            foreach (var le1Material in le1Package.Exports.Where(x => vtest_DonorMaterials.Contains(x.InstancedFullPath)).ToList())
            {
                Debug.WriteLine($"Correcting material inputs for donor material: {le1Material.InstancedFullPath}");
                var donorinputs = new List<string>();
                var expressions = le1Material.GetProperty<ArrayProperty<ObjectProperty>>("Expressions");
                foreach (var express in expressions.Select(x => x.ResolveToEntry(le1Package) as ExportEntry))
                {
                    if (express.ClassName == "MaterialExpressionVectorParameter")
                    {
                        donorinputs.Add(express.GetProperty<NameProperty>("ParameterName").Value.Name);
                    }
                }

                Debug.WriteLine(@"Donor has the following inputs:");
                foreach (var di in donorinputs)
                {
                    Debug.WriteLine(di);
                }

                var me1Material = me1Package.FindExport(le1Material.InstancedFullPath);

                var sourceMatInst = vTestOptions.vTestHelperPackage.Exports.First(x => x.ClassName == "MaterialInstanceConstant"); // cause it can change names here
                sourceMatInst.ObjectName = $"{le1Material.ObjectName}_MatInst";
                RelinkerOptionsPackage rop = new RelinkerOptionsPackage()
                {
                    Cache = vTestOptions.cache,
                    ImportExportDependencies = true,
                    IsCrossGame = true,
                    TargetGameDonorDB = vTestOptions.objectDB
                };
                EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, sourceMatInst, le1Package, le1Material.Parent, true, rop, out var le1MatInstEntry);

                var le1MatInst = le1MatInstEntry as ExportEntry;
                var le1MatInstProps = le1MatInst.GetProperties();

                le1MatInstProps.AddOrReplaceProp(new ObjectProperty(le1Material, "Parent")); // Update the parent

                // VECTOR EXPRESSIONS
                var vectorExpressions = new ArrayProperty<StructProperty>("VectorParameterValues");
                foreach (var v in me1Material.GetProperty<ArrayProperty<ObjectProperty>>("Expressions").Select(x => x.ResolveToEntry(me1Package) as ExportEntry))
                {
                    if (v.ClassName == "MaterialExpressionVectorParameter")
                    {
                        var exprInput = v.GetProperty<NameProperty>("ParameterName").Value.Name;
                        if (donorinputs.Contains(exprInput))
                        {
                            var vpv = v.GetProperty<StructProperty>("DefaultValue");
                            PropertyCollection pc = new PropertyCollection();
                            pc.AddOrReplaceProp(CommonStructs.LinearColorProp(vpv.GetProp<FloatProperty>("R"), vpv.GetProp<FloatProperty>("G"), vpv.GetProp<FloatProperty>("B"), vpv.GetProp<FloatProperty>("A"), "ParameterValue"));
                            pc.AddOrReplaceProp(new FGuid(Guid.Empty).ToStructProperty("ExpressionGUID"));
                            pc.AddOrReplaceProp(new NameProperty(exprInput, "ParameterName"));
                            vectorExpressions.Add(new StructProperty("VectorParameterValue", pc));
                            donorinputs.Remove(exprInput);
                        }
                    }
                    else
                    {
                        //Debugger.Break();
                    }
                }

                if (vectorExpressions.Any())
                {
                    le1MatInstProps.AddOrReplaceProp(vectorExpressions);
                }

                // SCALAR EXPRESSIONS
                var me1MatInfo = ObjectBinary.From<Material>(me1Material);
                var scalarExpressions = new ArrayProperty<StructProperty>("ScalarParameterValues");
                foreach (var v in me1MatInfo.SM3MaterialResource.UniformPixelScalarExpressions)
                {
                    if (v is MaterialUniformExpressionScalarParameter spv)
                    {
                        PropertyCollection pc = new PropertyCollection();
                        pc.AddOrReplaceProp(new FGuid(Guid.Empty).ToStructProperty("ExpressionGUID"));
                        pc.AddOrReplaceProp(new NameProperty(spv.ParameterName, "ParameterName"));
                        pc.AddOrReplaceProp(new FloatProperty(spv.DefaultValue, "ParameterValue"));
                        scalarExpressions.Add(new StructProperty("ScalarParameterValue", pc));
                    }
                }

                if (scalarExpressions.Any())
                {
                    le1MatInstProps.AddOrReplaceProp(scalarExpressions);
                }

                le1MatInst.WriteProperties(le1MatInstProps);

                // Find things that reference this material and repoint them
                var entriesToUpdate = le1Material.GetEntriesThatReferenceThisOne();
                foreach (var entry in entriesToUpdate.Keys)
                {
                    if (entry == le1MatInst)
                        continue;
                    le1MatInst.GetProperties();
                    var relinkDict = new ListenableDictionary<IEntry, IEntry>();
                    relinkDict[le1Material] = le1MatInst; // This is a ridiculous hack

                    rop = new RelinkerOptionsPackage()
                    {
                        CrossPackageMap = relinkDict,
                        Cache = vTestOptions.cache,
                        ImportExportDependencies = false // This is same-package so there's nothing to import.
                    };

                    Relinker.Relink(entry as ExportEntry, entry as ExportEntry, rop);
                    le1MatInst.GetProperties();
                }
            }
        }

        // Files we know are referenced by name but do not exist
        private static string[] VTest_NonExistentBTSFiles =
        {
            "bioa_prc2_ccahern_l",
            "bioa_prc2_cccave01",
            "bioa_prc2_cccave02",
            "bioa_prc2_cccave03",
            "bioa_prc2_cccave04",
            "bioa_prc2_cccrate01",
            "bioa_prc2_cccrate02",
            "bioa_prc2_cclobby01",
            "bioa_prc2_cclobby02",
            "bioa_prc2_ccmid01",
            "bioa_prc2_ccmid02",
            "bioa_prc2_ccmid03",
            "bioa_prc2_ccmid04",
            "bioa_prc2_ccscoreboard",
            "bioa_prc2_ccsim01",
            "bioa_prc2_ccsim02",
            "bioa_prc2_ccsim03",
            "bioa_prc2_ccsim04",
            "bioa_prc2_ccspace02",
            "bioa_prc2_ccspace03",
            "bioa_prc2_ccthai01",
            "bioa_prc2_ccthai02",
            "bioa_prc2_ccthai03",
            "bioa_prc2_ccthai04",
            "bioa_prc2_ccthai05",
            "bioa_prc2_ccthai06",
        };

        private static void PreCorrectBioWorldInfoStreamingLevels(ExportEntry exp)
        {
            // Older games (ME1 at least) can reference levels that don't exist. This didn't breka game. Later games this does break
            // has a bunch of level references that don't exist

            //if (triggerStream.ObjectName.Instanced == "BioTriggerStream_0")
            //    Debugger.Break();
            var streamingLevels = exp.GetProperty<ArrayProperty<ObjectProperty>>("StreamingLevels");
            if (streamingLevels != null)
            {
                for (int i = streamingLevels.Count - 1; i >= 0; i--)
                {
                    var lsk = streamingLevels[i].ResolveToEntry(exp.FileRef) as ExportEntry;
                    var packageName = lsk.GetProperty<NameProperty>("PackageName");
                    if (VTest_NonExistentBTSFiles.Contains(packageName.Value.Instanced.ToLower()))
                    {
                        // Do not port this
                        Debug.WriteLine($@"Removed non-existent LSK package: {packageName.Value.Instanced} in {Path.GetFileNameWithoutExtension(exp.FileRef.FilePath)}");
                        streamingLevels.RemoveAt(i);
                    }
                    else
                    {
                        Debug.WriteLine($@"LSK package exists: {packageName.Value.Instanced} in {Path.GetFileNameWithoutExtension(exp.FileRef.FilePath)}");
                    }
                }

                exp.WriteProperty(streamingLevels);
            }
        }

        public static void PreCorrectMaterialInstanceConstant(ExportEntry exp)
        {
            // Some parameters need updated to match new materials

            // VECTORS
            var vectorParams = exp.GetProperty<ArrayProperty<StructProperty>>("VectorParameterValues");
            if (vectorParams != null)
            {
                foreach (var vp in vectorParams)
                {
                    var parameterName = vp.GetProp<NameProperty>("ParameterName").Value.Name;
                    var expressionGuid = CommonStructs.GetGuid(vp.GetProp<StructProperty>("ExpressionGUID"));
                    if (VTestMaterial.expressionGuidMap.TryGetValue(expressionGuid, out var newGuid) && VTestMaterial.parameterNameMap.TryGetValue(parameterName, out var newParameterName))
                    {
                        Debug.WriteLine($"Updating VP MIC {exp.InstancedFullPath}");
                        vp.GetProp<NameProperty>("ParameterName").Value = newParameterName;
                        vp.Properties.AddOrReplaceProp(CommonStructs.GuidProp(newGuid, "ExpressionGUID"));
                    }
                }
                exp.WriteProperty(vectorParams);
            }

            // TEXTURES
            var textureParams = exp.GetProperty<ArrayProperty<StructProperty>>("TextureParameterValues");
            if (textureParams != null)
            {
                foreach (var tp in textureParams)
                {
                    var parameterName = tp.GetProp<NameProperty>("ParameterName").Value.Name;
                    var expressionGuid = CommonStructs.GetGuid(tp.GetProp<StructProperty>("ExpressionGUID"));
                    if (VTestMaterial.expressionGuidMap.TryGetValue(expressionGuid, out var newGuid) && VTestMaterial.parameterNameMap.TryGetValue(parameterName, out var newParameterName))
                    {
                        Debug.WriteLine($"Updating TP MIC {exp.InstancedFullPath}");
                        tp.GetProp<NameProperty>("ParameterName").Value = newParameterName;
                        tp.Properties.AddOrReplaceProp(CommonStructs.GuidProp(newGuid, "ExpressionGUID"));
                    }
                }
                exp.WriteProperty(textureParams);
            }
        }

        /// <summary>
        /// PRECORRECTED as we do not want to have trash exports in target
        /// </summary>
        /// <param name="killTriggerSeq"></param>
        /// <param name="vTestOptions"></param>
        public static void PreCorrectKillTriggerVolume(ExportEntry killTriggerSeq, VTestOptions vTestOptions)
        {
            var sequenceObjects = KismetHelper.GetSequenceObjects(killTriggerSeq).OfType<ExportEntry>().ToList();
            var cursor = sequenceObjects.FirstOrDefault(x => x.ClassName == "SeqVar_Object");

            var compareObject = SequenceObjectCreator.CreateSequenceObject(killTriggerSeq.FileRef, "SeqCond_CompareObject", vTestOptions.cache);
            var playerObj = SequenceObjectCreator.CreateSequenceObject(killTriggerSeq.FileRef, "SeqVar_Player", vTestOptions.cache);

            KismetHelper.CreateVariableLink(compareObject, "A", cursor);
            KismetHelper.CreateVariableLink(compareObject, "B", playerObj);

            var takeDamage = SequenceObjectCreator.CreateSequenceObject(killTriggerSeq.FileRef, "BioSeqAct_CauseDamage", vTestOptions.cache);
            takeDamage.WriteProperty(new FloatProperty(50, "m_fDamageAmountAsPercentOfMaxHealth"));

            KismetHelper.AddObjectsToSequence(killTriggerSeq, false, takeDamage, compareObject, playerObj);

            KismetHelper.CreateVariableLink(takeDamage, "Target", cursor); // Hook up target to damage

            var doAction = sequenceObjects.FirstOrDefault(x => x.ClassName == "BioSeqAct_DoActionInVolume");
            var log = sequenceObjects.FirstOrDefault(x => x.ClassName == "SeqAct_Log");
            KismetHelper.RemoveOutputLinks(doAction);


            KismetHelper.CreateOutputLink(doAction, "Next", compareObject, 0); // Connect DoAction Next to CompareObject
            KismetHelper.CreateOutputLink(compareObject, "A == B", takeDamage); // TEST ONLY - TOUCHING PLAYER
                                                                                //KismetHelper.CreateOutputLink(compareObject, "A == B", doAction, 1); // Connect CompareObj to DoAction (The touching pawn is Player, skip damage)
            KismetHelper.CreateOutputLink(compareObject, "A != B", takeDamage); // Connect CompareObj to DoAction (The touching pawn is Player, skip damage)

            KismetHelper.CreateOutputLink(takeDamage, "Out", doAction, 1); // Connect DoAction Next to Damage In
            KismetHelper.CreateOutputLink(takeDamage, "Out", log); // Connect takedamage to log

            // TRASH AND REMOVE FROM SEQUENCE
            var destroy = sequenceObjects.FirstOrDefault(x => x.ClassName == "SeqAct_Destroy");
            KismetHelper.RemoveAllLinks(destroy);

            //remove from sequence
            var seqObjs = KismetHelper.GetParentSequence(destroy).GetProperty<ArrayProperty<ObjectProperty>>("SequenceObjects");
            seqObjs.Remove(new ObjectProperty(destroy));
            KismetHelper.GetParentSequence(destroy).WriteProperty(seqObjs);

            //Trash
            EntryPruner.TrashEntryAndDescendants(destroy);

            // Porting in object added BIOC_Base import
            // BIOC_BASE -> SFXGame
            var bcBaseIdx = killTriggerSeq.FileRef.findName("BIOC_Base");
            killTriggerSeq.FileRef.replaceName(bcBaseIdx, "SFXGame");
        }

        private static void ReduceDirectionalLights(IMEPackage le1File, float multiplier)
        {
            // Disabled due to static lighting changes
            return;
            foreach (var exp in le1File.Exports.Where(x => x.ClassName == "DirectionalLightComponent").ToList())
            {
                var brightness = exp.GetProperty<FloatProperty>("Brightness");
                if (brightness != null)
                {
                    brightness.Value *= multiplier;
                    exp.WriteProperty(brightness);
                }
            }
        }

        private static void FixGethFlashlights(ExportEntry sequence, VTestOptions vTestOptions)
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

        private static void InstallAhernAntiCheese(IMEPackage le1File)
        {
            // Clones and adds 2 blocking volumes to prevent you from getting out of the playable area of the map.
            // One is due to bad collision, the other is due to how cover calcuation changed in LE1 which likely allowed
            // cover where it should be and you could spin out of cover through a box.

            var sourcebv = le1File.FindExport("TheWorld.PersistentLevel.BlockingVolume_23");
            var ds3d = CommonStructs.Vector3Prop(0.5f, 0.5f, 0.25f, "DrawScale3D");

            // Northern cheese point
            var northBV = EntryCloner.CloneTree(sourcebv);
            northBV.RemoveProperty("bCollideActors");
            LevelTools.SetLocation(northBV, -38705.57f, -28901.904f, -2350.1252f);
            (northBV.GetProperty<ObjectProperty>("BrushComponent").ResolveToEntry(le1File) as ExportEntry).WriteProperty(new BoolProperty(false, "BlockZeroExtent")); // do not block gunfire
            northBV.WriteProperty(ds3d);

            // South cheese
            var southBV = EntryCloner.CloneTree(northBV); // already has scaling and guns fire through it
            LevelTools.SetLocation(southBV, -38702.812f, -24870.682f, -2355.5256f);
        }

        private static void RemoveBitExplosionEffect(ExportEntry exp)
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
        }

        private static void FixAhernConversation(IMEPackage le1File, VTestOptions vTestOptions)
        {
            // Ahern's conversation switches the position of Ahern and Competitors depending on which conversation branch you choose
            // I have no idea why Demiurge did this but it's confusing and bad design.
            // This conversation is in multiple files so we just check for the IFP, if we find it, we make the corrections.

            var ahernConv = le1File.FindExport("prc2_ahern_D.prc2_ahern_dlg");
            if (ahernConv == null)
                return; // Not in this file

            var entryIndicesToFix = new[] { 59, 61, 66, 64, 71, 53, 58, 55 };

            var properties = ahernConv.GetProperties();

            var entryList = properties.GetProp<ArrayProperty<StructProperty>>("m_EntryList");
            foreach (var idx in entryIndicesToFix)
            {
                var entry = entryList[idx];
                var replyList = entry.GetProp<ArrayProperty<StructProperty>>("ReplyListNew");

                // Heuristic to tell if we need to update this.
                var stringRef = replyList[2].GetProp<StringRefProperty>("srParaphrase");
                if (stringRef.Value == 182514) // Competitors
                    continue; // competitors is already in slot 3 (left middle)
                else if (stringRef.Value == 182517) // Ahern
                {
                    Debug.WriteLine($"Fixing ahern conversation for entry node {idx} in {le1File.FileNameNoExtension}");
                    var temp = replyList[2];
                    replyList[2] = replyList[3]; // Move competitors to slot 3 (left) from slot 4 (bottom right)
                    replyList[3] = temp; // Move ahern to bottom right, slot 4
                }
            }


            ahernConv.WriteProperties(properties);
        }

        private static void FixPlanters(IMEPackage le1File, VTestOptions vTestOptions)
        {
            // Planters need a mesh copied in since they got split into two pieces
            var fPath = Path.GetFileNameWithoutExtension(le1File.FilePath);
            switch (fPath)
            {
                case "BIOA_PRC2AA":
                    {
                        // PLANTER HIGH
                        using var planterSource = MEPackageHandler.OpenMEPackage(Path.Combine(MEDirectories.GetCookedPath(MEGame.LE1), "BIOA_ICE20_03_DSG.pcc"), forceLoadFromDisk: true);
                        EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, planterSource.FindExport("TheWorld.PersistentLevel.InterpActor_67"), le1File, le1File.FindEntry("TheWorld.PersistentLevel"), true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out var leavesSMA);
                        LevelTools.SetLocation(leavesSMA as ExportEntry, -35797.312f, 10758.975f, 6777.0386f);
                    }
                    break;
                case "BIOA_PRC2AA_00_LAY":
                    {
                        // PLANTER HIGH (DOOR)
                        using var planterSource = MEPackageHandler.OpenMEPackage(Path.Combine(MEDirectories.GetCookedPath(MEGame.LE1), "BIOA_ICE20_03_DSG.pcc"), forceLoadFromDisk: true);
                        EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, planterSource.FindExport("TheWorld.PersistentLevel.InterpActor_67"), le1File, le1File.FindEntry("TheWorld.PersistentLevel"), true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out var leavesHighSMA);
                        LevelTools.SetLocation(leavesHighSMA as ExportEntry, -35043.76f, 10664f, 6792.9917f);

                        // PLANTER MEDIUM (NEARBED)
                        EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, planterSource.FindExport("TheWorld.PersistentLevel.InterpActor_23"), le1File, le1File.FindEntry("TheWorld.PersistentLevel"), true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out var leavesMedSMA);
                        LevelTools.SetLocation(leavesMedSMA as ExportEntry, -35470.273f, 11690.752f, 6687.2974f + (112f * 0.6f)); // 112f is the offset

                        // PLANTER MEDIUM (TABLE)
                        var leavesMed2SMA = EntryCloner.CloneTree(leavesMedSMA);
                        LevelTools.SetLocation(leavesMed2SMA as ExportEntry, -34559.5f, 11378.695f, 6687.457f + (112f * 0.6f)); // 112f is the offset

                    }
                    break;
            }
        }
    }
}
