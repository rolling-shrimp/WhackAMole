using System.Drawing;
using WhackAMole;
using Xunit;

namespace WhackAMole.Tests;

public class InteractionTests
{
    [Fact]
    public void Only_painted_pixels_of_the_current_target_can_score()
    {
        new StaRunner().Run(() =>
        {
            ManualTimeProvider clock = new ManualTimeProvider();
            Game game = new Game(clock, new ScriptedRandom());
            using MainWindow form = new MainWindow(game);
            form.Show();
            form.ActionButton.PerformClick();
            Assert.Equal("FirstPlaying", form.CurrentScreen);
            Assert.Equal(16, CountDistinctHolePositions(form.Board));

            TargetState target = form.Board.RenderedTarget!;
            Rectangle bounds = form.Board.GetTargetBounds(target.HoleId);
            Point transparent = new Point(bounds.Left + 1, bounds.Top + 1);
            Assert.False(form.Board.TryGetClickedTarget(transparent, out _, out _));
            form.HandleBoardClick(transparent);
            form.HandleBoardClick(new Point(0, 0));
            Assert.Equal(0, game.GetLevelState(1).Points);

            Point solid = FindSolidPixel(form.Board, bounds);
            clock.Advance(TimeSpan.FromMilliseconds(700));
            form.HandleBoardClick(solid);
            Assert.Equal(0, game.GetLevelState(1).Points);
            Assert.NotEqual(target.Generation, form.Board.RenderedTarget?.Generation);

            Point currentSolid = FindSolidPixel(form.Board,
                form.Board.GetTargetBounds(form.Board.RenderedTarget!.HoleId));
            form.HandleBoardClick(currentSolid);
            Assert.Equal(1, game.GetLevelState(1).Points);
            Assert.Null(form.Board.RenderedTarget);
            form.HandleBoardClick(currentSolid);
            Assert.Equal(1, game.GetLevelState(1).Points);
            form.Close();
        });
    }

    [Fact]
    public void Level_one_completion_shows_second_rules_before_second_round_starts()
    {
        new StaRunner().Run(() =>
        {
            ManualTimeProvider clock = new ManualTimeProvider();
            Game game = new Game(clock, new ScriptedRandom());
            using MainWindow form = new MainWindow(game);
            form.Show();
            form.ActionButton.PerformClick();
            for (int index = 0; index < 42; index++)
            {
                Point solid = FindSolidPixel(form.Board,
                    form.Board.GetTargetBounds(form.Board.RenderedTarget!.HoleId));
                form.HandleBoardClick(solid);
                clock.Advance(TimeSpan.FromMilliseconds(700));
                form.HandleBoardClick(new Point(0, 0));
            }

            clock.Advance(TimeSpan.FromSeconds(60));
            form.HandleBoardClick(new Point(0, 0));
            Assert.Equal("SecondReady", form.CurrentScreen);
            Assert.True(form.LegendShown);
            Assert.Equal(42, form.FirstResultCard.CurrentState?.Points);
            Assert.Equal(0, form.SecondLiveCard.CurrentState?.Points);
            Assert.Equal("開始", form.ActionLabel);
            Assert.False(form.Board.IsInteractive);
            form.ActionButton.PerformClick();
            Assert.Equal("SecondPlaying", form.CurrentScreen);
            Assert.True(form.LegendShown);
            form.Close();
        });
    }

    private Point FindSolidPixel(GameBoard board, Rectangle bounds)
    {
        for (int y = bounds.Top; y < bounds.Bottom; y++)
        {
            for (int x = bounds.Left; x < bounds.Right; x++)
            {
                Point point = new Point(x, y);
                if (board.TryGetClickedTarget(point, out _, out _))
                {
                    return point;
                }
            }
        }

        throw new Xunit.Sdk.XunitException("No clickable sprite pixel was found");
    }

    private int CountDistinctHolePositions(GameBoard board)
    {
        HashSet<Point> positions = new HashSet<Point>();
        for (int holeId = 1; holeId <= 16; holeId++)
        {
            positions.Add(board.GetTargetBounds(holeId).Location);
        }

        return positions.Count;
    }
}
