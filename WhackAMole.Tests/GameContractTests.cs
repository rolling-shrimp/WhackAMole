using System.Reflection;
using WhackAMole;
using Xunit;

namespace WhackAMole.Tests;

public class GameContractTests
{
    [Fact]
    public void InitializeGame_resets_both_levels_and_targets()
    {
        ManualTimeProvider clock = new ManualTimeProvider();
        Game game = new Game(clock, new ScriptedRandom());
        game.InitializeGame();
        game.StartFirstLevel();
        game.HitHole(1);

        GameState reset = game.InitializeGame();

        Assert.Equal(LevelPhase.Ready, reset.FirstLevel.Phase);
        Assert.Equal(LevelPhase.NotStarted, reset.SecondLevel.Phase);
        Assert.Equal(0, reset.FirstLevel.Points);
        Assert.Equal(0, reset.FirstLevel.MouseHits);
        Assert.Equal(0, reset.SecondLevel.Points);
        Assert.Equal(TimeSpan.FromSeconds(60), reset.FirstLevel.Remaining);
        Assert.Equal(TimeSpan.FromSeconds(60), reset.SecondLevel.Remaining);
        Assert.Null(reset.FirstLevel.ActiveTarget);
        Assert.Null(reset.SecondLevel.ActiveTarget);
    }

    [Fact]
    public void StartFirstLevel_only_starts_from_ready_and_keeps_existing_round()
    {
        ManualTimeProvider clock = new ManualTimeProvider();
        Game game = new Game(clock, new ScriptedRandom(1));
        game.InitializeGame();

        LevelState started = game.StartFirstLevel();
        Assert.Equal(LevelPhase.Playing, started.Phase);
        Assert.Null(started.ActiveTarget);
        clock.Advance(TimeSpan.FromMilliseconds(700));
        game.UpdateTime();
        Assert.NotNull(game.GetLevelState(1).ActiveTarget);
        game.HitHole(1);

        LevelState repeated = game.StartFirstLevel();
        Assert.Equal(1, repeated.Points);
        Assert.Equal(TimeSpan.FromMilliseconds(59300), repeated.Remaining);
    }

    [Fact]
    public void StartSecondLevel_requires_first_pass_and_starts_own_round()
    {
        ManualTimeProvider clock = new ManualTimeProvider();
        Game game = new Game(clock, new ScriptedRandom());
        game.InitializeGame();
        Assert.Equal(LevelPhase.NotStarted, game.StartSecondLevel().Phase);
        PassFirstLevel(game, clock);

        LevelState started = game.StartSecondLevel();
        Assert.Equal(LevelPhase.Playing, started.Phase);
        Assert.Equal(TimeSpan.FromSeconds(60), started.Remaining);
        Assert.Equal(0, started.Points);
        Assert.Equal(LevelPhase.Passed, game.GetLevelState(1).Phase);
        Assert.Equal(42, game.GetLevelState(1).Points);
        Assert.Equal(LevelPhase.Playing, game.StartSecondLevel().Phase);
    }

