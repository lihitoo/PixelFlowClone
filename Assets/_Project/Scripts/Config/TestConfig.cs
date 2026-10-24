using System;
using Dreamy.DataConfig;
using Newtonsoft.Json;

[Serializable]
public sealed class TestConfig : DataConfigRow
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("value")]
    public int Value { get; set; }
}
[DataConfig("testConfigs")]
public sealed class TestConfigTable : DataConfigTable<TestConfig>
{
}