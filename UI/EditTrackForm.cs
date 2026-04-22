using LastFmScrobbler.Models;

namespace LastFmScrobbler.UI;

/// <summary>
/// Shown before a scrobble when "Edit before scrobble" is enabled.
/// </summary>
public class EditTrackForm : Form
{
    private readonly Track _track;
    private TextBox _titleBox = null!;
    private TextBox _artistBox = null!;
    private TextBox _albumBox = null!;

    public EditTrackForm(Track track)
    {
        _track = track;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Edit track before scrobbling";
        Size = new Size(420, 230);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Font = new Font("Segoe UI", 9f);

        int y = 16;

        Controls.Add(MakeLabel("Title:", 12, y + 3));
        _titleBox = new TextBox { Text = _track.Title, Location = new Point(100, y), Size = new Size(295, 23) };
        Controls.Add(_titleBox);
        y += 36;

        Controls.Add(MakeLabel("Artist:", 12, y + 3));
        _artistBox = new TextBox { Text = _track.Artist, Location = new Point(100, y), Size = new Size(295, 23) };
        Controls.Add(_artistBox);
        y += 36;

        Controls.Add(MakeLabel("Album:", 12, y + 3));
        _albumBox = new TextBox { Text = _track.Album, Location = new Point(100, y), Size = new Size(295, 23) };
        Controls.Add(_albumBox);
        y += 48;

        var scrobbleBtn = new Button
        {
            Text = "Scrobble",
            Location = new Point(100, y),
            Size = new Size(100, 32),
            DialogResult = DialogResult.OK
        };
        scrobbleBtn.Click += (_, _) =>
        {
            _track.Title = _titleBox.Text.Trim();
            _track.Artist = _artistBox.Text.Trim();
            _track.Album = _albumBox.Text.Trim();
        };

        var skipBtn = new Button
        {
            Text = "Skip",
            Location = new Point(210, y),
            Size = new Size(80, 32),
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange([scrobbleBtn, skipBtn]);
        AcceptButton = scrobbleBtn;
        CancelButton = skipBtn;
    }

    private static Label MakeLabel(string text, int x, int y) =>
        new() { Text = text, Location = new Point(x, y), Size = new Size(82, 20), TextAlign = ContentAlignment.MiddleLeft };
}
