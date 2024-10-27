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
                    AddEnglishStringRefs();
                    break;
                case "ES":
                    AddEnglishStringRefs();
                    break;
                case "IT":
                    AddEnglishStringRefs();
                    break;
                case "PL":
                case "PLPC":
                    AddEnglishStringRefs();
                    break;
                case "FR":
                    AddEnglishStringRefs();
                    break;
                case "DE":
                    AddEnglishStringRefs();
                    break;
                case "JA":
                    AddEnglishStringRefs();
                    break;
            }
            
            var huff = new HuffmanCompression();
            huff.LoadInputData(stringRefs);
            huff.SerializeTalkfileToExport(tlkExport);

            void AddEnglishStringRefs()
            {
                stringRefs.Add(new TLKStringRef(338464, "Downloading Data"));
                stringRefs.Add(new TLKStringRef(338465, "Simulator Settings"));
                stringRefs.Add(new TLKStringRef(338466, "Open"));
                stringRefs.Add(new TLKStringRef(338467, "ENABLED"));
                stringRefs.Add(new TLKStringRef(338468, "DISABLED"));
                stringRefs.Add(new TLKStringRef(338469, "Enable"));
                stringRefs.Add(new TLKStringRef(338470, "Disable"));
                stringRefs.Add(new TLKStringRef(338471, "Music"));
                stringRefs.Add(new TLKStringRef(338472, "Appropriate music tracks will be added to each simulator map, which will change as the intensity ramps up."));
                stringRefs.Add(new TLKStringRef(338473, "As in the original DLC, no music will be played in the simulator."));
                stringRefs.Add(new TLKStringRef(338474, "XP on 1st Place"));
                stringRefs.Add(new TLKStringRef(338475, "You will be granted experience upon getting 1st place in a simulator map. 1/3 of a level's worth of XP will be granted upon achieving 1st place for the first time on each simulator map. Additional XP will be granted upon beating your record, and upon completing the special scenario."));
                stringRefs.Add(new TLKStringRef(338476, "No experience will be granted upon completing a simulator map."));
                stringRefs.Add(new TLKStringRef(338477, "Survival: Enemy Count Ramping"));
                stringRefs.Add(new TLKStringRef(338478, "Enemy Ramping"));
                stringRefs.Add(new TLKStringRef(338479, "The amount of enemies that spawn will be fixed to the default, original version."));
                stringRefs.Add(new TLKStringRef(338480, "The amount of enemies in survival mode will increase over time, creating a increasing difficulty curve as time progresses."));
                stringRefs.Add(new TLKStringRef(338481, "Difficulty Ramping: Talents"));
                stringRefs.Add(new TLKStringRef(338482, "As a mission progresses, enemies will gain talents and powers that make them more lethal."));
                stringRefs.Add(new TLKStringRef(338483, "Enemies will not gain talents as simulator missions progress. This is the default value."));
                stringRefs.Add(new TLKStringRef(338484, "Difficulty Ramping: Weapons"));
                stringRefs.Add(new TLKStringRef(338485, "As a mission progresses, enemies will gain weapon mods that make them more lethal."));
                stringRefs.Add(new TLKStringRef(338486, "Enemies will not gain weapon mods as simulator missions progress. This is the default value."));
                stringRefs.Add(new TLKStringRef(338487, "Simulator: Enemy Selector"));
                stringRefs.Add(new TLKStringRef(338488, "Select enemy type"));
                stringRefs.Add(new TLKStringRef(338489, "This set of enemies is currently not enabled."));
                stringRefs.Add(new TLKStringRef(338490, "Asari enemies will replace the normal enemies in the combat simulator."));
                stringRefs.Add(new TLKStringRef(338491, "Salarian enemies will replace the normal enemies in the combat simulator."));
                stringRefs.Add(new TLKStringRef(338492, "Batarian enemies will replace the normal enemies in the combat simulator."));
                stringRefs.Add(new TLKStringRef(338493, "Monsters"));
                stringRefs.Add(new TLKStringRef(338494, "Monsterous enemies will replace the normal enemies in the combat simulator."));
                stringRefs.Add(new TLKStringRef(338495, "Mixed Enemies"));
                stringRefs.Add(new TLKStringRef(338496, "Enemies from all available factions will appear in the combat simulator."));
                stringRefs.Add(new TLKStringRef(338497, "All Settings: Vanilla"));
                stringRefs.Add(new TLKStringRef(338498, "All Settings"));
                stringRefs.Add(new TLKStringRef(338499, "All simulator settings will be set to their vanilla, original Mass Effect 1 value. For Pinnacle Station purists."));
                stringRefs.Add(new TLKStringRef(338500, "All Settings: Recommended"));
                stringRefs.Add(new TLKStringRef(338501, "All simulator settings will be set to their recommended states for this remaster."));
                stringRefs.Add(new TLKStringRef(338502, "Selected"));
                stringRefs.Add(new TLKStringRef(338503, "Not Selected"));
                stringRefs.Add(new TLKStringRef(338504, "Enemy types will not be changed from the defaults."));
                stringRefs.Add(new TLKStringRef(338505, "Enemy types have been changed from the defaults."));
                stringRefs.Add(new TLKStringRef(338506, "Default"));
            }
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
