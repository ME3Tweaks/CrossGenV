using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossGenV.Classes
{
    /// <summary>
    /// Class for storing paths, so other devs can work with their own pathing. This should be a config file or something, but I'm way too lazy.
    /// </summary>
    public static class VTestPaths
    {
        // VTest

        /// <summary>
        /// CookedPCConsole output directory
        /// </summary>
        internal const string VTest_FinalDestDir = @"Z:\ModLibrary\LE1\V Test\DLC_MOD_Vegas\CookedPCConsole";

        /// <summary>
        /// ModdedSource directory. Contains files with some pre-made corrections to them.
        /// </summary>
        internal const string VTest_SourceDir = @"Z:\ModLibrary\LE1\V Test\ModdedSource";

        /// <summary>
        /// Folder that contains the manually built donors
        /// </summary>
        internal const string VTest_DonorsDir = @"Z:\ModLibrary\LE1\V Test\Donors";

        /// <summary>
        /// Folder that contains the precomputed files for the mod
        /// </summary>
        internal const string VTest_PrecomputedDir = @"Z:\ModLibrary\LE1\V Test\PrecomputedFiles";

        /// <summary>
        /// Folder that contains the precomputed files for the mod
        /// </summary>
        internal const string VTest_StaticLightingDir = @"Z:\ModLibrary\LE1\V Test\StaticLighting";
    }
}
