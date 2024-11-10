using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Pathing;
using LegendaryExplorerCore.Unreal;
using System.Linq;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCAhern_DSG : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PostPortingCorrection()
        {
            // Kill streak voice line sequence
            VTestKismet.InstallVTestHelperSequenceNoInput(le1File, "TheWorld.PersistentLevel.Main_Sequence", "HelperSequences.KillStreakVoiceLine", vTestOptions);

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
                    BIOA_PRC2_CC_DSG.FixSimMapTextureLoading(VTestKismet.FindSequenceObjectByClassAndPosition(exp, "BioSeqAct_Delay", -8501, -1086), vTestOptions, streamInLocNode);

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
                    BIOA_PRC2_CC_DSG.RemoveBitExplosionEffect(exp, vTestOptions);
                    BIOA_PRC2_CC_DSG.FixGethFlashlights(exp, vTestOptions); // This is just for consistency
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
            var chargeAi = EntryImporter.EnsureClassIsInFile(le1File, "CrossgenAI_Charge", new RelinkerOptionsPackage() { PortExportsAsImportsWhenPossible = true, Cache = vTestOptions.cache });
            foreach (var exp in le1File.Exports.Where(x => x.idxLink == root.UIndex))
            {
                if (!exp.ObjectName.Name.Contains("Sniper"))
                {
                    exp.WriteProperty(new ObjectProperty(chargeAi, "AIController"));
                }
            }
            // Make memory unique
            root.ObjectName = "Mercenary_Ahern_Crossgen";

            BIOA_PRC2_CC_DSG.PreventSavingOnSimLoad(le1File, vTestOptions);
            BIOA_PRC2_CC_DSG.ResetRamping(le1File, vTestOptions);

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

            InstallAhernAntiCheese();
        }

        private void InstallAhernAntiCheese()
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
    }
}
