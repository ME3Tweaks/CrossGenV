using CrossGenV.Classes.Levels;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Gammtek.Extensions;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CrossGenV.Classes
{
    /// <summary>
    /// Corrections to run before porting is done for VTest
    /// </summary>
    public static class VTestPreCorrections
    {
        public static void PrePortingCorrections(IMEPackage me1Package, IMEPackage le1Package, VTestOptions vTestOptions)
        {
            // 10/14/24 - All level specific corrections have been moved to their own classes in the Levels folder.
            var sourcePackageName = Path.GetFileNameWithoutExtension(me1Package.FilePath).ToUpper();
            var levelSpecificCorrections = LevelCorrectionFactory.GetLevel(sourcePackageName, me1Package, le1Package, vTestOptions);
            levelSpecificCorrections.PrePortingCorrection();


            // Strip static mesh light maps since they don't work crossgen. Strip them from
            // the source so they don't port
            foreach (var exp in me1Package.Exports.ToList())
            {
                PruneUnusedProperties(exp);
                #region Remove Light and Shadow Maps
                if (exp.ClassName == "StaticMeshComponent")
                {
                    //if Non-Collection Static add LE Bool
                    if (exp.Parent.ClassName == "StaticMeshActor")
                    {
                        var props = exp.GetProperties();
                        props.AddOrReplaceProp(new BoolProperty(true, "bIsOwnerAStaticMeshActor"));
                        exp.WriteProperties(props);
                    }

                    if (vTestOptions == null || vTestOptions.useDynamicLighting || vTestOptions.stripShadowMaps)
                    {
                        if (vTestOptions != null && vTestOptions.allowTryingPortedMeshLightMap && !sourcePackageName.StartsWith("BIOA_PRC2AA")) // BIOA_PRC2AA doesn't seem to work with lightmaps
                        {
                            var sm = exp.GetProperty<ObjectProperty>("StaticMesh"); // name might need changed?
                            if (sm != null)
                            {
                                if (me1Package.TryGetEntry(sm.Value, out var smEntry))
                                {
                                    // Disabled due to static lighting build
                                    //if (ShouldPortLightmaps(smEntry))
                                    //{
                                    //    continue; // Do not port
                                    //}
                                }
                            }
                        }

                        var b = ObjectBinary.From<StaticMeshComponent>(exp);
                        foreach (var lod in b.LODData)
                        {
                            // Clear light and shadowmaps
                            if (vTestOptions == null || vTestOptions.stripShadowMaps || vTestOptions.useDynamicLighting)
                            {
                                lod.ShadowMaps = [0];
                            }

                            if (vTestOptions == null || vTestOptions.useDynamicLighting)
                            {
                                lod.LightMap = new LightMap() { LightMapType = ELightMapType.LMT_None };
                            }
                        }

                        exp.WriteBinary(b);
                    }
                }
                #endregion
                //// These are precomputed and stored in VTestHelper.pcc 
                //else if (exp.ClassName == "Terrain")
                //{
                //    exp.RemoveProperty("TerrainComponents"); // Don't port the components; we will port them ourselves in post
                //}
                else if (exp.ClassName == "BioTriggerStream")
                {
                    PreCorrectBioTriggerStream(exp);
                }
                else if (exp.ClassName == "BioWorldInfo")
                {
                    // Remove streaminglevels that don't do anything
                    //PreCorrectBioWorldInfoStreamingLevels(exp);
                }
                else if (exp.ClassName == "MaterialInstanceConstant")
                {
                    PreCorrectMaterialInstanceConstant(exp);
                }
                else if (exp.ClassName == "ModelComponent")
                {
                    var mcb = ObjectBinary.From<ModelComponent>(exp);
                    if (vTestOptions.useDynamicLighting)
                    {
                        foreach (var elem in mcb.Elements)
                        {
                            elem.ShadowMaps = [0]; // We want no shadowmaps
                            elem.LightMap = new LightMap() { LightMapType = ELightMapType.LMT_None }; // Strip the lightmaps
                        }
                    }
                    else
                    {
                        //foreach (var elem in mcb.Elements)
                        //{
                        //    KeepLightmapTextures(elem.LightMap, me1Package);
                        //}
                    }

                    exp.WriteBinary(mcb);

                }
                else if (exp.ClassName == "Sequence" && exp.GetProperty<StrProperty>("ObjName")?.Value == "PRC2_KillTriggerVolume")
                {
                    PreCorrectKillTriggerVolume(exp, vTestOptions);
                }
                else if (exp.IsA("LightComponent"))
                {
                    // 08/24/2024 - Enable all lights
                    exp.RemoveProperty("bEnabled");
                }

                // KNOWN BAD NAMES
                // Rename duplicate texture/material objects to be unique to avoid some issues with tooling
                if (exp.ClassName == "Texture2D")
                {
                    if (exp.InstancedFullPath == "BIOA_JUG80_T.JUG80_SAIL")
                    {
                        // Rename to match crossgen
                        exp.ObjectName = "JUG80_SAIL_CROSSGENFIX";
                    }
                    else if (exp.InstancedFullPath == "BIOA_ICE60_T.checker")
                    {
                        // Rename to match crossgen
                        exp.ObjectName = "BIOA_ICE60_T.checker_CROSSGENFIX";
                    }
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
                //Debug.WriteLine($"{triggerStream.InstancedFullPath} in {triggerStream} has NO StreamingStates!!");
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
                        // Debug.WriteLine($"Updating VP MIC {exp.InstancedFullPath}");
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
                        // Debug.WriteLine($"Updating TP MIC {exp.InstancedFullPath}");
                        tp.GetProp<NameProperty>("ParameterName").Value = newParameterName;
                        tp.Properties.AddOrReplaceProp(CommonStructs.GuidProp(newGuid, "ExpressionGUID"));
                    }
                }
                exp.WriteProperty(textureParams);
            }
        }

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

        private static void PruneUnusedProperties(ExportEntry exp)
        {
            // Lots of components are not used or don't exist and can't be imported in LE1
            // Get rid of them here
            PropertyCollection props = exp.GetProperties();

            // Might be better to enumerate all object properties and trim out ones that reference
            // known non-existent things
            if (exp.IsA("LightComponent"))
            {
                props.RemoveNamedProperty("PreviewInnerCone");
                props.RemoveNamedProperty("PreviewOuterCone");
                props.RemoveNamedProperty("PreviewLightRadius");
            }

            if (exp.IsA("NavigationPoint"))
            {
                props.RemoveNamedProperty("GoodSprite");
                props.RemoveNamedProperty("BadSprite");
            }

            if (exp.IsA("BioArtPlaceable"))
            {
                props.RemoveNamedProperty("CoverMesh"); // Property exists but is never set
            }

            if (exp.IsA("SoundNodeAttenuation"))
            {
                props.RemoveNamedProperty("LPFMinRadius");
                props.RemoveNamedProperty("LPFMaxRadius");
            }

            if (exp.IsA("BioAPCoverMeshComponent"))
            {
                exp.Archetype = null; // Remove the archetype. This is on BioDoor's and does nothing in practice, in ME1 there is nothing to copy from the archetype
            }

            if (exp.IsA("BioSquadCombat"))
            {
                props.RemoveNamedProperty("m_oSprite");
            }

            if (exp.IsA("CameraActor"))
            {
                props.RemoveNamedProperty("MeshComp"); // some actors have a camera mesh that was probably used to better visualize in-editor
            }

            exp.WriteProperties(props);
        }


        /// <summary>
        /// Creates a static mesh actor from an SMC (that is not under collection - e.g. interpactor)
        /// </summary>
        /// <param name="smc"></param>
        /// <returns></returns>
        public static ExportEntry CreateSMAFromSMC(ExportEntry smc)
        {
            var level = smc.FileRef.GetLevel();
            var sma = ExportCreator.CreateExport(smc.FileRef, "StaticMeshActor", "StaticMeshActor", level, createWithStack: true);

            PropertyCollection props = new PropertyCollection();
            props.AddOrReplaceProp(new ObjectProperty(smc, "StaticMeshComponent"));
            props.AddOrReplaceProp(new ObjectProperty(smc, "CollisionComponent"));
            props.AddOrReplaceProp(new BoolProperty(true, "bCollideActors"));
            sma.WriteProperties(props);

            var parent = smc.Parent as ExportEntry;

            var loc = parent.GetProperty<StructProperty>("Location");
            if (loc != null) sma.WriteProperty(loc);

            var rot = parent.GetProperty<StructProperty>("Rotation");
            if (rot != null) sma.WriteProperty(rot);

            smc.Archetype = GetImportArchetype(smc.FileRef, "Engine", "Default__StaticMeshActor.StaticMeshComponent0");
            var lightingChannels = smc.GetProperty<StructProperty>("LightingChannels")?.Properties ?? new PropertyCollection();
            lightingChannels.AddOrReplaceProp(new BoolProperty(true, "bInitialized"));
            lightingChannels.AddOrReplaceProp(new BoolProperty(true, "Static")); // Add static
            smc.WriteProperty(new StructProperty("LightingChannelContainer", lightingChannels, "LightingChannels"));
            smc.RemoveProperty("bUsePrecomputedShadows"); // Required for static lighting
            smc.Parent = sma; // Move under SMA

            return sma;
        }
        
        internal static IEntry GetImportArchetype(IMEPackage package, string packageFile, string ifp)
        {
            IEntry result = package.FindExport($"{packageFile}.{ifp}");
            if (result != null)
                return result;

            var file = $"{packageFile}.{(package.Game is MEGame.ME1 or MEGame.UDK ? "u" : "pcc")}";
            var fullPath = Path.Combine(MEDirectories.GetCookedPath(package.Game), file);
            using var lookupPackage = MEPackageHandler.UnsafePartialLoad(fullPath, x => false);
            var entry = lookupPackage.FindExport(ifp) as IEntry;
            if (entry == null)
                Debugger.Break();

            Stack<IEntry> children = new Stack<IEntry>();
            children.Push(entry); // Must port at least the found IFP.
            while (entry.Parent != null && package.FindEntry(entry.ParentInstancedFullPath) == null)
            {
                children.Push(entry.Parent);
                entry = entry.Parent;
            }

            // Create imports from top down.
            var packageExport = (IEntry)ExportCreator.CreatePackageExport(package, packageFile);

            // This doesn't work if the part of the parents already exist.
            var attachParent = packageExport;
            foreach (var item in children)
            {
                ImportEntry imp = new ImportEntry(item as ExportEntry, packageExport.UIndex, package);
                imp.idxLink = attachParent.UIndex;
                package.AddImport(imp);
                attachParent = imp;
                result = imp;
            }

            return result;
        }
    }
}
