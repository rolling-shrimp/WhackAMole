using System.Drawing;
using WhackAMole;
using Xunit;

namespace WhackAMole.Tests;

public class ScreenContractTests
{
    [Fact]
    public void ShowInitialScreen_has_empty_board_first_score_and_start_action()
    {
        new StaRunner().Run(() =>
        {
            using MainWindow form = CreateForm(out Game game, out ManualTimeProvider clock);
            form.Show();
            form.ShowInitialScreen();
            Assert.Equal("FirstReady", form.CurrentScreen);
            Assert.Null(form.Board.RenderedTarget);
            Assert.False(form.Board.IsInteractive);
            Assert.Equal(0, form.FirstLiveCard.CurrentState?.Points);
            Assert.Equal("開始", form.ActionLabel);
            Assert.False(form.LegendShown);
            form.Close();
        });
    }

    [Fact]
    public void ShowPlayingScreen_uses_current_level_and_only_shows_second_rules_in_level_two()
    {
        new StaRunner().Run(() =>
        {
            using MainWindow form = CreateForm(out Game game, out ManualTimeProvider clock);
            form.Show();
            game.StartFirstLevel();
            form.ShowPlayingScreen(1);
            Assert.Equal("FirstPlaying", form.CurrentScreen);
            Assert.True(form.Board.IsInteractive);
            Assert.False(form.LegendShown);
            Assert.Equal(string.Empty, form.ActionLabel);

            PassFirstLevel(game, clock);
            game.StartSecondLevel();
            form.ShowPlayingScreen(2);
            Assert.Equal("SecondPlaying", form.CurrentScreen);
            Assert.True(form.LegendShown);
            Assert.Equal(0, form.SecondLiveCard.CurrentState?.Points);
            Assert.Equal(string.Empty, form.ActionLabel);
            form.Close();
        });
    }

    [Fact]
    public void RenderTarget_matches_game_target_and_clears_after_hit()
    {
        new StaRunner().Run(() =>
        {
            using MainWindow form = CreateForm(out Game game, out ManualTimeProvider clock);
            form.Show();
            game.StartFirstLevel();
            form.ShowPlayingScreen(1);
            TargetState expected = game.GetLevelState(1).ActiveTarget!;
            Assert.Equal(expected.Generation, form.Board.RenderedTarget?.Generation);
            Assert.Equal(expected.HoleId, form.Board.RenderedTarget?.HoleId);

            game.HitHole(expected.HoleId);
            form.RenderTarget(1);
            Assert.Null(form.Board.RenderedTarget);
            form.Close();
        });
    }

    [Fact]
    public void UpdateScoreboard_shows_processed_score_and_hit_count()
    {
        new StaRunner().Run(() =>
        {
            using MainWindow form = CreateForm(out Game game, out ManualTimeProvider clock);
            form.Show();
            game.StartFirstLevel();
            game.HitHole(1);
            form.UpdateScoreboard(1);
            Assert.Equal(1, form.FirstLiveCard.CurrentState?.Points);
            Assert.Equal(1, form.FirstLiveCard.CurrentState?.MouseHits);
            form.Close();
        });
    }

    [Fact]
    public void ShowFirstLevelResult_disables_board_and_shows_result_action()
    {
        new StaRunner().Run(() =>
        {
            using MainWindow form = CreateForm(out Game game, out ManualTimeProvider clock);
            form.Show();
            game.StartFirstLevel();
            clock.Advance(TimeSpan.FromSeconds(60));
            game.UpdateTime();
            form.ShowFirstLevelResult();
            Assert.Equal("FirstFailed", form.CurrentScreen);
            Assert.Equal("重玩", form.ActionLabel);
            Assert.Equal(0, form.FirstResultCard.CurrentState?.Points);
            Assert.Null(form.Board.RenderedTarget);
            Assert.False(form.Board.IsInteractive);
            form.Close();
        });
    }

