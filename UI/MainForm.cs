using LastFmScrobbler.Core;
using LastFmScrobbler.Data;
using LastFmScrobbler.Localization;
using LastFmScrobbler.Models;

namespace LastFmScrobbler.UI;

public class MainForm : Form
{
    private static readonly Color CSidebar = Color.FromArgb(15, 15, 15);
    private static readonly Color CMain    = Color.FromArgb(24, 24, 24);
    private static readonly Color CFg      = Color.FromArgb(220, 220, 220);
    private static readonly Color CDim     = Color.FromArgb(110, 110, 110);
    private static readonly Color CInput   = Color.FromArgb(36, 36, 36);

    // Instance-level accent color so it can be changed at runtime
    private Color _cAccent;

    private readonly Database _db;
    private readonly ScrobbleEngine _engine;
    private AppSettings _settings;

    // Nav buttons (5 pages)
    private NavButton _btnMonitor  = null!;
    private NavButton _btnHistory  = null!;
    private NavButton _btnAccount  = null!;
    private NavButton _btnScrobble = null!;
    private NavButton _btnNorm     = null!;

    // Pages
    private Panel _content      = null!;
    private Panel _pageMonitor  = null!;
    private Panel _pageHistory  = null!;
    private Panel _pageAccount  = null!;
    private Panel _pageScrobble = null!;
    private Panel _pageNorm     = null!;

    // Monitor page
    private Label       _monTitle  = null!;
    private Label       _monArtist = null!;
    private Label       _monAlbum  = null!;
    private ProgressBar _monBar    = null!;
    private Label       _monStatus = null!;
    private Label       _monEta    = null!;
    private ListBox     _monLog    = null!;
    private PictureBox  _albumArt  = null!;
    private Button      _loveBtn   = null!;
    private bool        _trackLoved;
    private CancellationTokenSource? _artCts;

    // Scrobbling page
    private ComboBox _langCombo = null!;

    private readonly System.Windows.Forms.Timer _tick = new() { Interval = 500 };
    private Track?   _currentTrack;
    private DateTime _startedAt;
    private int      _threshMs;
    private bool     _scrobbled;

    // Account page
    private TextBox _apiKeyBox       = null!;
    private TextBox _apiSecretBox    = null!;
    private Label   _authStatusLabel = null!;
    private Button  _authBtn         = null!;
    private LinkLabel _profileLink   = null!;

    // Scrobbling page
    private NumericUpDown _threshPct    = null!;
    private NumericUpDown _threshMax    = null!;
    private NumericUpDown _dupWindow    = null!;
    private CheckBox _filterAppleChk   = null!;
    private CheckBox _editBeforeChk    = null!;
    private CheckBox _showNotifChk     = null!;
    private CheckBox _startWinChk      = null!;

    // Normalization page
    private CheckBox     _autoNormChk = null!;
    private DataGridView _rulesGrid   = null!;

    // History page
    private Label        _statTotal    = null!;
    private Label        _statToday    = null!;
    private Label        _statWeek     = null!;
    private Label        _statPending  = null!;
    private DataGridView _historyGrid  = null!;

    // Stats page
    private NavButton _btnStats    = null!;
    private Panel     _pageStats   = null!;
    private Panel     _chartPanel  = null!;
    private Panel     _artistsPanel = null!;
    private Panel     _tracksPanel  = null!;
    private Label     _bigTotal    = null!;
    private Label     _bigWeek     = null!;
    private Label     _bigToday    = null!;
    private (string day, int count)[]                  _dailyData      = [];
    private (string artist, int count)[]               _topArtistsData = [];
    private (string artist, string title, int count)[] _topTracksData  = [];

    // Accent tracking
    private Panel         _accentBar   = null!;
    private NavButton?    _activeNavBtn;
    private readonly List<Button> _accentBtns = new();

    // Title bar
    private Button _maxBtn = null!;

    public MainForm(Database db, ScrobbleEngine engine, AppSettings settings)
    {
        _db = db; _engine = engine; _settings = settings;
        _cAccent = ColorFromHex(settings.AccentColor, Color.FromArgb(186, 0, 0));

        InitializeComponent();
        BuildMonitorPage();
        BuildHistoryPage();
        BuildAccountPage();
        BuildScrobblePage();
        BuildNormPage();
        BuildStatsPage();
        LoadSettings();
        LoadRules();

        _engine.NowPlayingChanged  += OnNowPlaying;
        _engine.TrackScrobbled     += OnScrobbled;
        _engine.PendingQueueFlushed += OnQueueFlushed;
        _tick.Tick += OnTick;
        _tick.Start();

        if (_engine.CurrentTrack is Track t) OnNowPlaying(null, t);
        Navigate(_pageMonitor, _btnMonitor);
    }

    // ── Shell ─────────────────────────────────────────────────────────────────

    private void InitializeComponent()
    {
        Text            = "Last.fm Scrobbler";
        Size            = new Size(720, 500);
        MinimumSize     = new Size(640, 440);
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        BackColor       = CMain;
        ForeColor       = CFg;
        Font            = new Font("Segoe UI", 9f);
        FormClosing    += (_, e) => { e.Cancel = true; Hide(); };
        SizeChanged    += (_, _) => { if (_maxBtn != null) _maxBtn.Text = WindowState == FormWindowState.Maximized ? "❐" : "□"; };
        Load           += (_, _) => SizePages();

        var sidebar = new Panel { Dock = DockStyle.Left, Width = 150, BackColor = CSidebar };

        _btnMonitor  = NavBtn(Loc.T("NavMonitor"),       "▶");
        _btnHistory  = NavBtn(Loc.T("NavHistory"),       "◎");
        _btnStats    = NavBtn(Loc.T("NavStats"),          "∑");
        _btnAccount  = NavBtn(Loc.T("NavAccount"),       "◉");
        _btnScrobble = NavBtn(Loc.T("NavScrobbling"),    "⚙");
        _btnNorm     = NavBtn(Loc.T("NavNormalization"), "≡");

        _btnMonitor.Click  += (_, _) => Navigate(_pageMonitor,  _btnMonitor);
        _btnHistory.Click  += (_, _) => { Navigate(_pageHistory, _btnHistory); LoadHistory(); RefreshStats(); };
        _btnStats.Click    += (_, _) => { Navigate(_pageStats,   _btnStats);   LoadStatsPage(); };
        _btnAccount.Click  += (_, _) => Navigate(_pageAccount,  _btnAccount);
        _btnScrobble.Click += (_, _) => Navigate(_pageScrobble, _btnScrobble);
        _btnNorm.Click     += (_, _) => Navigate(_pageNorm,     _btnNorm);

        var bugBtn = new Button
        {
            Text      = Loc.T("ReportBug"),
            Dock      = DockStyle.Bottom,
            Height    = 34,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(70, 70, 70),
            BackColor = Color.Transparent,
            Font      = new Font("Segoe UI", 8f),
            Cursor    = Cursors.Hand,
            UseVisualStyleBackColor = false,
        };
        bugBtn.FlatAppearance.BorderSize = 0;
        bugBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(25, 25, 25);
        bugBtn.MouseEnter += (_, _) => bugBtn.ForeColor = Color.FromArgb(130, 130, 130);
        bugBtn.MouseLeave += (_, _) => bugBtn.ForeColor = Color.FromArgb(70, 70, 70);
        bugBtn.Click += (_, _) => OpenUrl("mailto:support@spacechild.dev?subject=Last.fm%20Scrobbler%20-%20Hata%20Bildirimi");

        sidebar.Controls.Add(bugBtn);
        sidebar.Controls.Add(_btnNorm);
        sidebar.Controls.Add(_btnScrobble);
        sidebar.Controls.Add(_btnAccount);
        sidebar.Controls.Add(_btnStats);
        sidebar.Controls.Add(_btnHistory);
        sidebar.Controls.Add(_btnMonitor);

        _content = new Panel { Dock = DockStyle.Fill, BackColor = CMain };

        _pageMonitor  = new Panel { BackColor = CMain, Visible = false, Padding = new Padding(22, 14, 22, 10) };
        _pageHistory  = new Panel { BackColor = CMain, Visible = false };
        _pageAccount  = new Panel { BackColor = CMain, Visible = false };
        _pageScrobble = new Panel { BackColor = CMain, Visible = false };
        _pageNorm     = new Panel { BackColor = CMain, Visible = false };
        _pageStats    = new Panel { BackColor = CMain, Visible = false };

        _content.Controls.AddRange([_pageMonitor, _pageHistory, _pageAccount, _pageScrobble, _pageNorm, _pageStats]);
        _content.Resize += (_, _) => SizePages();

        var titleBar = BuildTitleBar();

        Controls.Add(_content);
        Controls.Add(sidebar);
        Controls.Add(titleBar);
    }

