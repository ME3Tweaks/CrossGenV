using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCSIM05_DSG : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PostPortingCorrection()
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
    }
}
