using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using BikeFit.Web.Models;

namespace BikeFit.Web.Services;

public sealed record ImportResult(BikeModel? Bike, List<string> Messages)
{
    public bool Success => Bike is { Sizes.Count: > 0 };
}

/// <summary>
/// Turns a geometry table - pasted from geometrygeeks.bike, a dealer page or a PDF - into a
/// <see cref="BikeModel"/>. Handles both layouts (one column per size, or one row per size),
/// tab / comma / multi-space separated text, and cm-or-mm values.
/// </summary>
public sealed partial class GeometryImporter(IHttpClientFactory httpFactory)
{
    private static readonly (string Field, string[] Synonyms)[] Fields =
    [
        ("Stack",            ["stack"]),
        ("Reach",            ["reach"]),
        // "Head Tube" on its own means the length (that is how geometrygeeks labels it); the angle
        // always says "angle" or is abbreviated.
        ("HeadTubeAngle",    ["head tube angle", "head angle", "headtube angle", "hta"]),
        ("SeatTubeAngle",    ["seat tube angle", "seat angle", "effective seat tube angle", "sta"]),
        ("TopTubeEffective", ["effective top tube", "top tube effective", "horizontal top tube", "top tube (effective)", "virtual top tube", "top tube length", "top tube"]),
        ("HeadTubeLength",   ["head tube length", "headtube length", "head tube (length)", "head tube"]),
        ("SeatTubeLength",   ["seat tube length", "seat tube (c-t)", "seat tube c-t", "seat tube"]),
        ("ChainstayLength",  ["chainstay length", "chain stay", "chainstay", "rear centre", "rear center"]),
        ("Wheelbase",        ["wheelbase", "wheel base"]),
        ("BbDrop",           ["bb drop", "bottom bracket drop"]),
        ("ForkRake",         ["fork rake", "fork offset", "rake", "offset"]),
        ("Trail",            ["trail"]),
        ("StandoverHeight",  ["standover", "stand over"]),
    ];

    // Note: "head tube" alone means the angle, "head tube length" means the length. MatchField
    // resolves that by preferring the longest matching synonym.

    public async Task<ImportResult> FetchAsync(string url, CancellationToken ct = default)
    {
        var messages = new List<string>();
        string html;
        try
        {
            var client = httpFactory.CreateClient("geo");
            html = await client.GetStringAsync(url, ct);
        }
        catch (Exception ex)
        {
            return new ImportResult(null, [$"Could not fetch {url}: {ex.Message}. Copy the geometry table from the page and paste it instead."]);
        }

        // geometrygeeks (and sites built the same way) render the geometry client side and serve
        // randomised placeholder numbers to plain HTTP clients. Importing those silently would be
        // worse than not importing at all.
        if (PlaceholderTable().IsMatch(html))
            return new ImportResult(null,
            [
                "That page builds its geometry table with JavaScript and serves randomised placeholder numbers to plain HTTP requests, so nothing fetched from it can be trusted.",
                "Open the page in your browser, select the geometry table, copy it, and paste it into the box below - the parser handles it."
            ]);

        var tables = ExtractTables(html);
        if (tables.Count == 0)
            return new ImportResult(null, ["No HTML table found on that page. Paste the geometry table instead."]);

        ImportResult? best = null;
        foreach (var table in tables)
        {
            var attempt = ParseTable(table, GuessBrand(url), GuessModel(html, url), url);
            if (attempt.Bike is null) continue;
            var score = attempt.Bike.Sizes.Count(s => s.Stack > 0 && s.Reach > 0);
            if (best?.Bike is null || score > best.Bike.Sizes.Count(s => s.Stack > 0 && s.Reach > 0))
                best = attempt;
        }

        if (best?.Bike is null)
            return new ImportResult(null, ["Found tables but no recognisable stack/reach rows. Paste the table manually."]);

        best.Messages.Insert(0, $"Imported from {url}. Always sanity-check the numbers against the manufacturer.");
        return best;
    }

