using WhackAMole.Core;

namespace WhackAMole.Tests;

public sealed class GameTests
{
    private sealed class ManualClock : TimeProvider
    {
        private long ticks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => ticks;
        public void Advance(double milliseconds) => ticks += TimeSpan.FromMilliseconds(milliseconds).Ticks;
    }

    private sealed class FixedRandom : Random
    {
        public TargetKind Kind { get; set; }
        public int Hole { get; set; } = 7;
        public override int Next(int maxValue) => maxValue == 16 ? Hole : (int)Kind;
    }

    private sealed class Fixture
    {
        public ManualClock Clock { get; } = new();
        public FixedRandom Random { get; } = new();
        public Game Game { get; }
        public Fixture() => Game = new(Clock, Random);
        public void HitTargets(int count)
        {
            for (var i = 0; i < count; i++)
            {
                Game.Tick();
                Assert.True(Game.Hit(Random.Hole));
                Clock.Advance(700);
            }
        }
        public void EndRound() { Clock.Advance(60_000); Game.Tick(); }
        public void StartSecond()
        {
            Game.Start();
            HitTargets(42);
            EndRound();
            Assert.Equal(Phase.ReadySecond, Game.Phase);
            Game.Start();
        }
    }

    [Fact]
    public void InitialStateWaitsForStart()
    {
        var f = new Fixture();
        f.Clock.Advance(90_000);
        f.Game.Tick();
        Assert.Equal(Phase.ReadyFirst, f.Game.Phase);
        Assert.Equal(new Score(), f.Game.First);
        Assert.Equal(new Score(), f.Game.Second);
        Assert.Null(f.Game.ActiveTarget);
        Assert.Equal(TimeSpan.FromSeconds(60), f.Game.Remaining);
        Assert.False(f.Game.Hit(7));
    }

    [Fact]
    public void FirstRoundShowsMouseImmediatelyAndCountsOneClick()
    {
        var f = new Fixture();
        f.Game.Start();
        Assert.Equal(new Target(7, TargetKind.Mouse), f.Game.ActiveTarget);
        Assert.False(f.Game.Hit(6));
        Assert.True(f.Game.Hit(7));
        Assert.False(f.Game.Hit(7));
        Assert.Equal(new Score(1, 1), f.Game.First);
        Assert.Null(f.Game.ActiveTarget);
        f.Clock.Advance(699);
        f.Game.Tick();
        Assert.Null(f.Game.ActiveTarget);
        f.Clock.Advance(1);
        f.Game.Tick();
        Assert.NotNull(f.Game.ActiveTarget);
    }

    [Theory]
    [InlineData(699, true)]
    [InlineData(700, false)]
    [InlineData(701, false)]
    public void ClickChecksExpiryEvenWhenUiTimerHasNotRun(int milliseconds, bool accepted)
    {
        var f = new Fixture();
        f.Game.Start();
        f.Clock.Advance(milliseconds);
        Assert.Equal(accepted, f.Game.Hit(7));
        Assert.Equal(accepted ? 1 : 0, f.Game.First.Points);
    }

    [Fact]
    public void TimerReplacesExpiredTargetAndSkipsMissedSlots()
    {
        var f = new Fixture();
        f.Game.Start();
        var initial = f.Game.ActiveTarget;
        f.Clock.Advance(699);
        f.Game.Tick();
        Assert.Same(initial, f.Game.ActiveTarget);
        f.Random.Hole = 15;
        f.Clock.Advance(1401);
        f.Game.Tick();
        Assert.Equal(new Target(15, TargetKind.Mouse), f.Game.ActiveTarget);
        Assert.Equal(new Score(), f.Game.First);
    }

    [Theory]
    [InlineData(0, Phase.LostFirst)]
    [InlineData(41, Phase.LostFirst)]
    [InlineData(42, Phase.ReadySecond)]
    [InlineData(43, Phase.ReadySecond)]
    public void FirstThresholdUsesOnlyFirstScore(int hits, Phase expected)
    {
        var f = new Fixture();
        f.Game.Start();
        f.HitTargets(hits);
        f.EndRound();
        Assert.Equal(expected, f.Game.Phase);
        Assert.Equal(new Score(hits, hits), f.Game.First);
        Assert.Null(f.Game.ActiveTarget);
        Assert.False(f.Game.Hit(7));
    }

    [Fact]
    public void RoundEndsAtExactlySixtySecondsWithoutLatePoints()
    {
        var f = new Fixture();
        f.Game.Start();
        f.Clock.Advance(59_999);
        f.Game.Tick();
        Assert.True(f.Game.IsPlaying);
        Assert.Equal(TimeSpan.FromMilliseconds(1), f.Game.Remaining);
        f.Clock.Advance(1);
        Assert.False(f.Game.Hit(7));
        Assert.Equal(Phase.LostFirst, f.Game.Phase);
        Assert.Equal(TimeSpan.Zero, f.Game.Remaining);
        Assert.Null(f.Game.ActiveTarget);
    }

