using System.Drawing.Drawing2D;
using WhackAMole.Core;

namespace WhackAMole.WinForms;

internal sealed class HoleButton : Button
{
    private TargetKind? target;

    public HoleButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.FromArgb(224, 234, 215);
        DoubleBuffered = true;
        TextAlign = ContentAlignment.BottomCenter;
    }

    public void ShowTarget(TargetKind? value)
    {
        if (target == value && AccessibleName is not null) return;
        target = value;
        Text = value switch
        {
            TargetKind.Mouse => "灰色老鼠",
            TargetKind.Hamster => "棕色倉鼠",
            TargetKind.Bomb => "炸彈",
            _ => ""
        };
        AccessibleName = $"{Name} {(value is null ? "空洞" : Text)}";
        Enabled = value is not null;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var graphicHeight = Math.Max(12, Height - Font.Height - 10);
        var size = Math.Min(Width * .45F, graphicHeight * .78F);
        var x = (Width - size) / 2;
        var y = Math.Max(7, (graphicHeight - size) / 2);
        using var soil = new SolidBrush(Color.FromArgb(91, 79, 62));
        g.FillEllipse(soil, Width * .18F, graphicHeight * .70F, Width * .64F, Math.Max(4, graphicHeight * .23F));
        if (target is null) return;
        using var body = new SolidBrush(target switch
        {
            TargetKind.Mouse => Color.FromArgb(139, 148, 150),
            TargetKind.Hamster => Color.FromArgb(178, 115, 65),
            _ => Color.FromArgb(47, 54, 57)
        });
        if (target != TargetKind.Bomb)
        {
            g.FillEllipse(body, x - size * .10F, y - size * .08F, size * .4F, size * .4F);
            g.FillEllipse(body, x + size * .70F, y - size * .08F, size * .4F, size * .4F);
        }
        else
        {
            using var fuse = new Pen(Color.DarkOrange, 3);
            g.DrawLine(fuse, x + size / 2, y + 2, x + size * .7F, y - 7);
        }
        g.FillEllipse(body, x, y, size, size);
        if (target != TargetKind.Bomb)
        {
            g.FillEllipse(Brushes.White, x + size * .21F, y + size * .30F, size * .18F, size * .22F);
            g.FillEllipse(Brushes.White, x + size * .61F, y + size * .30F, size * .18F, size * .22F);
            g.FillEllipse(Brushes.Black, x + size * .27F, y + size * .38F, size * .07F, size * .10F);
            g.FillEllipse(Brushes.Black, x + size * .67F, y + size * .38F, size * .07F, size * .10F);
            g.FillEllipse(Brushes.MistyRose, x + size * .42F, y + size * .61F, size * .16F, size * .13F);
        }
        else
        {
            using var markFont = new Font(Font.FontFamily, Math.Max(10, size * .38F), FontStyle.Bold, GraphicsUnit.Pixel);
            g.DrawString("!", markFont, Brushes.White, x + size * .40F, y + size * .25F);
        }
    }
}
