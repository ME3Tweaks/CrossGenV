using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.TLK.ME1;
using LegendaryExplorerCore.TLK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using LegendaryExplorerCore.Misc;

namespace CrossGenV.Classes
{
    public enum ECreditType
    {
        HEADER,
        TITLE,
        NAMES
    }

    public class LocalizedStringRef
    {
        public int StringID { get; set; }

        // Assigned at runtime
        public int RemappedStringID { get; set; }
        public string INT { get; set; }
        public string ESN { get; set; }
        public string DEU { get; set; }
        public string FRA { get; set; }
        public string ITA { get; set; }
        public string RUS { get; set; }
        public string POL { get; set; }
        public string HUN { get; set; }
        public string CZE { get; set; }
        public string JP { get; set; }

        // Only set on credit strings.
        public ECreditType? CreditType { get; set; }

        /// <summary>
        /// If this string is not localized and should just use INT
        /// </summary>
        public bool CopyINT { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("new() {");
            sb.AppendLine($"\tStringID = {StringID},");
            sb.AppendLine($"\tINT = \"{INT}\",");
            if (!CopyINT)
            {
                sb.AppendLine($"\tESN = \"{ESN}\",");
                sb.AppendLine($"\tDEU = \"{DEU}\",");
                sb.AppendLine($"\tFRA = \"{FRA}\",");
                sb.AppendLine($"\tITA = \"{ITA}\",");
                sb.AppendLine($"\tRUS = \"{RUS}\",");
                sb.AppendLine($"\tPOL = \"{POL}\",");
                sb.AppendLine($"\tHUN = \"{HUN}\",");
                sb.AppendLine($"\tCZE = \"{CZE}\",");
                sb.AppendLine($"\tJP = \"{JP}\"");
            }

            sb.Append("}");

            return sb.ToString();
        }

        public void SetString(string str, string locSuffix)
        {
            switch (locSuffix)
            {
                case "ES":
                    ESN = str;
                    return;
                case "DE":
                    DEU = str;
                    return;
                case "IT":
                    ITA = str;
                    return;
                case "FR":
                    FRA = str;
                    return;
                case "PLPC":
                    POL = str;
                    return;
                case "RU":
                    RUS = str;
                    return;
                case "HU":
                    HUN = str;
                    return;
                case "CZ":
                    CZE = str;
                    return;
                case "JP":
                    JP = str;
                    return;
                case "INT":
                    INT = str;
                    return;
                default:
                    Debugger.Break();
                    return;
            }
        }

        public TLKStringRef GetStringRef(string lang)
        {
            return new TLKStringRef(StringID, GetString(lang));
        }

        public string GetString(string lang)
        {
            if (CopyINT)
                return INT;

            switch (lang)
            {
                case "ES":
                    return ESN;
                case "DE":
                    return DEU;
                case "IT":
                    return ITA;
                case "FR":
                    return FRA;
                case "PLPC":
                case "PL":
                    return POL;
                case "RU":
                case "RA":
                    return RUS;
                case "HU":
                    return HUN;
                case "CZ":
                    return CZE;
                case "JP":
                    return JP;
                default:
                    return INT;
            }
        }
    }

    /// <summary>
    /// TLK-specific things
    /// </summary>
    public static class VTestTLK
    {
        // For converting to new vtest tlk system

        private static void TLKConvert()
        {
            var strings = File.ReadAllLines(@"B:\UserProfile\Desktop\tlk.txt");
            Dictionary<int, LocalizedStringRef> tlkStrings = new Dictionary<int, LocalizedStringRef>();
            string locSuffix = null;

            foreach (string s in strings)
            {
                var str = s.Trim();
                var oString = str;

                if (str.StartsWith("case "))
                {
                    str = str[6..];
                    str = str.Substring(0, str.IndexOf('"'));
                    locSuffix = str;
                    Console.WriteLine($"SUFFIX: {locSuffix}");
                    continue;
                }

                bool isShort = false;
                if (str.StartsWith("stringRefs.Add("))
                {
                    str = str.Substring("stringRefs.Add(new TLKStringRef(".Length);
                }
                else if (str.StartsWith("new TLKStringRef"))
                {
                    isShort = true;
                    str = str.Substring("new TLKStringRef(".Length);
                }
                else
                {
                    continue;
                }

                var idStr = str.Substring(0, str.IndexOf(','));
                var tlkId = int.Parse(idStr);
                str = str.Substring(idStr.Length + 1).Trim().Substring(1);
                if (isShort)
                {
                    str = str.Substring(0, str.IndexOf("\"),"));
                }
                else
                {
                    str = str.Substring(0, str.IndexOf("\"));"));
                }

                //
                //
                //
                //
                //
                //
                //
                //
                // Console.WriteLine(str);



                if (!tlkStrings.TryGetValue(tlkId, out var localizedVal))
                {
                    localizedVal = new LocalizedStringRef() { StringID = tlkId };
                    tlkStrings[tlkId] = localizedVal;
                }

                localizedVal.SetString(str, locSuffix);
            }

            foreach (var tlk in tlkStrings)
            {
                Debug.WriteLine(tlk.Value.ToString() + ",");
            }
        }


        private static void AddVTestSpecificStrings(ExportEntry tlkExport, string lang)
        {
            // Must be called first so TLK list is populated.
            BuildCreditsList();


            ME1TalkFile talkF = new ME1TalkFile(tlkExport);
            var stringRefs = talkF.StringRefs.ToList();

            foreach (var lString in VTestStrings.ModStrings)
            {
                stringRefs.Add(lString.GetStringRef(lang));
            }

            var huff = new HuffmanCompression();
            huff.LoadInputData(stringRefs);
            huff.SerializeTalkfileToExport(tlkExport);
        }

        private static bool HasBuiltCredits = false;

