using System.Drawing;
using System.Windows.Forms;

namespace WhackAMole;

public class LegendPanel : Control
{
    private PixelTargetRenderer _renderer;

    public LegendPanel()
    {
        _renderer = new PixelTargetRenderer();
        BackColor = Color.FromArgb(190, 204, 135);
        ForeColor = Color.FromArgb(28, 43, 29);
        DoubleBuffered = true;
        Height = 136;
        AccessibleName = "第二關計分規則：灰色老鼠加一分，棕色倉鼠加三分，炸彈扣兩分";
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.Clear(BackColor);
        using Pen border = new Pen(ForeColor, 2);
        using Font titleFont = new Font("Microsoft JhengHei UI", 13, FontStyle.Bold);
        using Font rowFont = new Font("Microsoft JhengHei UI", 12, FontStyle.Bold);
        using SolidBrush textBrush = new SolidBrush(ForeColor);
        e.Graphics.DrawRectangle(border, 1, 1, Width - 3, Height - 3);
        e.Graphics.DrawString("第二關計分規則", titleFont, textBrush, 10, 6);
        DrawRule(e.Graphics, TargetKind.Mouse, "灰色老鼠  +1", 34, rowFont, textBrush);
        DrawRule(e.Graphics, TargetKind.Hamster, "棕色倉鼠  +3", 67, rowFont, textBrush);
        DrawRule(e.Graphics, TargetKind.Bomb, "炸彈  −2", 100, rowFont, textBrush);
    }

    private void DrawRule(Graphics graphics, TargetKind kind, string text, int top,
        Font font, Brush brush)
    {
        _renderer.Draw(graphics, kind, new Rectangle(12, top, 32, 32));
        graphics.DrawString(text, font, brush, 53, top + 2);
    }
}
