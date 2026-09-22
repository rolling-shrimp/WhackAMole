namespace WhackAMole.Core;

public enum Phase { ReadyFirst, PlayingFirst, ReadySecond, PlayingSecond, LostFirst, Won, LostSecond }
public enum TargetKind { Mouse, Hamster, Bomb }
public sealed record Target(int Hole, TargetKind Kind);
public sealed record Score(int Points = 0, int Mice = 0, int Hamsters = 0, int Bombs = 0);

public sealed class Game
{
    public static readonly TimeSpan RoundLength = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan TargetLength = TimeSpan.FromMilliseconds(700);
    private readonly TimeProvider clock;
    private readonly Random random;
    private long startedAt;
    private long currentSlot = -1;

    public Game(TimeProvider? clock = null, Random? random = null)
    {
        this.clock = clock ?? TimeProvider.System;
        this.random = random ?? Random.Shared;
    }

    public Phase Phase { get; private set; } = Phase.ReadyFirst;
    public Score First { get; private set; } = new();
    public Score Second { get; private set; } = new();
    public Target? ActiveTarget { get; private set; }
    public TimeSpan Remaining { get; private set; } = RoundLength;
    public bool IsPlaying => Phase is Phase.PlayingFirst or Phase.PlayingSecond;

    public void Start()
    {
        if (Phase is not (Phase.ReadyFirst or Phase.ReadySecond)) return;
        Phase = Phase == Phase.ReadyFirst ? Phase.PlayingFirst : Phase.PlayingSecond;
        startedAt = clock.GetTimestamp();
        currentSlot = -1;
        Remaining = RoundLength;
        Tick();
    }

    public void Tick()
    {
        if (!IsPlaying) return;
        var elapsed = clock.GetElapsedTime(startedAt);
        if (elapsed >= RoundLength)
        {
            Remaining = TimeSpan.Zero;
            ActiveTarget = null;
            Phase = Phase == Phase.PlayingFirst
                ? (First.Points >= 42 ? Phase.ReadySecond : Phase.LostFirst)
                : (Second.Points >= 52 ? Phase.Won : Phase.LostSecond);
            return;
        }

        Remaining = RoundLength - elapsed;
        var slot = elapsed.Ticks / TargetLength.Ticks;
        if (slot == currentSlot) return;
        currentSlot = slot;
        ActiveTarget = new Target(random.Next(16),
            Phase == Phase.PlayingFirst ? TargetKind.Mouse : (TargetKind)random.Next(3));
    }

    public bool Hit(int hole)
    {
        // A click belongs to the target actually displayed before this event.
        var displayedTarget = ActiveTarget;
        var displayedSlot = currentSlot;
        Tick();
        if (!IsPlaying || displayedSlot != currentSlot || displayedTarget is null ||
            !ReferenceEquals(displayedTarget, ActiveTarget) || displayedTarget.Hole != hole) return false;

        var score = Phase == Phase.PlayingFirst ? First : Second;
        score = displayedTarget.Kind switch
        {
            TargetKind.Mouse => score with { Points = score.Points + 1, Mice = score.Mice + 1 },
            TargetKind.Hamster => score with { Points = score.Points + 3, Hamsters = score.Hamsters + 1 },
            TargetKind.Bomb => score with { Points = Math.Max(0, score.Points - 2), Bombs = score.Bombs + 1 },
            _ => throw new InvalidOperationException()
        };
        if (Phase == Phase.PlayingFirst) First = score;
        else Second = score;
        ActiveTarget = null;
        return true;
    }

    public void Replay()
    {
        if (Phase is not (Phase.LostFirst or Phase.Won or Phase.LostSecond)) return;
        First = new();
        Second = new();
        ActiveTarget = null;
        Phase = Phase.ReadyFirst;
        Start();
    }
}
