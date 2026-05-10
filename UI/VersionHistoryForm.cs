using LastFmScrobbler.Core;

namespace LastFmScrobbler.UI;

public class VersionHistoryForm : Form
{
    private static readonly Color CMain   = Color.FromArgb(24, 24, 24);
    private static readonly Color CInput  = Color.FromArgb(36, 36, 36);
    private static readonly Color CFg     = Color.FromArgb(220, 220, 220);
    private static readonly Color CDim    = Color.FromArgb(110, 110, 110);

    private readonly UpdateChecker _checker;
    private readonly Color         _accent;
    private ListBox  _list    = null!;
    private Button   _installBtn = null!;
    private Label    _statusLbl  = null!;
    private ProgressBar _progress = null!;
    private List<UpdateChecker.VersionEntry> _versions = [];
    private string _currentVersion;

    public VersionHistoryForm(UpdateChecker checker, Color accent)
    {
        _checker        = checker;
        _accent         = accent;
        _currentVersion = UpdateChecker.DisplayVersion;
        InitializeComponent();
        _ = LoadVersionsAsync();
    }

    private void InitializeComponent()
    {
        Text            = "Version History";
        Size            = new Size(480, 380);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        BackColor       = CMain;
        ForeColor       = CFg;
        Font            = FontManager.Regular(9.5f);

        var heading = new Label
        {
            Text     = "Available Versions",
            Dock     = DockStyle.Top,
            Height   = 40,
            Padding  = new Padding(16, 12, 0, 0),
            Font     = FontManager.Bold(10f),
            ForeColor = CFg,
        };

        _list = new ListBox
        {
            Dock            = DockStyle.Fill,
            BackColor       = CInput,
            ForeColor       = CFg,
            BorderStyle     = BorderStyle.None,
            Font            = FontManager.Regular(9.5f),
            ItemHeight      = 26,
            DrawMode        = DrawMode.OwnerDrawFixed,
        };
        _list.DrawItem       += ListDrawItem;
        _list.SelectedIndexChanged += (_, _) => UpdateButtons();

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.FromArgb(18, 18, 18) };

        _statusLbl = new Label
        {
            Location  = new Point(16, 18),
            Size      = new Size(200, 20),
            ForeColor = CDim,
            Font      = FontManager.Regular(8.5f),
        };

        _progress = new ProgressBar
        {
            Location = new Point(16, 14),
            Size     = new Size(220, 22),
            Visible  = false,
            Style    = ProgressBarStyle.Continuous,
        };

        _installBtn = new Button
        {
            Text      = "Install Selected",
            Size      = new Size(140, 32),
            Location  = new Point(480 - 140 - 16, 10),
            FlatStyle = FlatStyle.Flat,
            BackColor = _accent,
            ForeColor = Color.White,
            Font      = FontManager.Bold(9f),
            Enabled   = false,
            Cursor    = Cursors.Hand,
        };
        _installBtn.FlatAppearance.BorderSize = 0;
        _installBtn.Click += InstallClicked;

        bottom.Controls.AddRange([_statusLbl, _progress, _installBtn]);
        Controls.Add(_list);
        Controls.Add(heading);
        Controls.Add(bottom);
    }

    private void ListDrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _versions.Count) return;
        var v = _versions[e.Index];

        e.DrawBackground();
        var selected = (e.State & DrawItemState.Selected) != 0;
        using var bg = new SolidBrush(selected ? Color.FromArgb(45, 45, 45) : (e.Index % 2 == 0 ? CInput : Color.FromArgb(30, 30, 30)));
        e.Graphics.FillRectangle(bg, e.Bounds);

        var isCurrent = v.Version == _currentVersion;
        var versionText = isCurrent ? $"v{v.Version}  (current)" : $"v{v.Version}";
        using var fgBrush = new SolidBrush(isCurrent ? Color.FromArgb(80, 200, 80) : CFg);
        using var dateBrush = new SolidBrush(CDim);

        e.Graphics.DrawString(versionText, FontManager.Bold(9f), fgBrush, e.Bounds.X + 16, e.Bounds.Y + 6);
        if (!string.IsNullOrEmpty(v.Date))
            e.Graphics.DrawString(v.Date, FontManager.Regular(8.5f), dateBrush, e.Bounds.X + 160, e.Bounds.Y + 8);
    }

    private async Task LoadVersionsAsync()
    {
        _statusLbl.Text = "Loading…";
        _versions = await _checker.GetVersionHistoryAsync();
        _list.Items.Clear();
        foreach (var v in _versions) _list.Items.Add(v.Version);
        _statusLbl.Text = _versions.Count > 0 ? $"{_versions.Count} version(s) available" : "No versions found.";
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        _installBtn.Enabled = _list.SelectedIndex >= 0;
    }

    private async void InstallClicked(object? sender, EventArgs e)
    {
        if (_list.SelectedIndex < 0) return;
        var entry = _versions[_list.SelectedIndex];

        _installBtn.Enabled = false;
        _statusLbl.Visible  = false;
        _progress.Visible   = true;
        _progress.Value     = 0;

        try
        {
            var progress = new Progress<int>(p => { if (!IsDisposed) _progress.Value = p; });
            var info     = new UpdateChecker.UpdateInfo(entry.Version, entry.Url, entry.Sha256);
            var path     = await _checker.DownloadAsync(info, progress);
            UpdateChecker.LaunchAndExit(path);
            Application.Exit();
        }
        catch (Exception ex)
        {
            _progress.Visible  = false;
            _statusLbl.Visible = true;
            _statusLbl.Text    = $"Error: {ex.Message}";
            _statusLbl.ForeColor = Color.FromArgb(220, 60, 60);
            _installBtn.Enabled = true;
        }
    }
}
