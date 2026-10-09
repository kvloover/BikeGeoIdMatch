namespace BikeFit.Web.Models;

/// <summary>
/// The parts between the seat tube and the BRP. None of this moves the BRP - the fit sheet already
/// fixes where the BRP has to be - it decides whether a given frame, post and saddle can actually
/// put it there.
/// </summary>
public sealed class SaddleSpec
{
    /// <summary>
    /// Vertical drop from the BRP down to the rail CENTRELINE, measured at the 70 mm-width point
    /// rather than at the saddle's midpoint or its highest point.
    ///
    /// Fitters call the related figure "saddle stack" and measure it from the BOTTOM of the rail to
    /// the top of the saddle. Rails are 7 mm, so drop = stack - 3.5. Manufacturers do not publish
    /// either, but reported stacks cluster tightly for road saddles - roughly 38-48 mm, i.e. a
    /// 35-45 mm centreline drop, with about 10 mm of spread between models:
    ///
    ///     Selle Italia SLR   38-46 mm stack      S-Works Power   ~40 mm stack (~36.5 mm drop)
    ///     WTB Volt           43 mm               BG Phenom Comp  48 mm
    ///
    /// 40 mm is a fair default for a road saddle. It also matters less than it looks: the
    /// requirement moves by 1/tan(seat angle) per millimetre, about 0.3 mm/mm, so the entire
    /// 35-45 mm spread shifts the answer by only ~3 mm.
    /// </summary>
    public double RailDrop { get; set; } = 40;

    /// <summary>
    /// Seatpost setback: the offset from the post's tube axis to the centre of the rail cradle,
    /// measured perpendicular to the post, which is how the part is made and quoted. Zero for an
    /// inline post.
    /// </summary>
    public double PostOffset { get; set; }

    /// <summary>
    /// How far the saddle can be slid each way from the neutral clamp position before hitting the
    /// limit marks. Unlike the rail drop this one genuinely matters, and 25 mm is optimistic for a
    /// short-nose saddle - those carry shorter rails than a traditional 270 mm shell. Check the
    /// marks on the actual rails.
    /// </summary>
    public double RailRange { get; set; } = 25;

    /// <summary>Overall saddle length. Drawing only.</summary>
    public double Length { get; set; } = 240;

    /// <summary>
    /// Distance from the nose back to the BRP. Drawing only - it positions the saddle outline
    /// around the BRP and changes no calculation.
    /// </summary>
    public double BrpFromNose { get; set; } = 95;

    public SaddleSpec Clone() => (SaddleSpec)MemberwiseClone();
}
