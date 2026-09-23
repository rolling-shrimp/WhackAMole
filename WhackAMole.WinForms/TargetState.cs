namespace WhackAMole;

public class TargetState
{
    public TargetState(int holeId, TargetKind kind, long generation)
    {
        HoleId = holeId;
        Kind = kind;
        Generation = generation;
    }

    public int HoleId { get; }
    public TargetKind Kind { get; }
    public long Generation { get; }
}
