using System.Text.Json.Serialization;

namespace BloodScroll;

//
// What kind of layer this is. Comes from "LayerType" in mobWaves.json,
// where it is still written the old way ("Boss Layer", "Ground Layer").
//

public enum LayerType
{
    [JsonStringEnumMemberName("Layer")]
    Normal,

    [JsonStringEnumMemberName("Ground Layer")]
    Ground,

    [JsonStringEnumMemberName("Boss Layer")]
    Boss
}

public static class LayerTypeExtensions
{
    // Text shown in the HUD
    public static string ToDisplayString(this LayerType type) => type switch
    {
        LayerType.Ground => "Ground Layer",
        LayerType.Boss => "Boss Layer",
        _ => "Layer"
    };
}
