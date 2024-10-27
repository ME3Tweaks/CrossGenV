using CrossGenV.Classes.Levels;
using LegendaryExplorerCore.Gammtek.Extensions.Collections.Generic;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Pathing;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Kismet;

namespace CrossGenV.Classes
{
    /// <summary>
    /// Corrections to run after porting is done for VTest
    /// </summary>
    public static class VTestPostCorrections
    {
        /// <summary>
        /// Corrections to run after porting is done
        /// </summary>
        /// <param name="me1File"></param>
        /// <param name="le1File"></param>
        /// <param name="vTestOptions"></param>
        public static void PostPortingCorrections(IMEPackage me1File, IMEPackage le1File, VTestOptions vTestOptions)
        {
            var fName = Path.GetFileNameWithoutExtension(le1File.FilePath);

            // Copy over the AdditionalPackagesToCook
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
            vTestOptions.SetStatusText($"PPC (Localizations)");
            VTestLocalization.FixLocalizations(le1File, vTestOptions);
            //vTestOptions.SetStatusText($"PPC (Lighting)"); // Disabled in static build
            //FixLighting(le1File, vTestOptions);
            vTestOptions.SetStatusText($"PPC (Ahern Conversation)");
            FixAhernConversation(le1File, vTestOptions);

            // 10/19/2024 - Stream in all materials when map load signal occurs
            vTestOptions.SetStatusText($"PPC (StreamInTextures)");
            VTestTextures.InstallPrepTextures(le1File, vTestOptions);

            // Disabled 08/18/2024 - Do not use assets for optimization as we now rebake lighting
            //vTestOptions.SetStatusText($"PPC (Optimization)");
            //PortME1OptimizationAssets(me1File, le1File, vTestOptions);

            // 10/19/2024 - Remove incompatible properties on sequence objects as it prevents script compiler from working
            CorrectSequenceOpProperties(le1File, vTestOptions);

            vTestOptions.SetStatusText($"PPC (LEVEL SPECIFIC)");
            // Global lighting changes
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

            // Reduce directional lights - these are not in their own files because ReduceDirectionalLights doesn't do anything anymore and I'm lazy
            switch (fName.ToUpper())
            {
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
            }

            // 10/14/24 - All level specific corrections have been moved to their own classes in the Levels folder.
            var levelSpecificCorrections = LevelCorrectionFactory.GetLevel(fName, me1File, le1File, vTestOptions);
            levelSpecificCorrections.PostPortingCorrection();

            var level = le1File.FindExport("TheWorld.PersistentLevel");
            if (level != null)
            {
                LevelTools.RebuildPersistentLevelChildren(level);
            }
            //CorrectTriggerStreamsMaybe(me1File, le1File);
        }

        private static void CorrectSequenceOpProperties(IMEPackage package, VTestOptions options)
        {
            bool prunedTransient = false;

            foreach (var instancedDefaults in package.Exports.Where(x => x.IsA("SequenceOp")))
            {
                // This gets class defaults as well as instances of the class, but not the class itself, which is exactly what we want.

                var props = instancedDefaults.GetProperties();

                var inputLinks = props.GetProp<ArrayProperty<StructProperty>>("InputLinks")?.Properties;
                var variableLinks = props.GetProp<ArrayProperty<StructProperty>>("VariableLinks");
                var outputLinks = props.GetProp<ArrayProperty<StructProperty>>("OutputLinks");
                var eventLinks = props.GetProp<ArrayProperty<StructProperty>>("EventLinks");

                bool pruned = false;
                if (inputLinks != null)
                {
                    foreach (var link in inputLinks)
                    {
                        link.Properties = EntryPruner.RemoveIncompatibleProperties(package, link.Properties, "SeqOpInputLink", MEGame.LE1, ref prunedTransient);
                        pruned |= prunedTransient;
                    }
                }

                if (variableLinks != null)
                {
                    foreach (var link in variableLinks)
                    {
                        link.Properties = EntryPruner.RemoveIncompatibleProperties(package, link.Properties, "SeqVarLink", MEGame.LE1, ref prunedTransient);
                        pruned |= prunedTransient;
                    }
                }

                if (outputLinks != null)
                {
                    foreach (var link in outputLinks)
                    {
                        link.Properties = EntryPruner.RemoveIncompatibleProperties(package, link.Properties, "SeqOpOutputLink", MEGame.LE1, ref prunedTransient);
                        pruned |= prunedTransient;
                    }
                }

                if (eventLinks != null)
                {
                    foreach (var link in eventLinks)
                    {
                        link.Properties = EntryPruner.RemoveIncompatibleProperties(package, link.Properties, "SeqEventLink", MEGame.LE1, ref prunedTransient);
                        pruned |= prunedTransient;
                    }
                }

                if (prunedTransient)
                {
                    instancedDefaults.WriteProperties(props);
                }
            }

            if (prunedTransient)
            {
                options.SetStatusText("PPC (Pruned incompatible properties)");
            }
        }

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

