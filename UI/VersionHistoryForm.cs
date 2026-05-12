using System.Runtime.InteropServices;
using LastFmScrobbler.Core;

namespace LastFmScrobbler.UI;

public class VersionHistoryForm : Form
{
    private static readonly Color CMain  = Color.FromArgb(24, 24, 24);
    private static readonly Color CInput = Color.FromArgb(32, 32, 32);
    private static readonly Color CFg    = Color.FromArgb(220, 220, 220);
    private static readonly Color CDim   = Color.FromArgb(100, 100, 100);

    private readonly UpdateChecker _checker;
    private readonly Color         _accent;
    private RichTextBox  _changelog   = null!;
    private ComboBox     _versionPick = null!;
    private Button       _installBtn  = null!;
    private Label        _statusLbl   = null!;
    private ProgressBar  _progress    = null!;
    private List<UpdateChecker.VersionEntry> _versions = [];
    private string _currentVersion;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    public VersionHistoryForm(UpdateChecker checker, Color accent)
    {
        _checker        = checker;
        _accent         = accent;
        _currentVersion = UpdateChecker.DisplayVersion;
        InitializeComponent();
        _ = LoadAsync();
    }

    private void InitializeComponent()
    {
        Text            = "What's New";
        Size            = new Size(480, 540);
        MinimumSize     = new Size(400, 400);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        BackColor       = CMain;
        ForeColor       = CFg;
        Font            = FontManager.Regular(9.5f);

        var heading = new Label
        {
            Text      = "What's New",
            Dock      = DockStyle.Top,
            Height    = 44,
            Padding   = new Padding(18, 13, 0, 0),
            Font      = FontManager.Bold(11f),
            ForeColor = CFg,
        };

        var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(38, 38, 38) };

        _changelog = new RichTextBox
        {
            Dock        = DockStyle.Fill,
            BackColor   = CMain,
            ForeColor   = CFg,
            Font        = FontManager.Regular(9.5f),
            ReadOnly    = true,
            BorderStyle = BorderStyle.None,
            ScrollBars  = RichTextBoxScrollBars.Vertical,
            TabStop     = false,
        };
        _changelog.LinkClicked += (_, _) => { };

