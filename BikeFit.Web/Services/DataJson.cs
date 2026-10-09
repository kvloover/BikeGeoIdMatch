using System.Text.Json;
using System.Text.Json.Serialization;

namespace BikeFit.Web.Services;

/// <summary>One JSON convention for everything read from or written to a file.</summary>
public static class DataJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        // camelCase on write so exports match the shipped catalogue files
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };
}
