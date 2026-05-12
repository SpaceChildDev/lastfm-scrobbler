using System.Drawing.Drawing2D;
using System.Drawing.Text;
using LastFmScrobbler.Models;

namespace LastFmScrobbler.UI;

public class HistoryList : ListBox
{
    private const int RowHeight = 52;

    public Color Accent { get; set; } = Color.FromArgb(186, 0, 0);

    public HistoryList()
    {
        DrawMode       = DrawMode.OwnerDrawFixed;
        ItemHeight     = RowHeight;
        BackColor      = Color.FromArgb(18, 18, 18);
        BorderStyle    = BorderStyle.None;
        SelectionMode  = SelectionMode.One;
        Font           = FontManager.Regular(9f);
        IntegralHeight = false;
    }

    public void Load(IEnumerable<ScrobbleRecord> records)
    {
        BeginUpdate();
        Items.Clear();
        foreach (var r in records) Items.Add(r);
        EndUpdate();
    }

    public ScrobbleRecord? SelectedRecord => SelectedItem as ScrobbleRecord;

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= Items.Count) return;
        if (Items[e.Index] is not ScrobbleRecord rec) return;

        var g = e.Graphics;
        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var b        = e.Bounds;
        bool selected = (e.State & DrawItemState.Selected) != 0;
        bool even     = e.Index % 2 == 0;

        var bgColor = selected
            ? Color.FromArgb(36, 36, 36)
            : even ? Color.FromArgb(22, 22, 22) : Color.FromArgb(20, 20, 20);

        using (var bg = new SolidBrush(bgColor))
            g.FillRectangle(bg, b);

        // Status dot
        const int dotSize = 7;
        var dotColor = rec.Success ? Color.FromArgb(60, 180, 60) : Color.FromArgb(190, 55, 55);
        var dotRect  = new Rectangle(b.X + 18, b.Y + (b.Height - dotSize) / 2, dotSize, dotSize);
        using (var brush = new SolidBrush(dotColor))
            g.FillEllipse(brush, dotRect);

        // Timestamp (right-aligned)
        var timeStr = FormatTime(rec.ScrobbledAt.ToLocalTime());
        using var tf = FontManager.Regular(7.5f);
        var tSize   = TextRenderer.MeasureText(timeStr, tf);
        int right   = b.Right - 20;
        using (var tb = new SolidBrush(Color.FromArgb(55, 55, 55)))
            g.DrawString(timeStr, tf, tb, right - tSize.Width, b.Y + 10);

        int textRight = right - tSize.Width - 8;
        int textX     = dotRect.Right + 14;
        var sf        = new StringFormat
        {
            Trimming    = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap,
        };

        // Line 1: Artist — Title
        var line1   = string.IsNullOrEmpty(rec.Artist) ? rec.Title : $"{rec.Artist}  —  {rec.Title}";
        var fgColor = rec.Success ? Color.FromArgb(205, 205, 205) : Color.FromArgb(170, 85, 85);
        using var lf1 = FontManager.Bold(9.5f);
        using (var lb1 = new SolidBrush(fgColor))
            g.DrawString(line1, lf1, lb1, new RectangleF(textX, b.Y + 9, textRight - textX, 19), sf);

        // Line 2: Album (dim)
        if (!string.IsNullOrEmpty(rec.Album))
        {
            using var lf2 = FontManager.Regular(8.5f);
            using (var lb2 = new SolidBrush(Color.FromArgb(68, 68, 68)))
                g.DrawString(rec.Album, lf2, lb2, new RectangleF(textX, b.Y + 30, textRight - textX, 15), sf);
        }

        // Separator
        using (var pen = new Pen(Color.FromArgb(27, 27, 27)))
            g.DrawLine(pen, b.X + 12, b.Bottom - 1, b.Right - 12, b.Bottom - 1);
    }

    private static string FormatTime(DateTime dt)
    {
        var now = DateTime.Now;
        if (dt.Date == now.Date)             return dt.ToString("HH:mm");
        if (dt.Date == now.Date.AddDays(-1)) return $"Dün · {dt:HH:mm}";
        if ((now - dt).TotalDays < 6)        return dt.ToString("ddd · HH:mm");
        return dt.ToString("dd/MM · HH:mm");
    }
}
