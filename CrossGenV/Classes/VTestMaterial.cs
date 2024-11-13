using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal.Classes;

namespace CrossGenV.Classes
{
    /// <summary>
    /// VTest Material code. Most of this is actually in corrections or manually done via donors, or in LEC.
    /// </summary>
    public class VTestMaterial
    {
        // Terrible performance, but i don't care
        private static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }

        public static Dictionary<Guid, Guid> expressionGuidMap = new()
        {
            // INT, RA, RU
            { new Guid(StringToByteArray("AD2F8F9FB837D8499EF1FC9799289A3E")), new Guid(StringToByteArray("896318E56A762B4FAEA9AA29B4B968CD")) }, // Alpha_Map -> Texture (Scoreboard)
            { new Guid(StringToByteArray("32144A9CDE189141BC421589B7EF3C0A")), new Guid(StringToByteArray("A1A3A72858C9DC45A10D3E9967BE4EE8")) }, // Character_Color -> ColorSelected (Scoreboard)
            { new Guid(StringToByteArray("E1EC0FC0E38D07439505E7C1EBB17F6D")), new Guid(StringToByteArray("896318E56A762B4FAEA9AA29B4B968CD")) }, // Alpha_Map -> Texture (Scoreboard Pulse)

            // FR
            { new Guid(StringToByteArray("89A949CF1307894385007C3F7E294B31")), new Guid(StringToByteArray("896318E56A762B4FAEA9AA29B4B968CD")) }, // Alpha_Map -> Texture (Scoreboard)
            { new Guid(StringToByteArray("37CFE5EE4EBB19448C1145BC9CE552DF")), new Guid(StringToByteArray("A1A3A72858C9DC45A10D3E9967BE4EE8")) }, // Character_Color -> ColorSelected (Scoreboard)
            { new Guid(StringToByteArray("4C7796E7E334BC418E3D223A3A0C57D4")), new Guid(StringToByteArray("896318E56A762B4FAEA9AA29B4B968CD")) }, // Alpha_Map -> Texture (Scoreboard Pulse)

            // IT
            { new Guid(StringToByteArray("05E20F645EEC884DA4CEEFD9E7A0CF90")), new Guid(StringToByteArray("896318E56A762B4FAEA9AA29B4B968CD")) }, // Alpha_Map -> Texture (Scoreboard)
            { new Guid(StringToByteArray("4F149E20719FF740B3B2AC822ECA5DFA")), new Guid(StringToByteArray("A1A3A72858C9DC45A10D3E9967BE4EE8")) }, // Character_Color -> ColorSelected (Scoreboard)
            { new Guid(StringToByteArray("C60CD51DDE59A640BC776D3248FC5BBB")), new Guid(StringToByteArray("896318E56A762B4FAEA9AA29B4B968CD")) }, // Character_Color -> ColorSelected (Scoreboard)

            // DE
            { new Guid(StringToByteArray("B70FAC0E0EFB33448ABE57CB3D786FC8")), new Guid(StringToByteArray("896318E56A762B4FAEA9AA29B4B968CD")) }, // Alpha_Map -> Texture (Scoreboard)
            { new Guid(StringToByteArray("392F5A5B38808A4B90E4AD2421875C16")), new Guid(StringToByteArray("A1A3A72858C9DC45A10D3E9967BE4EE8")) }, // Character_Color -> ColorSelected (Scoreboard)
            { new Guid(StringToByteArray("61EFE5604E71624D8B8A934EB297BBD8")), new Guid(StringToByteArray("896318E56A762B4FAEA9AA29B4B968CD")) }, // Alpha_Map -> Texture (Scoreboard Pulse)

            // what is this used for?
            //{ new Guid(StringToByteArray("E1EC0FC0E38D07439505E7C1EBB17F6D")), new Guid(StringToByteArray("896318E56A762B4FAEA9AA29B4B968CD")) }, // Alpha_Map -> Texture (Scoreboard Pulse)
        };

        public static Dictionary<string, string> parameterNameMap = new()
        {
            { "Alpha_Map", "Texture" }, // PRC2 Scoreboard Materials
            { "Character_Color", "ColorSelected" } // PRC2 Scoreboard Materials
        };
    }
}