    private void SizePages()
    {
        var r = _content.ClientRectangle;
        foreach (var p in new[] { _pageMonitor, _pageHistory, _pageAccount, _pageScrobble, _pageNorm, _pageStats })
            p.Bounds = r;
    }

    private void Navigate(Panel page, NavButton btn)
    {
        foreach (var p in new[] { _pageMonitor, _pageHistory, _pageAccount, _pageScrobble, _pageNorm, _pageStats })
            p.Visible = false;
        foreach (var b in new[] { _btnMonitor, _btnHistory, _btnStats, _btnAccount, _btnScrobble, _btnNorm })
        {
            b.BackColor = Color.Transparent;
            b.ForeColor = CDim;
        }
        page.Visible  = true;
        btn.BackColor = _cAccent;
        btn.ForeColor = Color.White;
        _activeNavBtn = btn;
    }

    public void ShowMonitor()
    {
        Show();
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate();
        Navigate(_pageMonitor, _btnMonitor);
    }

    public void ShowSettings()
    {
        Show();
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate();
        Navigate(_pageAccount, _btnAccount);
    }

    // ── Monitor Page ──────────────────────────────────────────────────────────

    private void BuildMonitorPage()
    {
        var card = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 120,
            BackColor = Color.FromArgb(28, 28, 28),
        };

        _accentBar = new Panel { Dock = DockStyle.Left, Width = 3, BackColor = _cAccent };

        // Album art on the right — wrapped in a panel to add 5px padding on all sides
        _albumArt = new PictureBox
        {
            Dock     = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.StretchImage,
            BackColor = Color.Transparent,
        };
        var artWrap = new Panel
        {
            Dock      = DockStyle.Right,
            Width     = 120,
            BackColor = Color.FromArgb(20, 20, 20),
            Padding   = new Padding(5),
        };
        artWrap.Controls.Add(_albumArt);

