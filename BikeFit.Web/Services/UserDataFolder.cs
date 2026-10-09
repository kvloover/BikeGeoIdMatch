namespace BikeFit.Web.Services;

/// <summary>
/// Where everything personal lives: the bikes, cockpit corrections and fit sheets the user has
/// entered. It is git-ignored, so the shipped catalogue in Data/ stays free of personal data.
/// Files saved by earlier versions next to the catalogue are moved in on first use.
/// </summary>
public sealed class UserDataFolder
{
    // earlier name in Data/ -> current name in Data/user/
    private static readonly Dictionary<string, string> LegacyNames = new()
    {
        ["user-bikes.json"] = "bikes.json",
        ["user-cockpits.json"] = "cockpits.json",
        ["fits.json"] = "fits.json"
    };

    private readonly string _path;

    public UserDataFolder(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "Data");
        _path = Path.Combine(dataDir, "user");
        Directory.CreateDirectory(_path);
        MoveLegacyFiles(dataDir);
    }

    public string FilePath(string name) => Path.Combine(_path, name);

    private void MoveLegacyFiles(string dataDir)
    {
        foreach (var (legacy, current) in LegacyNames)
        {
            var from = Path.Combine(dataDir, legacy);
            var to = FilePath(current);
            if (File.Exists(from) && !File.Exists(to))
                File.Move(from, to);
        }
    }
}