    [Fact]
    public void ShowSecondLevelReadyScreen_combines_first_result_and_second_rules()
    {
        new StaRunner().Run(() =>
        {
            using MainWindow form = CreateForm(out Game game, out ManualTimeProvider clock);
            form.Show();
            PassFirstLevel(game, clock);
            form.ShowSecondLevelReadyScreen();
            Assert.Equal("SecondReady", form.CurrentScreen);
            Assert.Equal(42, form.FirstResultCard.CurrentState?.Points);
            Assert.Equal(0, form.SecondLiveCard.CurrentState?.Points);
            Assert.True(form.LegendShown);
            Assert.Equal("開始", form.ActionLabel);
            Assert.False(form.Board.IsInteractive);
            form.Close();
        });
    }

    [Fact]
    public void ShowGameResult_shows_two_level_cards_and_replay_action()
    {
        new StaRunner().Run(() =>
        {
            using MainWindow form = CreateForm(out Game game, out ManualTimeProvider clock);
            form.Show();
            PassFirstLevel(game, clock);
            game.StartSecondLevel();
            game.HitHole(1);
            clock.Advance(TimeSpan.FromSeconds(60));
            game.UpdateTime();
            form.ShowGameResult();
            Assert.Equal("SecondFailed", form.CurrentScreen);
            Assert.Equal(42, form.FirstResultCard.CurrentState?.Points);
            Assert.Equal(1, form.SecondResultCard.CurrentState?.Points);
            Assert.Equal(1, form.SecondResultCard.CurrentState?.MouseHits);
            Assert.Equal(0, form.SecondResultCard.CurrentState?.BombHits);
            Assert.Equal("重玩", form.ActionLabel);
            Assert.False(form.LegendShown);
            Assert.False(form.Board.IsInteractive);
            form.Close();
        });
    }

    [Fact]
    public void Won_result_shows_both_cards_and_success_heading()
    {
        new StaRunner().Run(() =>
        {
            using MainWindow form = CreateForm(out Game game, out ManualTimeProvider clock);
            form.Show();
            PassFirstLevel(game, clock);
            game.StartSecondLevel();
            for (int index = 0; index < 52; index++)
            {
                Assert.True(game.HitHole(1));
                clock.Advance(TimeSpan.FromMilliseconds(700));
                game.UpdateTime();
            }

            clock.Advance(TimeSpan.FromSeconds(60));
            game.UpdateTime();
            form.ShowGameResult();
            Assert.Equal("SecondWon", form.CurrentScreen);
            Assert.Equal(42, form.FirstResultCard.CurrentState?.Points);
            Assert.Equal(52, form.SecondResultCard.CurrentState?.Points);
            Assert.Equal("重玩", form.ActionLabel);
            Assert.False(form.LegendShown);
            form.Close();
        });
    }

    [Fact]
    public void Screen_input_and_phase_violations_leave_the_previous_screen_intact()
    {
        new StaRunner().Run(() =>
        {
            using MainWindow form = CreateForm(out Game game, out ManualTimeProvider clock);
            form.Show();
            Assert.Throws<InvalidOperationException>(() => form.ShowPlayingScreen(1));
            Assert.Throws<InvalidOperationException>(() => form.ShowSecondLevelReadyScreen());
            Assert.Throws<ArgumentOutOfRangeException>(() => form.RenderTarget(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => form.UpdateScoreboard(3));
            Assert.Equal("FirstReady", form.CurrentScreen);
            Assert.Null(form.Board.RenderedTarget);
            Assert.Equal("開始", form.ActionLabel);
            Assert.Equal(0, form.FirstLiveCard.CurrentState?.Points);
            form.Close();
        });
    }

    private MainWindow CreateForm(out Game game, out ManualTimeProvider clock)
    {
        clock = new ManualTimeProvider();
        game = new Game(clock, new ScriptedRandom());
        return new MainWindow(game);
    }

    private void PassFirstLevel(Game game, ManualTimeProvider clock)
    {
        game.StartFirstLevel();
        for (int index = 0; index < 42; index++)
        {
            Assert.True(game.HitHole(1));
            clock.Advance(TimeSpan.FromMilliseconds(700));
            game.UpdateTime();
        }

        clock.Advance(TimeSpan.FromSeconds(60));
        game.UpdateTime();
        Assert.Equal(LevelPhase.Passed, game.GetLevelState(1).Phase);
    }
}
