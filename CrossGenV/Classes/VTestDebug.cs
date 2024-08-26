using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossGenV.Classes
{
    public class VTestDebug
    {
        /// <summary>
        /// Unlocks the Ahern Mission early on Debug builds for testing purposes
        /// </summary>
        /// <param name="le1File"></param>
        /// <param name="vTestOptions"></param>
        public static void DebugUnlockAhernMission(IMEPackage le1File, VTestOptions vTestOptions)
        {
            if (vTestOptions.debugBuild && le1File.FindExport("prc2_ochren_D.prc2_ochren_dlg") is { } conversation)
            {
                var replies = conversation.GetProperty<ArrayProperty<StructProperty>>("m_ReplyList");
                replies[100].Properties.AddOrReplaceProp(new IntProperty(-1, "nConditionalFunc"));
                replies[108].Properties.AddOrReplaceProp(new IntProperty(-1, "nConditionalFunc"));
                Debug.WriteLine($"Unlocking Ahern Mission in Ochren Conversation in file {le1File.FileNameNoExtension}");
                conversation.WriteProperty(replies);
            }
        }
    }
}
