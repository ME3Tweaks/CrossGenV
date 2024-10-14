using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;

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


            // Install texture streaming for Ocaren
            var sequence = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence");
            var remoteEvent = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqEvent_RemoteEvent", vTestOptions.cache);
            var streamInTextures = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqAct_StreamInTextures", vTestOptions.cache);
            KismetHelper.AddObjectsToSequence(sequence, false, remoteEvent, streamInTextures);
            KismetHelper.CreateOutputLink(remoteEvent, "Out", streamInTextures);

            remoteEvent.WriteProperty(new NameProperty("PrimeTexturesAhern", "EventName"));
            // OCAREN
            var materials = new ArrayProperty<ObjectProperty>("ForceMaterials")
            {
                new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_11.BioMaterialInstanceConstant_151")),
                new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_11.BioMaterialInstanceConstant_152")),
                new ObjectProperty(le1File.FindExport("TheWorld.PersistentLevel.BioPawn_11.BioMaterialInstanceConstant_153"))
            };

            streamInTextures.WriteProperty(materials);
            streamInTextures.WriteProperty(new FloatProperty(12f, "Seconds")); // How long to force stream. We set this to 12 to ensure blackscreen and any delays between fully finish
        }
    }
}
