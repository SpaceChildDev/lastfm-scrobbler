using System.Drawing.Drawing2D;

namespace LastFmScrobbler.UI;

public class ProfileCard : Panel
{
    private static readonly Color CBg     = Color.FromArgb(15, 15, 15);
    private static readonly Color CHover  = Color.FromArgb(22, 22, 22);
    private static readonly Color CFg     = Color.FromArgb(220, 220, 220);
    private static readonly Color CDim    = Color.FromArgb(110, 110, 110);
    private static readonly Color CSep    = Color.FromArgb(28, 28, 28);
    private static readonly Color CCircle = Color.FromArgb(30, 30, 30);

    private bool   _hovered;
    private string _username = "";
    private string _subtitle = "Sign in to Last.fm";
    private bool   _loggedIn;
    private Image? _avatar;

    public ProfileCard()
    {
        Height          = 60;
        BackColor       = CBg;
        Cursor          = Cursors.Hand;
        DoubleBuffered  = true;
        SetStyle(ControlStyles.UserPaint
               | ControlStyles.AllPaintingInWmPaint
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.ResizeRedraw, true);
    }

    public void SetState(bool loggedIn, string username, string subtitle, Image? avatar)
    {
        _loggedIn = loggedIn;
        _username = username ?? "";
        _subtitle = subtitle ?? "";
        _avatar?.Dispose();
        _avatar   = avatar;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) { _hovered = true;  Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var bg = new SolidBrush(_hovered ? CHover : CBg))
            g.FillRectangle(bg, ClientRectangle);

        using (var sep = new Pen(CSep))
            g.DrawLine(sep, 0, 0, Width, 0);

        const int avatarSize = 34;
        var avatarRect = new Rectangle(14, (Height - avatarSize) / 2, avatarSize, avatarSize);

        if (_avatar is not null)
        {
            using var path = new GraphicsPath();
            path.AddEllipse(avatarRect);
            g.SetClip(path);
            g.DrawImage(_avatar, avatarRect);
            g.ResetClip();
            using var border = new Pen(Color.FromArgb(45, 45, 45));
            g.DrawEllipse(border, avatarRect);
        }
        else
        {
            using var fill = new SolidBrush(CCircle);
            g.FillEllipse(fill, avatarRect);
            var initial = _loggedIn && _username.Length > 0
                ? _username[0].ToString().ToUpperInvariant()
                : "?";
            using var font = FontManager.Bold(13f);
            using var brush = new SolidBrush(CDim);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(initial, font, brush, avatarRect, sf);
        }

        int textX     = avatarRect.Right + 12;
        int textWidth = Math.Max(20, Width - textX - 12);

        var nameRect = new Rectangle(textX, 12, textWidth, 18);
        var subRect  = new Rectangle(textX, 32, textWidth, 16);

        var nameText = _loggedIn ? _username : "Sign in";
        TextRenderer.DrawText(g, nameText, FontManager.Bold(9.5f),
            nameRect, CFg, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

        TextRenderer.DrawText(g, _subtitle, FontManager.Regular(8f),
            subRect, CDim, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
    }
}
