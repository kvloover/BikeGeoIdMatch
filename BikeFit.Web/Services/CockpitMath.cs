using BikeFit.Web.Models;

namespace BikeFit.Web.Services;

/// <summary>
/// The geometry chain from the bottom bracket to the handlebar.
///
///   HTT   = (Reach, Stack)                         top of the head tube
///   +  headset stack        along the steerer axis
///   +  spacer stack         along the steerer axis
///   +  stem axis offset     along the steerer axis  -> P, the stem pivot
///   +  stem length          along the stem axis     -> bar clamp centre
///   +  bar centre offset    perpendicular to the stem axis
///
/// The steerer axis unit vector (pointing up) is (-cos HTA, sin HTA): going up the steerer
/// moves you backwards, because the head tube leans back.
///
/// The stem angle is quoted the way the industry quotes it: from the perpendicular to the
/// steerer. So on a 73 deg head angle a -17 deg stem is horizontal, and a +6 deg stem rises
/// 23 deg. Installed angle from horizontal = (90 - HTA) + stemAngle.
/// </summary>
public static class CockpitMath
{
    public static Pt SteererUp(double headTubeAngle) =>
        new(-Math.Cos(Rad(headTubeAngle)), Math.Sin(Rad(headTubeAngle)));

    public static double StemAngleFromHorizontal(double headTubeAngle, double installedStemAngle) =>
        90 - headTubeAngle + installedStemAngle;

    public static Pt StemDirection(double headTubeAngle, double installedStemAngle) =>
        Pt.FromAngle(StemAngleFromHorizontal(headTubeAngle, installedStemAngle));

    /// <summary>Perpendicular to the stem axis, pointing "up" off the top of the stem.</summary>
    public static Pt StemUp(double headTubeAngle, double installedStemAngle) =>
        Pt.FromAngle(StemAngleFromHorizontal(headTubeAngle, installedStemAngle) + 90);

    public static Pt HeadTubeTop(SizeGeometry s) => new(s.Reach, s.Stack);

    /// <summary>Every interesting point of the front end, for a concrete build.</summary>
    public sealed record Chain(
        Pt HeadTubeTop,
        Pt HeadsetTop,
        Pt SpacerTop,
        Pt StemPivot,
        Pt BarClampCentre,
        Pt BarCentre,
        Pt SteererAxisUp,
        Pt StemAxis,
        double StemAngleFromHorizontal);

    public static Chain Build(SizeGeometry size, CockpitSpec cockpit, double spacerStack,
                              double stemLength, double installedStemAngle)
    {
        var u = SteererUp(size.HeadTubeAngle);
        var d = StemDirection(size.HeadTubeAngle, installedStemAngle);
        var up = StemUp(size.HeadTubeAngle, installedStemAngle);

        var htt = HeadTubeTop(size);
        var headsetTop = htt + u * size.EffectiveHeadsetStack;
        var spacerTop = headsetTop + u * spacerStack;
        var pivot = spacerTop + u * cockpit.EffectiveStemAxisOffset;
        var clamp = pivot + d * stemLength;
        var barCentre = clamp + up * cockpit.BarCentreOffset;

        return new Chain(htt, headsetTop, spacerTop, pivot, clamp, barCentre, u, d,
                         StemAngleFromHorizontal(size.HeadTubeAngle, installedStemAngle));
    }

    /// <summary>
    /// Offset from the bar clamp centre to the point the HB coordinate refers to. Bar reach is a
    /// horizontal measurement at neutral bar roll, so it is not rotated with the stem.
    /// </summary>
    public static Pt HbOffset(FitTarget target, CockpitSpec cockpit) => target.HbReference switch
    {
        HbReference.BarTop => new Pt(0, cockpit.BarTubeDiameter / 2.0),
        HbReference.BarFront => new Pt(cockpit.BarReach, 0),
        _ => new Pt(0, 0)
    };

    /// <summary>Converts the HB target into the bar-tube centre it implies.</summary>
    public static Pt TargetBarCentre(FitTarget target, CockpitSpec cockpit) =>
        new Pt(target.HbX, target.HbZ) - HbOffset(target, cockpit);

    /// <summary>Reverse of the above: from an achieved bar centre back to the HB reference point.</summary>
    public static Pt HbFromBarCentre(Pt barCentre, FitTarget target, CockpitSpec cockpit) =>
        barCentre + HbOffset(target, cockpit);

    public static double Rad(double deg) => deg * Math.PI / 180;
    public static double Deg(double rad) => rad * 180 / Math.PI;
}
