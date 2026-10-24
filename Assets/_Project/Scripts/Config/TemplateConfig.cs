using Dreamy.DataConfig;
using Newtonsoft.Json;

namespace Dreamy.Template
{
    [DataConfig("templateConfig")]
    public sealed class TemplateConfig : ConfigBase
    {
        public int StartingScore { get; set; }

        [JsonProperty("startingCoins")]
        private int LegacyStartingCoins { set => StartingScore = value; }
    }
}
