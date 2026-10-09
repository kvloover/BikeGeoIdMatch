using System.Text.Json;
using BikeFit.Web.Models;

namespace BikeFit.Web.Services;

/// <summary>The outcome of reading an uploaded file: what was merged, and the working state it carried, if any.</summary>
public sealed record UserDataImport(ImportSummary Summary, UserSettings? Settings);

/// <summary>
/// Turns a visitor's bikes, fits and cockpits into one file and back. This is the only way their
/// data outlives the session, since nothing is stored on the server.
/// </summary>
public sealed class UserDataExchange(BikeLibrary bikes, FitLibrary fits)
{
    public string Export(UserSettings settings) => JsonSerializer.Serialize(new UserData
    {
        Bikes = [.. bikes.UserBikes],
        Fits = [.. fits.All],
        Cockpits = [.. bikes.UserCockpits],
        Settings = settings
    }, DataJson.Options);

    /// <summary>Accepts a full export, or one of the single-purpose lists earlier versions saved.</summary>
    /// <exception cref="JsonException">The text is not a recognisable export.</exception>
    public UserDataImport Import(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.ValueKind switch
        {
            JsonValueKind.Object => ImportBundle(json),
            JsonValueKind.Array => ImportList(doc.RootElement),
            _ => throw new JsonException("Not an export file.")
        };
    }

    private UserDataImport ImportBundle(string json)
    {
        var data = JsonSerializer.Deserialize<UserData>(json, DataJson.Options) ?? new UserData();
        var summary = bikes.ImportBikes(data.Bikes) + fits.Import(data.Fits) + bikes.ImportCockpits(data.Cockpits);
        return new UserDataImport(summary, data.Settings);
    }

    private UserDataImport ImportList(JsonElement list)
    {
        var raw = list.GetRawText();
        var first = list.EnumerateArray().FirstOrDefault();
        var summary = first.ValueKind != JsonValueKind.Object ? default
            : Has(first, "sizes") ? bikes.ImportBikes(JsonSerializer.Deserialize<List<BikeModel>>(raw, DataJson.Options) ?? [])
            : Has(first, "brpSetback") ? fits.Import(JsonSerializer.Deserialize<List<FitProfile>>(raw, DataJson.Options) ?? [])
            : bikes.ImportCockpits(JsonSerializer.Deserialize<List<CockpitPreset>>(raw, DataJson.Options) ?? []);
        return new UserDataImport(summary, null);
    }

    private static bool Has(JsonElement obj, string name) =>
        obj.EnumerateObject().Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
