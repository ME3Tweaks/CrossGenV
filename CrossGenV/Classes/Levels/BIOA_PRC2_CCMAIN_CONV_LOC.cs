using System.Collections.Generic;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.UnrealScript;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCMAIN_CONV_LOC : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }
        
        public void PostPortingCorrection()
        {
            FixVidinosPoundFistPost();
        }

        private void FixVidinosPoundFistPost()
        {
            // Replace with references on the DynamicAnimSet - PRC2's was missing PounFistExit
            var poundFistIFP = "prc2_jealous_jerk_N.Node_Data_Sequence.KIS_DYN_HMM_DP_PoundFist";
            var poundFist = le1File.FindExport(poundFistIFP);

            var sourcePoundFist =
                vTestOptions.vTestHelperPackage.FindExport("CCMain_Conv_LOC.KIS_DYN_HMM_DP_PoundFist");
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.ReplaceSingularWithRelink, sourcePoundFist,
                le1File, poundFist, true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out _);
            
            var fileLib = new FileLib(le1File);
            var usop = new UnrealScriptOptionsPackage() { Cache = new PackageCache() };
            var flOk = fileLib.Initialize(usop);
            if (!flOk) return;
            
            // Update TLK ID 183267
            var gestureTrackInterpData29 = le1File.FindExport("prc2_jealous_jerk_N.Node_Data_Sequence.InterpData_29.InterpGroup_7.BioEvtSysTrackGesture_16");
            var gestureTrackProperties29 = """
                               properties
                               {
                                   m_aGestures = ({
                                                   ePose = EBioGestureValidPoses.No_Pose_Change, 
                                                   eGesture = EBioGestureValidGestures.GestValidGest_Unset, 
                                                   nmPoseSet = 'None', 
                                                   nmPoseAnim = 'None', 
                                                   nmGestureSet = 'DL_Body', 
                                                   nmGestureAnim = 'DL_BodyNormal', 
                                                   nmTransitionSet = 'None', 
                                                   nmTransitionAnim = 'None', 
                                                   fPlayRate = 1.0, 
                                                   fStartOffset = 16.9599991, 
                                                   fEndOffset = 2.98999262, 
                                                   bInvalidData = FALSE, 
                                                   bOneShotAnim = FALSE, 
                                                   fStartBlendDuration = 2.57500434, 
                                                   fEndBlendDuration = 2.57500434, 
                                                   fWeight = 0.85833478, 
                                                   bChainToPrevious = FALSE, 
                                                   bPlayUntilNext = FALSE, 
                                                   aChainedGestures = (), 
                                                   bUseDynAnimSets = FALSE
                                                  }, 
                                                  {
                                                   ePose = EBioGestureValidPoses.No_Pose_Change, 
                                                   eGesture = EBioGestureValidGestures.GestValidGest_Unset, 
                                                   nmPoseSet = 'None', 
                                                   nmPoseAnim = 'None', 
                                                   nmGestureSet = 'HMM_DL_Shrug', 
                                                   nmGestureAnim = 'DL_Shrug02', 
                                                   nmTransitionSet = 'None', 
                                                   nmTransitionAnim = 'None', 
                                                   fPlayRate = 0.75, 
                                                   fStartOffset = 0.0, 
                                                   fEndOffset = 0.0, 
                                                   bInvalidData = FALSE, 
                                                   bOneShotAnim = FALSE, 
                                                   fStartBlendDuration = 0.100000001, 
                                                   fEndBlendDuration = 0.100000001, 
                                                   fWeight = 1.0, 
                                                   bChainToPrevious = FALSE, 
                                                   bPlayUntilNext = FALSE, 
                                                   aChainedGestures = (), 
                                                   bUseDynAnimSets = FALSE
                                                  }, 
                                                  {
                                                   ePose = EBioGestureValidPoses.No_Pose_Change, 
                                                   eGesture = EBioGestureValidGestures.GestValidGest_Unset, 
                                                   nmPoseSet = 'HMM_DP_PoundFist', 
                                                   nmPoseAnim = 'DP_PounFistIdle', 
                                                   nmGestureSet = 'None', 
                                                   nmGestureAnim = 'None', 
                                                   nmTransitionSet = 'HMM_DP_PoundFist', 
                                                   nmTransitionAnim = 'DP_PounFistEnter', 
                                                   fPlayRate = 1.0, 
                                                   fStartOffset = 0.0, 
                                                   fEndOffset = 0.0, 
                                                   bInvalidData = FALSE, 
                                                   bOneShotAnim = FALSE, 
                                                   fStartBlendDuration = 0.100000001, 
                                                   fEndBlendDuration = 0.100000001, 
                                                   fWeight = 1.0, 
                                                   bChainToPrevious = FALSE, 
                                                   bPlayUntilNext = FALSE, 
                                                   aChainedGestures = (), 
                                                   bUseDynAnimSets = FALSE
                                                  }, 
                                                  {
                                                   ePose = EBioGestureValidPoses.No_Pose_Change, 
                                                   eGesture = EBioGestureValidGestures.GestValidGest_Unset, 
                                                   nmPoseSet = 'HMM_DL_StandingDefault', 
                                                   nmPoseAnim = 'DL_Idle', 
                                                   nmGestureSet = 'None', 
                                                   nmGestureAnim = 'None', 
                                                   nmTransitionSet = 'HMM_DP_PoundFist', 
                                                   nmTransitionAnim = 'DP_PounFistExit', 
                                                   fPlayRate = 1.0, 
                                                   fStartOffset = 0.0, 
                                                   fEndOffset = 0.0, 
                                                   bInvalidData = FALSE, 
                                                   bOneShotAnim = FALSE, 
                                                   fStartBlendDuration = 0.100000001, 
                                                   fEndBlendDuration = 0.100000001, 
                                                   fWeight = 1.0, 
                                                   bChainToPrevious = FALSE, 
                                                   bPlayUntilNext = FALSE, 
                                                   aChainedGestures = (), 
                                                   bUseDynAnimSets = FALSE
                                                  }
                                                 )
                                   sActorTag = "Owner"
                                   eStartingPose = EBioGestureAllPoses.GestPose_Unset
                                   nmStartingPoseSet = 'HMM_DL_StandingDefault'
                                   nmStartingPoseAnim = 'DL_Idle'
                                   m_fStartPoseOffset = 15.8699999
                                   m_aTrackKeys = ({fTime = 0.0, KeyName = 'None'}, 
                                                   {fTime = 0.65154177, KeyName = 'Gesture'}, 
                                                   {fTime = 3.26654673, KeyName = 'Gesture'}, 
                                                   {fTime = 4.5, KeyName = 'Gesture'}
                                                  )
                               }
                               """;
            UnrealScriptCompiler.CompileDefaultProperties(gestureTrackInterpData29, gestureTrackProperties29, fileLib, usop);
            
            // Update TLK ID 183269 - this one is more complicated because we clone the interp for it for different line lengths but only on INT
            var convNode = le1File.FindExport("prc2_jealous_jerk_N.Node_Data_Sequence.BioSeqEvt_ConvNode_31");
            var nextOp = KismetHelper.GetOutputLinksOfNode(convNode)[0][0].LinkedOp;
            List<IEntry> interpToProcess = new();

            if (nextOp.ClassName == "BioSeqAct_PMCheckConditional")
            {
                // we duplicated the line. we don't necessarily know the IFP of the interp tracks
                var conditionalOuts = KismetHelper.GetOutputLinksOfNode(nextOp as ExportEntry);
                interpToProcess.Add(conditionalOuts[0][0].LinkedOp);
                interpToProcess.Add(conditionalOuts[1][0].LinkedOp);
            }
            else
            {
                // didn't duplicate - it's going to be this interp
                interpToProcess.Add(le1File.FindExport(
                    "prc2_jealous_jerk_N.Node_Data_Sequence.SeqAct_Interp_31"));
            }
         
            var gestureTrackProperties31 = """
                                           properties
                                           {
                                               m_aGestures = ({
                                                               ePose = EBioGestureValidPoses.No_Pose_Change, 
                                                               eGesture = EBioGestureValidGestures.GestValidGest_Unset, 
                                                               nmPoseSet = 'None', 
                                                               nmPoseAnim = 'None', 
                                                               nmGestureSet = 'DL_Body', 
                                                               nmGestureAnim = 'DL_BodyNormal', 
                                                               nmTransitionSet = 'None', 
                                                               nmTransitionAnim = 'None', 
                                                               fPlayRate = 1.0, 
                                                               fStartOffset = 8.03999996, 
                                                               fEndOffset = 11.5433207, 
                                                               bInvalidData = FALSE, 
                                                               bOneShotAnim = FALSE, 
                                                               fStartBlendDuration = 2.75834036, 
                                                               fEndBlendDuration = 2.75834036, 
                                                               fWeight = 0.919446826, 
                                                               bChainToPrevious = FALSE, 
                                                               bPlayUntilNext = FALSE, 
                                                               aChainedGestures = (), 
                                                               bUseDynAnimSets = FALSE
                                                              }, 
                                                              {
                                                               ePose = EBioGestureValidPoses.No_Pose_Change, 
                                                               eGesture = EBioGestureValidGestures.GestValidGest_Unset, 
                                                               nmPoseSet = 'None', 
                                                               nmPoseAnim = 'None', 
                                                               nmGestureSet = 'HMM_DL_StandingDefault', 
                                                               nmGestureAnim = 'DL_HeadNod', 
                                                               nmTransitionSet = 'None', 
                                                               nmTransitionAnim = 'None', 
                                                               fPlayRate = 1.0, 
                                                               fStartOffset = 0.0, 
                                                               fEndOffset = 0.0, 
                                                               bInvalidData = FALSE, 
                                                               bOneShotAnim = FALSE, 
                                                               fStartBlendDuration = 0.100000001, 
                                                               fEndBlendDuration = 0.100000001, 
                                                               fWeight = 1.0, 
                                                               bChainToPrevious = FALSE, 
                                                               bPlayUntilNext = FALSE, 
                                                               aChainedGestures = (), 
                                                               bUseDynAnimSets = FALSE
                                                              }, 
                                                              {
                                                               ePose = EBioGestureValidPoses.No_Pose_Change, 
                                                               eGesture = EBioGestureValidGestures.GestValidGest_Unset, 
                                                               nmPoseSet = 'None', 
                                                               nmPoseAnim = 'None', 
                                                               nmGestureSet = 'HMM_DL_StandingDefault', 
                                                               nmGestureAnim = 'DL_HandRight', 
                                                               nmTransitionSet = 'None', 
                                                               nmTransitionAnim = 'None', 
                                                               fPlayRate = 1.0, 
                                                               fStartOffset = 0.0, 
                                                               fEndOffset = 0.0, 
                                                               bInvalidData = FALSE, 
                                                               bOneShotAnim = FALSE, 
                                                               fStartBlendDuration = 0.100000001, 
                                                               fEndBlendDuration = 0.100000001, 
                                                               fWeight = 1.0, 
                                                               bChainToPrevious = FALSE, 
                                                               bPlayUntilNext = FALSE, 
                                                               aChainedGestures = (), 
                                                               bUseDynAnimSets = FALSE
                                                              }, 
                                                              {
                                                               ePose = EBioGestureValidPoses.No_Pose_Change, 
                                                               eGesture = EBioGestureValidGestures.GestValidGest_Unset, 
                                                               nmPoseSet = 'None', 
                                                               nmPoseAnim = 'None', 
                                                               nmGestureSet = 'None', 
                                                               nmGestureAnim = 'None', 
                                                               nmTransitionSet = 'None', 
                                                               nmTransitionAnim = 'None', 
                                                               fPlayRate = 1.0, 
                                                               fStartOffset = 0.0, 
                                                               fEndOffset = 0.0, 
                                                               bInvalidData = FALSE, 
                                                               bOneShotAnim = FALSE, 
                                                               fStartBlendDuration = 0.100000001, 
                                                               fEndBlendDuration = 0.100000001, 
                                                               fWeight = 1.0, 
                                                               bChainToPrevious = FALSE, 
                                                               bPlayUntilNext = FALSE, 
                                                               aChainedGestures = (), 
                                                               bUseDynAnimSets = FALSE
                                                              }, 
                                                              {
                                                               ePose = EBioGestureValidPoses.No_Pose_Change, 
                                                               eGesture = EBioGestureValidGestures.GestValidGest_Unset, 
                                                               nmPoseSet = 'HMM_DP_PoundFist', 
                                                               nmPoseAnim = 'DP_PounFistIdle', 
                                                               nmGestureSet = 'None', 
                                                               nmGestureAnim = 'None', 
                                                               nmTransitionSet = 'HMM_DP_PoundFist', 
                                                               nmTransitionAnim = 'DP_PounFistEnter', 
                                                               fPlayRate = 1.0, 
                                                               fStartOffset = 0.0, 
                                                               fEndOffset = 0.0, 
                                                               bInvalidData = FALSE, 
                                                               bOneShotAnim = FALSE, 
                                                               fStartBlendDuration = 0.100000001, 
                                                               fEndBlendDuration = 0.100000001, 
                                                               fWeight = 1.0, 
                                                               bChainToPrevious = FALSE, 
                                                               bPlayUntilNext = FALSE, 
                                                               aChainedGestures = (), 
                                                               bUseDynAnimSets = FALSE
                                                              }, 
                                                              {
                                                               ePose = EBioGestureValidPoses.No_Pose_Change, 
                                                               eGesture = EBioGestureValidGestures.GestValidGest_Unset, 
                                                               nmPoseSet = 'HMM_DL_StandingDefault', 
                                                               nmPoseAnim = 'DL_Idle', 
                                                               nmGestureSet = 'None', 
                                                               nmGestureAnim = 'None', 
                                                               nmTransitionSet = 'HMM_DP_PoundFist', 
                                                               nmTransitionAnim = 'DP_PounFistExit', 
                                                               fPlayRate = 1.0, 
                                                               fStartOffset = 0.0, 
                                                               fEndOffset = 0.0, 
                                                               bInvalidData = FALSE, 
                                                               bOneShotAnim = FALSE, 
                                                               fStartBlendDuration = 0.100000001, 
                                                               fEndBlendDuration = 0.100000001, 
                                                               fWeight = 1.0, 
                                                               bChainToPrevious = FALSE, 
                                                               bPlayUntilNext = FALSE, 
                                                               aChainedGestures = (), 
                                                               bUseDynAnimSets = FALSE
                                                              }
                                                             )
                                               sActorTag = "Owner"
                                               eStartingPose = EBioGestureAllPoses.GestPose_Unset
                                               nmStartingPoseSet = 'HMM_DL_StandingDefault'
                                               nmStartingPoseAnim = 'DL_Idle'
                                               m_fStartPoseOffset = 14.0
                                               m_aTrackKeys = ({fTime = -0.0106541235, KeyName = 'None'}, 
                                                               {fTime = 0.167917907, KeyName = 'Gesture'}, 
                                                               {fTime = 2.38281989, KeyName = 'Gesture'}, 
                                                               {fTime = 3.73550701, KeyName = 'Gesture'}, 
                                                               {fTime = 3.81363153, KeyName = 'Gesture'}, 
                                                               {fTime = 5.0, KeyName = 'Gesture'}
                                                              )
                                           }
                                           """;
            foreach (var interp in interpToProcess)
            {
                var interpData = KismetHelper.GetVariableLinksOfNode(interp as ExportEntry)[0].LinkedNodes[0];
                var gestureTrack = le1File.FindExport(interpData.InstancedFullPath + ".InterpGroup_9.BioEvtSysTrackGesture_18"); 
                UnrealScriptCompiler.CompileDefaultProperties(gestureTrack, gestureTrackProperties31, fileLib, 
                     usop);
            }
            
        }
    }
}
