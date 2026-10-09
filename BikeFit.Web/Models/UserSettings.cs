namespace BikeFit.Web.Models;

/// <summary>
/// Everything the calculator page would otherwise forget on a restart: what was being worked on,
/// not the library of bikes and fits (those are saved explicitly).
/// </summary>
public sealed class UserSettings
{
    /// <summary>The target as it stands in the form, whether or not it has been saved as a fit.</summary>
    public FitProfile Target { get; set; } = new();

    /// <summary>Id of the saved fit the form was last loaded from or saved to, if any.</summary>
    public string FitId { get; set; } = "";

    public SaddleSpec Saddle { get; set; } = new();
    public CockpitSpec Cockpit { get; set; } = new();

    /// <summary>Id of the cockpit preset last chosen in the editor, if any.</summary>
    public string CockpitPresetId { get; set; } = "";

    public List<string> SelectedBikeIds { get; set; } = [];

    /// <summary>Bike id -> cockpit preset id. A bike with no entry uses the custom cockpit.</summary>
    public Dictionary<string, string> BikeCockpits { get; set; } = [];

    public string SortMode { get; set; } = "";
}
