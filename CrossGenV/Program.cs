using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CrossGenV.Classes;
using LegendaryExplorerCore;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using Microsoft.Win32;

namespace CrossGenV
{
    class Program
    {
        // From https://stackoverflow.com/a/46226327
        public static string ConsoleReadLineWithTimeout(TimeSpan timeout)
        {
            Task<ConsoleKeyInfo> task = Task.Factory.StartNew(Console.ReadKey);

            string result = Task.WaitAny(new Task[] { task }, timeout) == 0
                ? task.Result.KeyChar.ToString()
                : string.Empty;
            return result;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("VTest by ME3Tweaks");

            // Initialize Legendary Explorer Core
            LegendaryExplorerCoreLib.InitLib(TaskScheduler.Current, x => Console.WriteLine($"ERROR: {x}"));

            // ASK FOR GAME BOOT
            Console.WriteLine("-------------------------------");
            Console.WriteLine("Install mod when compiling completes [Y/N]? (5 second timeout)");
            var input = ConsoleReadLineWithTimeout(TimeSpan.FromSeconds(5));

            bool installAndBootGame = "Y".CaseInsensitiveEquals(input);

            //bool installAndBootGame = false;

            // RUN VTEST

            // This object is passed through to all the methods so we don't have to constantly update the signatures
            var vTestOptions = new VTestOptions()
            {
                SetStatusText = Console.WriteLine,
                cache = TieredPackageCache.GetGlobalPackageCache(MEGame.LE1)
            };

#if !DEBUG
            // Release builds will NOT have debug features enabled
            vTestOptions.resynthesizePackages = true;
            vTestOptions.debugBuild = false;
            vTestOptions.useStreamedLighting = false;
#endif

            // For debugging resynth
            //foreach (var packagePath in Directory.GetFiles(@"S:\SteamLibrary\steamapps\common\Mass Effect Legendary Edition\Game\ME1\BioGame\DLC\DLC_MOD_Vegas", "*.pcc", SearchOption.AllDirectories).Where(x => x.RepresentsPackageFilePath()))
            //{
            //    var package = MEPackageHandler.OpenMEPackage(packagePath);
            //    vTestOptions.SetStatusText($"Resynthesizing package {package.FileNameNoExtension}");

            //    var newPackage = PackageResynthesizer.ResynthesizePackage(package, vTestOptions.cache);
            //    newPackage.Save();
            //}

            //return;


            vTestOptions.SetStatusText("Performing VTest");
            VTestExperiment.RunVTest(vTestOptions);

            vTestOptions.SetStatusText($"VTest run completed at {DateTime.Now:G}");
            if (installAndBootGame)
            {
                var mmPath = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\ME3Tweaks", "ExecutableLocation", null);
                if (mmPath != null && File.Exists(mmPath))
                {

                    var moddesc = Path.Combine(Directory.GetParent(VTestPaths.VTest_DonorsDir).FullName, "moddesc.ini");
                    if (File.Exists(moddesc))
                    {
                        vTestOptions.SetStatusText("Installing VTest and running game, check ME3Tweaks Mod Manager");
                        ProcessStartInfo psi = new ProcessStartInfo(mmPath, $"--installmod \"{moddesc}\" --bootgame LE1");
                        Process.Start(psi);
                    }
                }
            }
        }
    }
}