        var changelogWrapper = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = CMain,
            Padding   = new Padding(14, 10, 4, 0),
        };
        changelogWrapper.Controls.Add(_changelog);

        // ── Bottom bar ──────────────────────────────────────────────────────
        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 54, BackColor = Color.FromArgb(18, 18, 18) };

        _statusLbl = new Label
        {
            Location  = new Point(18, 19),
            Size      = new Size(180, 18),
            ForeColor = CDim,
            Font      = FontManager.Regular(8.5f),
            Text      = "Loading…",
        };

        _progress = new ProgressBar
        {
            Location = new Point(18, 15),
            Size     = new Size(200, 22),
            Visible  = false,
            Style    = ProgressBarStyle.Continuous,
        };

        _installBtn = new Button
        {
            Text      = "Install",
            Size      = new Size(80, 30),
            Location  = new Point(480 - 80 - 18, 12),
            FlatStyle = FlatStyle.Flat,
            BackColor = _accent,
            ForeColor = Color.White,
            Font      = FontManager.Bold(9f),
            Enabled   = false,
            Cursor    = Cursors.Hand,
        };
        _installBtn.FlatAppearance.BorderSize = 0;
        _installBtn.Click += InstallClicked;

        _versionPick = new ComboBox
        {
            Size          = new Size(140, 30),
            Location      = new Point(480 - 80 - 18 - 148, 14),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor     = Color.FromArgb(40, 40, 40),
            ForeColor     = CFg,
            FlatStyle     = FlatStyle.Flat,
            Font          = FontManager.Regular(9f),
            Enabled       = false,
        };
        _versionPick.SelectedIndexChanged += (_, _) => _installBtn.Enabled = _versionPick.SelectedIndex >= 0;

        bottom.Controls.AddRange([_statusLbl, _progress, _versionPick, _installBtn]);

        Controls.Add(heading);
        Controls.Add(divider);
        Controls.Add(bottom);
        Controls.Add(changelogWrapper);
    }

    private async Task LoadAsync()
    {
        try
        {
            _versions = await _checker.GetVersionHistoryAsync();
        }
        catch
        {
            _versions = [];
        }

        if (_versions.Count == 0)
        {
            _statusLbl.Text = "Could not load version history.";
            AppendText("Unable to load changelog. Check your connection.", FontManager.Regular(9.5f), CDim);
            return;
        }

        PopulateChangelog();
        PopulateVersionPicker();
        _statusLbl.Text = $"{_versions.Count} versions";
    }

    private void PopulateChangelog()
    {
        SendMessage(_changelog.Handle, 0x000B, IntPtr.Zero, IntPtr.Zero); // WM_SETREDRAW false
        _changelog.Clear();

        for (int i = 0; i < _versions.Count; i++)
        {
            var v          = _versions[i];
            var isCurrent  = v.Version == _currentVersion;
            var headerColor = isCurrent ? Color.FromArgb(80, 200, 80) : CFg;

            if (i > 0)
                AppendText(Environment.NewLine, FontManager.Regular(5f), CMain);

            // Version + date
            AppendText($"v{v.Version}", FontManager.Bold(10.5f), headerColor);
            if (!string.IsNullOrEmpty(v.Date))
                AppendText($"   ·   {v.Date}", FontManager.Regular(8.5f), CDim);
            if (isCurrent)
                AppendText("   current", FontManager.Bold(8f), Color.FromArgb(60, 160, 60));
            AppendText(Environment.NewLine, FontManager.Regular(4f), CMain);

            // Notes
            if (v.Notes.Length > 0)
            {
                foreach (var note in v.Notes)
                    AppendText($"  •  {note}{Environment.NewLine}", FontManager.Regular(9.5f), Color.FromArgb(180, 180, 180));
            }
            else
            {
                AppendText($"  No release notes.{Environment.NewLine}", FontManager.Italic(9f), Color.FromArgb(75, 75, 75));
            }

            // Separator (not after last item)
            if (i < _versions.Count - 1)
            {
                AppendText(Environment.NewLine, FontManager.Regular(3f), CMain);
                AppendText(new string('─', 52) + Environment.NewLine, FontManager.Regular(7f), Color.FromArgb(42, 42, 42));
            }
        }

        _changelog.Select(0, 0);
        SendMessage(_changelog.Handle, 0x000B, (IntPtr)1, IntPtr.Zero); // WM_SETREDRAW true
        _changelog.Invalidate();
    }

    private void PopulateVersionPicker()
    {
        _versionPick.Items.Clear();
        foreach (var v in _versions)
            _versionPick.Items.Add($"v{v.Version}");

        _versionPick.Enabled = _versions.Count > 0;
        if (_versions.Count > 0)
        {
            var currentIdx = _versions.FindIndex(v => v.Version == _currentVersion);
            _versionPick.SelectedIndex = currentIdx >= 0 ? currentIdx : 0;
        }
    }

    private void AppendText(string text, Font font, Color color)
    {
        _changelog.SelectionStart  = _changelog.TextLength;
        _changelog.SelectionLength = 0;
        _changelog.SelectionFont   = font;
        _changelog.SelectionColor  = color;
        _changelog.AppendText(text);
    }

    private async void InstallClicked(object? sender, EventArgs e)
    {
        if (_versionPick.SelectedIndex < 0) return;
        var entry = _versions[_versionPick.SelectedIndex];

        _installBtn.Enabled   = false;
        _versionPick.Enabled  = false;
        _statusLbl.Visible    = false;
        _progress.Visible     = true;
        _progress.Value       = 0;

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
            _progress.Visible    = false;
            _statusLbl.Visible   = true;
            _statusLbl.Text      = $"Error: {ex.Message}";
            _statusLbl.ForeColor = Color.FromArgb(220, 60, 60);
            _installBtn.Enabled  = true;
            _versionPick.Enabled = true;
        }
    }
}
