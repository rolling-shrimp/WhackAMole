using System.Drawing;
using System.Windows.Forms;

namespace WhackAMole;

public class ConsoleShell : Panel
{
    public ConsoleShell()
    {
        BackColor = Color.FromArgb(219, 220, 216);
        Padding = new Padding(76, 28, 76, 28);
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using SolidBrush key = new SolidBrush(Color.FromArgb(139, 141, 135));
        using SolidBrush keyDark = new SolidBrush(Color.FromArgb(89, 92, 87));
        using Pen border = new Pen(Color.FromArgb(96, 101, 95), 3);
        int middle = Height / 2;
        e.Graphics.FillRectangle(key, 16, middle - 12, 48, 24);
        e.Graphics.FillRectangle(key, 28, middle - 24, 24, 48);
        e.Graphics.FillEllipse(keyDark, Width - 62, middle - 28, 34, 34);
        e.Graphics.FillEllipse(keyDark, Width - 73, middle + 15, 34, 34);
        e.Graphics.DrawRectangle(border, 2, 2, Width - 5, Height - 5);
    }
}
