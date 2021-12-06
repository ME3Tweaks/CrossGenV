using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CrossGenV.Classes
{
    /// <summary>
    /// Utility methods for the console version of VTets
    /// </summary>
    class VTestUtility
    {
        /// <summary>
        /// Fetches an embedded file from the Embedded folder.
        /// </summary>
        /// <param name="embeddedFilename"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static bool LoadEmbeddedFile(string embeddedFilename, out Stream stream)
        {
            var assembly = Assembly.GetExecutingAssembly();
            //var resources = assembly.GetManifestResourceNames();
            //debug
            var assetName = $"CrossGenV.Embedded.{embeddedFilename}";
            stream = assembly.GetManifestResourceStream(assetName);
            if (stream == null)
            {
                Debug.WriteLine($"{assetName} not found in embedded resources");
                return false;
            }
            return true;
        }
    }
}
