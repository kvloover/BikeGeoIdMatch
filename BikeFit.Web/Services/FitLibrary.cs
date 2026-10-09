using BikeFit.Web.Models;

namespace BikeFit.Web.Services;

/// <summary>One visitor's saved fit sheets. Held in memory for the session and kept by exporting.</summary>
public sealed class FitLibrary
{
    private readonly List<FitProfile> _fits = [];

    public event Action? Changed;

    public IReadOnlyList<FitProfile> All => _fits;

    public void Save(FitProfile fit)
    {
        _fits.RemoveAll(f => f.Id == fit.Id);
        _fits.Add(fit);
        Changed?.Invoke();
    }

    public void Remove(string id)
    {
        if (_fits.RemoveAll(f => f.Id == id) == 0) return;
        Changed?.Invoke();
    }

    /// <summary>Merges uploaded fit sheets in; a sheet with an existing id replaces it.</summary>
    public ImportSummary Import(IReadOnlyCollection<FitProfile> uploaded)
    {
        var usable = uploaded.Where(f => !string.IsNullOrWhiteSpace(f.Id)).ToList();
        foreach (var fit in usable)
        {
            _fits.RemoveAll(f => f.Id == fit.Id);
            _fits.Add(fit);
        }
        if (usable.Count > 0) Changed?.Invoke();
        return new ImportSummary(usable.Count, uploaded.Count - usable.Count);
    }
}
