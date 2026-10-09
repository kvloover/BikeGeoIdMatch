using System.Text.Json;
using System.Text.Json.Serialization;
using BikeFit.Web.Models;

namespace BikeFit.Web.Services;

/// <summary>
/// Persists <see cref="UserSettings"/> to the git-ignored user folder. Save is cheap to call on
/// every render: it only touches the disk when something actually changed.
/// </summary>
public sealed class UserSettingsStore
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;
    private readonly Lock _gate = new();
    private string _lastWritten = "";

    public UserSettingsStore(UserDataFolder userData)
    {
        _path = userData.FilePath("settings.json");
    }

    /// <summary>The saved settings, or null on a first run or when the file cannot be read.</summary>
    public UserSettings? Load()
    {
        lock (_gate)
        {
            if (!File.Exists(_path)) return null;
            try
            {
                _lastWritten = File.ReadAllText(_path);
                return JsonSerializer.Deserialize<UserSettings>(_lastWritten, Json);
            }
            catch
            {
                return null;
            }
        }
    }

    public void Save(UserSettings settings)
    {
        var text = JsonSerializer.Serialize(settings, Json);
        lock (_gate)
        {
            if (text == _lastWritten) return;
            File.WriteAllText(_path, text);
            _lastWritten = text;
        }
    }
}
