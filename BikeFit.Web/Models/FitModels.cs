namespace BikeFit.Web.Models;

/// <summary>A point in the fit coordinate system: origin = bottom bracket centre,
/// X positive towards the front wheel, Y positive up. Millimetres.</summary>
public readonly record struct Pt(double X, double Y)
{
    public static Pt operator +(Pt a, Pt b) => new(a.X + b.X, a.Y + b.Y);
    public static Pt operator -(Pt a, Pt b) => new(a.X - b.X, a.Y - b.Y);
    public static Pt operator *(Pt a, double k) => new(a.X * k, a.Y * k);
    public double Length => Math.Sqrt(X * X + Y * Y);
    public static Pt FromAngle(double degrees, double length = 1) =>
        new(Math.Cos(degrees * Math.PI / 180) * length, Math.Sin(degrees * Math.PI / 180) * length);
}

/// <summary>Where the HB coordinate is taken on the bar.</summary>
public enum HbReference
{
    /// <summary>Centre of the bar tube at the stem clamp (idmatch default).</summary>
    BarCentre,
    /// <summary>Top surface of the bar tube at the stem clamp.</summary>
    BarTop,
    /// <summary>Centre of the furthest forward point of the tops, one bar reach ahead of the clamp.</summary>
    BarFront
}

/// <summary>The target rider position, as produced by an idmatch (or similar) fit session.</summary>
public sealed class FitTarget
{
    // idmatch calls the horizontal axis X and the vertical axis Z; internally the drawing code
    // works in X/Y, so BrpZ / HbZ are the vertical values.
    //
    // BRP is the Biomechanical Reference Point, an idmatch standard: the point on the saddle where
    // its width reaches 70 mm. Defining it by width rather than by the nose or the midpoint is what
    // makes it portable - the same coordinates describe the same seating position on any saddle,
    // whatever its length. It is NOT the centre of the saddle.

    /// <summary>BRP X in drawing coordinates: horizontal distance from BB to the saddle reference
    /// point, negative behind the BB. Set it through <see cref="BrpSetback"/>, which is the way the
    /// number is quoted and entered.</summary>
    public double BrpX { get; set; } = -200;

    /// <summary>
    /// BRP X as it is read off a fit sheet and typed in: a positive number is saddle setback, i.e.
    /// millimetres BEHIND the bottom bracket. A negative value puts the saddle in front of the BB.
    /// </summary>
    public double BrpSetback
    {
        get => -BrpX;
        set => BrpX = -value;
    }
    /// <summary>BRP Z: height of the Biomechanical Reference Point above the BB - the saddle height.</summary>
    public double BrpZ { get; set; } = 720;

    /// <summary>HB X: horizontal distance from BB to the handlebar point (positive = in front of the BB).</summary>
    public double HbX { get; set; } = 490;
    /// <summary>HB Z: height of the handlebar point above the BB.</summary>
    public double HbZ { get; set; } = 620;

    public HbReference HbReference { get; set; } = HbReference.BarCentre;

    /// <summary>Acceptable miss distance before a solution is flagged. mm.</summary>
    public double ToleranceMm { get; set; } = 5;
}

/// <summary>Everything about the parts between the top of the head tube and the HB point.</summary>
public sealed class CockpitSpec
{
    // The headset stack lives on SizeGeometry, not here: it is a property of the frame rather than
    // of the cockpit bolted to it.

    /// <summary>Height of the stem's steerer clamp (bottom face to top face of the stem body).</summary>
    public double StemClampHeight { get; set; } = 40;

    /// <summary>Distance from the bottom face of the stem to the centreline of the extension.
    /// Defaults to half the clamp height; set explicitly for stems whose extension is not centred.</summary>
    public double? StemAxisOffset { get; set; }

    public double EffectiveStemAxisOffset => StemAxisOffset ?? StemClampHeight / 2.0;

    /// <summary>Stem / cockpit angle in the usual industry sense: measured from the perpendicular
    /// to the steerer. A -17 deg stem is horizontal on a 73 deg head angle. Positive = rise.</summary>
    public double StemAngle { get; set; } = -6;

