using System.Collections.Generic;
using Newtonsoft.Json;
using NHLAPI;

namespace StarsBot.Files
{
    public class DataModel
    {
        [JsonProperty("knownPlays")]
        public List<int> KnownPlays { get; set; }

        [JsonProperty("lastKnownData")]
        public NHLData LastKnownData { get; set; }
    }

    public class RunningData : DataModel
    {
        public RunningData(List<int> knownPlays, NHLData lastKnownData)
        {
            KnownPlays = knownPlays;
            LastKnownData = lastKnownData;
        }

        public RunningData() : this(new List<int>(), null) { }

        public override string ToString()
            => JsonConvert.SerializeObject(this, Formatting.Indented);

        public static RunningData FromString(string s)
            => JsonConvert.DeserializeObject<RunningData>(s);
    }
}
