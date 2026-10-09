using System.Text.Json;
using BikeFit.Web.Models;

namespace BikeFit.Web.Services;

/// <summary>
/// The read-only catalogue shipped with the app: sample bikes and cockpit presets. Loaded once and
/// shared by every visitor; nothing here is ever written back.
/// </summary>
public sealed class Catalogue
{
    public IReadOnlyList<BikeModel> Bikes { get; }
    public IReadOnlyList<CockpitPreset> Cockpits { get; }

    public Catalogue(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "Data");
        Bikes = Load<BikeModel>(Path.Combine(dataDir, "bikes.json"));
        Cockpits = Load<CockpitPreset>(Path.Combine(dataDir, "cockpits.json"));
    }

    private static List<T> Load<T>(string path)
    {
        if (!File.Exists(path)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path), DataJson.Options) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