    /// <summary>True when the stem is mounted with the angle pointing down (negates the angle).</summary>
    public bool Flipped { get; set; }

    public double InstalledStemAngle => Flipped ? -StemAngle : StemAngle;

    /// <summary>
    /// Height of the bar section at the centre - the "thickness of the cockpit". Half of it is the
    /// step from the extension centreline up to the top surface, so it matters whenever the HB point
    /// is referenced to the top of the bar.
    ///
    /// On a two-piece bar this is genuinely the 31.8 mm clamp diameter, and half of it really is the
    /// physical distance from the bar's centreline to its top surface.
    ///
    /// On a ONE-PIECE cockpit it is NOT a physical dimension. The section at the centre is a deep,
    /// thin aerofoil that rounds out towards the hoods and is fully round only in the drops, so its
    /// actual thickness there is nothing like 31.8. What this number represents instead is the
    /// vertical offset between the HB point and the notional point the maker measures the quoted
    /// stem length to - and one-piece cockpits are quoted as conventional-equivalents ("90 x 380"),
    /// i.e. to where the centre of a round 31.8 bar would sit. So 31.8 is right as an EQUIVALENCE
    /// even when the blade is thin.
    ///
    /// The fit data backs that up. Solving three bikes for the horizontal run to the HB point:
    ///   offset 0 mm  -> implied stem lengths 95.4 / 95.2 / 105.3, a consistent +5.3 mm bias
    ///   offset 15.9  -> 90.5 / 90.5 / 100.7 against stock 90 / 90 / 100
    /// A near-zero offset is not supported; the quoted length is measured well below the top surface.
    /// </summary>
    public double BarTubeDiameter { get; set; } = 31.8;

    /// <summary>Perpendicular offset from the stem extension axis to the bar tube centre at the clamp.
    /// Zero for a normal stem + round bar. Integrated cockpits often sit a few mm above or below.</summary>
    public double BarCentreOffset { get; set; }

    /// <summary>Handlebar reach: horizontal distance from the clamp centre to the furthest forward
    /// point of the tops, with the bar rolled to neutral.</summary>
    public double BarReach { get; set; } = 75;

    /// <summary>Handlebar drop: vertical distance from the tops to the centre of the bar ends.</summary>
    public double BarDrop { get; set; } = 127;

    /// <summary>Nominal bar width, centre to centre. Treated as the width at the hoods / lever clamp,
    /// which is where the quoted size is normally taken on a flared bar.</summary>
    public double BarWidth { get; set; } = 400;

    /// <summary>Flare at the drops, in degrees per side.</summary>
    public double DropFlare { get; set; }

    /// <summary>
    /// Vertical run over which the flare actually acts. It is NOT the full drop: the bend curves
    /// through its upper half and only the lower leg is flared, so using the whole drop overstates
    /// the spread badly. Owner measurements of a 4 deg Roval Rapide give 10-12 mm of spread where
    /// the full 127 mm drop would predict 17.8 mm, implying a run of about 62% of the drop.
    /// </summary>
    public double? FlareRun { get; set; }

    public double EffectiveFlareRun => FlareRun ?? BarDrop * 0.62;

    /// <summary>How much wider the drops are than the hoods, centre to centre.</summary>
    public double DropSpread => 2 * EffectiveFlareRun * Math.Tan(DropFlare * Math.PI / 180);

    public double WidthAtHoods => BarWidth;
    public double WidthAtDrops => BarWidth + DropSpread;

    /// <summary>Max spacer stack the fitter is willing to run below the stem, mm.</summary>
    public double MaxSpacerStack { get; set; } = 40;

    /// <summary>Spacer granularity, mm.</summary>
    public double SpacerStep { get; set; } = 5;

    /// <summary>Stem lengths available, mm.</summary>
    public List<double> StemLengths { get; set; } = [60, 70, 80, 90, 100, 110, 120, 130, 140];

    /// <summary>Stem angles available (industry sense, absolute value; both orientations are tried).</summary>
    public List<double> StemAngles { get; set; } = [6, 7, 8, 10, 12, 17];

