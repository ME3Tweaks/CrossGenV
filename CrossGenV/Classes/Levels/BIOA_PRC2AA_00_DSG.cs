using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using System.Collections.Generic;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2AA_00_DSG : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PostPortingCorrection()
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
    }
}