        class Credit
        {
            public LocalizedStringRef Header { get; set; }
            public LocalizedStringRef Title { get; set; }
            public LocalizedStringRef Names { get; set; }

            public void SetString(LocalizedStringRef creditString)
            {
                switch (creditString.CreditType)
                {
                    case ECreditType.HEADER:
                        Header = creditString;
                        return;
                    case ECreditType.TITLE:
                        Title = creditString;
                        return;
                    case ECreditType.NAMES:
                        Names = creditString;
                        return;
                }
            }

            public string GenerateIniEntry(int placeAfter)
            {
                bool needsComma = false;
                StringBuilder sb = new StringBuilder();
                sb.Append("ScrollingCreditInserts=(");

                if (Header != null)
                {
                    sb.Append($"srHeading={Header.StringID}");
                    needsComma = true;
                }

                if (Title != null)
                {
                    if (needsComma)
                    {
                        sb.Append(",");
                    }
                    sb.Append($"srTitle={Title.StringID}");
                    needsComma = true;
                }

                if (Names != null)
                {
                    if (needsComma)
                    {
                        sb.Append(",");
                    }
                    sb.Append($"srNames={Names.StringID}");
                    // no more needs comma after this, we always will have one required
                }

                sb.Append($", PlaceAfterNames={placeAfter})");
                return sb.ToString();
            }
        }

        private static void BuildCreditsList()
        {
            if (HasBuiltCredits)
                return;

            var outFile = Path.Combine(VTestPaths.VTest_FinalDestDir, "ConfigDelta-CrossgenCredits.m3cd");

            var ini = new DuplicatingIni();
            var creditsSegment = ini.GetOrAddSection("BioCredits.ini SharedCreditsSF.BioSFHandler_Credits_Shared");

            // CREDITS
            Credit currentCredit = null;
            ECreditType lastType = ECreditType.NAMES;

            int startingCreditIndex = VTestStrings.ModStrings.Last().StringID;
            int currentCreditIndex = startingCreditIndex;
            var placeAfter = 783106; // End of Bring Down The Sky
            foreach (var creditString in VTestStrings.CreditStrings)
            {
                creditString.StringID = ++currentCreditIndex;
                VTestStrings.ModStrings.Add(creditString);

                if (currentCredit == null)
                {
                    // First
                    currentCredit = new Credit();
                    currentCredit.SetString(creditString);
                    lastType = creditString.CreditType.Value;
                }
                else
                {
                    if (creditString.CreditType < lastType)
                    {
                        // New credit
                        creditsSegment.Entries.Add(new DuplicatingIni.IniEntry(currentCredit.GenerateIniEntry(creditsSegment.Entries.Count == 0 ? 783106 : placeAfter))); // First entry chains onto the end of BDTS
                        placeAfter = currentCredit.Names.StringID;
                        currentCredit = new Credit();
                    }
                    currentCredit.SetString(creditString);
                    lastType = creditString.CreditType.Value;
                }
            }

            // Add final credit.
            creditsSegment.Entries.Add(new DuplicatingIni.IniEntry(currentCredit.GenerateIniEntry(placeAfter)));

            // Original DLC credits.
            creditsSegment.Entries.Add(new DuplicatingIni.IniEntry("; The following is from the original DLC"));
            creditsSegment.Entries.Add(new DuplicatingIni.IniEntry($"ScrollingCreditInserts=(srTitle=183717, srNames=182828, srHeading=172358, PlaceAfterNames={currentCredit.Names.StringID})"));
            creditsSegment.Entries.Add(new DuplicatingIni.IniEntry("ScrollingCreditInserts=(srTitle=183718, srNames=182844, PlaceAfterNames=182828)"));
            creditsSegment.Entries.Add(new DuplicatingIni.IniEntry("ScrollingCreditInserts=(srTitle=172345, srNames=183719, PlaceAfterNames=182844)"));
            creditsSegment.Entries.Add(new DuplicatingIni.IniEntry("ScrollingCreditInserts=(srTitle=172349, srNames=183720, PlaceAfterNames=183719)"));
            creditsSegment.Entries.Add(new DuplicatingIni.IniEntry("ScrollingCreditInserts=(srTitle=183721, srNames=183722, PlaceAfterNames=183720)"));

            File.WriteAllText(outFile, ini.ToString());
            HasBuiltCredits = true;
        }

        public static void PostUpdateTLKs(VTestOptions vTestOptions)
        {
            var basePath = Path.Combine(VTestPaths.VTest_FinalDestDir, "DLC_MOD_Vegas_GlobalTlk");
            // "" = English. It has no suffix
            var langsToUpdate = new[] { "", "RA", "RU", "DE", "FR", "IT", "ES", "JA", "PL", "PLPC", "HU" };
            foreach (var lang in langsToUpdate)
            {
                var intermedBasePath = basePath;
                if (lang != "")
                {
                    intermedBasePath += "_";
                }

                var tlkPackage = MEPackageHandler.OpenMEPackage(intermedBasePath + lang + ".pcc");

                // Add our specific TLK strings.
                AddVTestSpecificStrings(tlkPackage.FindExport("GlobalTlk_tlk"), lang);
                AddVTestSpecificStrings(tlkPackage.FindExport("GlobalTlk_tlk_M"), lang);

                tlkPackage.Save();

                // Add English VO TLKs
                switch (lang)
                {
                    case "DE":
                        tlkPackage.Save(intermedBasePath + "GE.pcc"); // English VO
                        break;
                    case "FR":
                        tlkPackage.Save(intermedBasePath + "FE.pcc"); // English VO
                        break;
                    case "IT":
                        tlkPackage.Save(intermedBasePath + "IE.pcc"); // English VO
                        break;
                }
            }
        }
    }
}
