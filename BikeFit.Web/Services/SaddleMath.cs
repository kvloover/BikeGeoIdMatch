using BikeFit.Web.Models;

namespace BikeFit.Web.Services;

/// <summary>
/// The chain from the bottom bracket to the BRP:
///
///   BB -> up the seat tube axis -> the post -> the rail cradle (offset perpendicular
///   by the post setback) -> along the rails to the BRP.
///
/// The BRP itself is fixed by the fit sheet. What this works out is where the rail cradle lands,
/// and therefore how far the saddle has to sit forward or back on its rails to reach the BRP -
/// which is the number that decides whether a frame, post and saddle combination is possible.
///
/// Note the axis reference matters: comparing the BRP with the seat tube axis at BRP height
/// overstates the requirement, because the cradle sits lower, where the rearward-leaning axis has
/// already moved forward. This works at cradle height.
/// </summary>
public static class SaddleMath
{
    public sealed record Chain(
        Pt Bb,
        Pt Brp,
        Pt SeatTubeTop,
        /// <summary>Point on the post axis level with the cradle offset.</summary>
        Pt PostAxisAtCradle,
        Pt Cradle,
        /// <summary>Where the seat tube axis passes at BRP height.</summary>
        Pt AxisAtBrpHeight,
        /// <summary>Where the seat tube axis passes at cradle height.</summary>
        Pt AxisAtCradleHeight,
        /// <summary>How far forward of the cradle the BRP must sit. Negative = behind.</summary>
        double ForwardOfCradle,
        /// <summary>How far forward of the seat tube axis the BRP sits, measured at BRP height.</summary>
        double ForwardOfAxisAtBrp,
        bool WithinRailRange,
        double SaddleHeight,
        double Setback,
        double VirtualSeatAngle,
        /// <summary>Least setback this frame and post can deliver at this saddle height, with the
        /// saddle slid fully forward on its rails.</summary>
        double MinSetback,
        /// <summary>Most setback the same combination can deliver, saddle slid fully back.</summary>
        double MaxSetback);

    public static Chain Build(SizeGeometry size, FitTarget target, SaddleSpec saddle)
    {
        var s = CockpitMath.Rad(size.SeatTubeAngle);
        var (cos, sin) = (Math.Cos(s), Math.Sin(s));

        var brp = new Pt(target.BrpX, target.BrpZ);
        var axisUp = new Pt(-cos, sin);
        var back = new Pt(-sin, -cos);   // perpendicular to the post, pointing back and down

        // the cradle sits RailDrop below the BRP; solve for the point along the post axis that puts
        // it there once the perpendicular setback is applied
        var cradleY = brp.Y - saddle.RailDrop;
        var t = (cradleY + saddle.PostOffset * cos) / sin;
        var postAxisAtCradle = axisUp * t;
        var cradle = postAxisAtCradle + back * saddle.PostOffset;

        var forwardOfCradle = brp.X - cradle.X;
        var axisAtBrp = new Pt(-brp.Y / Math.Tan(s), brp.Y);
        var axisAtCradle = new Pt(-cradleY / Math.Tan(s), cradleY);

        var stl = size.SeatTubeLength > 0 ? size.SeatTubeLength : 500;

        return new Chain(
            Bb: new Pt(0, 0),
            Brp: brp,
            SeatTubeTop: axisUp * stl,
            PostAxisAtCradle: postAxisAtCradle,
            Cradle: cradle,
            AxisAtBrpHeight: axisAtBrp,
            AxisAtCradleHeight: axisAtCradle,
            ForwardOfCradle: forwardOfCradle,
            ForwardOfAxisAtBrp: brp.X - axisAtBrp.X,
            WithinRailRange: Math.Abs(forwardOfCradle) <= saddle.RailRange,
            SaddleHeight: brp.Length,
            Setback: -brp.X,
            VirtualSeatAngle: CockpitMath.Deg(Math.Atan2(brp.Y, -brp.X)),
            // sliding forward on the rails reduces setback, sliding back increases it
            MinSetback: -(cradle.X + saddle.RailRange),
            MaxSetback: -(cradle.X - saddle.RailRange));
    }
}
