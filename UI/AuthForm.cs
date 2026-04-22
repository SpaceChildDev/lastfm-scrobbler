using System.Diagnostics;
using LastFmScrobbler.Core;

namespace LastFmScrobbler.UI;

public class AuthForm : Form
{
    private readonly LastFmClient _client;
    public string? SessionKey { get; private set; }
    public string? Username { get; private set; }

    private string? _pendingToken;
    private Label _statusLabel = null!;
    private Button _openBrowserBtn = null!;
    private Button _doneBtn = null!;
    private System.Windows.Forms.Timer _pollTimer = null!;
    private int _pollAttempts;

    public AuthForm(LastFmClient client)
    {
        _client = client;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Last.fm Authentication";
        Size = new Size(420, 230);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var lbl = new Label
        {
            Text = "Click 'Open Browser' to authorize this app on Last.fm.\nThen come back and click 'I've Authorized'.",
            Location = new Point(16, 16),
            Size = new Size(380, 50),
            Font = new Font("Segoe UI", 9f)
        };

        _statusLabel = new Label
        {
            Text = "Ready.",
            Location = new Point(16, 72),
            Size = new Size(380, 24),
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8.5f)
        };

        _openBrowserBtn = new Button
        {
            Text = "Open Browser",
            Location = new Point(16, 110),
            Size = new Size(140, 34),
            Font = new Font("Segoe UI", 9f)
        };
        _openBrowserBtn.Click += OpenBrowserClicked;

        _doneBtn = new Button
        {
            Text = "I've Authorized",
            Location = new Point(170, 110),
            Size = new Size(140, 34),
            Font = new Font("Segoe UI", 9f),
            Enabled = false
        };
        _doneBtn.Click += DoneClicked;

        var cancelBtn = new Button
        {
            Text = "Cancel",
            Location = new Point(320, 110),
            Size = new Size(80, 34),
            Font = new Font("Segoe UI", 9f),
            DialogResult = DialogResult.Cancel
        };

        _pollTimer = new System.Windows.Forms.Timer { Interval = 2000 };
        _pollTimer.Tick += PollTick;

        Controls.AddRange([lbl, _statusLabel, _openBrowserBtn, _doneBtn, cancelBtn]);
    }

    private async void OpenBrowserClicked(object? sender, EventArgs e)
    {
        try
        {
            _openBrowserBtn.Enabled = false;
            _statusLabel.Text = "Fetching token...";
            _statusLabel.ForeColor = Color.DarkOrange;

            var (token, url) = await _client.GetAuthUrlAsync();
            _pendingToken = token;

            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

            _statusLabel.Text = "Waiting for authorization...";
            _doneBtn.Enabled = true;
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Error: {ex.Message}";
            _statusLabel.ForeColor = Color.Red;
            _openBrowserBtn.Enabled = true;
        }
    }

    private void DoneClicked(object? sender, EventArgs e)
    {
        _doneBtn.Enabled = false;
        _statusLabel.Text = "Verifying...";
        _pollAttempts = 0;
        _pollTimer.Start();
    }

    private async void PollTick(object? sender, EventArgs e)
    {
        _pollAttempts++;
        if (_pollAttempts > 15 || _pendingToken is null)
        {
            _pollTimer.Stop();
            _statusLabel.Text = "Timed out. Did you authorize in the browser?";
            _statusLabel.ForeColor = Color.Red;
            _doneBtn.Enabled = true;
            return;
        }

        try
        {
            var (sk, name) = await _client.GetSessionAsync(_pendingToken);
            _pollTimer.Stop();
            SessionKey = sk;
            Username = name;
            _statusLabel.Text = $"Authenticated as {name}!";
            _statusLabel.ForeColor = Color.Green;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch
        {
            _statusLabel.Text = $"Not authorized yet... (attempt {_pollAttempts}/15)";
        }
    }
}