    [Fact]
    public void HitHole_scores_once_only_for_the_active_hole_and_applies_level_two_rules()
    {
        ManualTimeProvider clock = new ManualTimeProvider();
        ScriptedRandom random = new ScriptedRandom();
        Game game = new Game(clock, random);
        game.InitializeGame();
        game.StartFirstLevel();
        Assert.False(game.HitHole(2));
        Assert.True(game.HitHole(1));
        Assert.False(game.HitHole(1));
        Assert.Equal(1, game.GetLevelState(1).Points);
        Assert.Throws<ArgumentOutOfRangeException>(() => game.HitHole(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => game.HitHole(17));

        clock.Advance(TimeSpan.FromMilliseconds(700));
        game.UpdateTime();
        HitMice(game, clock, 41);
        clock.Advance(TimeSpan.FromSeconds(60));
        game.UpdateTime();
        random.Enqueue(0, 1, 1, 1, 2, 1, 0);
        game.StartSecondLevel();
        Assert.Equal(TargetKind.Hamster, game.GetLevelState(2).ActiveTarget?.Kind);
        Assert.True(game.HitHole(1));
        Assert.Equal(3, game.GetLevelState(2).Points);
        clock.Advance(TimeSpan.FromMilliseconds(700));
        game.UpdateTime();
        Assert.Equal(TargetKind.Bomb, game.GetLevelState(2).ActiveTarget?.Kind);
        Assert.True(game.HitHole(1));
        Assert.Equal(1, game.GetLevelState(2).Points);
        clock.Advance(TimeSpan.FromMilliseconds(700));
        game.UpdateTime();
        Assert.Equal(TargetKind.Mouse, game.GetLevelState(2).ActiveTarget?.Kind);
        Assert.True(game.HitHole(1));
        LevelState second = game.GetLevelState(2);
        Assert.Equal(2, second.Points);
        Assert.Equal(1, second.MouseHits);
        Assert.Equal(1, second.HamsterHits);
        Assert.Equal(1, second.BombHits);
    }

    [Fact]
    public void GetLevelState_is_a_snapshot_and_rejects_invalid_level_or_corrupt_output()
    {
        ManualTimeProvider clock = new ManualTimeProvider();
        Game game = new Game(clock, new ScriptedRandom());
        game.InitializeGame();
        game.StartFirstLevel();
        LevelState before = game.GetLevelState(1);
        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(TimeSpan.FromSeconds(60), game.GetLevelState(1).Remaining);
        game.UpdateTime();
        Assert.Equal(TimeSpan.FromSeconds(60), before.Remaining);
        Assert.Equal(TimeSpan.FromSeconds(50), game.GetLevelState(1).Remaining);
        Assert.Throws<ArgumentOutOfRangeException>(() => game.GetLevelState(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => game.GetLevelState(3));

        FieldInfo points = typeof(Game).GetField("_firstPoints",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        points.SetValue(game, -1);
        OutputContractViolationException violation =
            Assert.Throws<OutputContractViolationException>(() => game.GetLevelState(1));
        Assert.Equal(nameof(Game.GetLevelState), violation.Method);
    }

    [Fact]
    public void UpdateTime_expires_targets_on_700ms_slots_and_finishes_at_60s()
    {
        ManualTimeProvider clock = new ManualTimeProvider();
        Game game = new Game(clock, new ScriptedRandom());
        game.InitializeGame();
        game.StartFirstLevel();
        TargetState original = game.GetLevelState(1).ActiveTarget!;

        clock.Advance(TimeSpan.FromMilliseconds(699));
        game.UpdateTime();
        Assert.Equal(original.Generation, game.GetLevelState(1).ActiveTarget?.Generation);
        clock.Advance(TimeSpan.FromMilliseconds(1));
        game.UpdateTime();
        Assert.NotEqual(original.Generation, game.GetLevelState(1).ActiveTarget?.Generation);
        clock.Advance(TimeSpan.FromMilliseconds(59300));
        game.UpdateTime();
        Assert.Equal(LevelPhase.Failed, game.GetLevelState(1).Phase);
        Assert.Equal(TimeSpan.Zero, game.GetLevelState(1).Remaining);
        Assert.Null(game.GetLevelState(1).ActiveTarget);
        Assert.False(game.HitHole(1));
    }

    [Fact]
    public void Expired_click_with_existing_score_keeps_score_and_finishes_normally()
    {
        ManualTimeProvider clock = new ManualTimeProvider();
        Game game = new Game(clock, new ScriptedRandom());
        game.InitializeGame();
        game.StartFirstLevel();
        Assert.True(game.HitHole(1));
        clock.Advance(TimeSpan.FromSeconds(60));

        Assert.Throws<ArgumentOutOfRangeException>(() => game.HitHole(17));
        Assert.Equal(LevelPhase.Playing, game.GetLevelState(1).Phase);

        Assert.False(game.HitHole(1));
        Assert.Equal(LevelPhase.Failed, game.GetLevelState(1).Phase);
        Assert.Equal(1, game.GetLevelState(1).Points);
    }

    [Fact]
    public void Second_level_bomb_at_zero_still_counts_and_delayed_update_skips_old_targets()
    {
        ManualTimeProvider clock = new ManualTimeProvider();
        ScriptedRandom random = new ScriptedRandom();
        Game game = new Game(clock, random);
        game.InitializeGame();
        PassFirstLevel(game, clock);
        random.Enqueue(0, 1, 2);
        game.StartSecondLevel();
        TargetState bomb = game.GetLevelState(2).ActiveTarget!;
        Assert.True(game.HitHole(1));
        Assert.Equal(0, game.GetLevelState(2).Points);
        Assert.Equal(1, game.GetLevelState(2).BombHits);

        clock.Advance(TimeSpan.FromMilliseconds(2100));
        game.UpdateTime();
        TargetState current = game.GetLevelState(2).ActiveTarget!;
        Assert.True(current.Generation > bomb.Generation);
        Assert.False(game.HitHole(2));
        clock.Advance(TimeSpan.FromMilliseconds(700));
        game.UpdateTime();
        Assert.NotEqual(current.Generation, game.GetLevelState(2).ActiveTarget?.Generation);
    }

    [Fact]
    public void FinishLevel_rejects_early_finish_and_uses_42_and_52_point_thresholds()
    {
        ManualTimeProvider clock = new ManualTimeProvider();
        Game game = new Game(clock, new ScriptedRandom());
        game.InitializeGame();
        Assert.Throws<InvalidOperationException>(() => game.FinishLevel());
        game.StartFirstLevel();
        Assert.Throws<InvalidOperationException>(() => game.FinishLevel());
        HitMice(game, clock, 41);
        clock.Advance(TimeSpan.FromSeconds(60));
        game.FinishLevel();
        Assert.Equal(LevelPhase.Failed, game.GetLevelState(1).Phase);

        game.RestartGame();
        HitMice(game, clock, 42);
        clock.Advance(TimeSpan.FromSeconds(60));
        game.FinishLevel();
        Assert.Equal(LevelPhase.Passed, game.GetLevelState(1).Phase);
        Assert.Equal(LevelPhase.Ready, game.GetLevelState(2).Phase);
        game.StartSecondLevel();
        HitMice(game, clock, 51);
        clock.Advance(TimeSpan.FromSeconds(60));
        game.FinishLevel();
        Assert.Equal(LevelPhase.Failed, game.GetLevelState(2).Phase);

        game.RestartGame();
        HitMice(game, clock, 42);
        clock.Advance(TimeSpan.FromSeconds(60));
        game.FinishLevel();
        game.StartSecondLevel();
        HitMice(game, clock, 52);
        clock.Advance(TimeSpan.FromSeconds(60));
        game.FinishLevel();
        Assert.Equal(LevelPhase.Won, game.GetLevelState(2).Phase);
    }

    [Fact]
    public void RestartGame_clears_both_levels_and_enters_first_playing_round()
    {
        ManualTimeProvider clock = new ManualTimeProvider();
        Game game = new Game(clock, new ScriptedRandom());
        game.InitializeGame();
        Assert.Equal(LevelPhase.Ready, game.RestartGame().Phase);
        PassFirstLevel(game, clock);
        game.StartSecondLevel();
        game.HitHole(1);
        clock.Advance(TimeSpan.FromSeconds(60));
        game.UpdateTime();

        LevelState restarted = game.RestartGame();
        Assert.Equal(LevelPhase.Playing, restarted.Phase);
        Assert.Equal(0, restarted.Points);
        Assert.Equal(0, restarted.MouseHits);
        Assert.Equal(TimeSpan.FromSeconds(60), restarted.Remaining);
        LevelState second = game.GetLevelState(2);
        Assert.Equal(LevelPhase.NotStarted, second.Phase);
        Assert.Equal(0, second.Points);
        Assert.Equal(0, second.MouseHits);
        Assert.Equal(0, second.HamsterHits);
        Assert.Equal(0, second.BombHits);
    }

    private void PassFirstLevel(Game game, ManualTimeProvider clock)
    {
        game.StartFirstLevel();
        HitMice(game, clock, 42);
        clock.Advance(TimeSpan.FromSeconds(60));
        game.UpdateTime();
        Assert.Equal(LevelPhase.Passed, game.GetLevelState(1).Phase);
    }

    private void HitMice(Game game, ManualTimeProvider clock, int count)
    {
        for (int index = 0; index < count; index++)
        {
            Assert.True(game.HitHole(1));
            clock.Advance(TimeSpan.FromMilliseconds(700));
            game.UpdateTime();
        }
    }
}
