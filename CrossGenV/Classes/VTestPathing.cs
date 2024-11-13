using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Pathing;
using LegendaryExplorerCore.Unreal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Gammtek.Extensions;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using System.IO;

namespace CrossGenV.Classes
{
    public static class VTestPathing
    {
        /// <summary>
        /// Map of where every node is as we compile map files
        /// </summary>
        private static Dictionary<FGuid, Point3D> NavGuidPositionMap { get; } = new();

        public static void InventoryNavigationNodes(IMEPackage package, VTestOptions options)
        {
            var levelActors = package.GetLevelActors();
            if (levelActors == null)
                return; // No level
            options.SetStatusText($"Inventorying navigation points in {package.FileNameNoExtension}");
            var levelNodes = levelActors.Where(x => x.IsA("NavigationPoint"));
            foreach (var node in levelNodes)
            {
                var guid = new FGuid(node.GetProperty<StructProperty>("NavGuid"));
                NavGuidPositionMap[guid] = PathTools.GetLocation(node);
            }
        }


        public static void ComputeReachspecs(IMEPackage package, VTestOptions options)
        {
            var fileHasReachspecs = package.Exports.Any(x => x.IsA("ReachSpec"));
            if (fileHasReachspecs)
            {
                options.SetStatusText($"Computing reachspecs in {package.FileNameNoExtension}");
                foreach (var spec in package.Exports.Where(x => x.IsA("ReachSpec")))
                {
                    ComputeSingleReachspec(spec, options);
                }
            }
        }

        private static bool ComputeSingleReachspec(ExportEntry spec, VTestOptions options)
        {
            //Get start and end exports.
            var properties = spec.GetProperties();
            var start = properties.GetProp<ObjectProperty>("Start");
            if (start == null || start.Value <= 0)
            {
                if (start == null)
                    options.SetStatusText($"  Skipping reachspec compute: {spec.ObjectName.Instanced} - start is null");
                return false;
            }

            var endStruct = properties.GetProp<StructProperty>("End");
            var endActorObj = endStruct.GetProp<ObjectProperty>("Actor");

            Point3D endPoint = null;
            if (endActorObj.Value <= 0)
            {
                // Try by guid
                var guid = new FGuid(endStruct.GetProp<StructProperty>("Guid"));
                if (!NavGuidPositionMap.TryGetValue(guid, out endPoint))
                {
                    if (spec.ObjectName.Instanced == "ReachSpec_6284")
                    {

                    }
                    options.SetStatusText($"  Skipping reachspec compute: {spec.ObjectName.Instanced} - end actor is null and guid was not found in map");
                    return false;
                }
            }

            var startNode = start.ResolveToExport(spec.FileRef, options.cache);
            var endNode = endActorObj.ResolveToExport(spec.FileRef, options.cache);

            var startPoint = PathTools.GetLocation(startNode);
            if (endNode != null)
            {
                endPoint = PathTools.GetLocation(endNode);
            }

            var distance = (float)startPoint.GetDistanceToOtherPoint(endPoint);
            bool isZeroDistance = distance == 0;
            if (isZeroDistance)
                distance = 0.01f; // Prevents exception

            float dirX = (float)((endPoint.X - startPoint.X) / distance);
            float dirY = (float)((endPoint.Y - startPoint.Y) / distance);
            float dirZ = (float)((endPoint.Z - startPoint.Z) / distance);

            properties.AddOrReplaceProp(new FloatProperty(isZeroDistance ? 0f : distance, "Distance"));
            properties.AddOrReplaceProp(CommonStructs.Vector3Prop(dirX, dirY, dirZ, "Direction"));
            spec.WriteProperties(properties);
            return true;
        }
    }
}
