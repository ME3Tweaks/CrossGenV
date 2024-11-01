using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;

namespace CrossGenV.Classes
{
    public class VTestOptions
    {
        #region Configurable options
        /// <summary>
        /// List of levels to port. DO NOT INCLUDE BIOA_.
        /// </summary>
        public string[] vTestLevels = new[]
        {
            // Comment/uncomment these to select which files to build. Files commented out will not be included in the DLC folder
            "PRC2",
            "PRC2AA"
        };

        /// <summary>
        /// If lightmaps and shadowmaps should be stripped and dynamic lighting turned on
        /// </summary>
        public bool useDynamicLighting = false;

        /// <summary>
        /// Strips shadow maps off. If using dynamic lighting, shadow maps are always stripped
        /// </summary>
        public bool stripShadowMaps = false;

        /// <summary>
        /// If light and shadowmaps for meshes ported from ME1 (not using LE1 donor) should be ported instead of stripped. This may not look good but may be possible to adjust.
        /// </summary>
        public bool allowTryingPortedMeshLightMap = true;

        /// <summary>
        /// If terrains should have their lightmaps ported over (if they exist)
        /// </summary>
        public bool portTerrainLightmaps = true;

        /// <summary>
        /// If the audio localizations should be ported
        /// </summary>
        public bool portAudioLocalizations = true;

        /// <summary>
        /// If a level's list of StreamableTextureInstance's should be copied over.
        /// </summary>
        public bool installTexturesInstanceMap = false;

        /// <summary>
        /// If a level's list of textures to force streaming should be copied over.
        /// </summary>
        public bool installForceTextureStreaming = false;

        /// <summary>
        /// The intensity scalar for the CCLava lightmap
        /// </summary>
        public float LavaLightmapScalar = 0.15f;

        /// <summary>
        /// If debug features should be enabled in the build
        /// </summary>
        public bool debugBuild = true;

        /// <summary>
        /// If static lighting should be converted to non-static lighting. Only works if debugBuild is true
        /// </summary>
        public bool debugConvertStaticLightingToNonStatic = false;

        /// <summary>
        /// If each actor porting should also import into a new asset package that can speed up build 
        /// </summary>
        public bool debugBuildAssetCachePackage = false;

        /// <summary>
        /// If this build is for files that will be used in static lighting; do not make adjustments that could affect proper light baking.
        /// </summary>
        public bool isBuildForStaticLightingBake = false;

        /// <summary>
        /// Turn this only for rendering loading shots; otherwise lighting streams in once levels become visible and it looks bad until they load
        /// </summary>
        public bool useStreamedLighting = false;

        /// <summary>
        /// If packages should be resynthesized for cleanliness at the end
        /// </summary>
        public bool resynthesizePackages = false;

        /// <summary>
        /// If we should multithread level build
        /// </summary>
        public bool parallelizeLevelBuild = true;

        /// <summary>
        /// The cache that is passed through to sub operations.
        /// </summary>
        public TieredPackageCache cache { get; set; }
        #endregion

        #region Autoset options - Do not change these
        public IMEPackage vTestHelperPackage;
        
        public ObjectInstanceDB objectDB;
        public IMEPackage assetCachePackage;
        internal int assetCacheIndex;
        #endregion

        /// <summary>
        /// Delegate to invoke when a status message should be presented to the user
        /// </summary>
        public Action<string> SetStatusText { get; init; }

    }
}
