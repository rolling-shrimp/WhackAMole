using System.Drawing;
using System.Windows.Forms;

namespace WhackAMole;

public class MainWindow : Form
{
    private Game _game;
    private ErrorReporter _errors;
    private System.Windows.Forms.Timer _timer = null!;
    private TableLayoutPanel _screenLayout = null!;
    private FlowLayoutPanel _information = null!;
    private GameBoard _board = null!;
    private Label _heading = null!;
    private ScoreCard _firstLiveCard = null!;
    private ScoreCard _secondLiveCard = null!;
    private ScoreCard _firstResultCard = null!;
    private ScoreCard _secondResultCard = null!;
    private LegendPanel _legend = null!;
    private Button _actionButton = null!;
    private bool _stoppedByFault;
    private string _currentScreen = string.Empty;

    public MainWindow() : this(new Game(TimeProvider.System, new Random()))
    {
    }

    public MainWindow(Game game)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _errors = new ErrorReporter();
        BuildWindow();
        RunOperation(nameof(Game.InitializeGame), () =>
        {
            _game.InitializeGame();
            ShowInitialScreen();
        });
    }

    public string CurrentScreen
    {
        get { return _currentScreen; }
    }

    public GameBoard Board
    {
        get { return _board; }
    }

    public bool LegendShown
    {
        get { return _legend.Visible; }
    }

    public string ActionLabel
    {
        get { return _actionButton.Visible ? _actionButton.Text : string.Empty; }
    }

    public ScoreCard FirstResultCard
    {
        get { return _firstResultCard; }
    }

    public ScoreCard FirstLiveCard
    {
        get { return _firstLiveCard; }
    }

    public ScoreCard SecondLiveCard
    {
        get { return _secondLiveCard; }
    }

    public ScoreCard SecondResultCard
    {
        get { return _secondResultCard; }
    }

    public Button ActionButton
    {
        get { return _actionButton; }
    }

    public void HandleBoardClick(Point location)
    {
        RunOperation("TargetClick", () =>
        {
            bool paintedHit = _board.TryGetClickedTarget(location,
                out int holeId, out long generation);
            _game.UpdateTime();
            if (paintedHit)
            {
                _game.HitRenderedTarget(holeId, generation);
            }

            ReconcileScreen();
        });
    }

    public void ShowInitialScreen()
    {
        LevelState first = _game.GetLevelState(1);
        if (first.Phase != LevelPhase.Ready)
        {
            ThrowScreenPhase(nameof(ShowInitialScreen));
        }

        HideInformation();
        SetBoardWidth(58);
        _firstLiveCard.SetState(first);
        _firstLiveCard.Visible = true;
        _board.Render(null);
        _board.IsInteractive = false;
        SetAction("開始");
        _currentScreen = "FirstReady";
        EnsureScreen(_board.RenderedTarget == null && !_board.IsInteractive &&
            _firstLiveCard.CurrentState?.Points == 0 && _actionButton.Text == "開始" &&
            (!Visible || !_legend.Visible), nameof(ShowInitialScreen),
            "初始空棋盤、歸零記分板、開始按鈕且沒有第二關提示");
    }

    public void ShowPlayingScreen(int levelNumber)
    {
        ValidateLevelNumber(levelNumber);
        LevelState state = _game.GetLevelState(levelNumber);
        if (state.Phase != LevelPhase.Playing)
        {
            ThrowScreenPhase(nameof(ShowPlayingScreen));
        }

        HideInformation();
        SetBoardWidth(58);
        _board.IsInteractive = true;
        if (levelNumber == 1)
        {
            _firstLiveCard.Visible = true;
            _currentScreen = "FirstPlaying";
        }
        else
        {
            _secondLiveCard.Visible = true;
            _legend.Visible = true;
            _currentScreen = "SecondPlaying";
        }

        UpdateScoreboard(levelNumber);
        RenderTarget(levelNumber);
        EnsureScreen(_board.IsInteractive &&
            (levelNumber == 1 ? (!Visible || !_legend.Visible) :
                (!Visible || _legend.Visible)) &&
            (!Visible || !_actionButton.Visible),
            nameof(ShowPlayingScreen), "只顯示指定關卡的遊玩資訊與正確提示");
    }

    public void RenderTarget(int levelNumber)
    {
        ValidateLevelNumber(levelNumber);
        LevelState state = _game.GetLevelState(levelNumber);
        _board.Render(state.ActiveTarget);
        TargetState? painted = _board.RenderedTarget;
        EnsureScreen((painted == null && state.ActiveTarget == null) ||
            (painted != null && state.ActiveTarget != null &&
             painted.HoleId == state.ActiveTarget.HoleId &&
             painted.Generation == state.ActiveTarget.Generation &&
             painted.Kind == state.ActiveTarget.Kind),
            nameof(RenderTarget), "棋盤只能繪製指定關卡目前有效的目標");
    }

    public void UpdateScoreboard(int levelNumber)
    {
        ValidateLevelNumber(levelNumber);
        LevelState state = _game.GetLevelState(levelNumber);
        ScoreCard card = levelNumber == 1 ? _firstLiveCard : _secondLiveCard;
        card.SetState(state);
        EnsureScreen(card.CurrentState?.LevelNumber == levelNumber &&
            card.CurrentState.Points == state.Points &&
            card.CurrentState.MouseHits == state.MouseHits &&
            card.CurrentState.HamsterHits == state.HamsterHits &&
            card.CurrentState.BombHits == state.BombHits,
            nameof(UpdateScoreboard), "記分板應呈現指定關卡的已處理分數與紀錄");
    }

    public void ShowFirstLevelResult()
    {
        LevelState first = _game.GetLevelState(1);
        LevelState second = _game.GetLevelState(2);
        bool canShowFirstLevelResult =
            (first.Phase == LevelPhase.Failed && second.Phase == LevelPhase.NotStarted) ||
            (first.Phase == LevelPhase.Passed && second.Phase == LevelPhase.Ready);
        if (!canShowFirstLevelResult)
        {
            ThrowScreenPhase(nameof(ShowFirstLevelResult));
        }

        HideInformation();
        SetBoardWidth(48);
        _board.Render(null);
        _board.IsInteractive = false;
        _heading.Text = first.Phase == LevelPhase.Passed ? "第一關過關" : "Game Over";
        _heading.Visible = true;
        _firstResultCard.SetHeading("第一關");
        _firstResultCard.SetState(first);
        _firstResultCard.Visible = true;
        SetAction(first.Phase == LevelPhase.Passed ? "開始" : "重玩");
        _currentScreen = first.Phase == LevelPhase.Passed ? "FirstPassed" : "FirstFailed";
        EnsureScreen(_board.RenderedTarget == null && !_board.IsInteractive &&
            _firstResultCard.CurrentState?.Points == first.Points &&
            _actionButton.Text == (first.Phase == LevelPhase.Passed ? "開始" : "重玩"),
            nameof(ShowFirstLevelResult), "空棋盤、第一關成績卡與正確按鈕");
    }

    public void ShowSecondLevelReadyScreen()
    {
        LevelState first = _game.GetLevelState(1);
        LevelState second = _game.GetLevelState(2);
        if (first.Phase != LevelPhase.Passed || second.Phase != LevelPhase.Ready)
        {
            ThrowScreenPhase(nameof(ShowSecondLevelReadyScreen));
        }

        HideInformation();
        SetBoardWidth(48);
        _board.Render(null);
        _board.IsInteractive = false;
        _heading.Text = "第一關過關";
        _heading.Visible = true;
        _firstResultCard.SetState(first);
        _firstResultCard.Visible = true;
        _secondLiveCard.SetState(second);
        _secondLiveCard.Visible = true;
        _legend.Visible = true;
        SetAction("開始");
        _currentScreen = "SecondReady";
        EnsureScreen(_board.RenderedTarget == null && !_board.IsInteractive &&
            _firstResultCard.CurrentState?.Points == first.Points &&
            _secondLiveCard.CurrentState?.Points == 0 &&
            _actionButton.Text == "開始" && (!Visible || _legend.Visible),
            nameof(ShowSecondLevelReadyScreen),
            "第一關成績、第二關歸零記分板、得扣分提示與開始同畫面");
    }

    public void ShowGameResult()
    {
        LevelState first = _game.GetLevelState(1);
        LevelState second = _game.GetLevelState(2);
        if (second.Phase != LevelPhase.Won && second.Phase != LevelPhase.Failed)
        {
            ThrowScreenPhase(nameof(ShowGameResult));
        }

        HideInformation();
        SetBoardWidth(48);
        _board.Render(null);
        _board.IsInteractive = false;
        _heading.Text = second.Phase == LevelPhase.Won ? "闖關成功" : "Game Over";
        _heading.Visible = true;
        _firstResultCard.SetState(first);
        _firstResultCard.Visible = true;
        _secondResultCard.SetState(second);
        _secondResultCard.Visible = true;
        SetAction("重玩");
        _currentScreen = second.Phase == LevelPhase.Won ? "SecondWon" : "SecondFailed";
        EnsureScreen(_board.RenderedTarget == null && !_board.IsInteractive &&
            _firstResultCard.CurrentState?.Points == first.Points &&
            _secondResultCard.CurrentState?.Points == second.Points &&
            _actionButton.Text == "重玩" && (!Visible || !_legend.Visible),
            nameof(ShowGameResult), "空棋盤、兩張完整成績卡、結果及重玩按鈕");
    }

    private void BuildWindow()
    {
        Text = "打地鼠遊戲";
        ClientSize = new Size(1280, 720);
        MinimumSize = new Size(1100, 730);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft JhengHei UI", 10);

        ConsoleShell shell = new ConsoleShell();
        shell.Dock = DockStyle.Fill;
        Controls.Add(shell);

        _screenLayout = new TableLayoutPanel();
        _screenLayout.Dock = DockStyle.Fill;
        _screenLayout.ColumnCount = 2;
        _screenLayout.RowCount = 1;
        _screenLayout.BackColor = Color.FromArgb(177, 194, 122);
        _screenLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        _screenLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        _screenLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.Controls.Add(_screenLayout);

        _board = new GameBoard();
        _board.Dock = DockStyle.Fill;
        _board.Margin = new Padding(8);
        _board.MouseDown += BoardMouseDown;
        _screenLayout.Controls.Add(_board, 0, 0);

        _information = new FlowLayoutPanel();
        _information.Dock = DockStyle.Fill;
        _information.FlowDirection = FlowDirection.TopDown;
        _information.WrapContents = false;
        _information.AutoScroll = true;
        _information.Padding = new Padding(4);
        _information.ClientSizeChanged += InformationSizeChanged;
        _screenLayout.Controls.Add(_information, 1, 0);

        _heading = new Label();
        _heading.Height = 50;
        _heading.TextAlign = ContentAlignment.MiddleCenter;
        _heading.Font = new Font("Microsoft JhengHei UI", 27, FontStyle.Bold);
        _heading.BackColor = Color.FromArgb(190, 204, 135);
        _heading.ForeColor = Color.FromArgb(28, 43, 29);
        _heading.Margin = new Padding(3, 3, 3, 8);

        _firstLiveCard = new ScoreCard(1, true);
        _secondLiveCard = new ScoreCard(2, true);
        _firstResultCard = new ScoreCard(1, false);
        _secondResultCard = new ScoreCard(2, false);
        _legend = new LegendPanel();
        _actionButton = new Button();
        _actionButton.Height = 59;
        _actionButton.Font = new Font("Microsoft JhengHei UI", 27, FontStyle.Bold);
        _actionButton.BackColor = Color.FromArgb(192, 207, 133);
        _actionButton.ForeColor = Color.FromArgb(29, 43, 28);
        _actionButton.FlatStyle = FlatStyle.Flat;
        _actionButton.FlatAppearance.BorderSize = 3;
        _actionButton.Margin = new Padding(3, 9, 3, 3);
        _actionButton.Click += ActionButtonClick;

        _information.Controls.Add(_heading);
        _information.Controls.Add(_firstLiveCard);
        _information.Controls.Add(_firstResultCard);
        _information.Controls.Add(_secondLiveCard);
        _information.Controls.Add(_secondResultCard);
        _information.Controls.Add(_legend);
        _information.Controls.Add(_actionButton);
        ResizeInformationCards();

        _timer = new System.Windows.Forms.Timer();
        _timer.Interval = 50;
        _timer.Tick += TimerTick;
        FormClosed += WindowClosed;
    }

    private void InformationSizeChanged(object? sender, EventArgs e)
    {
        ResizeInformationCards();
    }

    private void ResizeInformationCards()
    {
        if (_information == null)
        {
            return;
        }

        int width = Math.Max(100, _information.ClientSize.Width - 25);
        foreach (Control child in _information.Controls)
        {
            child.Width = width;
        }
    }

    private void SetBoardWidth(float percent)
    {
        _screenLayout.ColumnStyles[0].Width = percent;
        _screenLayout.ColumnStyles[1].Width = 100 - percent;
    }

    private void HideInformation()
    {
        _information.SuspendLayout();
        _heading.Visible = false;
        _firstLiveCard.Visible = false;
        _secondLiveCard.Visible = false;
        _firstResultCard.Visible = false;
        _secondResultCard.Visible = false;
        _legend.Visible = false;
        _actionButton.Visible = false;
        _information.ResumeLayout();
    }

    private void SetAction(string text)
    {
        _actionButton.Text = text;
        _actionButton.AccessibleName = text;
        _actionButton.Visible = true;
        _actionButton.Enabled = !_stoppedByFault;
    }

    private void ActionButtonClick(object? sender, EventArgs e)
    {
        RunOperation("ActionButton", () =>
        {
            LevelState first = _game.GetLevelState(1);
            LevelState second = _game.GetLevelState(2);
            if (first.Phase == LevelPhase.Ready)
            {
                _game.StartFirstLevel();
                ShowPlayingScreen(1);
                _timer.Start();
            }
            else if (first.Phase == LevelPhase.Passed && second.Phase == LevelPhase.Ready)
            {
                _game.StartSecondLevel();
                ShowPlayingScreen(2);
                _timer.Start();
            }
            else if (first.Phase == LevelPhase.Failed ||
                     second.Phase == LevelPhase.Won || second.Phase == LevelPhase.Failed)
            {
                _game.RestartGame();
                ShowPlayingScreen(1);
                _timer.Start();
            }
        });
    }

    private void BoardMouseDown(object? sender, MouseEventArgs e)
    {
        HandleBoardClick(e.Location);
    }

    private void TimerTick(object? sender, EventArgs e)
    {
        RunOperation(nameof(Game.UpdateTime), () =>
        {
            _game.UpdateTime();
            ReconcileScreen();
        });
    }

    private void ReconcileScreen()
    {
        LevelState first = _game.GetLevelState(1);
        LevelState second = _game.GetLevelState(2);
        if (first.Phase == LevelPhase.Playing)
        {
            if (_currentScreen != "FirstPlaying")
            {
                ShowPlayingScreen(1);
            }
            else
            {
                RenderTarget(1);
                UpdateScoreboard(1);
            }
        }
        else if (second.Phase == LevelPhase.Playing)
        {
            if (_currentScreen != "SecondPlaying")
            {
                ShowPlayingScreen(2);
            }
            else
            {
                RenderTarget(2);
                UpdateScoreboard(2);
            }
        }
        else if (first.Phase == LevelPhase.Passed && second.Phase == LevelPhase.Ready)
        {
            _timer.Stop();
            ShowFirstLevelResult();
            ShowSecondLevelReadyScreen();
        }
        else if (first.Phase == LevelPhase.Failed)
        {
            _timer.Stop();
            ShowFirstLevelResult();
        }
        else if (second.Phase == LevelPhase.Won || second.Phase == LevelPhase.Failed)
        {
            _timer.Stop();
            ShowGameResult();
        }
    }

    private void RunOperation(string entry, Action action)
    {
        if (_stoppedByFault)
        {
            return;
        }

        try
        {
            action();
        }
        catch (ArgumentOutOfRangeException exception)
        {
            _errors.Record(entry, exception);
            string message = exception.ParamName == "holeId" ?
                "洞口編號無效" : "關卡編號無效";
            MessageBox.Show(this, message, "操作錯誤", MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (InvalidOperationException exception)
        {
            _errors.Record(entry, exception);
            string message = exception.Data["Contract"] as string == nameof(Game.FinishLevel) ?
                "目前無法結算關卡" : "目前無法執行此畫面操作";
            MessageBox.Show(this, message, "操作錯誤", MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (OutputContractViolationException exception)
        {
            StopAfterFault(entry, exception);
        }
        catch (Exception exception)
        {
            StopAfterFault(entry, exception);
        }
    }

    private void StopAfterFault(string entry, Exception exception)
    {
        _timer.Stop();
        _board.IsInteractive = false;
        _actionButton.Enabled = false;
        _stoppedByFault = true;
        _errors.Record(entry, exception);
        MessageBox.Show(this, "遊戲發生錯誤，本局已停止", "遊戲錯誤",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void ValidateLevelNumber(int levelNumber)
    {
        if (levelNumber != 1 && levelNumber != 2)
        {
            throw new ArgumentOutOfRangeException(nameof(levelNumber), levelNumber,
                "關卡編號必須為 1 或 2");
        }
    }

    private void ThrowScreenPhase(string method)
    {
        InvalidOperationException exception = new InvalidOperationException(
            $"{method} 目前階段不適用");
        exception.Data["Contract"] = method;
        throw exception;
    }

    private void EnsureScreen(bool condition, string method, string guarantee)
    {
        if (!condition)
        {
            throw new OutputContractViolationException(method, guarantee,
                $"screen={_currentScreen}, action={_actionButton.Text}, target={_board.RenderedTarget?.Generation}");
        }
    }

    private void WindowClosed(object? sender, FormClosedEventArgs e)
    {
        _timer.Stop();
        _timer.Dispose();
    }
}