    public ImportResult ParseTable(string text, string brand, string model, string? sourceUrl = null)
    {
        var messages = new List<string>();
        var rows = text.Replace("\r", "").Split('\n')
            .Select(l => SplitCells(l).ToList())
            .Where(cells => cells.Count > 1)
            .ToList();

        if (rows.Count < 2)
            return new ImportResult(null, ["Need at least a header row and one data row."]);

        var byRow = ParseLabelsInFirstColumn(rows);
        var byCol = ParseLabelsInHeaderRow(rows);

        var chosen = Filled(byRow) >= Filled(byCol) ? byRow : byCol;
        if (chosen.Count == 0)
            return new ImportResult(null, ["Could not find stack / reach / head tube angle in that table."]);

        var bike = new BikeModel
        {
            Brand = brand,
            Model = model,
            SourceUrl = sourceUrl,
            Source = sourceUrl is null ? "pasted" : "imported",
            Sizes = chosen
        };

        foreach (var s in bike.Sizes.Where(s => s.Stack <= 0 || s.Reach <= 0).ToList())
            messages.Add($"Size {s.Name}: missing stack or reach, it will be skipped by the solver.");

        NormaliseUnits(bike, messages);
        SanityCheck(bike, messages);
        return new ImportResult(bike, messages);
    }

    /// <summary>
    /// Cheap plausibility checks. They catch mis-aligned columns, mixed-up rows and scrambled
    /// values far more reliably than trusting the parse.
    /// </summary>
    private static void SanityCheck(BikeModel bike, List<string> messages)
    {
        if (bike.Sizes.Any(s => s.HeadTubeAngle <= 0))
        {
            messages.Add("No head tube angle row was recognised - 73 deg has been filled in. The angle drives the whole spacer calculation, so correct it before trusting the result.");
            foreach (var s in bike.Sizes.Where(s => s.HeadTubeAngle <= 0)) s.HeadTubeAngle = 73;
        }
        if (bike.Sizes.Any(s => s.SeatTubeAngle <= 0))
        {
            messages.Add("No seat tube angle row was recognised - 73.5 deg has been filled in.");
            foreach (var s in bike.Sizes.Where(s => s.SeatTubeAngle <= 0)) s.SeatTubeAngle = 73.5;
        }

        var usable = bike.Sizes.Where(s => s.Stack > 0 && s.Reach > 0).ToList();
        if (usable.Count < 2) return;

        if (!IsMonotonic(usable.Select(s => s.Stack)))
            messages.Add("Stack does not increase steadily across the sizes - check that the columns line up with the size labels.");
        if (!IsMonotonic(usable.Select(s => s.Reach)))
            messages.Add("Reach does not increase steadily across the sizes - check that the columns line up with the size labels.");

        foreach (var s in usable)
        {
            if (s.Stack is < 350 or > 750) messages.Add($"Size {s.Name}: stack of {s.Stack:0} mm looks wrong.");
            if (s.Reach is < 300 or > 480) messages.Add($"Size {s.Name}: reach of {s.Reach:0} mm looks wrong.");
            if (s.HeadTubeAngle is < 62 or > 80) messages.Add($"Size {s.Name}: head tube angle of {s.HeadTubeAngle:0.#} deg looks wrong.");
        }
    }

    private static bool IsMonotonic(IEnumerable<double> values)
    {
        var v = values.ToList();
        var up = v.Zip(v.Skip(1), (a, b) => b >= a - 1).All(x => x);
        var down = v.Zip(v.Skip(1), (a, b) => b <= a + 1).All(x => x);
        return up || down;
    }

    private static int Filled(List<SizeGeometry> sizes) => sizes.Count(s => s.Stack > 0 && s.Reach > 0);

