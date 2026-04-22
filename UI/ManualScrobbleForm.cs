namespace LastFmScrobbler.UI;

public class ManualScrobbleForm : Form
{
    public string TrackTitle  { get; private set; } = string.Empty;
    public string Artist      { get; private set; } = string.Empty;
    public string Album       { get; private set; } = string.Empty;
    public DateTime PlayedAt  { get; private set; }

    private TextBox _titleBox  = null!;
    private TextBox _artistBox = null!;
    private TextBox _albumBox  = null!;
    private DateTimePicker _datePicker = null!;

    public ManualScrobbleForm(string? artist = null, string? title = null, string? album = null)
    {
        InitializeComponent();
        if (artist != null) _artistBox.Text = artist;
        if (title  != null) _titleBox.Text  = title;
        if (album  != null) _albumBox.Text  = album;
    }

    private void InitializeComponent()
    {
        Text            = "Manuel Scrobble";
        Size            = new Size(430, 256);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        Font            = new Font("Segoe UI", 9f);
        BackColor       = Color.FromArgb(28, 28, 28);
        ForeColor       = Color.FromArgb(220, 220, 220);

        int lx = 12, rx = 110, rw = 288, y = 16;

        _titleBox  = Input(rw, 24);
        _artistBox = Input(rw, 24);
        _albumBox  = Input(rw, 24);

        _datePicker = new DateTimePicker
        {
            Location    = new Point(rx, y + 3 * 36),
            Size        = new Size(rw, 24),
            Format      = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd  HH:mm",
            ShowUpDown  = false,
            Value       = DateTime.Now,
            CalendarForeColor   = Color.FromArgb(220, 220, 220),
            CalendarMonthBackground = Color.FromArgb(36, 36, 36),
        };

        void Row(string label, Control ctrl, int row)
        {
            Controls.Add(new Label
            {
                Text      = label,
                Location  = new Point(lx, y + row * 36 + 4),
                Size      = new Size(rw - 12, 20),
                ForeColor = Color.FromArgb(110, 110, 110),
            });
            Controls.Add(ctrl);
        }

        _titleBox.Location  = new Point(rx, y + 0 * 36);
        _artistBox.Location = new Point(rx, y + 1 * 36);
        _albumBox.Location  = new Point(rx, y + 2 * 36);

        Row("Parça Adı:", _titleBox,  0);
        Row("Sanatçı:",   _artistBox, 1);
        Row("Albüm:",     _albumBox,  2);
        Row("Çalındı:",   _datePicker, 3);
        Controls.Add(_datePicker);

        int by = y + 4 * 36 + 4;

        var scrobbleBtn = new Button
        {
            Text      = "Scrobble",
            Location  = new Point(rx, by),
            Size      = new Size(100, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(186, 0, 0),
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 9f),
            UseVisualStyleBackColor = false,
        };
        scrobbleBtn.FlatAppearance.BorderSize = 0;
        scrobbleBtn.Click += (_, _) =>
        {
            var t = _titleBox.Text.Trim();
            var a = _artistBox.Text.Trim();
            if (string.IsNullOrEmpty(t) || string.IsNullOrEmpty(a))
            {
                MessageBox.Show("Parça adı ve sanatçı gereklidir.", "Eksik Bilgi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            TrackTitle = t;
            Artist     = a;
            Album      = _albumBox.Text.Trim();
            PlayedAt   = _datePicker.Value;
            DialogResult = DialogResult.OK;
            Close();
        };

        var cancelBtn = new Button
        {
            Text         = "İptal",
            Location     = new Point(rx + 110, by),
            Size         = new Size(80, 30),
            FlatStyle    = FlatStyle.Flat,
            BackColor    = Color.FromArgb(40, 40, 40),
            ForeColor    = Color.FromArgb(220, 220, 220),
            Font         = new Font("Segoe UI", 9f),
            DialogResult = DialogResult.Cancel,
            UseVisualStyleBackColor = false,
        };
        cancelBtn.FlatAppearance.BorderSize = 0;

        Controls.AddRange([scrobbleBtn, cancelBtn]);
        AcceptButton = scrobbleBtn;
        CancelButton = cancelBtn;
    }

    private static TextBox Input(int w, int h) => new()
    {
        Size        = new Size(w, h),
        BackColor   = Color.FromArgb(36, 36, 36),
        ForeColor   = Color.FromArgb(220, 220, 220),
        BorderStyle = BorderStyle.FixedSingle,
        Font        = new Font("Segoe UI", 9f),
    };
}
