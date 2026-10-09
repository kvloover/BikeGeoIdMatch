namespace BikeFit.Web.Models;

/// <summary>
/// A saved fit sheet: the two coordinates and the convention they were measured in. Kept separate
/// from <see cref="CockpitPreset"/> because a fit outlives whichever bike it is applied to.
/// </summary>
public sealed class FitProfile
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Notes { get; set; }

    /// <summary>Saddle setback, positive behind the BB - the value as it is typed in.</summary>
    public double BrpSetback { get; set; }
    public double BrpZ { get; set; }
    public double HbX { get; set; }
    public double HbZ { get; set; }
    public HbReference HbReference { get; set; }
    public double ToleranceMm { get; set; } = 5;

    public void ApplyTo(FitTarget target)
    {
        target.BrpSetback = BrpSetback;
        target.BrpZ = BrpZ;
        target.HbX = HbX;
        target.HbZ = HbZ;
        target.HbReference = HbReference;
        target.ToleranceMm = ToleranceMm;
    }

    public static FitProfile CaptureFrom(FitTarget t, string id, string name, string? notes = null) => new()
    {
        Id = id,
        Name = name,
        Notes = notes,
        BrpSetback = t.BrpSetback,
        BrpZ = t.BrpZ,
        HbX = t.HbX,
        HbZ = t.HbZ,
        HbReference = t.HbReference,
        ToleranceMm = t.ToleranceMm
    };
}