        var cardInner = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding   = new Padding(14, 10, 14, 10),
        };

        var nowLbl = new Label
        {
            Dock      = DockStyle.Top,
            Height    = 16,
            Text      = Loc.T("NowPlaying"),
            Font      = new Font("Segoe UI", 7f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 100, 100),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        _monTitle  = new Label { Dock = DockStyle.Top, Height = 32, Font = new Font("Segoe UI", 15f, FontStyle.Bold), ForeColor = CFg, Text = "—", AutoEllipsis = true };
        _monArtist = new Label { Dock = DockStyle.Top, Height = 22, Font = new Font("Segoe UI", 10f), ForeColor = Color.FromArgb(180, 180, 180), Text = "", AutoEllipsis = true };
        _monAlbum  = new Label { Dock = DockStyle.Top, Height = 18, Font = new Font("Segoe UI", 8.5f, FontStyle.Italic), ForeColor = Color.FromArgb(90, 90, 90), Text = "", AutoEllipsis = true };

        cardInner.Controls.Add(_monAlbum);
        cardInner.Controls.Add(_monArtist);
        cardInner.Controls.Add(_monTitle);
        cardInner.Controls.Add(nowLbl);

        card.Controls.Add(cardInner);
        card.Controls.Add(artWrap);
        card.Controls.Add(_accentBar);

        var sp1 = Gap(10);

        // Status row with love button
        var statusRow = new Panel { Dock = DockStyle.Top, Height = 22 };

        _loveBtn = new Button
        {
            Text      = "♡",
            Dock      = DockStyle.Right,
            Width     = 30,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(120, 120, 120),
            BackColor = Color.Transparent,
            Font      = new Font("Segoe UI", 11f),
            Cursor    = Cursors.Hand,
            Enabled   = false,
            UseVisualStyleBackColor = false,
        };
        _loveBtn.FlatAppearance.BorderSize              = 0;
        _loveBtn.FlatAppearance.MouseOverBackColor      = Color.Transparent;
        _loveBtn.FlatAppearance.MouseDownBackColor      = Color.Transparent;
        _loveBtn.Click += LoveBtnClicked;

        _monStatus = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5f), ForeColor = CDim, Text = Loc.T("NotPlaying"), TextAlign = ContentAlignment.MiddleLeft };
        _monEta    = new Label { Dock = DockStyle.Right, Width = 52, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 8f), ForeColor = CDim };

        statusRow.Controls.Add(_monStatus);
        statusRow.Controls.Add(_monEta);
        statusRow.Controls.Add(_loveBtn);

        var barRow = new Panel { Dock = DockStyle.Top, Height = 6 };
        _monBar = new ProgressBar { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100, ForeColor = _cAccent };
        barRow.Controls.Add(_monBar);

        var sp2    = Gap(12);
        var line   = new Label { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(35, 35, 35) };
        var logLbl = SectionLabel("LOG");

        _monLog = new ListBox
        {
            Dock          = DockStyle.Fill,
            BackColor     = Color.FromArgb(18, 18, 18),
            ForeColor     = CDim,
            BorderStyle   = BorderStyle.None,
            SelectionMode = SelectionMode.None,
            Font          = new Font("Consolas", 8.5f),
        };

        _pageMonitor.Controls.Add(_monLog);
        _pageMonitor.Controls.Add(logLbl);
        _pageMonitor.Controls.Add(line);
        _pageMonitor.Controls.Add(sp2);
        _pageMonitor.Controls.Add(barRow);
        _pageMonitor.Controls.Add(statusRow);
        _pageMonitor.Controls.Add(sp1);
        _pageMonitor.Controls.Add(card);
    }

    // ── History Page ──────────────────────────────────────────────────────────

    private void BuildHistoryPage()
    {
        var heading = PageHeading(Loc.T("NavHistory"));
        heading.Dock = DockStyle.Top;

        // Stats row
        var statsRow = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = Color.FromArgb(20, 20, 20) };

        _statTotal   = StatLabel(string.Format(Loc.T("StatTotal"),   "—"));
        _statToday   = StatLabel(string.Format(Loc.T("StatToday"),   "—"));
        _statWeek    = StatLabel(string.Format(Loc.T("StatWeek"),    "—"));
        _statPending = StatLabel(Loc.T("StatQueueEmpty"));

        _statTotal.Location   = new Point(24, 10);
        _statToday.Location   = new Point(150, 10);
        _statWeek.Location    = new Point(270, 10);
        _statPending.Location = new Point(400, 10);

        statsRow.Controls.AddRange([_statTotal, _statToday, _statWeek, _statPending]);

        // History grid
        _historyGrid = new DataGridView
        {
            Dock                      = DockStyle.Fill,
            AllowUserToAddRows        = false,
            AllowUserToDeleteRows     = false,
            RowHeadersVisible         = false,
            SelectionMode             = DataGridViewSelectionMode.FullRowSelect,
            ReadOnly                  = true,
            AutoSizeColumnsMode       = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor           = Color.FromArgb(20, 20, 20),
            GridColor                 = Color.FromArgb(38, 38, 38),
            BorderStyle               = BorderStyle.None,
            ForeColor                 = CFg,
            EnableHeadersVisualStyles = false,
            MultiSelect               = false,
        };
        _historyGrid.DefaultCellStyle.BackColor                       = Color.FromArgb(28, 28, 28);
        _historyGrid.DefaultCellStyle.ForeColor                       = CFg;
        _historyGrid.DefaultCellStyle.SelectionBackColor              = Color.FromArgb(50, 50, 50);
        _historyGrid.DefaultCellStyle.SelectionForeColor              = CFg;
        _historyGrid.AlternatingRowsDefaultCellStyle.BackColor        = Color.FromArgb(24, 24, 24);
        _historyGrid.ColumnHeadersDefaultCellStyle.BackColor          = Color.FromArgb(18, 18, 18);
        _historyGrid.ColumnHeadersDefaultCellStyle.ForeColor          = CDim;
        _historyGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(18, 18, 18);
        _historyGrid.ColumnHeadersHeightSizeMode                      = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _historyGrid.ColumnHeadersHeight                              = 28;

        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Time",   HeaderText = Loc.T("ColTime"),   FillWeight = 15 });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Artist", HeaderText = Loc.T("ColArtist"), FillWeight = 25 });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title",  HeaderText = Loc.T("ColTrack"),  FillWeight = 30 });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Album",  HeaderText = Loc.T("ColAlbum"),  FillWeight = 25 });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = Loc.T("ColStatus"), FillWeight = 5  });

        // Button row
        var btnRow = new Panel { Dock = DockStyle.Bottom, Height = 42, BackColor = CMain };

        var refreshBtn = MakeBtn(Loc.T("BtnRefresh"), 100, 28);
        refreshBtn.Location = new Point(24, 7);
        refreshBtn.Click   += (_, _) => { LoadHistory(); RefreshStats(); };

        var manualBtn = MakeBtn(Loc.T("BtnManualScrobble"), 150, 28);
        manualBtn.Location  = new Point(134, 7);
        manualBtn.Click    += ManualScrobbleClicked;

        btnRow.Controls.AddRange([refreshBtn, manualBtn]);

        _pageHistory.Controls.Add(_historyGrid);
        _pageHistory.Controls.Add(statsRow);
        _pageHistory.Controls.Add(heading);
        _pageHistory.Controls.Add(btnRow);
    }

    private void LoadHistory()
    {
        if (InvokeRequired) { Invoke(LoadHistory); return; }

        _historyGrid.Rows.Clear();
        foreach (var rec in _db.LoadHistory(200))
        {
            var status = rec.Success ? "✓" : "✗";
            var i = _historyGrid.Rows.Add(
                rec.ScrobbledAt.ToLocalTime().ToString("MM-dd HH:mm"),
                rec.Artist, rec.Title, rec.Album, status);

            _historyGrid.Rows[i].DefaultCellStyle.ForeColor =
                rec.Success ? CFg : Color.FromArgb(180, 60, 60);
        }
    }

    private void RefreshStats()
    {
        if (InvokeRequired) { Invoke(RefreshStats); return; }

        var (total, today, week) = _db.GetStats();
        var pending              = _db.PendingCount();

        _statTotal.Text   = string.Format(Loc.T("StatTotal"), total.ToString("N0"));
        _statToday.Text   = string.Format(Loc.T("StatToday"), today.ToString("N0"));
        _statWeek.Text    = string.Format(Loc.T("StatWeek"),  week.ToString("N0"));
        _statPending.Text = pending > 0 ? string.Format(Loc.T("StatQueue"), pending) : Loc.T("StatQueueEmpty");
    }

    // ── Account Page ──────────────────────────────────────────────────────────

    private void BuildAccountPage()
    {
        const int lx = 24, rx = 150, rw = 390;
        int y = 16;

        _apiKeyBox    = MakeInput(rw, 24);
        _apiSecretBox = MakeInput(rw, 24);
        _apiSecretBox.UseSystemPasswordChar = true;

        var hint = new LinkLabel
        {
            Text      = Loc.T("GetApiKeyHint"),
            Size      = new Size(rw, 18),
            Font      = new Font("Segoe UI", 8.5f),
            LinkColor = _cAccent,
            ForeColor = CDim,
        };
        hint.Links.Add(0, hint.Text.Length, "https://www.last.fm/api/account/create");
        hint.LinkClicked += (_, e) => OpenUrl(e.Link?.LinkData?.ToString() ?? "");

        _authStatusLabel = new Label { Size = new Size(500, 22), ForeColor = CDim, Font = new Font("Segoe UI", 9f) };

        _profileLink = new LinkLabel
        {
            Size      = new Size(200, 18),
            Font      = new Font("Segoe UI", 8.5f),
            LinkColor = _cAccent,
            ForeColor = CDim,
            Visible   = false,
        };
        _profileLink.LinkClicked += (_, _) =>
        {
            if (!string.IsNullOrEmpty(_settings.Username))
                OpenUrl($"https://www.last.fm/user/{_settings.Username}");
        };

        _authBtn = MakeBtn(Loc.T("LoginWithLastFm"), 160, 30);
        _authBtn.Click += AuthClicked;

        var saveBtn = MakeBtn(Loc.T("Save"), 80, 30);
        saveBtn.BackColor = _cAccent;
        _accentBtns.Add(saveBtn);
        saveBtn.Click += (_, _) => SaveAccountSettings();

        var heading = PageHeading(Loc.T("NavAccount"));
        heading.Dock = DockStyle.Top;

        var inner = new Panel { Dock = DockStyle.Fill, BackColor = CMain };

        void Row(string label, Control ctrl)
        {
            inner.Controls.Add(RowLabel(label, lx, y + 3));
            ctrl.Location = new Point(rx, y);
            inner.Controls.Add(ctrl);
            y += 34;
        }

        Row("API Key",    _apiKeyBox);
        Row("API Secret", _apiSecretBox);

        hint.Location             = new Point(rx, y); inner.Controls.Add(hint);             y += 28;
        _authStatusLabel.Location = new Point(lx, y); inner.Controls.Add(_authStatusLabel); y += 28;
        _profileLink.Location     = new Point(lx, y); inner.Controls.Add(_profileLink);     y += 30;
        _authBtn.Location         = new Point(lx, y); inner.Controls.Add(_authBtn);
        saveBtn.Location          = new Point(lx + _authBtn.Width + 10, y); inner.Controls.Add(saveBtn);

        _pageAccount.Controls.Add(inner);
        _pageAccount.Controls.Add(heading);
    }

    // ── Scrobbling Page ───────────────────────────────────────────────────────

    private void BuildScrobblePage()
    {
        const int lx = 24, rx = 210;
        int y = 16;

        _threshPct = new NumericUpDown { Size = new Size(65, 24), Minimum = 10, Maximum = 100, Value = 50,  Increment = 5,  BackColor = CInput, ForeColor = CFg };
        _threshMax = new NumericUpDown { Size = new Size(65, 24), Minimum = 30, Maximum = 600, Value = 240, Increment = 30, BackColor = CInput, ForeColor = CFg };
        _dupWindow = new NumericUpDown { Size = new Size(65, 24), Minimum = 0,  Maximum = 60,  Value = 5,   Increment = 1,  BackColor = CInput, ForeColor = CFg };

        _filterAppleChk = MakeChk(Loc.T("FilterAppleOnly"));
        _editBeforeChk  = MakeChk(Loc.T("EditBeforeScrobble"));
        _showNotifChk   = MakeChk(Loc.T("ShowNotification"));
        _startWinChk    = MakeChk(Loc.T("StartWithWindows"));

        _langCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CInput, ForeColor = CFg, FlatStyle = FlatStyle.Flat, Size = new Size(140, 24) };
        foreach (var l in Loc.Available) _langCombo.Items.Add(l.NativeName);

        var saveBtn = MakeBtn(Loc.T("Save"), 80, 30);
        saveBtn.BackColor = _cAccent;
        _accentBtns.Add(saveBtn);
        saveBtn.Click += (_, _) => SaveScrobbleSettings();

        var accentBtn = MakeBtn(Loc.T("PickColor"), 90, 30);
        accentBtn.Click += PickAccentColorClicked;

        var heading = PageHeading(Loc.T("NavScrobbling"));
        heading.Dock = DockStyle.Top;

        var inner = new Panel { Dock = DockStyle.Fill, BackColor = CMain };

        void NumRow(string lbl, NumericUpDown ctrl, string unit)
        {
            inner.Controls.Add(RowLabel(lbl, lx, y + 4));
            ctrl.Location = new Point(rx, y);
            inner.Controls.Add(ctrl);
            inner.Controls.Add(new Label { Text = unit, Location = new Point(rx + 70, y + 4), Size = new Size(260, 20), ForeColor = CDim });
            y += 34;
        }

        NumRow(Loc.T("LblThreshold"), _threshPct, Loc.T("UnitAfterPercent"));
        NumRow(Loc.T("LblMaximum"),   _threshMax, Loc.T("UnitSecondsFirst"));

        inner.Controls.Add(new Label { Text = Loc.T("NoteMin30"), Location = new Point(lx, y), Size = new Size(500, 18), ForeColor = Color.FromArgb(65, 65, 65), Font = new Font("Segoe UI", 8.5f) });
        y += 28;

        NumRow(Loc.T("LblDuplicateProtection"), _dupWindow, Loc.T("UnitMinSkipDup"));

        foreach (var chk in new CheckBox[] { _filterAppleChk, _editBeforeChk, _showNotifChk, _startWinChk })
        {
            chk.Location = new Point(lx, y);
            inner.Controls.Add(chk);
            y += 28;
        }

        saveBtn.Location   = new Point(lx, y + 8);
        accentBtn.Location = new Point(lx + saveBtn.Width + 10, y + 8);
        inner.Controls.Add(saveBtn);
        inner.Controls.Add(accentBtn);
        inner.Controls.Add(new Label { Text = Loc.T("LblAccentColor"), Location = new Point(lx + saveBtn.Width + accentBtn.Width + 20, y + 14), Size = new Size(80, 18), ForeColor = CDim });

        y += 46;
        inner.Controls.Add(new Label { Text = Loc.T("LblLanguage"), Location = new Point(lx, y + 4), Size = new Size(180, 20), ForeColor = CDim, Font = new Font("Segoe UI", 9f) });
        _langCombo.Location = new Point(210, y);
        inner.Controls.Add(_langCombo);

        _pageScrobble.Controls.Add(inner);
        _pageScrobble.Controls.Add(heading);
    }

    // ── Normalization Page ────────────────────────────────────────────────────

    private void BuildNormPage()
    {
        var heading = PageHeading(Loc.T("NavNormalization"));
        heading.Dock = DockStyle.Top;

        _autoNormChk = new CheckBox
        {
            Text      = Loc.T("AutoNormalize"),
            Dock      = DockStyle.Top,
            Height    = 30,
            AutoSize  = false,
            Checked   = false,
            Padding   = new Padding(24, 6, 0, 0),
            ForeColor = CFg,
            Font      = new Font("Segoe UI", 9f),
        };

        _rulesGrid = new DataGridView
        {
            Dock                      = DockStyle.Fill,
            AllowUserToAddRows        = false,
            AllowUserToDeleteRows     = false,
            RowHeadersVisible         = false,
            SelectionMode             = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode       = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor           = Color.FromArgb(20, 20, 20),
            GridColor                 = Color.FromArgb(38, 38, 38),
            BorderStyle               = BorderStyle.None,
            ForeColor                 = CFg,
            EnableHeadersVisualStyles = false,
        };
        _rulesGrid.DefaultCellStyle.BackColor                       = Color.FromArgb(28, 28, 28);
        _rulesGrid.DefaultCellStyle.ForeColor                       = CFg;
        _rulesGrid.DefaultCellStyle.SelectionBackColor              = Color.FromArgb(50, 50, 50);
        _rulesGrid.DefaultCellStyle.SelectionForeColor              = CFg;
        _rulesGrid.AlternatingRowsDefaultCellStyle.BackColor        = Color.FromArgb(24, 24, 24);
        _rulesGrid.ColumnHeadersDefaultCellStyle.BackColor          = Color.FromArgb(18, 18, 18);
        _rulesGrid.ColumnHeadersDefaultCellStyle.ForeColor          = CDim;
        _rulesGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(18, 18, 18);

        _rulesGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Enabled", HeaderText = Loc.T("ColEnabled"),     FillWeight = 5,  Width = 40 });
        _rulesGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "Field",   HeaderText = Loc.T("ColField"),       FillWeight = 12, ReadOnly = true });
        _rulesGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "Desc",    HeaderText = Loc.T("ColDescription"), FillWeight = 28, ReadOnly = true });
        _rulesGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "Pattern", HeaderText = "Pattern",               FillWeight = 38 });
        _rulesGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "Replace", HeaderText = Loc.T("ColReplace"),     FillWeight = 17 });

        var addBtn  = MakeBtn(Loc.T("BtnAddRule"),       108, 28); addBtn.Click  += AddRuleClicked;
        var delBtn  = MakeBtn(Loc.T("BtnDeleteSelected"), 108, 28); delBtn.Click += DeleteRuleClicked;
        var saveBtn = MakeBtn(Loc.T("Save"),               80, 28);
        saveBtn.BackColor = _cAccent;
        _accentBtns.Add(saveBtn);
        saveBtn.Click += (_, _) => SaveNormSettings();

        var btnRow = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = CMain };
        addBtn.Location  = new Point(24,  6);
        delBtn.Location  = new Point(138, 6);
        saveBtn.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
        saveBtn.Location = new Point(500, 6);
        btnRow.Controls.AddRange([addBtn, delBtn, saveBtn]);

        _pageNorm.Controls.Add(_rulesGrid);
        _pageNorm.Controls.Add(_autoNormChk);
        _pageNorm.Controls.Add(heading);
        _pageNorm.Controls.Add(btnRow);
    }

    // ── Stats Page ────────────────────────────────────────────────────────────

    private void BuildStatsPage()
    {
        var heading = PageHeading(Loc.T("NavStats"));
        heading.Dock   = DockStyle.Top;
        heading.Height = 44;

        // Summary row: 3 stat cards
        var summaryRow = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = Color.FromArgb(20, 20, 20) };
        var cardTotal  = MakeStatCard(out _bigTotal, Loc.T("StatsTotalLabel"));
        var cardWeek   = MakeStatCard(out _bigWeek,  Loc.T("StatsThisWeek"));
        var cardToday  = MakeStatCard(out _bigToday, Loc.T("StatsToday"));
        cardTotal.Location = new Point(24,  5);
        cardWeek.Location  = new Point(220, 5);
        cardToday.Location = new Point(416, 5);
        summaryRow.Controls.AddRange([cardTotal, cardWeek, cardToday]);

        // Chart section
        var chartLbl = SectionLabel(Loc.T("StatsLast14Days"));
        chartLbl.Padding   = new Padding(24, 0, 0, 0);
        chartLbl.TextAlign = ContentAlignment.MiddleLeft;

        _chartPanel = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = Color.FromArgb(20, 20, 20) };
        _chartPanel.Paint += PaintDailyChart;

        var gap = Gap(6);

        // Bottom: artists (left) + tracks (right)
        var bottomSection = new Panel { Dock = DockStyle.Fill };

        var leftPanel    = new Panel { Dock = DockStyle.Left, Width = 260, BackColor = Color.FromArgb(22, 22, 22) };
        var artistsHdr   = SectionLabel(Loc.T("StatsTopArtists"));
        artistsHdr.Padding   = new Padding(16, 0, 0, 0);
        artistsHdr.TextAlign = ContentAlignment.MiddleLeft;
        _artistsPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        _artistsPanel.Paint += PaintTopArtists;
        leftPanel.Controls.Add(_artistsPanel);
        leftPanel.Controls.Add(artistsHdr);   // last added = topmost

        var divider = new Panel { Dock = DockStyle.Left, Width = 1, BackColor = Color.FromArgb(38, 38, 38) };

        var rightPanel  = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(22, 22, 22) };
        var tracksHdr   = SectionLabel(Loc.T("StatsTopTracks"));
        tracksHdr.Padding   = new Padding(16, 0, 0, 0);
        tracksHdr.TextAlign = ContentAlignment.MiddleLeft;
        _tracksPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        _tracksPanel.Paint += PaintTopTracks;
        rightPanel.Controls.Add(_tracksPanel);
        rightPanel.Controls.Add(tracksHdr);   // last added = topmost

        bottomSection.Controls.Add(rightPanel);
        bottomSection.Controls.Add(divider);
        bottomSection.Controls.Add(leftPanel);  // last added = leftmost

        _pageStats.Controls.Add(bottomSection);
        _pageStats.Controls.Add(gap);
        _pageStats.Controls.Add(_chartPanel);
        _pageStats.Controls.Add(chartLbl);
        _pageStats.Controls.Add(summaryRow);
        _pageStats.Controls.Add(heading);       // last added = topmost
    }

    private void LoadStatsPage()
    {
        var (total, today, week) = _db.GetStats();
        _bigTotal.Text = total.ToString("N0");
        _bigWeek.Text  = week.ToString("N0");
        _bigToday.Text = today.ToString("N0");

        _dailyData      = _db.GetDailyScrobbles(14);
        _topArtistsData = _db.GetTopArtists(8);
        _topTracksData  = _db.GetTopTracks(8);

        _chartPanel.Invalidate();
        _artistsPanel.Invalidate();
        _tracksPanel.Invalidate();
    }

    private void PaintDailyChart(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        var r = _chartPanel.ClientRectangle;
        const int pl = 24, pr = 24, pt = 8, pb = 18;
        int left = pl, top = pt, right = r.Width - pr, bottom = r.Height - pb;
        int chartW = right - left, chartH = bottom - top;

        // Fill 14-day array, missing days = 0
        var today = DateTime.Today;
        var days  = new int[14];
        foreach (var (day, count) in _dailyData)
        {
            if (DateTime.TryParse(day, out var d))
            {
                int idx = (int)(today - d).TotalDays;
                if (idx >= 0 && idx < 14) days[13 - idx] = count;
            }
        }

        int maxCount = days.Max();
        if (maxCount == 0)
        {
            using var noDataFont = new Font("Segoe UI", 9f);
            TextRenderer.DrawText(g, Loc.T("StatsNoData"), noDataFont, r, CDim,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        int slot = chartW / 14;
        int barW = Math.Max(4, slot - 4);

        using var barBrush  = new SolidBrush(_cAccent);
        using var emptyBrush = new SolidBrush(Color.FromArgb(42, 42, 42));
        using var lblFont   = new Font("Segoe UI", 7f);

        for (int i = 0; i < 14; i++)
        {
            int x    = left + i * slot + (slot - barW) / 2;
            int barH = (int)((double)days[i] / maxCount * (chartH - 18));
            int y    = bottom - 18 - barH;

            if (days[i] > 0)
                g.FillRectangle(barBrush, x, y, barW, barH);
            else
                g.FillRectangle(emptyBrush, x, bottom - 19, barW, 1);

            // Label for first, middle, last bar
            if (i == 0 || i == 7 || i == 13)
            {
                var date  = today.AddDays(i - 13);
                var label = date.ToString("M/d");
                TextRenderer.DrawText(g, label, lblFont,
                    new Rectangle(x - 8, bottom - 17, barW + 16, 16), CDim,
                    TextFormatFlags.HorizontalCenter);
            }
        }
    }

    private void PaintTopArtists(object? sender, PaintEventArgs e)
    {
        if (_topArtistsData.Length == 0) return;
        var g   = e.Graphics;
        var r   = _artistsPanel.ClientRectangle;
        int max = _topArtistsData.Max(a => a.count);
        if (max == 0) max = 1;
        const int rowH = 25, px = 16;
        int maxBarW = r.Width - px * 2 - 36;

        using var barBg       = new SolidBrush(Color.FromArgb(38, 38, 38));
        using var accentBrush = new SolidBrush(_cAccent);
        using var nameFont    = new Font("Segoe UI", 8.5f);
        using var numFont     = new Font("Segoe UI", 8f);

        for (int i = 0; i < Math.Min(8, _topArtistsData.Length); i++)
        {
            var (artist, count) = _topArtistsData[i];
            int y    = i * rowH + 4;
            int barW = (int)((double)count / max * maxBarW);

            g.FillRectangle(barBg,      px, y + 18, maxBarW, 5);
            g.FillRectangle(accentBrush, px, y + 18, barW,   5);

            TextRenderer.DrawText(g, artist, nameFont,
                new Rectangle(px, y, maxBarW, 16), CFg,
                TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, count.ToString("N0"), numFont,
                new Rectangle(r.Width - px - 32, y, 32, 16), CDim,
                TextFormatFlags.Right);
        }
    }

    private void PaintTopTracks(object? sender, PaintEventArgs e)
    {
        if (_topTracksData.Length == 0) return;
        var g = e.Graphics;
        var r = _tracksPanel.ClientRectangle;
        const int rowH = 27, px = 16;

        using var mainFont = new Font("Segoe UI", 8.5f);
        using var subFont  = new Font("Segoe UI", 7.5f);
        using var numFont  = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var sepBrush = new SolidBrush(Color.FromArgb(35, 35, 35));

        for (int i = 0; i < Math.Min(8, _topTracksData.Length); i++)
        {
            var (artist, title, count) = _topTracksData[i];
            int y = i * rowH;

            if (i > 0) g.FillRectangle(sepBrush, px, y, r.Width - px * 2, 1);

            TextRenderer.DrawText(g, $"{i + 1}", numFont,
                new Rectangle(px, y + 6, 22, 16), Color.FromArgb(70, 70, 70),
                TextFormatFlags.Right);

            int textX = px + 28;
            int textW = r.Width - textX - px - 42;
            TextRenderer.DrawText(g, title, mainFont,
                new Rectangle(textX, y + 4, textW, 14), CFg,
                TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, artist, subFont,
                new Rectangle(textX, y + 15, textW, 13), CDim,
                TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, count.ToString("N0"), subFont,
                new Rectangle(r.Width - px - 38, y + 9, 38, 14), CDim,
                TextFormatFlags.Right);
        }
    }

    private static Panel MakeStatCard(out Label bigNum, string label)
    {
        var card = new Panel { Size = new Size(182, 52), BackColor = Color.FromArgb(28, 28, 28) };
        bigNum = new Label
        {
            Location  = new Point(14, 3),
            Size      = new Size(154, 28),
            Text      = "—",
            Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = CFg,
        };
        var lbl = new Label
        {
            Location  = new Point(14, 34),
            Size      = new Size(154, 14),
            Text      = label,
            Font      = new Font("Segoe UI", 7f, FontStyle.Bold),
            ForeColor = Color.FromArgb(75, 75, 75),
        };
        card.Controls.AddRange([bigNum, lbl]);
        return card;
    }

    // ── Monitor Events ────────────────────────────────────────────────────────

    private void OnNowPlaying(object? sender, Track? track)
    {
        if (InvokeRequired) { Invoke(() => OnNowPlaying(sender, track)); return; }

        bool isNewTrack = track is null || _currentTrack is null || !track.IsSameTrack(_currentTrack);
        _currentTrack = track;

        if (track is null)
        {
            _scrobbled      = false;
            _monTitle.Text  = "—";
            _monArtist.Text = "";
            _monAlbum.Text  = "";
            SetStatus(Loc.T("NotPlaying"), CDim);
            _monBar.Value   = 0;
            _monEta.Text    = "";
            SetLoveBtn(enabled: false, loved: false);
            _albumArt.Image = null;
            return;
        }

        _monAlbum.Text = string.IsNullOrEmpty(track.Album) ? Loc.T("ResolvingAlbum") : track.Album;

        if (!isNewTrack) return;

        _scrobbled      = false;
        _startedAt      = track.DetectedAt;
        _threshMs       = _engine.GetScrobbleThresholdMs(track);
        _monTitle.Text  = track.Title;
        _monArtist.Text = track.Artist;
        SetStatus(Loc.T("WaitingToScrobble"), Color.FromArgb(200, 160, 0));
        AppendLog($"▶  {track.Artist} — {track.Title}");
        SetLoveBtn(enabled: _engine.IsAuthenticated, loved: false);
        _trackLoved = false;

        // Fetch album art asynchronously — delay to let SMTC update the thumbnail
        _albumArt.Image = null;
        _artCts?.Cancel();
        _artCts = new CancellationTokenSource();
        _ = LoadAlbumArtAsync(_artCts.Token);
    }

    private async Task LoadAlbumArtAsync(CancellationToken ct)
    {
        try
        {
            // SMTC caches the previous track's thumbnail; wait for Apple Music to push the new one
            await Task.Delay(1200, ct);
            var img = await _engine.GetCurrentThumbnailAsync();
            if (img is null || ct.IsCancellationRequested) return;
            if (InvokeRequired)
                Invoke(() => { _albumArt.Image?.Dispose(); _albumArt.Image = img; });
            else
                { _albumArt.Image?.Dispose(); _albumArt.Image = img; }
        }
        catch (OperationCanceledException) { }
    }

    private void OnScrobbled(object? sender, (Track track, bool success) e)
    {
        if (InvokeRequired) { Invoke(() => OnScrobbled(sender, e)); return; }
        _scrobbled = true;
        if (e.success)
        {
            SetStatus(Loc.T("ScrobbledOk"), Color.FromArgb(80, 200, 80));
            _monBar.Value = 100;
            AppendLog($"✓  {e.track.Artist} — {e.track.Title}  [{e.track.Album}]");
        }
        else
        {
            SetStatus(Loc.T("ScrobbleFailed"), Color.FromArgb(220, 60, 60));
            AppendLog($"✗  {e.track.Artist} — {e.track.Title}");
        }
    }

    private void OnQueueFlushed(object? sender, int count)
    {
        if (InvokeRequired) { Invoke(() => OnQueueFlushed(sender, count)); return; }
        AppendLog($"⬆  {string.Format(Loc.T("QueueFlushed"), count)}");
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_currentTrack is null || _scrobbled) return;
        if (_threshMs <= 0) return;
        var elapsed   = (DateTime.UtcNow - _startedAt).TotalMilliseconds;
        _monBar.Value = (int)Math.Min(100, elapsed / _threshMs * 100);
        var rem       = TimeSpan.FromMilliseconds(Math.Max(0, _threshMs - elapsed));
        _monEta.Text  = rem.TotalSeconds < 1 ? Loc.T("Now") : $"–{(int)rem.TotalSeconds}s";
    }

    private void SetStatus(string t, Color c) { _monStatus.Text = t; _monStatus.ForeColor = c; }

    private void SetLoveBtn(bool enabled, bool loved)
    {
        _loveBtn.Enabled   = enabled;
        _loveBtn.Text      = loved ? "♥" : "♡";
        _loveBtn.ForeColor = loved ? Color.FromArgb(220, 60, 80) : Color.FromArgb(120, 120, 120);
    }

    private async void LoveBtnClicked(object? sender, EventArgs e)
    {
        if (_currentTrack is null) return;
        _loveBtn.Enabled = false;
        _trackLoved      = !_trackLoved;
        SetLoveBtn(enabled: false, loved: _trackLoved);

        await _engine.LoveTrackAsync(_currentTrack, _trackLoved);
        AppendLog(_trackLoved
            ? $"♥  {_currentTrack.Artist} — {_currentTrack.Title}"
            : $"♡  {_currentTrack.Artist} — {_currentTrack.Title}");
        _loveBtn.Enabled = _engine.IsAuthenticated;
    }

    private void AppendLog(string msg)
    {
        _monLog.Items.Insert(0, $"{DateTime.Now:HH:mm:ss}  {msg}");
        if (_monLog.Items.Count > 50) _monLog.Items.RemoveAt(50);
    }

    // ── History Events ────────────────────────────────────────────────────────

    private async void ManualScrobbleClicked(object? sender, EventArgs e)
    {
        using var form = new ManualScrobbleForm(
            _currentTrack?.Artist, _currentTrack?.Title, _currentTrack?.Album);

        if (form.ShowDialog(this) != DialogResult.OK) return;

        bool ok = await _engine.ManualScrobbleAsync(form.Artist, form.TrackTitle, form.Album, form.PlayedAt);
        LoadHistory();
        RefreshStats();
        AppendLog(ok
            ? $"✓  Manuel: {form.Artist} — {form.TrackTitle}"
            : $"✗  Manuel başarısız: {form.Artist} — {form.TrackTitle}");
    }

    // ── Settings ──────────────────────────────────────────────────────────────

    private void LoadSettings()
    {
        _apiKeyBox.Text    = _settings.ApiKey    ?? "";
        _apiSecretBox.Text = _settings.ApiSecret ?? "";
        _threshPct.Value   = Math.Clamp(_settings.ScrobbleThresholdPercent,    10, 100);
        _threshMax.Value   = Math.Clamp(_settings.ScrobbleThresholdMaxSeconds, 30, 600);
        _dupWindow.Value   = Math.Clamp(_settings.DuplicateWindowMinutes,       0,  60);
        _filterAppleChk.Checked = _settings.FilterAppleMusicOnly;
        _editBeforeChk.Checked  = _settings.EditBeforeScrobble;
        _showNotifChk.Checked   = _settings.ShowNowPlayingNotification;
        _startWinChk.Checked    = _settings.StartWithWindows;
        _autoNormChk.Checked    = _settings.AutoNormalize;
        var langIdx = Loc.Available.ToList().FindIndex(l => l.Code == _settings.Language);
        _langCombo.SelectedIndex = langIdx >= 0 ? langIdx : 0;
        UpdateAuthStatus();
    }

    private void UpdateAuthStatus()
    {
        if (!string.IsNullOrEmpty(_settings.SessionKey))
        {
            _authStatusLabel.Text      = string.Format(Loc.T("LoggedInAs"), _settings.Username ?? "?");
            _authStatusLabel.ForeColor = Color.FromArgb(80, 200, 80);
            _authBtn.Text              = Loc.T("LogInAgain");
            _profileLink.Text          = $"last.fm/user/{_settings.Username} →";
            _profileLink.Visible       = true;
            _profileLink.Links.Clear();
            _profileLink.Links.Add(0, _profileLink.Text.Length);
        }
        else
        {
            _authStatusLabel.Text      = Loc.T("NotLoggedIn");
            _authStatusLabel.ForeColor = Color.FromArgb(220, 80, 80);
            _authBtn.Text              = Loc.T("LoginWithLastFm");
            _profileLink.Visible       = false;
        }
    }

    private void LoadRules()
    {
        _rulesGrid.Rows.Clear();
        foreach (var rule in _db.LoadRules())
        {
            var i = _rulesGrid.Rows.Add(rule.IsEnabled, rule.Field.ToString(), rule.Description, rule.Pattern, rule.Replacement);
            _rulesGrid.Rows[i].Tag = rule;
            if (rule.IsBuiltIn)
                _rulesGrid.Rows[i].DefaultCellStyle.ForeColor = Color.FromArgb(70, 70, 70);
        }
    }

    private void SaveAccountSettings()
    {
        _settings.ApiKey    = _apiKeyBox.Text.Trim();
        _settings.ApiSecret = _apiSecretBox.Text.Trim();
        _db.SaveSettings(_settings);
        _engine.UpdateSettings(_settings);
    }

    private void SaveScrobbleSettings()
    {
        _settings.ScrobbleThresholdPercent    = (int)_threshPct.Value;
        _settings.ScrobbleThresholdMaxSeconds = (int)_threshMax.Value;
        _settings.DuplicateWindowMinutes      = (int)_dupWindow.Value;
        _settings.FilterAppleMusicOnly        = _filterAppleChk.Checked;
        _settings.EditBeforeScrobble          = _editBeforeChk.Checked;
        _settings.ShowNowPlayingNotification  = _showNotifChk.Checked;
        _settings.StartWithWindows            = _startWinChk.Checked;
        if (_langCombo.SelectedIndex >= 0)
        {
            var newLang = Loc.Available[_langCombo.SelectedIndex].Code;
            if (newLang != _settings.Language)
            {
                _settings.Language = newLang;
                MessageBox.Show(Loc.T("RestartToApplyLang"), Loc.T("RestartRequired"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        _db.SaveSettings(_settings);
        _engine.UpdateSettings(_settings);
        ApplyStartWithWindows(_settings.StartWithWindows);
    }

    private void SaveNormSettings()
    {
        foreach (DataGridViewRow row in _rulesGrid.Rows)
        {
            if (row.Tag is not NormalizationRule rule) continue;
            rule.IsEnabled   = Convert.ToBoolean(row.Cells["Enabled"].Value);
            rule.Pattern     = row.Cells["Pattern"].Value?.ToString() ?? rule.Pattern;
            rule.Replacement = row.Cells["Replace"].Value?.ToString() ?? rule.Replacement;
            _db.SaveRule(rule);
        }
        _settings.AutoNormalize = _autoNormChk.Checked;
        _db.SaveSettings(_settings);
        _engine.UpdateSettings(_settings);
        _engine.ReloadRules();
    }

    private void AuthClicked(object? sender, EventArgs e)
    {
        var key = _apiKeyBox.Text.Trim();
        var sec = _apiSecretBox.Text.Trim();
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(sec))
        {
            MessageBox.Show(Loc.T("MsgEnterApiKey"), Loc.T("TitleMissingInfo"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _engine.LastFmClient.Configure(key, sec, null);
        using var af = new AuthForm(_engine.LastFmClient);
        if (af.ShowDialog(this) == DialogResult.OK)
        {
            _settings.ApiKey     = key;
            _settings.ApiSecret  = sec;
            _settings.SessionKey = af.SessionKey;
            _settings.Username   = af.Username;
            _db.SaveSettings(_settings);
            _engine.UpdateSettings(_settings);
            UpdateAuthStatus();
        }
    }

    private void AddRuleClicked(object? sender, EventArgs e)
    {
        _db.SaveRule(new NormalizationRule { Field = RuleField.Title, Pattern = @"\s*\(example\)", Replacement = "", Description = Loc.T("NewRule"), IsEnabled = true });
        LoadRules();
    }

    private void DeleteRuleClicked(object? sender, EventArgs e)
    {
        foreach (DataGridViewRow row in _rulesGrid.SelectedRows)
            if (row.Tag is NormalizationRule rule && !rule.IsBuiltIn)
                _db.DeleteRule(rule.Id);
        LoadRules();
    }

    // ── Accent Color ──────────────────────────────────────────────────────────

    private void PickAccentColorClicked(object? sender, EventArgs e)
    {
        using var dlg = new ColorDialog
        {
            Color            = _cAccent,
            FullOpen         = true,
            AllowFullOpen    = true,
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        UpdateAccentColor(dlg.Color);
        _settings.AccentColor = ColorTranslator.ToHtml(dlg.Color);
        _db.SaveSettings(_settings);
    }

    private void UpdateAccentColor(Color c)
    {
        _cAccent             = c;
        _accentBar.BackColor = c;
        _monBar.ForeColor    = c;
        foreach (var btn in _accentBtns)
            btn.BackColor = c;
        if (_activeNavBtn is not null)
            _activeNavBtn.BackColor = c;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static NavButton NavBtn(string text, string icon) => new(icon, text);

    private static Label PageHeading(string text) => new()
    {
        Text      = text,
        Height    = 50,
        Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
        ForeColor = CFg,
        Padding   = new Padding(24, 0, 0, 10),
        TextAlign = ContentAlignment.BottomLeft,
    };

    private static Label SectionLabel(string text) => new()
    {
        Text      = text,
        Dock      = DockStyle.Top,
        Height    = 20,
        Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
        ForeColor = Color.FromArgb(70, 70, 70),
        TextAlign = ContentAlignment.BottomLeft,
    };

    private static Label StatLabel(string text) => new()
    {
        Text      = text,
        AutoSize  = true,
        Font      = new Font("Segoe UI", 8.5f),
        ForeColor = CDim,
    };

    private static Label Gap(int h) => new() { Dock = DockStyle.Top, Height = h };

    private static TextBox MakeInput(int w, int h) => new()
    {
        Size        = new Size(w, h),
        BackColor   = CInput,
        ForeColor   = CFg,
        BorderStyle = BorderStyle.FixedSingle,
        Font        = new Font("Segoe UI", 9f),
    };

    private static Button MakeBtn(string text, int w, int h) => new()
    {
        Text                    = text,
        Size                    = new Size(w, h),
        FlatStyle               = FlatStyle.Flat,
        BackColor               = Color.FromArgb(40, 40, 40),
        ForeColor               = CFg,
        Cursor                  = Cursors.Hand,
        Font                    = new Font("Segoe UI", 9f),
        UseVisualStyleBackColor = false,
    };

    private static CheckBox MakeChk(string text) => new()
    {
        Text      = text,
        AutoSize  = true,
        ForeColor = CFg,
        Font      = new Font("Segoe UI", 9f),
    };

    private static Label RowLabel(string text, int x, int y) => new()
    {
        Text      = text,
        Location  = new Point(x, y),
        Size      = new Size(120, 20),
        ForeColor = CDim,
        Font      = new Font("Segoe UI", 9f),
    };

    private static Color ColorFromHex(string? hex, Color fallback)
    {
        if (string.IsNullOrEmpty(hex)) return fallback;
        try { return ColorTranslator.FromHtml(hex); }
        catch { return fallback; }
    }

    private static void ApplyStartWithWindows(bool enable)
    {
        try
        {
            var exe = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (exe is null) return;
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true);
            if (key is null) return;
            if (enable) key.SetValue("LastFmScrobbler", $"\"{exe}\"");
            else        key.DeleteValue("LastFmScrobbler", throwOnMissingValue: false);
        }
        catch { }
    }

    private static void OpenUrl(string url)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }

    // ── Custom title bar ──────────────────────────────────────────────────────

    private Panel BuildTitleBar()
    {
        var bar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 48,
            BackColor = Color.FromArgb(10, 10, 10),
        };

        var logoPane = new Panel
        {
            Dock      = DockStyle.Left,
            Width     = 150,
            BackColor = CSidebar,
        };
        var logoLbl = new Label
        {
            Text      = "last.fm Scrobbler",
            Dock      = DockStyle.Fill,
            ForeColor = _cAccent,
            Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(16, 0, 0, 0),
            BackColor = Color.Transparent,
        };
        logoPane.Controls.Add(logoLbl);

        var rightPane = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.FromArgb(18, 18, 18),
        };

        var closeBtn = TitleBtn("✕", 46);
        closeBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(196, 43, 28);
        closeBtn.FlatAppearance.MouseDownBackColor = Color.FromArgb(150, 25, 10);
        closeBtn.MouseEnter += (_, _) => closeBtn.ForeColor = Color.White;
        closeBtn.MouseLeave += (_, _) => closeBtn.ForeColor = Color.FromArgb(160, 160, 160);
        closeBtn.Click += (_, _) => Close();

        _maxBtn = TitleBtn("□", 40);
        _maxBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 40, 40);
        _maxBtn.Click += (_, _) =>
        {
            if (WindowState == FormWindowState.Maximized)
                WindowState = FormWindowState.Normal;
            else
            {
                MaximizedBounds = Screen.GetWorkingArea(this);
                WindowState = FormWindowState.Maximized;
            }
        };

        var minBtn = TitleBtn("─", 40);
        minBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 40, 40);
        minBtn.Click += (_, _) => WindowState = FormWindowState.Minimized;

        rightPane.Controls.Add(minBtn);
        rightPane.Controls.Add(_maxBtn);
        rightPane.Controls.Add(closeBtn);

        MouseEventHandler drag = (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            ReleaseCapture();
            SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero);
        };
        bar.MouseDown       += drag;
        logoPane.MouseDown  += drag;
        logoLbl.MouseDown   += drag;
        rightPane.MouseDown += drag;

        bar.DoubleClick       += (_, _) => _maxBtn.PerformClick();
        rightPane.DoubleClick += (_, _) => _maxBtn.PerformClick();

        bar.Controls.Add(rightPane);
        bar.Controls.Add(logoPane);

        return bar;
    }

    private static Button TitleBtn(string symbol, int width) => new()
    {
        Text                    = symbol,
        Size                    = new Size(width, 48),
        Dock                    = DockStyle.Right,
        FlatStyle               = FlatStyle.Flat,
        ForeColor               = Color.FromArgb(160, 160, 160),
        BackColor               = Color.Transparent,
        Font                    = new Font("Segoe UI", 9.5f),
        UseVisualStyleBackColor = false,
        Cursor                  = Cursors.Default,
        FlatAppearance          = { BorderSize = 0, MouseDownBackColor = Color.FromArgb(35, 35, 35) },
    };

    // ── Resize + shadow ───────────────────────────────────────────────────────

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
            return cp;
        }
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_NCHITTEST  = 0x84;
        const int HTLEFT        = 10, HTRIGHT      = 11;
        const int HTTOPLEFT     = 13, HTTOPRIGHT   = 14;
        const int HTBOTTOM      = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;
        const int grip = 6;

        if (m.Msg == WM_NCHITTEST)
        {
            var p = PointToClient(Cursor.Position);
            bool l = p.X < grip,               r = p.X >= ClientSize.Width  - grip;
            bool t = p.Y < grip,               b = p.Y >= ClientSize.Height - grip;

            if (t && l) { m.Result = (IntPtr)HTTOPLEFT;     return; }
            if (t && r) { m.Result = (IntPtr)HTTOPRIGHT;    return; }
            if (b && l) { m.Result = (IntPtr)HTBOTTOMLEFT;  return; }
            if (b && r) { m.Result = (IntPtr)HTBOTTOMRIGHT; return; }
            if (l)      { m.Result = (IntPtr)HTLEFT;        return; }
            if (r)      { m.Result = (IntPtr)HTRIGHT;       return; }
            if (b)      { m.Result = (IntPtr)HTBOTTOM;      return; }
        }
        base.WndProc(ref m);
    }

    // ── NavButton ─────────────────────────────────────────────────────────────

    private sealed class NavButton : Button
    {
        private static readonly Color CHover = Color.FromArgb(30, 30, 30);
        private static readonly Color CBg    = Color.FromArgb(15, 15, 15);

        private readonly string _icon;
        private bool _hovered;

        public NavButton(string icon, string label)
        {
            _icon     = icon;
            Text      = label;
            Dock      = DockStyle.Top;
            Height    = 44;
            FlatStyle = FlatStyle.Flat;
            ForeColor = CDim;
            BackColor = Color.Transparent;
            Font      = new Font("Segoe UI", 9.5f);
            Cursor    = Cursors.Hand;
            UseVisualStyleBackColor = false;
            FlatAppearance.BorderSize = 0;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e) { _hovered = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var bg = BackColor == Color.Transparent ? (_hovered ? CHover : CBg) : BackColor;
            e.Graphics.Clear(bg);
            var tf = TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;
            TextRenderer.DrawText(e.Graphics, _icon, Font, new Rectangle(12, 0, 26, Height), ForeColor, tf | TextFormatFlags.HorizontalCenter);
            TextRenderer.DrawText(e.Graphics, Text,  Font, new Rectangle(44, 0, Width - 48, Height), ForeColor, tf | TextFormatFlags.Left);
        }
    }

    // ── P/Invoke ──────────────────────────────────────────────────────────────

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    // ── Cleanup ───────────────────────────────────────────────────────────────

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _engine.NowPlayingChanged   -= OnNowPlaying;
        _engine.TrackScrobbled      -= OnScrobbled;
        _engine.PendingQueueFlushed -= OnQueueFlushed;
        _tick.Stop();
        _tick.Dispose();
        base.OnFormClosed(e);
    }
}
