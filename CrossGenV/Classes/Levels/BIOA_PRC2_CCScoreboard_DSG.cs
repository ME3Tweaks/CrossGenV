using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using System.Linq;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCScoreboard_DSG : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PostPortingCorrection()
        {
            // Porting in ANY of these crashes the game. Why??

            // Port in the UI switching and keybinding for PC
            // Port in the custom sequence used for switching UIs. Should only run if not skipping the scoreboard
            VTestKismet.InstallVTestHelperSequenceViaOut(le1File, "TheWorld.PersistentLevel.Main_Sequence.Play_Post_Scenario_Scoreboard_Matinee.UIAction_PlaySound_0", "ScoreboardSequence.UISwitcherLogic", false, vTestOptions, out _);

            // Port in the keybinding sequences
            VTestKismet.InstallVTestHelperSequenceViaOut(le1File, "TheWorld.PersistentLevel.Main_Sequence.Play_Post_Scenario_Scoreboard_Matinee.BioSeqAct_MiniGame_1", "ScoreboardSequence.KeybindsInstaller", true, vTestOptions, out _);
            VTestKismet.InstallVTestHelperSequenceViaOut(le1File, "TheWorld.PersistentLevel.Main_Sequence.Play_Post_Scenario_Scoreboard_Matinee.UIAction_PlaySound_1", "ScoreboardSequence.KeybindsUninstaller", false, vTestOptions, out _);

            // Port in the logic that changes "Mission Resolved" to "Mission Failed" if any scenarios are truly failed
            {
                VTestKismet.InstallVTestHelperSequenceViaOut(le1File, "TheWorld.PersistentLevel.Main_Sequence.Play_Post_Scenario_Scoreboard_Matinee.Setup_Post_Scenario_Scoreboard.SequenceReference_20", "ScoreboardSequence.FailedMissionLogic", false, vTestOptions, out var _, addInline: true);
                var parentSequence = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Play_Post_Scenario_Scoreboard_Matinee.Setup_Post_Scenario_Scoreboard");
                var failedLogic = le1File.FindExport("TheWorld.PersistentLevel.Main_Sequence.Play_Post_Scenario_Scoreboard_Matinee.Setup_Post_Scenario_Scoreboard.FailedMissionLogic");

                // Link helper sequence to existing interp
                var missionSuccessInterp = le1File.FindExport("TheWorld.PersistentLevel.InterpActor_43");
                var missionSuccessInterpVar = SequenceObjectCreator.CreateObject(parentSequence, missionSuccessInterp, vTestOptions.cache);
                KismetHelper.CreateVariableLink(failedLogic, "SuccessObj", missionSuccessInterpVar);

                // Clone new import for MIC for mission failed text - needs to exist in LOC file
                var newMatImport = EntryCloner.CloneEntry(le1File.FindImport("BIOA_PRC2_Scoreboard_T.Blue_Mission_Resolved"));
                newMatImport.ObjectName = "Blue_Mission_Failed";

                // Clone new interp actor, attach to camera, assign new import
                var newInterpActor = EntryCloner.CloneTree(missionSuccessInterp);
                var cameraActor3 = le1File.FindExport("TheWorld.PersistentLevel.CameraActor_3");
                var cProps = cameraActor3.GetProperties();
                cProps.GetProp<ArrayProperty<ObjectProperty>>("Attached").Add(new ObjectProperty(newInterpActor));
                cameraActor3.WriteProperties(cProps);
                var smc = newInterpActor.GetChildren().First() as ExportEntry;
                smc.WriteProperty(new ArrayProperty<ObjectProperty>("Materials") { new ObjectProperty(newMatImport) });

                // Hook into sequence
                var missionFailedInterpVar = SequenceObjectCreator.CreateObject(parentSequence, newInterpActor, vTestOptions.cache);
                KismetHelper.CreateVariableLink(failedLogic, "FailureObj", missionFailedInterpVar);
            }
        }
    }
}
