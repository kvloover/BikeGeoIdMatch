namespace BikeFit.Web.Models;

/// <summary>Everything a visitor owns, as one file: what an export writes and an upload reads.</summary>
public sealed class UserData
{
    public List<BikeModel> Bikes { get; set; } = [];
    public List<FitProfile> Fits { get; set; } = [];
    public List<CockpitPreset> Cockpits { get; set; } = [];

    /// <summary>Where they left off: target, saddle, cockpit, selections.</summary>
    public UserSettings? Settings { get; set; }
}
