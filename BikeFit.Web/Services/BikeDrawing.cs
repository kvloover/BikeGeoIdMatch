using BikeFit.Web.Models;

namespace BikeFit.Web.Services;

/// <summary>Every point needed to draw a bike side-on, in fit coordinates (BB = origin, Y up, mm).</summary>
public sealed record BikeDrawing(
    Pt Bb,
    Pt RearAxle,
    Pt FrontAxle,
    double WheelRadius,
    Pt HeadTubeTop,
    Pt HeadTubeBottom,
    Pt SeatTubeTop,
    Pt VirtualTopTubeEnd,
    Pt HeadsetTop,
    Pt SpacerTop,
    Pt StemPivot,
    Pt BarCentre,
    double BarRadius,
    Pt Saddle,
    double StemAngleFromHorizontal)
{
    public static BikeDrawing Build(SizeGeometry s, CockpitSpec cockpit, FitTarget target,
                                    double spacerStack, double stemLength, double installedStemAngle)
    {
        var chain = CockpitMath.Build(s, cockpit, spacerStack, stemLength, installedStemAngle);

        var htl = s.HeadTubeLength > 0 ? s.HeadTubeLength : 140;
        var down = new Pt(Math.Cos(CockpitMath.Rad(s.HeadTubeAngle)), -Math.Sin(CockpitMath.Rad(s.HeadTubeAngle)));
        var htb = chain.HeadTubeTop + down * htl;

        var drop = s.BbDrop > 0 ? s.BbDrop : 70;
        var cs = s.ChainstayLength > 0 ? s.ChainstayLength : 415;
        var rearX = -Math.Sqrt(Math.Max(1, cs * cs - drop * drop));
        var rearAxle = new Pt(rearX, drop);

        Pt frontAxle;
        if (s.Wheelbase > 0)
        {
            frontAxle = new Pt(rearX + s.Wheelbase, drop);
        }
        else
        {
            // slide down the steerer axis to axle height, then step forward by the fork rake
            var t = (htb.Y - drop) / Math.Sin(CockpitMath.Rad(s.HeadTubeAngle));
            var onAxis = htb + down * t;
            var rakeDir = new Pt(Math.Sin(CockpitMath.Rad(s.HeadTubeAngle)), Math.Cos(CockpitMath.Rad(s.HeadTubeAngle)));
            frontAxle = onAxis + rakeDir * (s.ForkRake > 0 ? s.ForkRake : 45);
        }

        var stl = s.SeatTubeLength > 0 ? s.SeatTubeLength : 520;
        var stAxis = new Pt(-Math.Cos(CockpitMath.Rad(s.SeatTubeAngle)), Math.Sin(CockpitMath.Rad(s.SeatTubeAngle)));
        var seatTubeTop = stAxis * stl;

        var tt = s.TopTubeEffective > 0 ? s.TopTubeEffective : s.Reach + 155;

        return new BikeDrawing(
            Bb: new Pt(0, 0),
            RearAxle: rearAxle,
            FrontAxle: frontAxle,
            WheelRadius: (s.WheelDiameter > 0 ? s.WheelDiameter : 682) / 2,
            HeadTubeTop: chain.HeadTubeTop,
            HeadTubeBottom: htb,
            SeatTubeTop: seatTubeTop,
            VirtualTopTubeEnd: new Pt(s.Reach - tt, s.Stack),
            HeadsetTop: chain.HeadsetTop,
            SpacerTop: chain.SpacerTop,
            StemPivot: chain.StemPivot,
            BarCentre: chain.BarCentre,
            BarRadius: cockpit.BarTubeDiameter / 2,
            Saddle: new Pt(target.BrpX, target.BrpZ),
            StemAngleFromHorizontal: chain.StemAngleFromHorizontal);
    }
}

/// <summary>
/// Side view of a drop bar built from its two catalogue numbers. Reach is the horizontal distance
/// from the clamp centre to the furthest forward point of the tops and drop is the vertical
/// distance down to the bar ends, both quoted with the bar rolled to neutral - so neither is
/// rotated with the stem.
/// </summary>
public static class DropBar
{
    public static Pt FrontOfTops(Pt clamp, CockpitSpec c) => clamp + new Pt(c.BarReach, 0);
    public static Pt End(Pt clamp, CockpitSpec c) => clamp + new Pt(c.BarReach * 0.35, -c.BarDrop);

    public static string Path(Pt clamp, CockpitSpec c, Projector p)
    {
        Pt At(double fx, double fy) => clamp + new Pt(c.BarReach * fx, -c.BarDrop * fy);
        string S(Pt q) => System.Globalization.CultureInfo.InvariantCulture is var ci
            ? $"{p.X(q).ToString("0.##", ci)} {p.Y(q).ToString("0.##", ci)}"
            : "";

        return $"M {S(clamp)} C {S(At(0.55, 0))} {S(At(1.00, 0.10))} {S(At(1.00, 0.38))} " +
               $"C {S(At(1.00, 0.72))} {S(At(0.66, 1.00))} {S(At(0.35, 1.00))}";
    }
}

