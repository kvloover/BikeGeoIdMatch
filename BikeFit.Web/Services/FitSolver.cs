using BikeFit.Web.Models;

namespace BikeFit.Web.Services;

public sealed class FitSolver
{
    /// <summary>
    /// For one size and one stem angle there is exactly one (spacer stack, stem length) pair that
    /// lands the bar dead on target - two unknowns, two equations. Solving that first gives the
    /// "ideal" numbers; the catalog search then snaps them to real parts.
    /// </summary>
    public static (double SpacerStack, double StemLength) SolveExact(
        SizeGeometry size, CockpitSpec cockpit, Pt targetBarCentre, double installedStemAngle)
    {
        var u = CockpitMath.SteererUp(size.HeadTubeAngle);
        var d = CockpitMath.StemDirection(size.HeadTubeAngle, installedStemAngle);
        var perp = CockpitMath.StemUp(size.HeadTubeAngle, installedStemAngle);

        // base point with zero spacers, corrected for the bar sitting off the stem axis
        var p0 = CockpitMath.HeadTubeTop(size)
                 + u * (size.EffectiveHeadsetStack + cockpit.EffectiveStemAxisOffset)
                 + perp * cockpit.BarCentreOffset;

        var v = targetBarCentre - p0;

        var det = u.X * d.Y - u.Y * d.X;
        if (Math.Abs(det) < 1e-9) return (double.NaN, double.NaN); // stem parallel to the steerer

        var spacer = (v.X * d.Y - v.Y * d.X) / det;
        var length = (u.X * v.Y - u.Y * v.X) / det;
        return (spacer, length);
    }

    /// <summary>Best stem length for a fixed spacer stack: project the remaining vector on the stem axis.</summary>
    private static double ProjectLength(SizeGeometry size, CockpitSpec cockpit, Pt targetBarCentre,
                                        double installedStemAngle, double spacerStack)
    {
        var chain = CockpitMath.Build(size, cockpit, spacerStack, 0, installedStemAngle);
        var v = targetBarCentre - chain.BarCentre;
        return v.X * chain.StemAxis.X + v.Y * chain.StemAxis.Y;
    }

    /// <summary>
    /// One bike paired with the cockpit it is being considered with. Each entry carries its own
    /// cockpit so stock builds can be compared against each other in a single table - an SL8 with
    /// its Roval Rapide against a Madone with its Bontrager, rather than all of them forced through
    /// one hypothetical cockpit.
    /// </summary>
    public sealed record Candidate(BikeModel Bike, CockpitSpec Cockpit, string CockpitName);

    public IReadOnlyList<FitSolution> Solve(IEnumerable<Candidate> candidates, FitTarget target)
    {
        var results = new List<FitSolution>();

        foreach (var (bike, cockpit, cockpitName) in candidates)
        {
            var targetCentre = CockpitMath.TargetBarCentre(target, cockpit);

            foreach (var size in bike.Sizes)
            {
                // built per size, because a level-tops cockpit takes its angle from the head tube
                var angles = BuildAngleSet(cockpit, size);

                if (size.Stack <= 0 || size.Reach <= 0) continue;

                foreach (var (installedAngle, rawAngle, flipped) in angles)
                {
                    var (idealSpacer, idealLength) = SolveExact(size, cockpit, targetCentre, installedAngle);
                    if (double.IsNaN(idealSpacer)) continue;

                    foreach (var candidate in CandidateBuilds(size, cockpit, targetCentre, installedAngle, idealSpacer, idealLength))
                    {
                        var chain = CockpitMath.Build(size, cockpit, candidate.Spacer, candidate.Length, installedAngle);
                        var hb = CockpitMath.HbFromBarCentre(chain.BarCentre, target, cockpit);
                        var err = Math.Sqrt(Math.Pow(hb.X - target.HbX, 2) + Math.Pow(hb.Y - target.HbZ, 2));

                        var spacerOk = candidate.Spacer >= -1e-6 && candidate.Spacer <= cockpit.MaxSpacerStack + 1e-6;
                        var lengthOk = cockpit.ContinuousLength
                            ? candidate.Length >= 40 && candidate.Length <= 200
                            : cockpit.StemLengths.Count == 0 || cockpit.StemLengths.Contains(candidate.Length);

                        var warnings = new List<string>();
                        if (idealSpacer < -1e-6)
                            warnings.Add($"Bar needs to be {Math.Abs(idealSpacer):0} mm below the top of the head tube - impossible without a lower stack frame, a negative-rise cockpit or cutting the head tube.");
                        if (idealSpacer > cockpit.MaxSpacerStack + 1e-6)
                            warnings.Add($"Needs {idealSpacer:0} mm of spacers, more than the {cockpit.MaxSpacerStack:0} mm limit.");
                        if (candidate.Length < 60) warnings.Add("Stem shorter than 60 mm - handling will be twitchy.");
                        if (candidate.Length > 140) warnings.Add("Stem longer than 140 mm - consider a bigger frame.");
                        if (size.HeadsetStack is null)
                            warnings.Add($"No headset stack recorded for this frame, so {SizeGeometry.AssumedHeadsetStack:0} mm was assumed. It shifts the spacer stack millimetre for millimetre - measure it.");

                        results.Add(new FitSolution
                        {
                            Bike = bike,
                            Size = size,
                            Cockpit = cockpit,
                            CockpitName = cockpitName,
                            StemAngle = installedAngle,
                            Flipped = flipped,
                            StemAngleFromHorizontal = chain.StemAngleFromHorizontal,
                            IdealSpacerStack = idealSpacer,
                            IdealStemLength = idealLength,
                            SpacerStack = candidate.Spacer,
                            SpacerCount = (int)Math.Round(candidate.Spacer / Math.Max(1, cockpit.SpacerStep)),
                            StemLength = candidate.Length,
                            BarCentre = chain.BarCentre,
                            HbActual = hb,
                            HbTarget = new Pt(target.HbX, target.HbZ),
                            SpacerStackFeasible = spacerOk,
                            StemLengthFeasible = lengthOk,
                            WithinTolerance = err <= target.ToleranceMm,
                            Warnings = warnings,
                            Score = Score(err, target.ToleranceMm, candidate, spacerOk, lengthOk,
                                      chain.StemAngleFromHorizontal)
                        });
                    }
                }
            }
        }

        // keep the best build per (bike, cockpit, size, angle) so the table is not flooded
        return results
            .GroupBy(r => (r.Bike.Id, r.CockpitName, r.Size.Name, r.StemAngle))
            .Select(g => g.OrderBy(r => r.Score).First())
            .OrderBy(r => r.Score)
            .ToList();
    }

