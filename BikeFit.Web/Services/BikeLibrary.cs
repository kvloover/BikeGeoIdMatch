using BikeFit.Web.Models;

namespace BikeFit.Web.Services;

/// <summary>
/// The bikes and cockpits one visitor can pick from: the shared catalogue plus whatever they have
/// imported, entered or corrected. Their own entries live in memory for the length of the session
/// and are kept only by exporting them.
/// </summary>
public sealed class BikeLibrary(Catalogue catalogue)
{
    private readonly List<BikeModel> _user = [];
    private readonly List<CockpitPreset> _userCockpits = [];

    public event Action? Changed;

    public IReadOnlyList<BikeModel> All => [.. catalogue.Bikes, .. _user];

    /// <summary>Only the bikes this visitor added or edited: what an export carries.</summary>
    public IReadOnlyList<BikeModel> UserBikes => _user;

    public BikeModel? Find(string id) => All.FirstOrDefault(b => b.Id == id);

    public void AddOrReplace(BikeModel bike)
    {
        _user.RemoveAll(b => b.Id == bike.Id);
        _user.Add(bike);
        Changed?.Invoke();
    }

    public void Remove(string id)
    {
        if (_user.RemoveAll(b => b.Id == id) == 0) return;
        Changed?.Invoke();
    }

    /// <summary>Merges uploaded bikes in; one with an existing id replaces it, a catalogue id is skipped.</summary>
    public ImportSummary ImportBikes(IReadOnlyCollection<BikeModel> uploaded)
    {
        var usable = uploaded
            .Where(b => !string.IsNullOrWhiteSpace(b.Id) && b.Sizes.Count > 0 && catalogue.Bikes.All(x => x.Id != b.Id))
            .ToList();
        foreach (var bike in usable)
        {
            _user.RemoveAll(b => b.Id == bike.Id);
            _user.Add(bike);
        }
        if (usable.Count > 0) Changed?.Invoke();
        return new ImportSummary(usable.Count, uploaded.Count - usable.Count);
    }

    /// <summary>
    /// Catalogue cockpits, with any saved version of the same id replacing it, plus cockpits the
    /// visitor has added.
    /// </summary>
    public IReadOnlyList<CockpitPreset> Cockpits
    {
        get
        {
            var overrides = _userCockpits.ToDictionary(c => c.Id);
            var merged = catalogue.Cockpits.Select(c => overrides.GetValueOrDefault(c.Id, c)).ToList();
            merged.AddRange(_userCockpits.Where(c => catalogue.Cockpits.All(b => b.Id != c.Id)));
            return merged;
        }
    }

    /// <summary>Only the cockpits this visitor corrected or added: what an export carries.</summary>
    public IReadOnlyList<CockpitPreset> UserCockpits => _userCockpits;

    /// <summary>True when the catalogue preset with this id has been overridden by a saved correction.</summary>
    public bool IsCockpitOverridden(string id) =>
        catalogue.Cockpits.Any(c => c.Id == id) && _userCockpits.Any(c => c.Id == id);

    public void SaveCockpit(CockpitPreset preset)
    {
        _userCockpits.RemoveAll(c => c.Id == preset.Id);
        _userCockpits.Add(preset);
        Changed?.Invoke();
    }

    /// <summary>Drops a saved correction; a catalogue preset reverts to its published values.</summary>
    public void ResetCockpit(string id)
    {
        if (_userCockpits.RemoveAll(c => c.Id == id) == 0) return;
        Changed?.Invoke();
    }

    public ImportSummary ImportCockpits(IReadOnlyCollection<CockpitPreset> uploaded)
    {
        var usable = uploaded.Where(c => !string.IsNullOrWhiteSpace(c.Id)).ToList();
        foreach (var cockpit in usable)
        {
            _userCockpits.RemoveAll(c => c.Id == cockpit.Id);
            _userCockpits.Add(cockpit);
        }
        if (usable.Count > 0) Changed?.Invoke();
        return new ImportSummary(usable.Count, uploaded.Count - usable.Count);
    }
}
