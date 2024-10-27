using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Kismet;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCLava_DSG : BIOA_PRC2_CC_DSG, ILevelSpecificCorrections
    {
        public override void PrePortingCorrection()
        {
            base.PrePortingCorrection();
        }

        public override void PostPortingCorrection()
        {
            base.PostPortingCorrection();
            PostPortingCorrection_CCLava_DSG();
            SetCaptureRingChanger("Lava");
        }

        /// <summary>
        /// Level specific corrections for CCLava_DSG
        /// </summary>
        private void PostPortingCorrection_CCLava_DSG()
        {
            // SeqAct_ChangeCollision changed and requires an additional property otherwise it doesn't work.
            string[] collisionsToTurnOff =
            [
                // Hut doors and kill volumes
                "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_1",
                "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_3",
                "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_5",
                "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_6",
            ];

            foreach (var cto in collisionsToTurnOff)
            {
                var exp = le1File.FindExport(cto);
                exp.WriteProperty(new EnumProperty("COLLIDE_NoCollision", "ECollisionType", MEGame.LE1, "CollisionType"));
            }

            string[] collisionsToTurnOn =
            [
                // Hut doors and kill volumes
                "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_0",
                "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_9",
                "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_2",
                "TheWorld.PersistentLevel.Main_Sequence.Set_Hut_Accessibility.SeqAct_ChangeCollision_4",
            ];

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

            string[] reachSpecsToDisable =
            [
                // NORTH ROOM
                "TheWorld.PersistentLevel.ReachSpec_1941", // CoverLink to PathNode
                "TheWorld.PersistentLevel.ReachSpec_1937", // CoverLink to CoverLink
                "TheWorld.PersistentLevel.ReachSpec_2529", // PathNode to PathNode

                // SOUTH ROOM
                "TheWorld.PersistentLevel.ReachSpec_1856", // CoverLink to PathNode
                "TheWorld.PersistentLevel.ReachSpec_1849", // CoverLink to CoverLink
            ];

            foreach (var rs in reachSpecsToDisable)
            {
                var obj = SequenceObjectCreator.CreateSequenceObject(le1File, "SeqVar_Object", vTestOptions.cache);
                KismetHelper.AddObjectToSequence(obj, hutSeq);
                obj.WriteProperty(new ObjectProperty(le1File.FindExport(rs), "ObjValue"));
                KismetHelper.CreateVariableLink(disableReachSpecs, "ReachSpecs", obj);
            }
        }
    }
}