    /// <summary>geometrygeeks / most manufacturer layout: measurement names down the left, sizes across the top.</summary>
    private static List<SizeGeometry> ParseLabelsInFirstColumn(List<List<string>> rows)
    {
        // the header row is the first row whose first cell is not a known measurement
        var headerIndex = rows.FindIndex(r => MatchField(r[0]) is null);
        if (headerIndex < 0) return [];

        var header = rows[headerIndex];

        // if the header lost its blank top-left corner it will be exactly one cell short of the
        // data rows, which would shift every size label by one column
        var dataRows = rows.Skip(headerIndex + 1).Where(r => MatchField(r[0]) is not null).ToList();
        if (dataRows.Count > 0)
        {
            var modal = dataRows.GroupBy(r => r.Count).OrderByDescending(g => g.Count()).First().Key;
            if (header.Count == modal - 1) header = ["", .. header];
        }

        var sizeNames = header.Skip(1).Select(Clean).ToList();
        if (sizeNames.Count == 0) return [];

        // angles start at zero so a missing row can be told apart from a real value
        var sizes = sizeNames
            .Select((n, i) => new SizeGeometry
            {
                Name = string.IsNullOrWhiteSpace(n) ? $"Size {i + 1}" : n,
                HeadTubeAngle = 0,
                SeatTubeAngle = 0
            }).ToList();
        var hits = 0;

        foreach (var row in rows.Skip(headerIndex + 1))
        {
            var field = MatchField(row[0]);
            if (field is null) continue;
            hits++;
            for (var i = 0; i < sizes.Count && i + 1 < row.Count; i++)
                if (TryNumber(row[i + 1], out var v))
                    Assign(sizes[i], field, v);
        }

        if (hits == 0) return [];
        return sizes;
    }

    /// <summary>Transposed layout: one row per size, measurement names across the top.</summary>
    private static List<SizeGeometry> ParseLabelsInHeaderRow(List<List<string>> rows)
    {
        var header = rows[0];
        var map = header.Select(MatchField).ToList();
        if (map.Count(m => m is not null) < 2) return [];

        var sizes = new List<SizeGeometry>();
        foreach (var row in rows.Skip(1))
        {
            var size = new SizeGeometry { Name = Clean(row[0]), HeadTubeAngle = 0, SeatTubeAngle = 0 };
            for (var i = 0; i < row.Count && i < map.Count; i++)
                if (map[i] is { } field && TryNumber(row[i], out var v))
                    Assign(size, field, v);
            if (!string.IsNullOrWhiteSpace(size.Name)) sizes.Add(size);
        }
        return sizes;
    }

    private static void Assign(SizeGeometry s, string field, double v)
    {
        switch (field)
        {
            case "Stack": s.Stack = v; break;
            case "Reach": s.Reach = v; break;
            case "HeadTubeAngle": s.HeadTubeAngle = v; break;
            case "SeatTubeAngle": s.SeatTubeAngle = v; break;
            case "TopTubeEffective": s.TopTubeEffective = v; break;
            case "HeadTubeLength": s.HeadTubeLength = v; break;
            case "SeatTubeLength": s.SeatTubeLength = v; break;
            case "ChainstayLength": s.ChainstayLength = v; break;
            case "Wheelbase": s.Wheelbase = v; break;
            case "BbDrop": s.BbDrop = v; break;
            case "ForkRake": s.ForkRake = v; break;
            case "Trail": s.Trail = v; break;
            case "StandoverHeight": s.StandoverHeight = v; break;
        }
    }

    /// <summary>Tables come in mm, cm or inches. Angles are always degrees, so they are left alone.</summary>
    private static void NormaliseUnits(BikeModel bike, List<string> messages)
    {
        var lengthSamples = bike.Sizes.Where(s => s.Stack > 0).Select(s => s.Stack).ToList();
        if (lengthSamples.Count == 0) return;
        var median = lengthSamples.OrderBy(v => v).ElementAt(lengthSamples.Count / 2);

        double factor;
        if (median is > 100 and < 900) return;               // already mm
        else if (median is > 10 and <= 100) factor = 10;     // cm
        else if (median is > 4 and <= 40) factor = 25.4;     // inches
        else return;

        foreach (var s in bike.Sizes)
        {
            s.Stack *= factor; s.Reach *= factor; s.TopTubeEffective *= factor;
            s.HeadTubeLength *= factor; s.SeatTubeLength *= factor; s.ChainstayLength *= factor;
            s.Wheelbase *= factor; s.BbDrop *= factor; s.ForkRake *= factor;
            s.Trail *= factor; s.StandoverHeight *= factor;
        }
        messages.Add($"Values looked like {(Math.Abs(factor - 10) < 0.1 ? "centimetres" : "inches")} and were converted to millimetres.");
    }

