namespace BikeFit.Web.Services;

/// <summary>What an upload did: entries taken in, and entries passed over because they were unusable.</summary>
public readonly record struct ImportSummary(int Imported, int Skipped)
{
    public override string ToString() =>
        Skipped == 0 ? $"Imported {Imported}." : $"Imported {Imported}, skipped {Skipped} that were incomplete or clash with a built-in bike.";
}
