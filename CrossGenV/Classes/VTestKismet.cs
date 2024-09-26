using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossGenV.Classes
{
    /// <summary>
    /// Handles Kismet-specific things
    /// </summary>
    public class VTestKismet
    {
        /// <summary>
        /// Checks if the specified sequence object is contained within a named sequence. Can be used to find sequences that are templated embedded within other different sequences.
        /// </summary>
        /// <param name="sequenceObject"></param>
        /// <param name="seqName"></param>
        /// <param name="fullParentChain"></param>
        /// <returns></returns>
        public static bool IsContainedWithinSequenceNamed(ExportEntry sequenceObject, string seqName, bool fullParentChain = true)
        {
            var parent = KismetHelper.GetParentSequence(sequenceObject);
            while (parent != null)
            {
                var parentName = parent.GetProperty<StrProperty>("ObjName");
                if (parentName?.Value == seqName)
                    return true;
                if (!fullParentChain)
                    break;
                parent = KismetHelper.GetParentSequence(parent);
            }

            return false;
        }

        public static ExportEntry FindSequenceObjectByClassAndPosition(ExportEntry sequence, string className, int posX = int.MinValue, int posY = int.MinValue)
        {
            var seqObjs = sequence.GetProperty<ArrayProperty<ObjectProperty>>("SequenceObjects")
                .Select(x => x.ResolveToEntry(sequence.FileRef)).OfType<ExportEntry>().Where(x => x.ClassName == className).ToList();

            foreach (var obj in seqObjs)
            {
                if (posX != int.MinValue && posY != int.MinValue)
                {
                    var props = obj.GetProperties();
                    var foundPosX = props.GetProp<IntProperty>("ObjPosX")?.Value;
                    var foundPosY = props.GetProp<IntProperty>("ObjPosY")?.Value;
                    if (foundPosX != null && foundPosY != null &&
                        foundPosX == posX && foundPosY == posY)
                    {
                        return obj;
                    }
                }
                else if (seqObjs.Count == 1)
                {
                    return obj; // First object
                }
                else
                {
                    throw new Exception($"COULD NOT FIND OBJECT OF TYPE {className} in {sequence.InstancedFullPath}");
                }
            }

            return null;
        }

        /// <summary>
        /// Adds a ActivateRemoteEvent kismet object as an output of the specified IFP.
        /// </summary>
        /// <param name="le1File"></param>
        /// <param name="vTestOptions"></param>
        public static void InstallRemoteEventSignal(IMEPackage le1File, string sourceIFP, string remoteEventName, VTestOptions vTestOptions, string outlinkName = "Out")
        {
            var source = le1File.FindExport(sourceIFP);
            var sequence = KismetHelper.GetParentSequence(source);
            var remoteEvent = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_ActivateRemoteEvent", vTestOptions.cache);
            KismetHelper.AddObjectToSequence(remoteEvent, sequence);
            remoteEvent.WriteProperty(new NameProperty(remoteEventName, "EventName"));
            KismetHelper.CreateOutputLink(source, outlinkName, remoteEvent);
        }

        /// <summary>
        /// Changes sequencing a bit to install a force-load of mips plus a delay
        /// </summary>
        /// <param name="findSequenceObjectByClassAndPosition"></param>
        public static void FixSimMapTextureLoading(ExportEntry startDelay, VTestOptions vTestOptions, ExportEntry streamingLocation = null)
        {
            var sequence = KismetHelper.GetParentSequence(startDelay);
            var stopLoadingMovie = FindSequenceObjectByClassAndPosition(sequence, "BioSeqAct_StopLoadingMovie");
            KismetHelper.RemoveOutputLinks(startDelay);

            var streamInTextures = SequenceObjectCreator.CreateSequenceObject(startDelay.FileRef, "SeqAct_StreamInTextures", vTestOptions.cache);
            var streamInDelay = SequenceObjectCreator.CreateSequenceObject(startDelay.FileRef, "SeqAct_Delay", vTestOptions.cache);
            var remoteEventStreamIn = SequenceObjectCreator.CreateSequenceObject(startDelay.FileRef, "SeqAct_ActivateRemoteEvent", vTestOptions.cache);

            KismetHelper.AddObjectToSequence(remoteEventStreamIn, sequence);
            KismetHelper.AddObjectToSequence(streamInTextures, sequence);
            KismetHelper.AddObjectToSequence(streamInDelay, sequence);

            streamInDelay.WriteProperty(new FloatProperty(4f, "Duration")); // Load screen will be 4s
            streamInTextures.WriteProperty(new FloatProperty(8f, "Seconds")); // Force textures to stream in at full res for a bit over the load screen time
            remoteEventStreamIn.WriteProperty(new NameProperty("CROSSGEN_PrepTextures", "EventName")); // This is used to signal other listeners that they should also stream in textures

            streamingLocation ??= KismetHelper.GetSequenceObjects(sequence).OfType<ExportEntry>().First(x => x.ClassName == "SeqVar_External" && x.GetProperty<StrProperty>("VariableLabel")?.Value == "Scenario_Start_Location");
            KismetHelper.CreateVariableLink(streamInTextures, "Location", streamingLocation);

            KismetHelper.CreateOutputLink(startDelay, "Finished", remoteEventStreamIn); // Initial 1 frame delay to event signal
            KismetHelper.CreateOutputLink(remoteEventStreamIn, "Out", streamInTextures); // Event Signal to StreamInTextures
            KismetHelper.CreateOutputLink(remoteEventStreamIn, "Out", streamInDelay); // Event Signal to Loading Screen Delay
            KismetHelper.CreateOutputLink(streamInDelay, "Finished", stopLoadingMovie); // Loading Screen Delay to Stop Loading Movie
        }

        /// <summary>
        /// Sets up sequencing to stream in the listed materials for 5 seconds in the specified stream
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="materialsToStreamIn"></param>
        public static void CreateSignaledTextureStreaming(ExportEntry sequence, string[] materialsToStreamIn, VTestOptions vTestOptions)
        {

            var remoteEvent = SequenceObjectCreator.CreateSequenceObject(sequence.FileRef, "SeqEvent_RemoteEvent", vTestOptions.cache);
            var streamInTextures = SequenceObjectCreator.CreateSequenceObject(sequence.FileRef, "SeqAct_StreamInTextures", vTestOptions.cache);

            KismetHelper.AddObjectToSequence(remoteEvent, sequence);
            KismetHelper.AddObjectToSequence(streamInTextures, sequence);

            streamInTextures.WriteProperty(new FloatProperty(5f, "Seconds")); // Force textures to stream in at full res for a bit over the load screen time
            var materials = new ArrayProperty<ObjectProperty>("ForceMaterials");
            foreach (var matIFP in materialsToStreamIn)
            {
                var entry = sequence.FileRef.FindEntry(matIFP);
                if (entry == null) Debugger.Break(); // THIS SHOULDN'T HAPPEN
                materials.Add(new ObjectProperty(entry));
            }
            streamInTextures.WriteProperty(materials);

            remoteEvent.WriteProperty(new NameProperty("CROSSGEN_PrepTextures", "EventName"));

            KismetHelper.CreateOutputLink(remoteEvent, "Out", streamInTextures);

        }

        /// <summary>
        /// Installs a sequence from VTestHelper. The sequence will be connected via the In pin.
        /// </summary>
        /// <param name="le1File"></param>
        /// <param name="sourceSequenceOpIFP"></param>
        /// <param name="vTestSequenceIFP"></param>
        /// <param name="vTestOptions"></param>
        public static void InstallVTestHelperSequenceViaOut(IMEPackage le1File, string sourceSequenceOpIFP, string vTestSequenceIFP, bool runOnceOnly, VTestOptions vTestOptions, out ExportEntry gate)
        {
            gate = null;
            var sourceItemToOutFrom = le1File.FindExport(sourceSequenceOpIFP);
            var parentSequence = KismetHelper.GetParentSequence(sourceItemToOutFrom, true);
            var donorSequence = vTestOptions.vTestHelperPackage.FindExport(vTestSequenceIFP);
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, donorSequence, le1File, parentSequence, true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out var newUiSeq);
            KismetHelper.AddObjectToSequence(newUiSeq as ExportEntry, parentSequence);

            if (runOnceOnly)
            {
                gate = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_Gate", vTestOptions.cache);
                KismetHelper.AddObjectToSequence(gate, parentSequence);
                // link it up
                KismetHelper.CreateOutputLink(sourceItemToOutFrom, "Out", gate);
                KismetHelper.CreateOutputLink(gate, "Out", gate, 2); // close self
                KismetHelper.CreateOutputLink(gate, "Out", newUiSeq as ExportEntry);
            }
            else
            {
                // link it up
                KismetHelper.CreateOutputLink(sourceItemToOutFrom, "Out", newUiSeq as ExportEntry);
            }
        }

        /// <summary>
        /// Installs a sequence from VTestHelper. The sequence should already contain it's own triggers like LevelLoaded.
        /// </summary>
        /// <param name="le1File"></param>
        /// <param name="eventIFP"></param>
        /// <param name="vTestSequenceIFP"></param>
        /// <param name="vTestOptions"></param>
        public static ExportEntry InstallVTestHelperSequenceNoInput(IMEPackage le1File, string sequenceIFP, string vTestSequenceIFP, VTestOptions vTestOptions)
        {
            var sequence = le1File.FindExport(sequenceIFP);
            var donorSequence = vTestOptions.vTestHelperPackage.FindExport(vTestSequenceIFP);
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, donorSequence, le1File, sequence, true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out var newUiSeq);
            KismetHelper.AddObjectToSequence(newUiSeq as ExportEntry, sequence);
            return newUiSeq as ExportEntry;
        }

        /// <summary>
        /// Installs a sequence from VTestHelper. The sequence should already contain it's own triggers like LevelLoaded.
        /// </summary>
        /// <param name="le1File"></param>
        /// <param name="eventIFP"></param>
        /// <param name="vTestSequenceIFP"></param>
        /// <param name="vTestOptions"></param>
        public static ExportEntry InstallVTestHelperSequenceViaEvent(IMEPackage le1File, string eventIFP, string vTestSequenceIFP, VTestOptions vTestOptions, string outName = "Out")
        {
            var targetEvent = le1File.FindExport(eventIFP);
            var sequence = KismetHelper.GetParentSequence(targetEvent);
            var donorSequence = vTestOptions.vTestHelperPackage.FindExport(vTestSequenceIFP);
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, donorSequence, le1File, sequence, true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out var newUiSeq);
            KismetHelper.AddObjectToSequence(newUiSeq as ExportEntry, sequence);
            KismetHelper.CreateOutputLink(targetEvent, outName, newUiSeq as ExportEntry);
            return newUiSeq as ExportEntry;
        }
        public static List<ExportEntry> GetSequenceObjectReferences(ExportEntry seq, string sequenceName)
        {
            var seqObjs = KismetHelper.GetSequenceObjects(seq).OfType<ExportEntry>().ToList();
            var seqRefs = seqObjs.Where(x => x.ClassName == "SequenceReference");
            var references = seqRefs.Where(x =>
                x.GetProperty<ObjectProperty>("oSequenceReference") is ObjectProperty op && op?.Value != 0 &&
                seq.FileRef.GetUExport(op.Value) is ExportEntry sequence &&
                sequence.GetProperty<StrProperty>("ObjName")?.Value == sequenceName).ToList();
            return references;
        }

        // We can spawn up to this many additional enemies.
        private static readonly int MaxEnemyRampCount = 12;

        /// <summary>
        /// Clones respawners, activating them over time.
        /// </summary>
        /// <param name="seq"></param>
        /// <param name="vTestOptions"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void InstallEnemyCountRamp(ExportEntry startTimerObj, ExportEntry seq, VTestOptions vTestOptions)
        {
            var respawners = GetSequenceObjectReferences(seq, "SUR_Respawner");

            List<ExportEntry> newRespawners = new List<ExportEntry>();
            if (respawners.Any())
            {
                int currentEnemyNum = respawners.Count + 1;

                var currentRamp = 0;
                while (currentRamp < MaxEnemyRampCount)
                {
                    var links = KismetHelper.GetVariableLinksOfNode(respawners[0]);
                    var newRespawner = KismetHelper.CloneObject(respawners[0], cloneChildren: true);
                    newRespawners.Add(newRespawner);
                    var enemyNum = SequenceObjectCreator.CreateInt(seq, currentEnemyNum, vTestOptions.cache);
                    links[2].LinkedNodes[0] = enemyNum; // Repoint to our new enemy number

                    KismetHelper.WriteVariableLinksToNode(newRespawner, links);


                    var outLinks = KismetHelper.GetOutputLinksOfNode(respawners[0]);
                    outLinks[1].Clear(); // Remove 'DoneInitializing'
                    KismetHelper.WriteOutputLinksToNode(newRespawner, outLinks);



                    // DoneInitializing -> Activate on itself to start the spawn
                    KismetHelper.CreateOutputLink(newRespawner, "DoneInitializing", newRespawner, 0);

                    currentEnemyNum++;
                    currentRamp++;
                }
            }

            float startTime = 30f;
            var currentTime = SequenceObjectCreator.CreateScopeNamed(seq, "SeqVar_Float", "OFFICIAL_TIME", vTestOptions.cache);
            var gameHandler = GetSequenceObjectReferences(seq, "Check_Capping_Completion");

            ExportEntry previousCompareFloat = null;
            foreach (var respawner in newRespawners)
            {
                var gate = SequenceObjectCreator.CreateGate(seq, vTestOptions.cache);
                KismetHelper.CreateOutputLink(gate, "Out", respawner, 1); // Gate to Initialize
                KismetHelper.CreateOutputLink(gate, "Out", gate, 2); // Close gate

                var time = SequenceObjectCreator.CreateFloat(seq, startTime);
                var compare = SequenceObjectCreator.CreateCompareFloat(seq, currentTime, time, vTestOptions.cache);

                KismetHelper.CreateOutputLink(compare, "A >= B", gate);
                if (previousCompareFloat != null)
                {
                    // Link to next compare
                    KismetHelper.CreateOutputLink(previousCompareFloat, "A >= B", compare);
                }
                else
                {
                    KismetHelper.CreateOutputLink(gameHandler[0], "Update_Guys", compare);
                }

                previousCompareFloat = compare;
                startTime += 10;
            }
        }
    }
}
