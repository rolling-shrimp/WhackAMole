using WhackAMole.Core;

namespace WhackAMole.WinForms;

public sealed class MainWindow : Form
{
    private readonly Game game = new();
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 25 };
    private readonly Label status = new();
    private readonly Label countdown = new();
    private readonly Label firstScore = new();
    private readonly Label secondScore = new();
    private readonly Button action = new();
    private readonly HoleButton[] holes = new HoleButton[16];

    public MainWindow()
    {
        Text = "打地鼠 · 兩關挑戰";
        Name = "WhackAMoleWindow";
        ClientSize = new Size(860, 820);
        MinimumSize = new Size(860, 820);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(244, 247, 241);
        ForeColor = Color.FromArgb(35, 58, 48);
        Font = new Font("Microsoft JhengHei UI", 10F);
        AutoScaleMode = AutoScaleMode.Dpi;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(28, 20, 28, 20),
            ColumnCount = 1, RowCount = 6
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 3; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        var title = new Label
        {
            Text = "打地鼠", Dock = DockStyle.Fill, AutoSize = true,
            Font = new Font(Font.FontFamily, 26, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(title, 0, 0);
        var heading = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 8, 0, 8) };
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        SetLabel(status, "Status", ContentAlignment.MiddleLeft);
        SetLabel(countdown, "Countdown", ContentAlignment.MiddleRight);
        heading.Controls.Add(status, 0, 0);
        heading.Controls.Add(countdown, 1, 0);
        layout.Controls.Add(heading, 0, 1);

        var scores = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 8, 0, 12) };
        scores.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        scores.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        SetLabel(firstScore, "FirstScore", ContentAlignment.MiddleLeft);
        SetLabel(secondScore, "SecondScore", ContentAlignment.MiddleLeft);
        scores.Controls.Add(firstScore, 0, 0);
        scores.Controls.Add(secondScore, 1, 0);
        layout.Controls.Add(scores, 0, 2);

        var board = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 4 };
        for (var i = 0; i < 4; i++)
        {
            board.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            board.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        }
        for (var i = 0; i < holes.Length; i++)
        {
            var hole = i;
            holes[i] = new HoleButton { Dock = DockStyle.Fill, Margin = new Padding(5), Name = $"Hole{i + 1:00}" };
            holes[i].Click += (_, _) => { game.Hit(hole); Render(); };
            board.Controls.Add(holes[i], i % 4, i / 4);
        }
        layout.Controls.Add(board, 0, 3);
        action.Name = "Action";
        action.AccessibleName = "開始或重玩";
        action.Size = new Size(180, 48);
        action.Anchor = AnchorStyles.None;
        action.FlatStyle = FlatStyle.Flat;
        action.BackColor = Color.FromArgb(38, 101, 73);
        action.ForeColor = Color.White;
        action.Click += (_, _) =>
        {
            if (game.Phase is Phase.ReadyFirst or Phase.ReadySecond) game.Start();
            else game.Replay();
            Render();
        };
        layout.Controls.Add(action, 0, 4);
        layout.Controls.Add(new Label
        {
            Text = "灰色老鼠 +1　 ·　 棕色倉鼠 +3　 ·　 炸彈 −2（最低 0 分）",
            Dock = DockStyle.Fill, AutoSize = true, TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Font.FontFamily, 10F)
        }, 0, 5);
        timer.Tick += (_, _) => { game.Tick(); Render(); };
        Render();
        timer.Start();
    }

    private static void SetLabel(Label label, string name, ContentAlignment alignment)
    {
        label.Name = name;
        label.AutoSize = true;
        label.AccessibleName = name;
        label.Dock = DockStyle.Fill;
        label.TextAlign = alignment;
    }

    private void Render()
    {
        status.Text = game.Phase switch
        {
            Phase.ReadyFirst => "準備挑戰第一關 · 目標 42 分",
            Phase.PlayingFirst => "第一關 · 目標 42 分",
            Phase.ReadySecond => $"第一關過關！按開始挑戰第二關\n第一關分數：{game.First.Points} · 命中灰色老鼠數量：{game.First.Mice}",
            Phase.PlayingSecond => "第二關 · 目標 52 分",
            Phase.LostFirst => "Game Over · 第一關未通過",
            Phase.Won => "闖關成功！",
            _ => "Game Over · 第二關未通過"
        };
        countdown.Text = $"剩餘 {Math.Ceiling(game.Remaining.TotalSeconds):0} 秒";
        firstScore.Text = $"第一關分數：{game.First.Points}\n命中灰色老鼠數量：{game.First.Mice}";
        secondScore.Text = $"第二關分數：{game.Second.Points}\n命中灰色老鼠數量：{game.Second.Mice}\n命中棕色倉鼠數量：{game.Second.Hamsters}\n點中炸彈數量：{game.Second.Bombs}";
        firstScore.Visible = game.Phase is not (Phase.ReadySecond or Phase.PlayingSecond);
        secondScore.Visible = game.Phase is Phase.ReadySecond or Phase.PlayingSecond or Phase.Won or Phase.LostSecond;
        action.Visible = !game.IsPlaying;
        action.Text = game.Phase is Phase.ReadyFirst or Phase.ReadySecond ? "開始" : "重玩";
        for (var i = 0; i < holes.Length; i++)
            holes[i].ShowTarget(game.ActiveTarget?.Hole == i ? game.ActiveTarget.Kind : null);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) timer.Dispose();
        base.Dispose(disposing);
    }
}