        public static void DisallowRunningUntilModeStarts(IMEPackage le1File, VTestOptions vTestOptions)
        {
            var mainSeq = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence");
            var levelLoaded = SequenceObjectCreator.CreateLevelLoaded(mainSeq, vTestOptions.cache);
            var remoteEvent = SequenceObjectCreator.CreateActivateRemoteEvent(mainSeq, "DisallowRunning", vTestOptions.cache);
            KismetHelper.CreateOutputLink(levelLoaded, "Loaded and Visible", remoteEvent);
        }

        private static void FixAhernConversation(IMEPackage le1File, VTestOptions vTestOptions)
        {

            var ahernConv = le1File.FindExport("prc2_ahern_D.prc2_ahern_dlg");
            if (ahernConv == null)
                return; // Not in this file

            var properties = ahernConv.GetProperties();

            var entryList = properties.GetProp<ArrayProperty<StructProperty>>("m_EntryList");
            var replyList = properties.GetProp<ArrayProperty<StructProperty>>("m_ReplyList");


            // Ahern's conversation switches the position of Ahern and Competitors depending on which conversation branch you choose
            // I have no idea why Demiurge did this but it's confusing and bad design.
            // This conversation is in multiple files so we just check for the IFP, if we find it, we make the corrections.
            {
                var entryIndicesToFix = new[] { 59, 61, 66, 64, 71, 53, 58, 55 };

                foreach (var idx in entryIndicesToFix)
                {
                    var entry = entryList[idx];
                    var replyListNew = entry.GetProp<ArrayProperty<StructProperty>>("ReplyListNew");

                    // Heuristic to tell if we need to update this.
                    var stringRef = replyListNew[2].GetProp<StringRefProperty>("srParaphrase");
                    if (stringRef.Value == 182514) // Competitors
                        continue; // competitors is already in slot 3 (left middle)
                    else if (stringRef.Value == 182517) // Ahern
                    {
                        Debug.WriteLine($"Fixing ahern conversation for entry node {idx} in {le1File.FileNameNoExtension}");
                        var temp = replyListNew[2];
                        replyListNew[2] = replyListNew[3]; // Move competitors to slot 3 (left) from slot 4 (bottom right)
                        replyListNew[3] = temp; // Move ahern to bottom right, slot 4
                    }
                }
            }


            // Install fix for refusal of ahern's mission leading right into his normal conversation
            {
                // State transitions and bool checks added to set a flag when Ahern brings his mission up a second time
                entryList[44].Properties.AddOrReplaceProp(new IntProperty(6436, "nStateTransition"));
                replyList[48].Properties.AddOrReplaceProp(new IntProperty(7658, "nConditionalFunc"));
                replyList[48].Properties.AddOrReplaceProp(new IntProperty(1, "nConditionalParam"));
                replyList[48].Properties.AddOrReplaceProp(new BoolProperty(false, "bFireConditional"));

                // New conversation end node after this scene. Only triggered if we're on the "impressive work" branch.
                // Otherwise, if this conversation is repeated, it continues on to Ahern's normal conversation
                var replyNode = GlobalUnrealObjectInfo.getDefaultStructValue(MEGame.LE1, "BioDialogReplyNode", true, le1File);
                replyNode.AddOrReplaceProp(new EnumProperty("REPLY_DIALOGEND", "EReplyTypes", MEGame.LE1, "ReplyType"));
                replyNode.AddOrReplaceProp(new StringRefProperty(183458, "srText"));
                replyNode.AddOrReplaceProp(new IntProperty(-1, "nConditionalFunc"));
                replyNode.AddOrReplaceProp(new IntProperty(-1, "nConditionalParam"));
                replyNode.AddOrReplaceProp(new IntProperty(-1, "nStateTransition"));
                replyNode.AddOrReplaceProp(new IntProperty(-1, "nStateTransitionParam"));
                replyNode.AddOrReplaceProp(new IntProperty(-1, "nScriptIndex"));
                replyNode.AddOrReplaceProp(new IntProperty(1, "nCameraIntimacy"));

                replyList.Add(new StructProperty("BioDialogReplyNode", replyNode));
                var newCheck = entryList[42].GetProp<ArrayProperty<StructProperty>>("ReplyListNew");
                newCheck.Add(new StructProperty("BioDialogReplyListDetails", false,
                    new IntProperty(replyList.Count - 1, "nIndex"),
                    new StringRefProperty(183458, "srParaphrase")));
            }
            ahernConv.WriteProperties(properties);
        }

        private static object _shaderDonorSync = new();

        /// <summary>
        /// Installs a custom shader from a donor package
        /// </summary>
        /// <param name="le1File"></param>
        public static void AddCustomShader(IMEPackage le1File, string materialIfp)
        {
            var target = le1File.FindExport(materialIfp);
            if (target == null)
            {
                Debug.WriteLine("Target material not found, this may be normal.");
                return;
            }

            // We will port out of existing package. We lock to prevent concurrent load
            IMEPackage donorPackage = null;
            lock (_shaderDonorSync)
            {
                donorPackage = MEPackageHandler.OpenMEPackage(Path.Combine(VTestPaths.VTest_DonorsDir, $"{materialIfp}.pcc"));
            }

            var source = donorPackage.FindExport(materialIfp);

            // Just replace with same data. It should trigger a ShaderCache update
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.ReplaceSingularWithRelink, source, le1File, target, true, new RelinkerOptionsPackage(), out _);
        }
    }
}
