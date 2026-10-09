using System.Text.Json;
using System.Text.Json.Serialization;
using BikeFit.Web.Models;

namespace BikeFit.Web.Services;

/// <summary>
/// Holds the bikes the calculator knows about: the sample library shipped in Data/bikes.json plus
/// anything imported or entered by hand, which is persisted to the git-ignored Data/user folder.
/// </summary>
public sealed class BikeLibrary
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        // camelCase on write so files the app saves match the shipped ones
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _builtInPath;
    private readonly string _userPath;
    private readonly List<BikeModel> _builtIn = [];
    private readonly List<BikeModel> _user = [];
    private readonly Lock _gate = new();

    private readonly string _cockpitPath;
    private readonly string _userCockpitPath;
    private readonly List<CockpitPreset> _cockpits;
    private readonly List<CockpitPreset> _userCockpits;

    /// <summary>
    /// Catalogue cockpits from Data/cockpits.json, with any user-saved version of the same id
    /// replacing it, plus cockpits the user has added. Corrections survive a restart.
    /// </summary>
    public IReadOnlyList<CockpitPreset> Cockpits
    {
        get
        {
            lock (_gate)
            {
                var overrides = _userCockpits.ToDictionary(c => c.Id);
                var merged = _cockpits.Select(c => overrides.GetValueOrDefault(c.Id, c)).ToList();
                merged.AddRange(_userCockpits.Where(c => _cockpits.All(b => b.Id != c.Id)));
                return merged;
            }
        }
    }

    /// <summary>True when the shipped preset with this id has been overridden by a saved correction.</summary>
    public bool IsCockpitOverridden(string id)
    {
        lock (_gate) return _cockpits.Any(c => c.Id == id) && _userCockpits.Any(c => c.Id == id);
    }

    public void SaveCockpit(CockpitPreset preset)
    {
        lock (_gate)
        {
            _userCockpits.RemoveAll(c => c.Id == preset.Id);
            _userCockpits.Add(preset);
            File.WriteAllText(_userCockpitPath, JsonSerializer.Serialize(_userCockpits, Json));
        }
        Changed?.Invoke();
    }

    /// <summary>Drops a saved correction; a shipped preset reverts to its catalogue values.</summary>
    public void ResetCockpit(string id)
    {
        lock (_gate)
        {
            if (_userCockpits.RemoveAll(c => c.Id == id) == 0) return;
            File.WriteAllText(_userCockpitPath, JsonSerializer.Serialize(_userCockpits, Json));
        }
        Changed?.Invoke();
    }

    public event Action? Changed;

    public BikeLibrary(IWebHostEnvironment env, UserDataFolder userData)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "Data");
        _builtInPath = Path.Combine(dataDir, "bikes.json");
        _userPath = userData.FilePath("bikes.json");
        _cockpitPath = Path.Combine(dataDir, "cockpits.json");
        _userCockpitPath = userData.FilePath("cockpits.json");
        _builtIn = Load<BikeModel>(_builtInPath);
        _user = Load<BikeModel>(_userPath);
        _cockpits = Load<CockpitPreset>(_cockpitPath);
        _userCockpits = Load<CockpitPreset>(_userCockpitPath);
    }

    public IReadOnlyList<BikeModel> All
    {
        get { lock (_gate) return [.. _builtIn, .. _user]; }
    }

    public BikeModel? Find(string id) => All.FirstOrDefault(b => b.Id == id);

    public void AddOrReplace(BikeModel bike)
    {
        lock (_gate)
        {
            _user.RemoveAll(b => b.Id == bike.Id);
            _user.Add(bike);
            Save();
        }
        Changed?.Invoke();
    }

    public void Remove(string id)
    {
        lock (_gate)
        {
            if (_user.RemoveAll(b => b.Id == id) == 0) return;
            Save();
        }
        Changed?.Invoke();
    }

    /// <summary>The user's own bikes as JSON, in the same shape the app saves them.</summary>
    public string ExportUserBikes()
    {
        lock (_gate) return JsonSerializer.Serialize(_user, Json);
    }

    /// <summary>Merges uploaded bikes into the user's list; a bike with an existing id replaces it.</summary>
    public ImportSummary ImportUserBikes(string json)
    {
        var uploaded = JsonSerializer.Deserialize<List<BikeModel>>(json, Json) ?? [];
        var imported = 0;
        lock (_gate)
        {
            foreach (var bike in uploaded.Where(b => !string.IsNullOrWhiteSpace(b.Id) && b.Sizes.Count > 0 && _builtIn.All(x => x.Id != b.Id)))
            {
                _user.RemoveAll(b => b.Id == bike.Id);
                _user.Add(bike);
                imported++;
            }
            if (imported > 0) Save();
        }
        if (imported > 0) Changed?.Invoke();
        return new ImportSummary(imported, uploaded.Count - imported);
    }

    private void Save() => File.WriteAllText(_userPath, JsonSerializer.Serialize(_user, Json));

    private static List<T> Load<T>(string path)
    {
        if (!File.Exists(path)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path), Json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