    private static IEnumerable<(double Installed, double Raw, bool Flipped)> BuildAngleSet(CockpitSpec cockpit, SizeGeometry size)
    {
        // Level tops: the installed angle is whatever puts the line to the HB point horizontal, so
        // it follows the frame rather than the part. Nothing else to search.
        if (cockpit.FlatTops)
        {
            var flat = size.HeadTubeAngle - 90;
            yield return (flat, Math.Abs(flat), flat < 0);
            yield break;
        }

        if (cockpit.FixedAngle)
        {
            yield return (cockpit.InstalledStemAngle, Math.Abs(cockpit.StemAngle), cockpit.Flipped);
            yield break;
        }

        // A signed entry in the list means exactly that orientation - write "-16" and only a 16 deg
        // stem mounted angle-down is considered. An unsigned entry tries both ways up.
        var seen = new HashSet<double>();
        foreach (var a in cockpit.StemAngles.DefaultIfEmpty(cockpit.StemAngle))
        {
            var mag = Math.Abs(a);
            if (a < 0)
            {
                if (seen.Add(-mag)) yield return (-mag, mag, true);
                continue;
            }
            if (seen.Add(mag)) yield return (mag, mag, false);
            if (mag > 0.01 && seen.Add(-mag)) yield return (-mag, mag, true);
        }
    }

    private readonly record struct Build(double Spacer, double Length);

    private static IEnumerable<Build> CandidateBuilds(SizeGeometry size, CockpitSpec cockpit, Pt targetCentre,
                                                      double installedAngle, double idealSpacer, double idealLength)
    {
        var step = Math.Max(0.5, cockpit.SpacerStep);
        var maxSteps = (int)Math.Round(cockpit.MaxSpacerStack / step);

        if (cockpit.ContinuousLength)
        {
            // an integrated cockpit can be made any length, so only the spacer stack is quantised
            var near = new[] { Math.Floor(idealSpacer / step), Math.Ceiling(idealSpacer / step) }
                .Select(n => Math.Clamp(n, 0, maxSteps))
                .Distinct();
            foreach (var n in near)
            {
                var spacer = n * step;
                yield return new Build(spacer, ProjectLength(size, cockpit, targetCentre, installedAngle, spacer));
            }
            yield break;
        }

        var lengths = cockpit.StemLengths.Count > 0 ? cockpit.StemLengths : [idealLength];
        for (var n = 0; n <= maxSteps; n++)
        {
            var spacer = n * step;
            foreach (var len in lengths)
                yield return new Build(spacer, len);
        }
    }

    /// <summary>
    /// A typical road build runs somewhere around this much spacer: enough that the bar can still
    /// come down, little enough that the frame is not obviously too small.
    /// </summary>
    private const double NeutralSpacerStack = 15;

    /// <summary>
    /// Inside the tolerance every build is effectively on target, so practicality decides: a stem
    /// near horizontal, a normal length, and a spacer stack near the neutral one. Outside the
    /// tolerance the miss dominates again.
    ///
    /// The spacer term is a deviation from neutral rather than a ceiling, because a big stack is
    /// evidence the frame is too small - and frame size matters far more than the millimetre or two
    /// of bar position that separates two builds already inside the tolerance. Without it a frame
    /// one size down wins on a fraction of a millimetre while running 30 mm of spacers.
    /// </summary>
    private static double Score(double error, double tolerance, Build build, bool spacerOk, bool lengthOk,
                                double angleFromHorizontal)
    {
        var score = error <= tolerance ? error * 0.15 : error;
        if (!spacerOk) score += 500;
        if (!lengthOk) score += 500;
        score += Math.Max(0, Math.Abs(angleFromHorizontal) - 12) * 0.25;
        score += Math.Max(0, Math.Abs(build.Length - 105) - 15) * 0.05;
        score += Math.Abs(build.Spacer - NeutralSpacerStack) * 0.03;
        return score;
    }

    public static RiderMetrics Metrics(FitTarget t)
    {
        var dx = t.HbX - t.BrpX;
        var dz = t.HbZ - t.BrpZ;
        return new RiderMetrics(
            SaddleHeight: Math.Sqrt(t.BrpX * t.BrpX + t.BrpZ * t.BrpZ),
            SaddleSetback: -t.BrpX,
            SaddleToBarDirect: Math.Sqrt(dx * dx + dz * dz),
            SaddleToBarHorizontal: dx,
            SaddleToBarDrop: -dz);
    }

    /// <summary>Horizontal distance the saddle rails sit behind the seat tube axis at saddle height.</summary>
    public static double RequiredSeatpostSetback(SizeGeometry size, FitTarget t)
    {
        var axisX = -t.BrpZ / Math.Tan(CockpitMath.Rad(size.SeatTubeAngle));
        return axisX - t.BrpX;
    }
}
