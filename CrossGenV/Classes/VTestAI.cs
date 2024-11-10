using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using LegendaryExplorerCore.UnrealScript;

namespace CrossGenV.Classes
{
    /// <summary>
    /// Handles generating the AI classes that we use to discard loot on death before it tries to give it to the player
    /// </summary>
    public static class VTestAI
    {
        public static IMEPackage GenerateAIClasses(IMEPackage package, VTestOptions options)
        {
            options.SetStatusText("Compiling CrossgenAI classes");
            var cggc = ExportCreator.CreatePackageExport(package, "Crossgen_GameContent", cache: options.cache);

            var classesToSub = LE1UnrealObjectInfo.ObjectInfo.Classes.Where(x => x.Value.IsA("BioAiController", MEGame.LE1) && x.Key.StartsWith("Bio"));

            var usop = new UnrealScriptOptionsPackage() { Cache = options.cache };
            FileLib lib = new FileLib(package);
            lib.Initialize(usop);
            List<ExportEntry> compiledClasses = new List<ExportEntry>();
            foreach (var cts in classesToSub.ToList())
            {
                if (cts.Key == "BioAI_PR1_LongDrone")
                    continue; // Don't compile this, it's not a global class, plus we will never use it

                var usp = cts.Key.IndexOf("_");
                if (usp < 0)
                    continue; // Do not subclass this

                var className = $"CrossgenAI{cts.Key.Substring(usp)}";
                if (package.FindExport($"{cggc.ObjectName}.{className}", "Class") != null)
                {
                    continue; // Already done
                }

                options.SetStatusText($"  {className}");

                var classText = $"Class {className} extends {cts.Key};";
                classText += "\npublic function OnDeath(Controller Killer) { \n";
                // Copied from SeqAct_DiscardInventory
                classText += "local BioPawnBehavior oBehavior; local BioInventory oInventory; if (Pawn != None){ if (Pawn.InvManager != None) {Pawn.InvManager.DiscardInventory(); } oBehavior = BioPawnBehavior(Pawn.oBioComponent); if (oBehavior != None) {oInventory = oBehavior.GetInventory();if (oInventory != None){oInventory.Empty();}}}";
                classText += "Super.OnDeath(Killer); }";

                var results = UnrealScriptCompiler.CompileClass(package, classText, lib, usop, parent: cggc);
                package.Save();
                var compiledClass = package.FindExport($"{cggc.InstancedFullPath}.{className}");
                if (compiledClass == null)
                {

                }
                compiledClasses.Add(compiledClass);
            }

            // Must keep these referenced or they will fall out of memory.
            VTestUtility.AddWorldReferencedObjects(package, compiledClasses.ToArray());

            // package.Save();
            return package;
        }

        public static void ConvertAIToCrossgen(VTestOptions options)
        {
            var packageFiles = Directory.GetFiles(VTestPaths.VTest_FinalDestDir, "*.pcc", SearchOption.AllDirectories);
            foreach (var pf in packageFiles)
            {
                // Only tables
                var quick = MEPackageHandler.UnsafePartialLoad(pf, x => false);
                var BPCSTs = quick.Exports.Where(x => x.ClassName == "BioPawnChallengeScaledType").ToList();
                if (BPCSTs.Any())
                {
                    // Open the package
                    options.SetStatusText($"Converting AIs to Crossgen in {quick.FileNameNoExtension}");
                    using var package = MEPackageHandler.OpenMEPackage(pf);

                    // Import from BIOA_PRC2
                    // This should work as we inventoried BIOA_PRC2 earlier in VTest so it will know where the package is that we just compiled
                    package.LECLTagData.ImportHintFiles.Add("BIOA_PRC2.pcc");
                    foreach (var bpcst in BPCSTs)
                    {
                        var matching = package.GetUExport(bpcst.UIndex);
                        var currentAiController = matching.GetProperty<ObjectProperty>("AIController");
                        if (currentAiController != null)
                        {
                            var aiObj = package.GetEntry(currentAiController.Value);
                            if (aiObj != null)
                            {
                                var matchingCrossgenAIClassName = GetCrossgenAI(aiObj.ObjectName);
                                currentAiController.Value = EntryImporter.EnsureClassIsInFile(package,
                                        matchingCrossgenAIClassName,
                                        new RelinkerOptionsPackage() { Cache = options.cache })
                                    .UIndex;
                                if (currentAiController.Value == 0)
                                {
                                    Debugger.Break();
                                }

                                matching.WriteProperty(currentAiController);
                            }
                        }
                    }

                    if (package.IsModified)
                    {
                        package.Save();
                    }
                }
            }
        }

        /// <summary>
        /// Returns the class name of the Crossgen version of an AI class
        /// </summary>
        /// <param name="aiClassName"></param>
        /// <returns></returns>
        private static string GetCrossgenAI(string aiClassName)
        {
            var usp = aiClassName.IndexOf("_");
            return $"CrossgenAI{aiClassName.Substring(usp)}";
        }
    }
}
