namespace WhackAMole.Tests;

public class ScriptedRandom : Random
{
    private Queue<int> _values;

    public ScriptedRandom(params int[] values)
    {
        _values = new Queue<int>(values);
    }

    public void Enqueue(params int[] values)
    {
        foreach (int value in values)
        {
            _values.Enqueue(value);
        }
    }

    public override int Next(int maxValue)
    {
        return Take(0, maxValue);
    }

    public override int Next(int minValue, int maxValue)
    {
        return Take(minValue, maxValue);
    }

    private int Take(int minValue, int maxValue)
    {
        if (_values.Count == 0)
        {
            return minValue;
        }

        int value = _values.Dequeue();
        if (value < minValue || value >= maxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value,
                $"Scripted value must be in [{minValue}, {maxValue})");
        }

        return value;
    }
}
