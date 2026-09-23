namespace WhackAMole;

public class Game
{
    private const int ROUND_MILLISECONDS = 60000;
    private const int TARGET_MILLISECONDS = 700;
    private TimeProvider _clock;
    private Random _random;
    private bool _initialized;
    private LevelPhase _firstPhase;
    private LevelPhase _secondPhase;
    private int _firstPoints;
    private int _firstMouseHits;
    private int _secondPoints;
    private int _secondMouseHits;
    private int _secondHamsterHits;
    private int _secondBombHits;
    private TimeSpan _firstRemaining;
    private TimeSpan _secondRemaining;
    private long _roundStartedAt;
    private int _firstTargetDelayMilliseconds;
    private long _processedSlot;
    private long _nextGeneration;
    private TargetState? _activeTarget;

    public Game(TimeProvider clock, Random random)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _random = random ?? throw new ArgumentNullException(nameof(random));
        _firstRemaining = TimeSpan.FromMilliseconds(ROUND_MILLISECONDS);
        _secondRemaining = TimeSpan.FromMilliseconds(ROUND_MILLISECONDS);
        _processedSlot = -1;
    }

    public GameState InitializeGame()
    {
        ResetState();
        GameState result = new GameState(CreateLevelState(1), CreateLevelState(2));
        Check(result.FirstLevel.Phase == LevelPhase.Ready &&
              result.SecondLevel.Phase == LevelPhase.NotStarted &&
              result.FirstLevel.Points == 0 && result.FirstLevel.MouseHits == 0 &&
              result.SecondLevel.Points == 0 && result.SecondLevel.MouseHits == 0 &&
              result.SecondLevel.HamsterHits == 0 && result.SecondLevel.BombHits == 0 &&
              result.FirstLevel.Remaining == TimeSpan.FromMilliseconds(ROUND_MILLISECONDS) &&
              result.SecondLevel.Remaining == TimeSpan.FromMilliseconds(ROUND_MILLISECONDS) &&
              result.FirstLevel.ActiveTarget == null && result.SecondLevel.ActiveTarget == null,
            nameof(InitializeGame), "整局初始狀態應有兩關歸零、各剩 60 秒且沒有目標",
            $"first={result.FirstLevel.Phase}, second={result.SecondLevel.Phase}");
        ValidateInvariants(nameof(InitializeGame));
        return result;
    }

    public LevelState StartFirstLevel()
    {
        EnsureInitialized();
        bool applicable = _firstPhase == LevelPhase.Ready;
        if (_firstPhase == LevelPhase.Ready)
        {
            _firstPhase = LevelPhase.Playing;
            _firstRemaining = TimeSpan.FromMilliseconds(ROUND_MILLISECONDS);
            StartRound(1);
        }

        LevelState result = CreateLevelState(1);
        ValidateLevelOutput(result, 1, nameof(StartFirstLevel));
        if (applicable)
        {
            Check(result.Phase == LevelPhase.Playing &&
                  result.Remaining == TimeSpan.FromMilliseconds(ROUND_MILLISECONDS) &&
                  ((_firstTargetDelayMilliseconds == 0 && result.ActiveTarget != null &&
                    result.ActiveTarget.Kind == TargetKind.Mouse) ||
                   (_firstTargetDelayMilliseconds == TARGET_MILLISECONDS &&
                    result.ActiveTarget == null)),
                nameof(StartFirstLevel), "開始後須進入 60 秒第一關並符合首個目標排程",
                $"phase={result.Phase}, remaining={result.Remaining}, delay={_firstTargetDelayMilliseconds}, target={result.ActiveTarget?.Kind}");
        }
        ValidateInvariants(nameof(StartFirstLevel));
        return result;
    }

    public LevelState StartSecondLevel()
    {
        EnsureInitialized();
        bool applicable = _firstPhase == LevelPhase.Passed && _secondPhase == LevelPhase.Ready;
        if (_firstPhase == LevelPhase.Passed && _secondPhase == LevelPhase.Ready)
        {
            _secondPoints = 0;
            _secondMouseHits = 0;
            _secondHamsterHits = 0;
            _secondBombHits = 0;
            _secondRemaining = TimeSpan.FromMilliseconds(ROUND_MILLISECONDS);
            _secondPhase = LevelPhase.Playing;
            StartRound(2);
        }

        LevelState result = CreateLevelState(2);
        ValidateLevelOutput(result, 2, nameof(StartSecondLevel));
        if (applicable)
        {
            Check(result.Phase == LevelPhase.Playing && result.Points == 0 &&
                  result.MouseHits == 0 && result.HamsterHits == 0 &&
                  result.BombHits == 0 &&
                  result.Remaining == TimeSpan.FromMilliseconds(ROUND_MILLISECONDS) &&
                  ((_firstTargetDelayMilliseconds == 0 && result.ActiveTarget != null) ||
                   (_firstTargetDelayMilliseconds == TARGET_MILLISECONDS &&
                    result.ActiveTarget == null)),
                nameof(StartSecondLevel), "第二關須以歸零紀錄和新的 60 秒排程開始",
                $"phase={result.Phase}, points={result.Points}, remaining={result.Remaining}, target={result.ActiveTarget?.Kind}");
        }
        ValidateInvariants(nameof(StartSecondLevel));
        return result;
    }

    public bool HitHole(int holeId)
    {
        ValidateHoleId(holeId);
        return HitHoleCore(holeId, null, nameof(HitHole));
    }

    // The UI supplies the generation it actually painted. This prevents a click on an
    // expired picture from hitting a newly spawned target in the same hole.
    internal bool HitRenderedTarget(int holeId, long renderedGeneration)
    {
        ValidateHoleId(holeId);
        return HitHoleCore(holeId, renderedGeneration, nameof(HitHole));
    }

    private bool HitHoleCore(int holeId, long? renderedGeneration, string method)
    {
        EnsureInitialized();
        if (!IsPlaying())
        {
            ValidateInvariants(method);
            return false;
        }

        int levelNumber = _firstPhase == LevelPhase.Playing ? 1 : 2;
        int beforePoints = levelNumber == 1 ? _firstPoints : _secondPoints;
        int beforeMice = levelNumber == 1 ? _firstMouseHits : _secondMouseHits;
        int beforeHamsters = _secondHamsterHits;
        int beforeBombs = _secondBombHits;
        UpdateTime();
        TargetKind? hitKind = _activeTarget != null && _activeTarget.HoleId == holeId &&
            (!renderedGeneration.HasValue ||
             _activeTarget.Generation == renderedGeneration.Value) ?
            _activeTarget.Kind : null;
        bool hit = ScoreActiveTarget(holeId, renderedGeneration);
        int afterPoints = levelNumber == 1 ? _firstPoints : _secondPoints;
        int afterMice = levelNumber == 1 ? _firstMouseHits : _secondMouseHits;
        int expectedPoints = beforePoints;
        if (hit && hitKind == TargetKind.Mouse)
        {
            expectedPoints++;
        }
        else if (hit && hitKind == TargetKind.Hamster)
        {
            expectedPoints += 3;
        }
        else if (hit && hitKind == TargetKind.Bomb)
        {
            expectedPoints = Math.Max(0, expectedPoints - 2);
        }

        Check(hit == hitKind.HasValue && afterPoints == expectedPoints &&
              afterMice == beforeMice + (hitKind == TargetKind.Mouse ? 1 : 0) &&
              _secondHamsterHits == beforeHamsters +
                  (hitKind == TargetKind.Hamster ? 1 : 0) &&
              _secondBombHits == beforeBombs +
                  (hitKind == TargetKind.Bomb ? 1 : 0) &&
              (!hit || _activeTarget == null),
            method, "命中結果、分數與各目標紀錄須一致且同一目標只計一次",
            $"hit={hit}, kind={hitKind}, points={beforePoints}->{afterPoints}, mice={beforeMice}->{afterMice}");
        ValidateInvariants(method);
        return hit;
    }

    public LevelState GetLevelState(int levelNumber)
    {
        ValidateLevelNumber(levelNumber);
        EnsureInitialized();
        LevelState result = CreateLevelState(levelNumber);
        ValidateLevelOutput(result, levelNumber, nameof(GetLevelState));
        ValidateInvariants(nameof(GetLevelState));
        return result;
    }

    public void UpdateTime()
    {
        EnsureInitialized();
        if (!IsPlaying())
        {
            ValidateInvariants(nameof(UpdateTime));
            return;
        }

        TimeSpan elapsed = GetRoundElapsed();
        if (elapsed >= TimeSpan.FromMilliseconds(ROUND_MILLISECONDS))
        {
            int finishedLevel = _firstPhase == LevelPhase.Playing ? 1 : 2;
            FinishLevel();
            Check(!IsPlaying() && _activeTarget == null &&
                  (finishedLevel == 1 ? _firstRemaining : _secondRemaining) == TimeSpan.Zero,
                nameof(UpdateTime), "60 秒截止後須結算並清除目標",
                $"phase={_firstPhase}/{_secondPhase}, target={_activeTarget?.HoleId}");
            ValidateInvariants(nameof(UpdateTime));
            return;
        }

        TimeSpan remaining = TimeSpan.FromMilliseconds(ROUND_MILLISECONDS) - elapsed;
        if (_firstPhase == LevelPhase.Playing)
        {
            _firstRemaining = remaining;
        }
        else
        {
            _secondRemaining = remaining;
        }

        double elapsedMilliseconds = elapsed.TotalMilliseconds;
        if (elapsedMilliseconds >= _firstTargetDelayMilliseconds)
        {
            long slot = (long)Math.Floor((elapsedMilliseconds - _firstTargetDelayMilliseconds) /
                TARGET_MILLISECONDS);
            if (slot > _processedSlot)
            {
                SpawnTarget(_firstPhase == LevelPhase.Playing ? 1 : 2, slot);
            }
        }

        Check(IsPlaying() &&
              (_firstPhase == LevelPhase.Playing ? _firstRemaining : _secondRemaining) == remaining &&
              (_activeTarget == null || _activeTarget.Generation > 0),
            nameof(UpdateTime), "未截止時須保留正在遊玩的關卡和正確倒數",
            $"phase={_firstPhase}/{_secondPhase}, remaining={remaining}, target={_activeTarget?.Generation}");
        ValidateInvariants(nameof(UpdateTime));
    }

    public void FinishLevel()
    {
        EnsureInitialized();
        int levelNumber = _firstPhase == LevelPhase.Playing ? 1 :
            _secondPhase == LevelPhase.Playing ? 2 : 0;
        if (levelNumber == 0 || GetRoundElapsed() < TimeSpan.FromMilliseconds(ROUND_MILLISECONDS))
        {
            InvalidOperationException exception = new InvalidOperationException(
                "目前關卡尚未達到合法結算階段");
            exception.Data["Contract"] = nameof(FinishLevel);
            throw exception;
        }

        _activeTarget = null;
        _processedSlot = -1;
        if (levelNumber == 1)
        {
            _firstRemaining = TimeSpan.Zero;
            _firstPhase = _firstPoints >= 42 ? LevelPhase.Passed : LevelPhase.Failed;
            if (_firstPhase == LevelPhase.Passed)
            {
                _secondPhase = LevelPhase.Ready;
            }
        }
        else
        {
            _secondRemaining = TimeSpan.Zero;
            _secondPhase = _secondPoints >= 52 ? LevelPhase.Won : LevelPhase.Failed;
        }

        ValidateInvariants(nameof(FinishLevel));
        Check(!IsPlaying() && _activeTarget == null &&
              (levelNumber == 1 ?
                  (_firstRemaining == TimeSpan.Zero &&
                   _firstPhase == (_firstPoints >= 42 ? LevelPhase.Passed : LevelPhase.Failed)) :
                  (_secondRemaining == TimeSpan.Zero &&
                   _secondPhase == (_secondPoints >= 52 ? LevelPhase.Won : LevelPhase.Failed))),
            nameof(FinishLevel), "結算後不得接受本關命中或保留目標",
            $"playing={IsPlaying()}, target={_activeTarget?.HoleId}");
    }

    public LevelState RestartGame()
    {
        EnsureInitialized();
        if (_firstPhase == LevelPhase.Failed ||
            _secondPhase == LevelPhase.Won || _secondPhase == LevelPhase.Failed)
        {
            ResetState();
            LevelState started = StartFirstLevel();
            ValidateLevelOutput(started, 1, nameof(RestartGame));
            Check(_secondPoints == 0 && _secondMouseHits == 0 &&
                  _secondHamsterHits == 0 && _secondBombHits == 0,
                nameof(RestartGame), "重玩後第二關紀錄應歸零",
                $"points={_secondPoints}, mice={_secondMouseHits}, hamsters={_secondHamsterHits}, bombs={_secondBombHits}");
            return started;
        }

        LevelState result = CreateLevelState(1);
        ValidateLevelOutput(result, 1, nameof(RestartGame));
        ValidateInvariants(nameof(RestartGame));
        return result;
    }

    private void ResetState()
    {
        _initialized = true;
        _firstPhase = LevelPhase.Ready;
        _secondPhase = LevelPhase.NotStarted;
        _firstPoints = 0;
        _firstMouseHits = 0;
        _secondPoints = 0;
        _secondMouseHits = 0;
        _secondHamsterHits = 0;
        _secondBombHits = 0;
        _firstRemaining = TimeSpan.FromMilliseconds(ROUND_MILLISECONDS);
        _secondRemaining = TimeSpan.FromMilliseconds(ROUND_MILLISECONDS);
        _roundStartedAt = 0;
        _firstTargetDelayMilliseconds = 0;
        _processedSlot = -1;
        _activeTarget = null;
    }

    private void StartRound(int levelNumber)
    {
        _activeTarget = null;
        _processedSlot = -1;
        _roundStartedAt = _clock.GetTimestamp();
        _firstTargetDelayMilliseconds = _random.Next(2) == 0 ? 0 : TARGET_MILLISECONDS;
        if (_firstTargetDelayMilliseconds == 0)
        {
            SpawnTarget(levelNumber, 0);
        }
    }

    private void SpawnTarget(int levelNumber, long slot)
    {
        int holeId = _random.Next(1, 17);
        TargetKind kind = levelNumber == 1 ? TargetKind.Mouse :
            (TargetKind)_random.Next(3);
        _nextGeneration++;
        _activeTarget = new TargetState(holeId, kind, _nextGeneration);
        _processedSlot = slot;
    }

    private bool ScoreActiveTarget(int holeId, long? renderedGeneration)
    {
        if (!IsPlaying() || _activeTarget == null || _activeTarget.HoleId != holeId ||
            (renderedGeneration.HasValue && _activeTarget.Generation != renderedGeneration.Value))
        {
            return false;
        }

        if (_firstPhase == LevelPhase.Playing)
        {
            _firstPoints++;
            _firstMouseHits++;
        }
        else if (_activeTarget.Kind == TargetKind.Mouse)
        {
            _secondPoints++;
            _secondMouseHits++;
        }
        else if (_activeTarget.Kind == TargetKind.Hamster)
        {
            _secondPoints += 3;
            _secondHamsterHits++;
        }
        else
        {
            _secondPoints = Math.Max(0, _secondPoints - 2);
            _secondBombHits++;
        }

        _activeTarget = null;
        return true;
    }

    private TimeSpan GetRoundElapsed()
    {
        TimeSpan elapsed = _clock.GetElapsedTime(_roundStartedAt, _clock.GetTimestamp());
        return elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
    }

    private bool IsPlaying()
    {
        return _firstPhase == LevelPhase.Playing || _secondPhase == LevelPhase.Playing;
    }

    private LevelState CreateLevelState(int levelNumber)
    {
        if (levelNumber == 1)
        {
            return new LevelState(1, _firstPhase, _firstRemaining, _firstPoints,
                _firstMouseHits, 0, 0,
                _firstPhase == LevelPhase.Playing ? _activeTarget : null);
        }

        return new LevelState(2, _secondPhase, _secondRemaining, _secondPoints,
            _secondMouseHits, _secondHamsterHits, _secondBombHits,
            _secondPhase == LevelPhase.Playing ? _activeTarget : null);
    }

    private void ValidateHoleId(int holeId)
    {
        if (holeId < 1 || holeId > 16)
        {
            throw new ArgumentOutOfRangeException(nameof(holeId), holeId,
                "洞口編號必須介於 1 與 16");
        }
    }

    private void ValidateLevelNumber(int levelNumber)
    {
        if (levelNumber != 1 && levelNumber != 2)
        {
            throw new ArgumentOutOfRangeException(nameof(levelNumber), levelNumber,
                "關卡編號必須為 1 或 2");
        }
    }

    private void ValidateLevelOutput(LevelState state, int requestedLevel, string method)
    {
        Check(state.LevelNumber == requestedLevel, method,
            "回傳結果只能屬於指定關卡", $"requested={requestedLevel}, actual={state.LevelNumber}");
        Check(state.Points >= 0 && state.Remaining >= TimeSpan.Zero &&
              state.Remaining <= TimeSpan.FromMilliseconds(ROUND_MILLISECONDS), method,
            "分數不得為負且剩餘時間必須在 0 到 60 秒內",
            $"points={state.Points}, remaining={state.Remaining}");
    }

    private void ValidateInvariants(string method)
    {
        Check(_firstPoints >= 0 && _secondPoints >= 0 &&
              _firstMouseHits >= 0 && _secondMouseHits >= 0 &&
              _secondHamsterHits >= 0 && _secondBombHits >= 0,
            method, "分數與命中／點中紀錄不得為負",
            $"first={_firstPoints}, second={_secondPoints}");
        Check(_firstPoints == _firstMouseHits &&
              _firstRemaining >= TimeSpan.Zero &&
              _firstRemaining <= TimeSpan.FromMilliseconds(ROUND_MILLISECONDS) &&
              _secondRemaining >= TimeSpan.Zero &&
              _secondRemaining <= TimeSpan.FromMilliseconds(ROUND_MILLISECONDS),
            method, "第一關分數及兩關剩餘時間應符合規則",
            $"firstPoints={_firstPoints}, firstMice={_firstMouseHits}, remaining={_firstRemaining}/{_secondRemaining}");
        Check(_activeTarget == null ||
              (IsPlaying() && _activeTarget.HoleId >= 1 && _activeTarget.HoleId <= 16 &&
               (_firstPhase != LevelPhase.Playing || _activeTarget.Kind == TargetKind.Mouse)),
            method, "只有遊玩階段可有符合該關的有效目標",
            $"phase={_firstPhase}/{_secondPhase}, target={_activeTarget?.Kind}/{_activeTarget?.HoleId}");
        Check(_secondPhase != LevelPhase.Playing || _firstPhase == LevelPhase.Passed,
            method, "第二關遊玩時第一關必須已過關",
            $"first={_firstPhase}, second={_secondPhase}");
    }

    private void Check(bool condition, string method, string guarantee, string actual)
    {
        if (!condition)
        {
            throw new OutputContractViolationException(method, guarantee, actual);
        }
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("遊戲尚未初始化");
        }
    }
}
