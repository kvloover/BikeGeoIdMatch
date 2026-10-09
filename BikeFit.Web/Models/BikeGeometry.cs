using System.Text.Json.Serialization;

namespace BikeFit.Web.Models;

/// <summary>
/// One frame size of one bike. All lengths in millimetres, all angles in degrees.
/// Only <see cref="Stack"/>, <see cref="Reach"/> and <see cref="HeadTubeAngle"/> are required by
/// the solver; the rest is used to draw the bike and to sanity-check the fit.
/// </summary>
public sealed class SizeGeometry
{
    public string Name { get; set; } = "";

    // --- required for the cockpit solve -------------------------------------
    public double Stack { get; set; }
    public double Reach { get; set; }
    public double HeadTubeAngle { get; set; } = 73;

    /// <summary>
    /// Upper headset stack, measured ALONG THE STEERER from the top of the head tube to the bottom
    /// face of the stem. This belongs to the frame, not the cockpit: it is set by the headset and
    /// top cover the frame is designed around, and the same stem on two frames sits at two
    /// different heights.
    ///
    /// Null means nobody has measured it. Manufacturers rarely publish it, though where a brand
    /// gives both a frame stack and a stack-to-stem the difference is this distance - divided by
    /// sin(head angle), since the published difference is vertical and this is along the steerer.
    /// </summary>
    public double? HeadsetStack { get; set; }

    /// <summary>Fallback used when <see cref="HeadsetStack"/> is unknown. A placeholder, not a spec.</summary>
    public const double AssumedHeadsetStack = 12;

    // JsonIgnore: derived, and persisting it would look like a measured value on a frame that has none
    [JsonIgnore]
    public double EffectiveHeadsetStack => HeadsetStack ?? AssumedHeadsetStack;

    // --- used for drawing / secondary numbers -------------------------------
    public double SeatTubeAngle { get; set; } = 73.5;
    public double TopTubeEffective { get; set; }
    public double HeadTubeLength { get; set; }
    public double SeatTubeLength { get; set; }
    public double ChainstayLength { get; set; } = 415;
    public double Wheelbase { get; set; }
    public double BbDrop { get; set; } = 70;
    public double ForkRake { get; set; } = 45;
    public double Trail { get; set; }
    public double StandoverHeight { get; set; }
    public double WheelDiameter { get; set; } = 682; // 700x28c outer diameter

    public SizeGeometry Clone() => (SizeGeometry)MemberwiseClone();
}

public sealed class BikeModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Brand { get; set; } = "";
    public string Model { get; set; } = "";
    public int? Year { get; set; }
    public string? SourceUrl { get; set; }
    /// <summary>Where the numbers came from: "library", "imported", "manual".</summary>
    public string Source { get; set; } = "library";
    /// <summary>Anything the fitter needs to know about these numbers - which stack was used,
    /// what the frame's own headset stack is, and so on.</summary>
    public string? Notes { get; set; }
    public List<SizeGeometry> Sizes { get; set; } = [];

    public BikeModel Clone() => new()
    {
        Id = Id,
        Brand = Brand,
        Model = Model,
        Year = Year,
        SourceUrl = SourceUrl,
        Source = Source,
        Notes = Notes,
        Sizes = Sizes.Select(s => s.Clone()).ToList()
    };

    [JsonIgnore]
    public string DisplayName =>
        string.Join(' ', new[] { Year?.ToString(), Brand, Model }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
