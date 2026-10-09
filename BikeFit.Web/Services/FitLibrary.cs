using System.Text.Json;
using System.Text.Json.Serialization;
using BikeFit.Web.Models;

namespace BikeFit.Web.Services;

/// <summary>Saved fit sheets, persisted to the git-ignored Data/user folder.</summary>
public sealed class FitLibrary
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;
    private readonly List<FitProfile> _fits;
    private readonly Lock _gate = new();

    public event Action? Changed;

    public FitLibrary(UserDataFolder userData)
    {
        _path = userData.FilePath("fits.json");
        _fits = Load();
    }

    public IReadOnlyList<FitProfile> All
    {
        get { lock (_gate) return [.. _fits]; }
    }

    public void Save(FitProfile fit)
    {
        lock (_gate)
        {
            _fits.RemoveAll(f => f.Id == fit.Id);
            _fits.Add(fit);
            Write();
        }
        Changed?.Invoke();
    }

    public void Remove(string id)
    {
        lock (_gate)
        {
            if (_fits.RemoveAll(f => f.Id == id) == 0) return;
            Write();
        }
        Changed?.Invoke();
    }

    /// <summary>The saved fit sheets (the idmatch coordinates) as JSON, in the same shape the app saves them.</summary>
    public string Export()
    {
        lock (_gate) return JsonSerializer.Serialize(_fits, Json);
    }

    /// <summary>Merges uploaded fit sheets in; a sheet with an existing id replaces it.</summary>
    public ImportSummary Import(string json)
    {
        var uploaded = JsonSerializer.Deserialize<List<FitProfile>>(json, Json) ?? [];
        var imported = 0;
        lock (_gate)
        {
            foreach (var fit in uploaded.Where(f => !string.IsNullOrWhiteSpace(f.Id)))
            {
                _fits.RemoveAll(f => f.Id == fit.Id);
                _fits.Add(fit);
                imported++;
            }
            if (imported > 0) Write();
        }
        if (imported > 0) Changed?.Invoke();
        return new ImportSummary(imported, uploaded.Count - imported);
    }

    private void Write() => File.WriteAllText(_path, JsonSerializer.Serialize(_fits, Json));

    private List<FitProfile> Load()
    {
        if (!File.Exists(_path)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<FitProfile>>(File.ReadAllText(_path), Json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
