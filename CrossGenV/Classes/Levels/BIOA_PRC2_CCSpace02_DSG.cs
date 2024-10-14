using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;

namespace CrossGenV.Classes.Levels
{
    internal class BIOA_PRC2_CCSpace02_DSG : ILevelSpecificCorrections
    {
        public IMEPackage me1File { get; init; }
        public IMEPackage le1File { get; init; }
        public VTestOptions vTestOptions { get; init; }

        public void PostPortingCorrection()
        {
            // Fixes for particle system
            // Port in a new DominantLight
            var sourceLight = vTestOptions.vTestHelperPackage.FindExport(@"CCSPACE02_DSG.DominantDirectionalLight_1");
            var destLevel = le1File.FindExport("TheWorld.PersistentLevel");
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, sourceLight, le1File, destLevel, true, new RelinkerOptionsPackage() { Cache = vTestOptions.cache }, out _);

            // Correct some lighting channels
            string[] unlitPSCs =
            [
                "BIOA_PRC2_S.Prefab.PRC2_Skybox_Vista.PRC2_Skybox_Vista_Arc17.ParticleSystemComponent0",
                "BIOA_PRC2_S.Prefab.PRC2_Skybox_Vista.PRC2_Skybox_Vista_Arc18.ParticleSystemComponent0",
                "BIOA_PRC2_S.Prefab.PRC2_Skybox_Vista.PRC2_Skybox_Vista_Arc19.ParticleSystemComponent0"
            ];

            foreach (var unlitPSC in unlitPSCs)
            {
                var exp = le1File.FindExport(unlitPSC);
                var lightingChannels = exp.GetProperty<StructProperty>("LightingChannels");
                lightingChannels.Properties.Clear();
                lightingChannels.Properties.Add(new BoolProperty(true, "bInitialized"));
                lightingChannels.Properties.Add(new BoolProperty(true, new NameReference("Cinematic", 4)));
                exp.WriteProperty(lightingChannels);
            }
        }
    }
}