    private static string? MatchField(string cell)
    {
        var c = Clean(cell).ToLowerInvariant().Trim(':', '*', '#').Trim();
        if (c.Length == 0) return null;
        c = UnitSuffix().Replace(c, "").Trim();

        string? best = null;
        var bestLen = 0;
        foreach (var (field, synonyms) in Fields)
            foreach (var syn in synonyms)
                if ((c == syn || c.StartsWith(syn + " ") || c.Contains(syn)) && syn.Length > bestLen)
                {
                    best = field;
                    bestLen = syn.Length;
                }
        return best;
    }

    private static bool TryNumber(string cell, out double value)
    {
        value = 0;
        var m = NumberPattern().Match(Clean(cell).Replace(',', '.'));
        if (!m.Success) return false;
        return double.TryParse(m.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Splits a pasted row into cells. A row that starts with a separator has an empty leading cell -
    /// geometry tables nearly always have a blank top-left corner, and losing it shifts every size
    /// label one column to the left.
    /// </summary>
    private static IEnumerable<string> SplitCells(string line)
    {
        var cells = CellSeparator().Split(line.Trim()).Select(Clean).ToList();
        if (line.Length > 0 && (line[0] is '\t' or ';' or '|' or ',' || (line[0] == ' ' && line.Length > 1 && line[1] == ' ')))
            cells.Insert(0, "");
        return cells;
    }

    private static string Clean(string s) => WebUtility.HtmlDecode(s).Replace(' ', ' ').Trim();

    private static List<string> ExtractTables(string html)
    {
        var result = new List<string>();
        foreach (Match table in TablePattern().Matches(html))
        {
            var sb = new StringBuilder();
            foreach (Match row in RowPattern().Matches(table.Value))
            {
                var cells = CellPattern().Matches(row.Value)
                    .Select(c => Clean(TagPattern().Replace(c.Groups[1].Value, " ")).Replace('\t', ' '))
                    .Select(c => WhitespacePattern().Replace(c, " ").Trim());
                sb.AppendLine(string.Join('\t', cells));
            }
            if (sb.Length > 0) result.Add(sb.ToString());
        }
        return result;
    }

    private static string GuessBrand(string url)
    {
        var m = Regex.Match(url, @"geometrygeeks\.bike/bike/([^/]+)", RegexOptions.IgnoreCase);
        if (m.Success) return m.Groups[1].Value.Replace('-', ' ');
        try { return new Uri(url).Host.Replace("www.", "").Split('.')[0]; }
        catch { return "Imported"; }
    }

    private static string GuessModel(string html, string url)
    {
        var m = TitlePattern().Match(html);
        return m.Success ? Clean(TagPattern().Replace(m.Groups[1].Value, "")) : url;
    }

    [GeneratedRegex(@"\t+|\s{2,}|\s*[;|]\s*|,(?=\s*\D)")] private static partial Regex CellSeparator();
    [GeneratedRegex(@"-?\d+(?:\.\d+)?")] private static partial Regex NumberPattern();
    [GeneratedRegex(@"\s*\((?:mm|cm|in|deg|°|inches)\)\s*$")] private static partial Regex UnitSuffix();
    [GeneratedRegex(@"<table\b.*?</table>", RegexOptions.Singleline | RegexOptions.IgnoreCase)] private static partial Regex TablePattern();
    [GeneratedRegex(@"<tr\b.*?</tr>", RegexOptions.Singleline | RegexOptions.IgnoreCase)] private static partial Regex RowPattern();
    [GeneratedRegex(@"<t[hd]\b[^>]*>(.*?)</t[hd]>", RegexOptions.Singleline | RegexOptions.IgnoreCase)] private static partial Regex CellPattern();
    [GeneratedRegex(@"<[^>]+>", RegexOptions.Singleline)] private static partial Regex TagPattern();
    [GeneratedRegex(@"\s+")] private static partial Regex WhitespacePattern();
    [GeneratedRegex(@"<title>(.*?)</title>", RegexOptions.Singleline | RegexOptions.IgnoreCase)] private static partial Regex TitlePattern();
    [GeneratedRegex(@"<t[dh][^>]*class=""[^""]*\bskeleton\b", RegexOptions.IgnoreCase)] private static partial Regex PlaceholderTable();
}
