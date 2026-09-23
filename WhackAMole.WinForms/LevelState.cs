namespace WhackAMole;

public class LevelState
{
    public LevelState(int levelNumber, LevelPhase phase, TimeSpan remaining, int points,
        int mouseHits, int hamsterHits, int bombHits, TargetState? activeTarget)
    {
        LevelNumber = levelNumber;
        Phase = phase;
        Remaining = remaining;
        Points = points;
        MouseHits = mouseHits;
        HamsterHits = hamsterHits;
        BombHits = bombHits;
        ActiveTarget = activeTarget;
    }

    public int LevelNumber { get; }
    public LevelPhase Phase { get; }
    public TimeSpan Remaining { get; }
    public int Points { get; }
    public int MouseHits { get; }
    public int HamsterHits { get; }
    public int BombHits { get; }
    public TargetState? ActiveTarget { get; }
}
