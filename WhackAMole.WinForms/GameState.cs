namespace WhackAMole;

public class GameState
{
    public GameState(LevelState firstLevel, LevelState secondLevel)
    {
        FirstLevel = firstLevel;
        SecondLevel = secondLevel;
    }

    public LevelState FirstLevel { get; }
    public LevelState SecondLevel { get; }
}
