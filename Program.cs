using LastFmScrobbler.Data;
using LastFmScrobbler.Core;
using LastFmScrobbler.UI;
using LastFmScrobbler.Localization;

Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

// Prevent multiple instances
using var mutex = new System.Threading.Mutex(true, "LastFmScrobblerSingleInstance", out bool firstInstance);
if (!firstInstance)
{
    MessageBox.Show("Last.fm Scrobbler is already running.\nCheck the system tray.",
        "Already running", MessageBoxButtons.OK, MessageBoxIcon.Information);
    return;
}

// Determine data directory: prefer portable (same dir as exe), fall back to AppData
var dataDir = GetDataDirectory();
var dbPath = Path.Combine(dataDir, "lastfm_scrobbler.db");

using var db = new Database(dbPath);
var settings = db.LoadSettings();

Loc.SetLanguage(settings.Language);

using var engine = new ScrobbleEngine(db, settings);
using var trayApp = new TrayApp(db, engine, settings);

Application.Run(trayApp);

static string GetDataDirectory()
{
    try
    {
        var exeDir = Path.GetDirectoryName(AppContext.BaseDirectory) ?? ".";
        var testFile = Path.Combine(exeDir, ".write_test");
        File.WriteAllText(testFile, "");
        File.Delete(testFile);
        return exeDir; // portable: writable next to exe
    }
    catch
    {
        // Fall back to AppData (e.g., if installed in Program Files)
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LastFmScrobbler");
        Directory.CreateDirectory(appData);
        return appData;
    }
}