    /// <summary>When true the solver only uses <see cref="StemAngle"/> (a specific cockpit the rider owns).</summary>
    public bool FixedAngle { get; set; }

    /// <summary>
    /// The cockpit's tops are designed to sit level, so the line from the steerer to the HB point is
    /// horizontal whatever the frame's head angle. The installed angle is then not a constant but
    /// HTA - 90 - about -16.5 deg on a 73.5 deg frame - and it changes from bike to bike.
    ///
    /// This matters on a one-piece aero cockpit, where the quoted angle (-6 for a Roval Rapide)
    /// describes the extension body from the steerer clamp to the notional bar clamp, while the HB
    /// point sits on the aero tops - a different feature of the same part. Using the quoted angle
    /// leaves a systematic error: across three bikes and three cockpits it made every implied stem
    /// length about 2.9 mm short of the stock fitment, and level tops removed it.
    ///
    /// A consequence worth knowing: with level tops the stem length no longer changes bar HEIGHT,
    /// only reach. On a +10.5 deg extension a 10 mm longer stem also lifts the bar about 1.8 mm.
    /// </summary>
    public bool FlatTops { get; set; }

    /// <summary>
    /// One-piece bar/stem: there is no separate stem body and no bar clamp, so the drawings render a
    /// single swept shape instead of a stem block plus a round bar. Affects presentation only.
    /// </summary>
    public bool OnePiece { get; set; }

    /// <summary>When true the stem length is treated as continuous (integrated cockpit made to measure,
    /// or "just tell me the exact number").</summary>
    public bool ContinuousLength { get; set; }

    public CockpitSpec Clone() => (CockpitSpec)MemberwiseClone();
}

/// <summary>One candidate build for one frame size.</summary>
public sealed class FitSolution
{
    public required BikeModel Bike { get; init; }
    public required SizeGeometry Size { get; init; }
    /// <summary>The cockpit this build was solved with. Each bike can carry a different one.</summary>
    public required CockpitSpec Cockpit { get; init; }
    public required string CockpitName { get; init; }

    /// <summary>Installed stem angle relative to the steerer perpendicular (already flipped).</summary>
    public double StemAngle { get; init; }
    public bool Flipped { get; init; }
    /// <summary>Angle of the stem measured from horizontal, as it sits on the bike.</summary>
    public double StemAngleFromHorizontal { get; init; }

    /// <summary>Exact spacer stack that would put the bar dead on target, mm (may be negative / over the limit).</summary>
    public double IdealSpacerStack { get; init; }
    /// <summary>Exact stem length that would put the bar dead on target, mm.</summary>
    public double IdealStemLength { get; init; }

    /// <summary>Chosen spacer stack, a multiple of the spacer step.</summary>
    public double SpacerStack { get; init; }
    public int SpacerCount { get; init; }
    /// <summary>Chosen stem length (catalog value, or the exact value when continuous).</summary>
    public double StemLength { get; init; }

    /// <summary>Where the bar centre actually ends up.</summary>
    public Pt BarCentre { get; init; }
    /// <summary>Where the HB reference point actually ends up (bar centre or bar top).</summary>
    public Pt HbActual { get; init; }
    public Pt HbTarget { get; init; }

    public double ErrorX => HbActual.X - HbTarget.X;
    public double ErrorY => HbActual.Y - HbTarget.Y;
    public double Error => Math.Sqrt(ErrorX * ErrorX + ErrorY * ErrorY);

    public bool SpacerStackFeasible { get; init; }
    public bool StemLengthFeasible { get; init; }
    public bool WithinTolerance { get; init; }
    public List<string> Warnings { get; init; } = [];

    /// <summary>Lower = better.</summary>
    public double Score { get; init; }
}

/// <summary>Numbers that fall out of the target itself, independent of the bike.</summary>
public sealed record RiderMetrics(
    double SaddleHeight,
    double SaddleSetback,
    /// <summary>Straight line from BRP to HB, which is what the idmatch fit converter reports.</summary>
    double SaddleToBarDirect,
    /// <summary>Horizontal component of the same distance.</summary>
    double SaddleToBarHorizontal,
    double SaddleToBarDrop);
