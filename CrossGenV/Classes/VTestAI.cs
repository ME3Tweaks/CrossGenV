using System.IO;
using System.Linq;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using LegendaryExplorerCore.UnrealScript;

namespace CrossGenV.Classes
{
    /// <summary>
    /// Handles generating the AI classes that we use to discard loot on death before it tries to give it to the player
    /// </summary>
    public static class VTestAI
    {
        private static object syncObj = new object();


        public static IMEPackage GenerateAIClassPackage(VTestOptions options)
        {
            options.SetStatusText("Compiling CrossgenAI classes");
            var package = MEPackageHandler.CreateAndOpenPackage(Path.Combine(VTestPaths.VTest_FinalDestDir,@"CrossgenAI.pcc"), MEGame.LE1);
            var cggc = ExportCreator.CreatePackageExport(package, "Crossgen_GameContent", cache: options.cache);

            var classesToSub = LE1UnrealObjectInfo.ObjectInfo.Classes.Where(x => x.Value.IsA("BioAiController", MEGame.LE1));

            var usop = new UnrealScriptOptionsPackage() { Cache = options.cache };
            FileLib lib = new FileLib(package);
            lib.Initialize(usop);

            foreach (var cts in classesToSub.ToList())
            {
                var usp = cts.Key.IndexOf("_");
                if (usp < 0)
                    continue; // Do not subclass this

                var className = $"CrossgenAI{cts.Key.Substring(usp)}";
                options.SetStatusText($"  {className}");

                var classText = $"Class {className} extends {cts.Key};";
                classText += "\npublic function OnDeath(Controller Killer) { \n";
                // Copied from SeqAct_DiscardInventory
                classText += "local BioPawnBehavior oBehavior; local BioInventory oInventory; if (Pawn != None){ if (Pawn.InvManager != None) {Pawn.InvManager.DiscardInventory(); } oBehavior = BioPawnBehavior(Pawn.oBioComponent); if (oBehavior != None) {oInventory = oBehavior.GetInventory();if (oInventory != None){oInventory.Empty();}}}";
                classText += "Super.OnDeath(Killer); }";

                UnrealScriptCompiler.CompileClass(package, classText, lib, usop, parent: cggc);
            }
            package.Save();
            return package;
        }
    }
}
