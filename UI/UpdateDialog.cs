using LastFmScrobbler.Core;

namespace LastFmScrobbler.UI;

public class UpdateDialog : Form
{
    private static readonly Color CMain  = Color.FromArgb(24, 24, 24);
    private static readonly Color CFg    = Color.FromArgb(220, 220, 220);
    private static readonly Color CDim   = Color.FromArgb(110, 110, 110);
    private static readonly Color CInput = Color.FromArgb(36, 36, 36);

    private readonly UpdateChecker _checker;
    private readonly Color         _accent;

    private Label       _statusIcon  = null!;
    private Label       _statusTitle = null!;
    private Label       _statusBody  = null!;
    private ProgressBar _progress    = null!;
    private Button      _actionBtn   = null!;
    private Button      _closeBtn    = null!;

    private UpdateChecker.UpdateInfo? _foundUpdate;

    public UpdateDialog(UpdateChecker checker, Color accent, UpdateChecker.UpdateInfo? preFound = null)
    {
        _checker     = checker;
        _accent      = accent;
        _foundUpdate = preFound;
        InitializeComponent();

        if (preFound is not null) SetState_UpdateFound(preFound);
        else _ = CheckAsync();
    }

    private void InitializeComponent()
    {
        Text            = "Software Update";
        Size            = new Size(480, 240);
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        BackColor       = CMain;
        ForeColor       = CFg;
        Font            = FontManager.Regular(9.5f);
        ShowInTaskbar   = true;
        TopMost         = true;

        _statusIcon = new Label
        {
            Location  = new Point(24, 28),
            Size      = new Size(48, 48),
            Font      = FontManager.Bold(22f),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = _accent,
        };

        _statusTitle = new Label
        {
            Location  = new Point(82, 28),
            Size      = new Size(370, 26),
            Font      = FontManager.Bold(11f),
            ForeColor = CFg,
        };

        _statusBody = new Label
        {
            Location  = new Point(82, 56),
            Size      = new Size(370, 44),
            Font      = FontManager.Regular(9.5f),
            ForeColor = Color.FromArgb(160, 160, 160),
        };

        _progress = new ProgressBar
        {
            Location = new Point(24, 116),
            Size     = new Size(428, 16),
            Style    = ProgressBarStyle.Marquee,
            Visible  = false,
        };

        _actionBtn          = MakeBtn("Install", 110, 34);
        _actionBtn.BackColor = _accent;
        _actionBtn.ForeColor = Color.White;
        _actionBtn.Font      = FontManager.Bold(9.5f);
        _actionBtn.Location  = new Point(480 - 110 - 110 - 24 - 8, 156);
        _actionBtn.Visible   = false;
        _actionBtn.Click    += ActionClicked;

        _closeBtn          = MakeBtn("Close", 100, 34);
        _closeBtn.Location = new Point(480 - 100 - 24, 156);
        _closeBtn.Click   += (_, _) => Close();

        Controls.AddRange([_statusIcon, _statusTitle, _statusBody, _progress, _actionBtn, _closeBtn]);
    }

    private static Button MakeBtn(string text, int w, int h)
    {
        var b = new Button
        {
            Text      = text,
            Size      = new Size(w, h),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(38, 38, 38),
            ForeColor = CFg,
            Cursor    = Cursors.Hand,
            Font      = FontManager.Regular(9.5f),
            UseVisualStyleBackColor = false,
        };
        b.FlatAppearance.BorderColor = Color.FromArgb(50, 50, 50);
        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(48, 48, 48);
        return b;
    }

    private void SetState_Checking()
    {
        _statusIcon.Text       = "⟳";
        _statusIcon.ForeColor  = _accent;
        _statusTitle.Text      = "Checking for updates…";
        _statusBody.Text       = "Connecting to update server.";
        _progress.Visible      = true;
        _progress.Style        = ProgressBarStyle.Marquee;
        _progress.MarqueeAnimationSpeed = 30;
        _actionBtn.Visible     = false;
        _closeBtn.Text         = "Cancel";
    }

    private void SetState_UpToDate()
    {
        _statusIcon.Text      = "✓";
        _statusIcon.ForeColor = Color.FromArgb(80, 200, 80);
        _statusTitle.Text     = "You're up to date";
        _statusBody.Text      = $"Last.fm Scrobbler v{Application.ProductVersion} is the latest version.";
        _progress.Visible     = false;
        _actionBtn.Visible    = false;
        _closeBtn.Text        = "Close";
    }

    private void SetState_UpdateFound(UpdateChecker.UpdateInfo info)
    {
        _foundUpdate          = info;
        _statusIcon.Text      = "↑";
        _statusIcon.ForeColor = _accent;
        _statusTitle.Text     = $"Update available — v{info.Version}";
        _statusBody.Text      = $"You're on v{Application.ProductVersion}. Click Install to download and update now.";
        _progress.Visible     = false;
        _actionBtn.Text       = "Install";
        _actionBtn.Visible    = true;
        _closeBtn.Text        = "Later";
    }

    private void SetState_Downloading(int percent)
    {
        _statusIcon.Text      = "↓";
        _statusIcon.ForeColor = _accent;
        _statusTitle.Text     = "Downloading update…";
        _statusBody.Text      = $"{percent}% — verifying integrity after download.";
        _progress.Visible     = true;
        _progress.Style       = ProgressBarStyle.Continuous;
        _progress.Value       = Math.Clamp(percent, 0, 100);
        _actionBtn.Visible    = false;
        _closeBtn.Enabled     = false;
    }

    private void SetState_Installing()
    {
        _statusIcon.Text      = "✓";
        _statusIcon.ForeColor = Color.FromArgb(80, 200, 80);
        _statusTitle.Text     = "Installing update…";
        _statusBody.Text      = "Launching the installer. The app will close and reopen automatically.";
        _progress.Visible     = false;
        _actionBtn.Visible    = false;
    }

    private void SetState_Error(string msg)
    {
        _statusIcon.Text      = "✕";
        _statusIcon.ForeColor = Color.FromArgb(220, 80, 80);
        _statusTitle.Text     = "Update check failed";
        _statusBody.Text      = msg;
        _progress.Visible     = false;
        _actionBtn.Text       = "Retry";
        _actionBtn.Visible    = true;
        _closeBtn.Text        = "Close";
        _closeBtn.Enabled     = true;
    }

    private async Task CheckAsync()
    {
        SetState_Checking();
        try
        {
            var info = await _checker.CheckAsync();
            if (info is null) SetState_UpToDate();
            else              SetState_UpdateFound(info);
        }
        catch (Exception ex)
        {
            SetState_Error(ex.Message);
        }
    }

    private async void ActionClicked(object? sender, EventArgs e)
    {
        if (_foundUpdate is not null)
        {
            await DownloadAndInstallAsync(_foundUpdate);
        }
        else
        {
            // Retry after error
            _ = CheckAsync();
        }
    }

    private async Task DownloadAndInstallAsync(UpdateChecker.UpdateInfo info)
    {
        try
        {
            var prog = new Progress<int>(SetState_Downloading);
            var path = await _checker.DownloadAsync(info, prog);

            SetState_Installing();
            await Task.Delay(800);

            UpdateChecker.LaunchAndExit(path);
            Application.Exit();
        }
        catch (Exception ex)
        {
            SetState_Error(ex.Message);
        }
    }
}
