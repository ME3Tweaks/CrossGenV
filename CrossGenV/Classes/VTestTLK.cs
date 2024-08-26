using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.TLK.ME1;
using LegendaryExplorerCore.TLK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossGenV.Classes
{
    /// <summary>
    /// TLK-specific things
    /// </summary>
    public static class VTestTLK
    {
        private static void AddVTestSpecificStrings(ExportEntry tlkExport, string lang)
        {
            ME1TalkFile talkF = new ME1TalkFile(tlkExport);
            var stringRefs = talkF.StringRefs.ToList();
            // LANGUAGE SPECIFIC STRINGS HERE
            switch (lang)
            {
                case "RA":
                case "RU":
                    stringRefs.Add(new TLKStringRef(338464, "Загрузка данных"));
                    stringRefs.Add(new TLKStringRef(338465, "Настройка музыки"));
                    stringRefs.Add(new TLKStringRef(338466, "Отключить музыку"));
                    stringRefs.Add(new TLKStringRef(338467, "Включить музыку"));
                    break;
                case "ES":
                    stringRefs.Add(new TLKStringRef(338465, "Configuración de Música"));
                    stringRefs.Add(new TLKStringRef(338466, "Desactivar Música"));
                    stringRefs.Add(new TLKStringRef(338467, "Activar Música"));
                    break;
                case "IT":
                    stringRefs.Add(new TLKStringRef(338465, "Ustawienia muzyki"));
                    stringRefs.Add(new TLKStringRef(338466, "Wyłącz muzykę"));
                    stringRefs.Add(new TLKStringRef(338467, "Włącz muzykę"));
                    break;
                case "PL":
                case "PLPC":
                    stringRefs.Add(new TLKStringRef(338465, "Music Setting"));
                    stringRefs.Add(new TLKStringRef(338466, "Disable Music"));
                    stringRefs.Add(new TLKStringRef(338467, "Enable Music"));
                    break;
                case "FR":
                    stringRefs.Add(new TLKStringRef(338465, "Music Setting"));
                    stringRefs.Add(new TLKStringRef(338466, "Disable Music"));
                    stringRefs.Add(new TLKStringRef(338467, "Enable Music"));
                    break;
                case "DE":
                    stringRefs.Add(new TLKStringRef(338465, "Musikeinstellungen"));
                    stringRefs.Add(new TLKStringRef(338466, "Musik Deaktivieren"));
                    stringRefs.Add(new TLKStringRef(338467, "Musik Aktivieren"));
                    break;
                case "JA":
                    stringRefs.Add(new TLKStringRef(338464, "Downloading Data"));
                    stringRefs.Add(new TLKStringRef(338465, "Music Setting"));
                    stringRefs.Add(new TLKStringRef(338466, "Disable Music"));
                    stringRefs.Add(new TLKStringRef(338467, "Enable Music"));
                    break;
            }


            var huff = new HuffmanCompression();
            huff.LoadInputData(stringRefs);
            huff.SerializeTalkfileToExport(tlkExport);
        }

        public static void PostUpdateTLKs(VTestOptions vTestOptions)
        {
            var basePath = Path.Combine(VTestPaths.VTest_FinalDestDir, "DLC_MOD_Vegas_GlobalTlk_");
            var langsToUpdate = new[] { "RA", "RU", "DE", "FR", "IT", "ES", "JA", "PL", "PLPC" };
            foreach (var lang in langsToUpdate)
            {
                var tlkPackage = MEPackageHandler.OpenMEPackage(basePath + lang + ".pcc");

                // Add our specific TLK strings.
                AddVTestSpecificStrings(tlkPackage.FindExport("GlobalTlk_tlk"), lang);
                AddVTestSpecificStrings(tlkPackage.FindExport("GlobalTlk_tlk_M"), lang);

                tlkPackage.Save();

                // Add English VO TLKs
                switch (lang)
                {
                    case "DE":
                        tlkPackage.Save(basePath + "GE.pcc"); // English VO
                        break;
                    case "FR":
                        tlkPackage.Save(basePath + "FE.pcc"); // English VO
                        break;
                    case "IT":
                        tlkPackage.Save(basePath + "IE.pcc"); // English VO
                        break;
                }
            }
        }
    }
}
