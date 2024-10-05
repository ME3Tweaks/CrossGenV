using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossGenV.Classes
{
    /// <summary>
    /// Contains plot data information.
    /// </summary>
    public static class VTestPlot
    {
        // Crossgen plot indices are within the PRC2 range so they should not conflict with other mods unless they tried to do the same thing, for whatever reason.

        // The following are the remaining available bools
        public const int CROSSGEN_PMB_INDEX_UNUSED1 = 7576;
        public const int CROSSGEN_PMB_INDEX_UNUSED2 = 7577;
        public const int CROSSGEN_PMB_INDEX_UNUSED3 = 7630;
        public const int CROSSGEN_PMB_INDEX_UNUSED4 = 7647;
        public const int CROSSGEN_PMB_INDEX_UNUSED7 = 7660;


        // BOOLEANS ============================================
        // Flag to indicate if enemies should gain talents as time goes on
        public const int CROSSGEN_PMB_INDEX_RAMPING_TALENTS = 7557;

        // Flag to indicate if enemies should gain weapon mods as time goes on
        public const int CROSSGEN_PMB_INDEX_RAMPING_WEAPONMODS = 7558;

        // Flag to indicate if the amount of enemies should increase as time goes on
        public const int CROSSGEN_PMB_INDEX_RAMPING_SPAWNCOUNT = 7564;


        // INTEGERS ==============================================
        // The following are unused integers at below the vanilla range cap
        public const int CROSSGEN_PMI_INDEX_UNUSED3 = 139;
        public const int CROSSGEN_PMI_INDEX_UNUSED4 = 140;

        // Used to indicate which faction is currently selected in the wave class list (LEXSeqAct_CrossgenEnemyListModifier, MSM)
        public const int CROSSGEN_PMI_INDEX_SIMULATOR_FACTION = 132;
        // Internally used to detect desyncs  (LEXSeqAct_CrossgenEnemyListModifier, MSM)
        public const int CROSSGEN_PMI_INDEX_SIMULATOR_FACTION_SYNC = 133;
    }
}
