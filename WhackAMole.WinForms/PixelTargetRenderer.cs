using System.Drawing;

namespace WhackAMole;

public class PixelTargetRenderer
{
    private const int PIXELS = 16;

    public void Draw(Graphics graphics, TargetKind kind, Rectangle bounds)
    {
        int cell = Math.Max(1, Math.Min(bounds.Width, bounds.Height) / PIXELS);
        int originX = bounds.X + (bounds.Width - cell * PIXELS) / 2;
        int originY = bounds.Y + (bounds.Height - cell * PIXELS) / 2;

        for (int y = 0; y < PIXELS; y++)
        {
            for (int x = 0; x < PIXELS; x++)
            {
                if (IsFilled(kind, x, y))
                {
                    using SolidBrush brush = new SolidBrush(GetColor(kind, x, y));
                    graphics.FillRectangle(brush,
                        originX + x * cell, originY + y * cell, cell, cell);
                }
            }
        }
    }

    public bool HitTest(TargetKind kind, Rectangle bounds, Point location)
    {
        int cell = Math.Max(1, Math.Min(bounds.Width, bounds.Height) / PIXELS);
        int originX = bounds.X + (bounds.Width - cell * PIXELS) / 2;
        int originY = bounds.Y + (bounds.Height - cell * PIXELS) / 2;
        int x = (location.X - originX) / cell;
        int y = (location.Y - originY) / cell;
        if (location.X < originX || location.Y < originY ||
            x < 0 || x >= PIXELS || y < 0 || y >= PIXELS)
        {
            return false;
        }

        return IsFilled(kind, x, y);
    }

    private bool IsFilled(TargetKind kind, int x, int y)
    {
        if (kind == TargetKind.Bomb)
        {
            bool body = Square(x - 8) + Square(y - 9) <= 30;
            bool fuse = (x == 8 || x == 9) && y >= 2 && y <= 4;
            bool spark = (x == 10 && y == 1) || (x == 11 && y == 2) ||
                (x == 10 && y == 3) || (x == 12 && y == 1);
            return body || fuse || spark;
        }

        bool torso = Square(x - 8) * 5 + Square(y - 10) * 4 <= 160;
        bool leftEar = Square(x - 4) + Square(y - 4) <= 7;
        bool rightEar = Square(x - 12) + Square(y - 4) <= 7;
        if (kind == TargetKind.Mouse)
        {
            bool tail = (x == 13 && y >= 10 && y <= 13) ||
                (x == 14 && y >= 8 && y <= 11);
            return torso || leftEar || rightEar || tail;
        }

        bool leftCheek = Square(x - 4) + Square(y - 10) <= 10;
        bool rightCheek = Square(x - 12) + Square(y - 10) <= 10;
        return torso || leftEar || rightEar || leftCheek || rightCheek;
    }

    private Color GetColor(TargetKind kind, int x, int y)
    {
        if (kind == TargetKind.Bomb)
        {
            if (y <= 3 && x >= 10)
            {
                return Color.FromArgb(243, 194, 53);
            }

            if (x <= 5 && y <= 9)
            {
                return Color.FromArgb(91, 94, 87);
            }

            return Color.FromArgb(35, 39, 34);
        }

        if ((x == 6 || x == 10) && y == 8)
        {
            return Color.FromArgb(31, 38, 29);
        }

        if (x == 8 && y == 10)
        {
            return Color.FromArgb(55, 42, 37);
        }

        if (kind == TargetKind.Mouse)
        {
            if ((y <= 6 && (x <= 5 || x >= 11)) || (x >= 6 && x <= 10 && y >= 11))
            {
                return Color.FromArgb(205, 207, 196);
            }

            return Color.FromArgb(132, 138, 132);
        }

        if (x >= 6 && x <= 10 && y >= 8)
        {
            return Color.FromArgb(235, 207, 151);
        }

        return Color.FromArgb(165, 105, 67);
    }

    private int Square(int value)
    {
        return value * value;
    }
}