/// <summary>
/// Side-view outline of a one-piece bar/stem, drawn as the single swept body it actually is rather
/// than a stem block bolted to a round bar. It runs from the steerer clamp, forward along a top
/// surface that is flat when <see cref="CockpitSpec.FlatTops"/> is set, out to the nose where the
/// drops begin, then back underneath to the clamp.
/// </summary>
public static class OnePieceCockpit
{
    public static string Path(CockpitMath.Chain chain, CockpitSpec c, Projector p)
    {
        var half = c.BarTubeDiameter / 2;
        var u = chain.SteererAxisUp;
        var fwd = new Pt(u.Y, -u.X);              // perpendicular to the steerer, pointing forward
        var hta = CockpitMath.Deg(Math.Atan2(u.Y, -u.X));

        var clampBottom = chain.SpacerTop;
        var clampTop = clampBottom + u * c.StemClampHeight;
        var bar = chain.BarCentre;
        var topY = bar.Y + half;
        var nose = bar.X + c.BarReach * 0.5;

        string S(Pt q) => string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"{p.X(q):0.##} {p.Y(q):0.##}");
        Pt At(double x, double y) => new(x, y);

        const double halfClamp = 25;
        var backBottom = clampBottom - fwd * halfClamp;
        var backTop = clampTop - fwd * (halfClamp - 6);
        var frontBottom = clampBottom + fwd * halfClamp;

        // The real part is a wedge with four straight edges, not a constant-depth tube: a squarish
        // steerer clamp at the back, a flat top, a thin leading edge, and an underside running
        // forward at the QUOTED stem angle. That is the reconciliation - the quoted -6 deg describes
        // this underside, while the HB point sits on the flat top above it.
        var noseThickness = Math.Min(14, c.BarTubeDiameter * 0.45);
        var underNose = At(nose, topY - noseThickness);
        var body = Pt.FromAngle(90 - hta + c.StemAngle);

        // where that underside runs back into the front face of the clamp
        var v = underNose - frontBottom;
        var det = body.X * u.Y - body.Y * u.X;
        var s = Math.Abs(det) < 1e-9
            ? c.StemClampHeight * 0.4
            : (body.X * v.Y - body.Y * v.X) / det;
        var junction = frontBottom + u * Math.Clamp(s, 4, c.StemClampHeight - 4);

        return
            // up the back of the steerer clamp
            $"M {S(backBottom)} L {S(backTop)} " +
            // short shoulder fillet, then the flat top out to the nose
            $"Q {S(At(backTop.X + 10, topY))} {S(At(bar.X - 30, topY))} " +
            $"L {S(At(nose, topY))} " +
            // nose rolls over onto a thin leading edge
            $"Q {S(At(nose + 8, topY - noseThickness * 0.45))} {S(underNose)} " +
            // underside straight back at the body angle, into the clamp, then down its front face
            $"L {S(junction)} L {S(frontBottom)} Z";
    }
}

/// <summary>Maps fit coordinates (mm, Y up) to SVG pixels (Y down).</summary>
public sealed class Projector
{
    private readonly double _minX, _maxY, _scale, _offsetX, _offsetY;

    public Projector(IEnumerable<Pt> points, double pixelWidth, double pixelHeight, double marginMm, double marginPx = 12)
    {
        var pts = points.ToList();
        var minX = pts.Min(p => p.X) - marginMm;
        var maxX = pts.Max(p => p.X) + marginMm;
        var minY = pts.Min(p => p.Y) - marginMm;
        var maxY = pts.Max(p => p.Y) + marginMm;

        _scale = Math.Min((pixelWidth - 2 * marginPx) / (maxX - minX), (pixelHeight - 2 * marginPx) / (maxY - minY));
        _minX = minX;
        _maxY = maxY;
        _offsetX = marginPx + (pixelWidth - 2 * marginPx - (maxX - minX) * _scale) / 2;
        _offsetY = marginPx + (pixelHeight - 2 * marginPx - (maxY - minY) * _scale) / 2;
        Scale = _scale;
    }

    /// <summary>Pixels per millimetre.</summary>
    public double Scale { get; }

    public double X(double mm) => _offsetX + (mm - _minX) * _scale;
    public double Y(double mm) => _offsetY + (_maxY - mm) * _scale;
    public double X(Pt p) => X(p.X);
    public double Y(Pt p) => Y(p.Y);
    public double L(double mm) => mm * _scale;
}
