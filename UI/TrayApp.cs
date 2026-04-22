using LastFmScrobbler.Core;
using LastFmScrobbler.Data;
using LastFmScrobbler.Models;

namespace LastFmScrobbler.UI;

public class TrayApp : ApplicationContext
{
    private readonly Database _db;
    private readonly ScrobbleEngine _engine;
    private readonly AppSettings _settings;
    private readonly NotifyIcon _tray;
    private readonly ToolStripMenuItem _nowPlayingItem;
    private readonly ToolStripMenuItem _scrobbleCountItem;
    private readonly MainForm _mainForm;
    private int _sessionScrobbles;

    public TrayApp(Database db, ScrobbleEngine engine, AppSettings settings)
    {
        _db       = db;
        _engine   = engine;
        _settings = settings;

        _mainForm = new MainForm(_db, _engine, _settings);
        _ = _mainForm.Handle; // force handle creation so Invoke() works before first Show()

        _nowPlayingItem    = new ToolStripMenuItem("Not playing") { Enabled = false };
        _scrobbleCountItem = new ToolStripMenuItem("0 scrobbles this session") { Enabled = false };

        var menu = new ContextMenuStrip();
        menu.Items.Add(_nowPlayingItem);
        menu.Items.Add(_scrobbleCountItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Monitor",  null, (_, _) => _mainForm.ShowMonitor());
        menu.Items.Add("Settings", null, (_, _) => _mainForm.ShowSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApp());

        var appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;

        _tray = new NotifyIcon
        {
            Icon             = appIcon,
            Text             = "Last.fm Scrobbler",
            Visible          = true,
            ContextMenuStrip = menu,
        };
        _tray.DoubleClick += (_, _) => _mainForm.ShowMonitor();

        _engine.ConfirmBeforeScrobble = async track =>
        {
            bool result = false;
            await Task.Run(() =>
            {
                _mainForm.Invoke(() =>
                {
                    using var form = new EditTrackForm(track);
                    result = form.ShowDialog() == DialogResult.OK;
                });
            });
            return result;
        };

        _engine.NowPlayingChanged += OnNowPlayingChanged;
        _engine.TrackScrobbled   += OnTrackScrobbled;

        _ = StartEngineAsync();
    }

    private async Task StartEngineAsync()
    {
        try
        {
            await _engine.StartAsync();

            if (!_engine.IsAuthenticated)
            {
                _tray.ShowBalloonTip(4000, "Last.fm Scrobbler",
                    "Not authenticated. Right-click → Settings to log in.", ToolTipIcon.Info);
            }
        }
        catch (Exception ex)
        {
            _tray.ShowBalloonTip(5000, "Startup error", ex.Message, ToolTipIcon.Error);
        }
    }

    private void OnNowPlayingChanged(object? sender, Track? track)
    {
        if (_tray.ContextMenuStrip!.InvokeRequired)
        {
            _tray.ContextMenuStrip.Invoke(() => OnNowPlayingChanged(sender, track));
            return;
        }

        if (track is null)
        {
            _nowPlayingItem.Text = "Not playing";
            _tray.Text = "Last.fm Scrobbler";
        }
        else
        {
            var display = $"{track.Artist} – {track.Title}";
            _nowPlayingItem.Text = Truncate(display, 60);
            _tray.Text = Truncate($"♪ {display}", 63);

            if (_settings.ShowNowPlayingNotification)
                _tray.ShowBalloonTip(2000, "Now Playing", display, ToolTipIcon.None);
        }
    }

    private void OnTrackScrobbled(object? sender, (Track track, bool success) e)
    {
        if (_tray.ContextMenuStrip!.InvokeRequired)
        {
            _tray.ContextMenuStrip.Invoke(() => OnTrackScrobbled(sender, e));
            return;
        }

        if (e.success)
        {
            _sessionScrobbles++;
            _scrobbleCountItem.Text = $"{_sessionScrobbles} scrobble{(_sessionScrobbles == 1 ? "" : "s")} this session";
        }
    }

    private void ExitApp()
    {
        _tray.Visible = false;
        _mainForm.Dispose();
        _engine.Dispose();
        _db.Dispose();
        Application.Exit();
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..(max - 1)] + "…";
}
