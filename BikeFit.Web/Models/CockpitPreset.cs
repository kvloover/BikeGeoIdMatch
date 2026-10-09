namespace BikeFit.Web.Models;

/// <summary>
/// A catalogue cockpit. Only the fields the manufacturer actually publishes are filled in;
/// the rest stay null so the calculator keeps whatever the fitter has typed rather than
/// inventing a number.
/// </summary>
public sealed class CockpitPreset
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? SourceUrl { get; set; }
    public string? Notes { get; set; }
    /// <summary>"catalogue" for the shipped presets, "custom" for anything saved from the UI.</summary>
    public string Source { get; set; } = "catalogue";

    public double? StemAngle { get; set; }
    public double? StemClampHeight { get; set; }
    /// <summary>Bottom of the stem to the centreline of the extension. Stored explicitly because it
    /// is rarely half the clamp height on a one-piece cockpit, and it trades off against the
    /// headset stack roughly 1:1 in the solve.</summary>
    public double? StemAxisOffset { get; set; }
    public double? BarReach { get; set; }
    public double? BarDrop { get; set; }
    /// <summary>Flare at the drops, degrees per side.</summary>
    public double? DropFlare { get; set; }
    /// <summary>Widths the cockpit is sold in, centre to centre at the hoods.</summary>
    public List<double> BarWidths { get; set; } = [];
    public double? BarTubeDiameter { get; set; }

    // The headset stack is not here: it belongs to the frame and lives on SizeGeometry.

    public List<double> StemLengths { get; set; } = [];
    /// <summary>True for a one-piece bar/stem: the length is chosen at purchase, not adjusted later.</summary>
    public bool Integrated { get; set; }

    /// <summary>
    /// The tops are designed to sit level, so the angle follows the frame's head tube rather than
    /// the quoted stem angle. Null leaves whatever is set. See <see cref="CockpitSpec.FlatTops"/>.
    /// </summary>
    public bool? FlatTops { get; set; }

    /// <summary>Copies the published values onto a spec, leaving everything else alone.</summary>
    public void ApplyTo(CockpitSpec spec)
    {
        if (StemAngle is { } a) { spec.StemAngle = a; spec.Flipped = false; spec.FixedAngle = true; }
        if (StemClampHeight is { } h) spec.StemClampHeight = h;
        spec.StemAxisOffset = StemAxisOffset;
        if (BarReach is { } r) spec.BarReach = r;
        if (BarDrop is { } d) spec.BarDrop = d;
        if (DropFlare is { } fl) spec.DropFlare = fl;
        if (BarTubeDiameter is { } t) spec.BarTubeDiameter = t;
        if (StemLengths.Count > 0) spec.StemLengths = [.. StemLengths];
        if (FlatTops is { } ft) spec.FlatTops = ft;
        spec.OnePiece = Integrated;
        spec.ContinuousLength = false;
    }

    /// <summary>Snapshots the cockpit fields currently on screen back into a preset.</summary>
    public static CockpitPreset CaptureFrom(CockpitSpec spec, string id, string name, CockpitPreset? basedOn = null) => new()
    {
        Id = id,
        Name = name,
        Source = "custom",
        SourceUrl = basedOn?.SourceUrl,
        Notes = basedOn?.Notes,
        StemAngle = spec.StemAngle,
        StemClampHeight = spec.StemClampHeight,
        StemAxisOffset = spec.StemAxisOffset,
        BarReach = spec.BarReach,
        BarDrop = spec.BarDrop,
        DropFlare = spec.DropFlare,
        BarWidths = [.. basedOn?.BarWidths ?? []],
        BarTubeDiameter = spec.BarTubeDiameter,
        StemLengths = [.. spec.StemLengths],
        FlatTops = spec.FlatTops,
        Integrated = basedOn?.Integrated ?? spec.ContinuousLength
    };
}
