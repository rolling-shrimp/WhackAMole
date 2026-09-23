using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WhackAMole;

public class GameBoard : Control
{
    private PixelTargetRenderer _renderer;
    private TargetState? _target;
    private bool _isInteractive;

    public GameBoard()
    {
        _renderer = new PixelTargetRenderer();
        _isInteractive = false;
        BackColor = Color.FromArgb(177, 194, 122);
        DoubleBuffered = true;
        AccessibleName = "4×4 打地鼠洞口";
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    public bool IsInteractive
    {
        get { return _isInteractive; }
        set
        {
            _isInteractive = value;
            Invalidate();
        }
    }

    public TargetState? RenderedTarget
    {
        get { return _target; }
    }

    public void Render(TargetState? target)
    {
        _target = target;
        Invalidate();
    }

    public Rectangle GetTargetBounds(int holeId)
    {
        Rectangle hole = GetHoleBounds(holeId);
        int size = (int)Math.Round(hole.Width * 1.12);
        return new Rectangle(hole.X + (hole.Width - size) / 2,
            hole.Y + (hole.Height - size) / 2 - size / 8, size, size);
    }

    public bool TryGetClickedTarget(Point location, out int holeId, out long generation)
    {
        holeId = 0;
        generation = 0;
        if (!_isInteractive || _target == null)
        {
            return false;
        }

        Rectangle bounds = GetTargetBounds(_target.HoleId);
        if (!_renderer.HitTest(_target.Kind, bounds, location))
        {
            return false;
        }

        holeId = _target.HoleId;
        generation = _target.Generation;
        return true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.None;
        e.Graphics.Clear(BackColor);
        using Pen border = new Pen(Color.FromArgb(31, 43, 29), 4);
        using SolidBrush shadow = new SolidBrush(Color.FromArgb(36, 45, 31));
        using SolidBrush rim = new SolidBrush(Color.FromArgb(75, 88, 50));
        e.Graphics.DrawRectangle(border, 3, 3, Width - 7, Height - 7);

        for (int holeId = 1; holeId <= 16; holeId++)
        {
            Rectangle hole = GetHoleBounds(holeId);
            DrawPixelHole(e.Graphics, hole, rim, shadow);
        }

        if (_target != null)
        {
            _renderer.Draw(e.Graphics, _target.Kind, GetTargetBounds(_target.HoleId));
        }
    }

    private void DrawPixelHole(Graphics graphics, Rectangle bounds,
        Brush rim, Brush shadow)
    {
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                double dx = x - 7.5;
                double dy = y - 7.5;
                double distance = dx * dx + dy * dy;
                if (distance > 54)
                {
                    continue;
                }

                int left = bounds.Left + x * bounds.Width / 16;
                int right = bounds.Left + (x + 1) * bounds.Width / 16;
                int top = bounds.Top + y * bounds.Height / 16;
                int bottom = bounds.Top + (y + 1) * bounds.Height / 16;
                graphics.FillRectangle(distance <= 39 ? shadow : rim,
                    left, top, right - left, bottom - top);
            }
        }
    }

    private Rectangle GetHoleBounds(int holeId)
    {
        int index = Math.Clamp(holeId - 1, 0, 15);
        int row = index / 4;
        int column = index % 4;
        int padding = Math.Max(16, Math.Min(Width, Height) / 26);
        int cellWidth = Math.Max(1, (Width - padding * 2) / 4);
        int cellHeight = Math.Max(1, (Height - padding * 2) / 4);
        int diameter = Math.Max(12, (int)(Math.Min(cellWidth, cellHeight) * 0.73));
        int x = padding + column * cellWidth + (cellWidth - diameter) / 2;
        int y = padding + row * cellHeight + (cellHeight - diameter) / 2;
        return new Rectangle(x, y, diameter, diameter);
    }
}
