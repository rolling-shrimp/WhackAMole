using System.Drawing;
using System.Windows.Forms;

namespace WhackAMole;

public class ScoreCard : Control
{
    private int _levelNumber;
    private bool _showTime;
    private string _heading;
    private LevelState? _state;

    public ScoreCard(int levelNumber, bool showTime)
    {
        _levelNumber = levelNumber;
        _showTime = showTime;
        _heading = levelNumber == 1 ? "第一關" : "第二關";
        BackColor = Color.FromArgb(190, 204, 135);
        ForeColor = Color.FromArgb(29, 43, 28);
        DoubleBuffered = true;
        Height = levelNumber == 1 ? (showTime ? 150 : 122) : (showTime ? 207 : 179);
    }

    public LevelState? CurrentState
    {
        get { return _state; }
    }

    public void SetHeading(string heading)
    {
        _heading = heading;
        Invalidate();
    }

    public void SetState(LevelState state)
    {
        _state = state;
        AccessibleName = $"{_heading}，{state.Points} 分，灰色老鼠 {state.MouseHits}" +
            (state.LevelNumber == 2 ?
                $"，棕色倉鼠 {state.HamsterHits}，炸彈 {state.BombHits}" : "");
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.Clear(BackColor);
        using Pen border = new Pen(ForeColor, 3);
        using SolidBrush ink = new SolidBrush(ForeColor);
        using Font titleFont = new Font("Microsoft JhengHei UI", 18, FontStyle.Bold);
        using Font scoreFont = new Font("Microsoft JhengHei UI", 22, FontStyle.Bold);
        using Font rowFont = new Font("Microsoft JhengHei UI", 14, FontStyle.Bold);
        e.Graphics.DrawRectangle(border, 2, 2, Width - 5, Height - 5);
        e.Graphics.DrawString(_heading, titleFont, ink, 12, 7);
        if (_state == null)
        {
            return;
        }

        int y = 40;
        if (_showTime)
        {
            int seconds = (int)Math.Ceiling(_state.Remaining.TotalSeconds);
            DrawRow(e.Graphics, rowFont, ink, "時間", seconds.ToString(), y);
            y += 27;
        }

        DrawRow(e.Graphics, scoreFont, ink, "得分", $"{_state.Points} 分", y);
        y += 36;
        DrawRow(e.Graphics, rowFont, ink, "灰色老鼠", _state.MouseHits.ToString(), y);
        y += 24;
        if (_levelNumber == 2)
        {
            DrawRow(e.Graphics, rowFont, ink, "棕色倉鼠", _state.HamsterHits.ToString(), y);
            y += 24;
            DrawRow(e.Graphics, rowFont, ink, "炸彈", _state.BombHits.ToString(), y);
        }
    }

    private void DrawRow(Graphics graphics, Font font, Brush brush,
        string label, string value, int y)
    {
        graphics.DrawString(label, font, brush, 12, y);
        SizeF valueSize = graphics.MeasureString(value, font);
        graphics.DrawString(value, font, brush,
            Math.Max(12, Width - valueSize.Width - 15), y);
    }
}
