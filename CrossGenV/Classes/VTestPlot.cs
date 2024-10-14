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


        // BOOLEANS ============================================
        // The following are the remaining available bools
        public const int CROSSGEN_PMB_INDEX_UNUSED2 = 7577;
        public const int CROSSGEN_PMB_INDEX_UNUSED3 = 7630;
        public const int CROSSGEN_PMB_INDEX_UNUSED4 = 7647;
        public const int CROSSGEN_PMB_INDEX_UNUSED7 = 7660;

        // MOD SETTINGS =====================
        // Flag to indicate if music should be enabled. default = false = music on
        public const int CROSSGEN_PMB_INDEX_MUSIC_DISABLED = 7657;

        // Flag to indicate if mission completion exp should be rewarded
        public const int CROSSGEN_PMB_INDEX_FIRSTPLACE_EXPERIENCE_ENABLED = 7576;

        // Flag to indicate if the amount of enemies should increase as time goes on
        public const int CROSSGEN_PMB_INDEX_RAMPING_SPAWNCOUNT_ENABLED = 7564;

        // Flag to indicate if enemies should gain talents as time goes on
        public const int CROSSGEN_PMB_INDEX_RAMPING_TALENTS_ENABLED = 7557;

        // Flag to indicate if enemies should gain weapon mods as time goes on
        public const int CROSSGEN_PMB_INDEX_RAMPING_WEAPONMODS_ENABLED = 7558;

        // OTHERS =====================

        // Conversation flag set if it is Ahern's second (or subsequent) try to ask you to do his mission
        public const int CROSSGEN_PMB_INDEX_REFUSEDAHERNMISSION_SECONDTRY = 7658;

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
