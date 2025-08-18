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
    /// <summary>
    /// Class representation of a BioTriggerStream's StreamingStates, since it's a lot of boilerplate to work with.
    /// </summary>
    internal class BioTriggerStreaming
    {
        public ExportEntry Export { get; set; }
        public List<StreamingState> StreamingStates { get; set; }

        public static BioTriggerStreaming FromExport(ExportEntry export)
        {
            BioTriggerStreaming bts = new BioTriggerStreaming();

            var streamingStates = export.GetProperty<ArrayProperty<StructProperty>>("StreamingStates");
            bts.StreamingStates = new List<StreamingState>();
            foreach (var ss in streamingStates)
            {
                bts.StreamingStates.Add(StreamingState.FromStruct(ss));
            }

            bts.Export = export;
            return bts;
        }

        public void WriteStreamingStates(ExportEntry target)
        {
            target.WriteProperty(new ArrayProperty<StructProperty>(StreamingStates.Select(x=>x.ToStruct()), "StreamingStates"));
        }
    }

    internal class StreamingState
    {
        public NameReference StateName { get; set; }
        public NameReference InChunkName { get; set; }
        public List<NameReference> VisibleChunkNames { get; set; }
        public List<NameReference> LoadChunkNames { get; set; }

        public static StreamingState FromStruct(StructProperty ss)
        {
            StreamingState state = new StreamingState();
            state.StateName = ss.GetProp<NameProperty>("StateName").Value;
            state.InChunkName = ss.GetProp<NameProperty>("InChunkName").Value;
            state.VisibleChunkNames = ss.GetProp<ArrayProperty<NameProperty>>("VisibleChunkNames").Select(x => x.Value).ToList();
            state.LoadChunkNames = ss.GetProp<ArrayProperty<NameProperty>>("LoadChunkNames").Select(x => x.Value).ToList();
            return state;
        }

        public StructProperty ToStruct()
        {
            PropertyCollection pc = new PropertyCollection();
            pc.AddOrReplaceProp(new NameProperty(StateName, "StateName"));
            pc.AddOrReplaceProp(new NameProperty(InChunkName, "InChunkName"));

            var visibleChunks = new ArrayProperty<NameProperty>("VisibleChunkNames");
            visibleChunks.AddRange(VisibleChunkNames.Select(x=>new NameProperty(x)));
            pc.AddOrReplaceProp(visibleChunks);

            // Load chunks cannot contain items in visible. They will override and break things.
            var loadChunks = new ArrayProperty<NameProperty>("LoadChunkNames");
            loadChunks.AddRange(LoadChunkNames.Where(x=>!VisibleChunkNames.Contains(x)).Select(x => new NameProperty(x)));
            pc.AddOrReplaceProp(loadChunks);

            return new StructProperty("BioStreamingState", pc);
        }
    }
}
