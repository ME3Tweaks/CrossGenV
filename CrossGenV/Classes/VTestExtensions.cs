using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Gammtek.Extensions.Collections.Generic;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;

namespace CrossGenV.Classes
{
    public static class VTestExtensions
    {
        /// <summary>
        /// Replaces a name in the name table. Only replaces the first one encountered.
        /// </summary>
        /// <param name="package">Package to update</param>
        /// <param name="oldName">The old name to find.</param>
        /// <param name="newName">New name to replace with.</param>
        /// <returns>True if replaced, false otherwise</returns>
        public static bool ReplaceName(this IMEPackage package, string oldName, string newName)
        {
            var nameIdx = package.findName(oldName);
            if (nameIdx >= 0)
            {
                package.replaceName(nameIdx, newName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Generates a lighting channels struct with the given channels set to true. They are all bool property names.
        /// </summary>
        /// <param name="export"></param>
        /// <param name="channels"></param>
        public static void SetLightingChannels(this ExportEntry export, params string[] channels)
        {
            PropertyCollection channelsP = new PropertyCollection();
            channelsP.ReplaceAll(channels.Select(x => new BoolProperty(true, NameReference.FromInstancedString(x))));
            export.WriteProperty(new StructProperty("LightingChannelContainer", channelsP, "LightingChannels"));
        }

        /// <summary>
        /// Generates a lighting channels struct with the given channels set to true. They are all bool property names.
        /// </summary>
        /// <param name="export"></param>
        /// <param name="channels"></param>
        public static void SetLightingChannels(this ExportEntry export, params NameReference[] channels)
        {
            PropertyCollection channelsP = new PropertyCollection();
            channelsP.ReplaceAll(channels.Select(x => new BoolProperty(true, x)));
            export.WriteProperty(new StructProperty("LightingChannelContainer", channelsP, "LightingChannels"));
        }

    }
}
