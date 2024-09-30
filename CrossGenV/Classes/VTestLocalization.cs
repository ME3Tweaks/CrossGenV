using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.UnrealScript.Lexing;

namespace CrossGenV.Classes
{
    /// <summary>
    /// Handles localization changes for this mod
    /// </summary>
    public static class VTestLocalization
    {
        /// <summary>
        /// Languages that are not present in the source but will be added. The initial implementation will clone them from ENG and then localizations of it will be substituted if, if available.
        /// </summary>
        private static string[] newLangs = ["CS", "HU", "PLPC"];

        /// <summary>
        /// Imports the localization data for _PL TLKs. Also reinstates the other languages.
        /// </summary>
        /// <param name="le1File"></param>
        /// <param name="options"></param>
        public static void FixLocalizations(IMEPackage le1File, VTestOptions options)
        {
            foreach (var btfsExp in le1File.Exports.Where(x => x.ClassName == "BioTlkFileSet").ToList())
            {
                var btfs = ObjectBinary.From<BioTlkFileSet>(btfsExp);

                var eng = btfs.TlkSets["Int"];
                var engTlkM = le1File.GetUExport(eng.Male);
                var engTlkF = le1File.GetUExport(eng.Female);

                // Add CS
                foreach (var lang in newLangs)
                {
                    if (!btfs.TlkSets.TryGetValue(lang, out _))
                    {
                        var newM = EntryCloner.CloneEntry(engTlkM);
                        var newF = EntryCloner.CloneEntry(engTlkF);

                        // Fix up names of exports
                        newM.indexValue = 0;
                        newF.indexValue = 0;
                        newM.ObjectName = new NameReference($"{newM.ObjectName.Name}_{lang}", 0);
                        newF.ObjectName = new NameReference($"{newF.ObjectName.Name}_{lang}", 0);

                        // Import localization from modded source directory, if any.
                        ImportLocalization(le1File.FileNameNoExtension, lang, newM, newF, options);

                        btfs.TlkSets[lang] = new BioTlkFileSet.BioTlkSet() { Female = newF.UIndex, Male = newM.UIndex };
                    }
                }

                btfsExp.WriteBinary(btfs);
            }

            // Fixes polish localizations.


        }

        private static void ImportLocalization(string packageName, string localization, ExportEntry tlkM, ExportEntry tlkF, VTestOptions options)
        {
            // Nested in the source dir under the language code.
            var localizedPath = Path.Combine(VTestPaths.VTest_SourceDir, localization, $"{packageName}.SFM");
            if (File.Exists(localizedPath))
            {
                var polishPackage = MEPackageHandler.OpenMEPackage(localizedPath);
                var locTlkM = polishPackage.FindExport(tlkM.InstancedFullPath);
                CopyLocalization(locTlkM, tlkM);

                var locTlkF = polishPackage.FindExport(tlkF.InstancedFullPath);
                CopyLocalization(locTlkF, tlkF);
            }
        }

        private static void CopyLocalization(ExportEntry source, ExportEntry dest)
        {
            if (source != null && dest != null)
            {
                // We copy properties as the superclass has different memory path and LEC will think it is different. So we don't do a direct import.
                Console.WriteLine($"Installing extra localization: {source.InstancedFullPath}");
                var props = source.GetProperties();
                var bin = source.GetBinaryData();
                dest.WritePropertiesAndBinary(props, bin);
            }
            else
            {
                Console.Error.WriteLine($"Could not find localized TLK - {source?.InstancedFullPath} - {dest?.InstancedFullPath}");
            }
        }
    }
}