    [Fact]
    public void PassingFirstWaitsForSecondStartWithFreshClockAndScore()
    {
        var f = new Fixture();
        f.Game.Start();
        f.HitTargets(42);
        Assert.Equal(Phase.PlayingFirst, f.Game.Phase);
        f.EndRound();
        f.Clock.Advance(90_000);
        f.Game.Tick();
        Assert.Equal(Phase.ReadySecond, f.Game.Phase);
        f.Game.Start();
        Assert.Equal(Phase.PlayingSecond, f.Game.Phase);
        Assert.Equal(TimeSpan.FromSeconds(60), f.Game.Remaining);
        Assert.Equal(new Score(), f.Game.Second);
        Assert.Equal(new Score(42, 42), f.Game.First);
    }

    [Theory]
    [InlineData(TargetKind.Mouse, 1, 1, 0, 0)]
    [InlineData(TargetKind.Hamster, 3, 0, 1, 0)]
    [InlineData(TargetKind.Bomb, 0, 0, 0, 1)]
    public void SecondTargetsScoreOnceAndDisappear(TargetKind kind, int points, int mice, int hamsters, int bombs)
    {
        var f = new Fixture();
        f.Random.Kind = kind;
        f.StartSecond();
        Assert.Equal(kind, f.Game.ActiveTarget!.Kind);
        Assert.True(f.Game.Hit(7));
        Assert.Null(f.Game.ActiveTarget);
        Assert.False(f.Game.Hit(7));
        Assert.Equal(new Score(points, mice, hamsters, bombs), f.Game.Second);
        Assert.Equal(new Score(42, 42), f.Game.First);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 1)]
    public void BombDeductsTwoWithZeroFloor(int points, int expected)
    {
        var f = new Fixture();
        f.StartSecond();
        f.HitTargets(points);
        f.Random.Kind = TargetKind.Bomb;
        f.Clock.Advance(700);
        f.Game.Tick();
        Assert.True(f.Game.Hit(7));
        Assert.Equal(new Score(expected, points, 0, 1), f.Game.Second);
    }

    [Theory]
    [InlineData(0, Phase.LostSecond)]
    [InlineData(51, Phase.LostSecond)]
    [InlineData(52, Phase.Won)]
    [InlineData(53, Phase.Won)]
    public void SecondThresholdDoesNotIncludeFirstScore(int hits, Phase expected)
    {
        var f = new Fixture();
        f.StartSecond();
        f.HitTargets(hits);
        f.EndRound();
        Assert.Equal(expected, f.Game.Phase);
        Assert.Equal(new Score(42, 42), f.Game.First);
        Assert.Equal(new Score(hits, hits), f.Game.Second);
        Assert.Null(f.Game.ActiveTarget);
        f.Clock.Advance(700);
        f.Game.Tick();
        Assert.False(f.Game.Hit(7));
        Assert.Null(f.Game.ActiveTarget);
    }

    [Theory]
    [InlineData(Phase.LostFirst)]
    [InlineData(Phase.LostSecond)]
    [InlineData(Phase.Won)]
    public void EveryReplayClearsBothRoundsAndStartsFirstImmediately(Phase result)
    {
        var f = new Fixture();
        if (result == Phase.LostFirst)
        {
            f.Game.Start();
            f.HitTargets(10);
        }
        else
        {
            f.StartSecond();
            f.HitTargets(result == Phase.Won ? 52 : 1);
            f.Random.Kind = TargetKind.Hamster;
            f.HitTargets(1);
            f.Random.Kind = TargetKind.Bomb;
            f.HitTargets(1);
        }
        f.EndRound();
        Assert.Equal(result, f.Game.Phase);
        f.Game.Replay();
        Assert.Equal(Phase.PlayingFirst, f.Game.Phase);
        Assert.Equal(new Score(), f.Game.First);
        Assert.Equal(new Score(), f.Game.Second);
        Assert.Equal(TimeSpan.FromSeconds(60), f.Game.Remaining);
        Assert.Equal(TargetKind.Mouse, f.Game.ActiveTarget!.Kind);
    }

    [Fact]
    public void StartAndReplayCannotResetAnActiveRound()
    {
        var f = new Fixture();
        f.Game.Replay();
        Assert.Equal(Phase.ReadyFirst, f.Game.Phase);
        f.Game.Start();
        f.HitTargets(5);
        f.Game.Tick();
        var remaining = f.Game.Remaining;
        f.Game.Start();
        f.Game.Replay();
        Assert.Equal(new Score(5, 5), f.Game.First);
        Assert.Equal(remaining, f.Game.Remaining);
        f.EndRound();
        f.Game.Start();
        Assert.Equal(Phase.LostFirst, f.Game.Phase);
    }

    [Fact]
    public void AllSixteenHolesCanReceiveTargets()
    {
        var f = new Fixture();
        f.Game.Start();
        for (var hole = 0; hole < 16; hole++)
        {
            f.Random.Hole = hole;
            f.Clock.Advance(700);
            f.Game.Tick();
            Assert.Equal(hole, f.Game.ActiveTarget!.Hole);
            Assert.True(f.Game.Hit(hole));
        }
        Assert.Equal(16, f.Game.First.Points);
    }
}
