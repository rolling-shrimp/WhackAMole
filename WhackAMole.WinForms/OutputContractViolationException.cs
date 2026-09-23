namespace WhackAMole;

public class OutputContractViolationException : Exception
{
    public OutputContractViolationException(string method, string guarantee, string actual)
        : base($"{method}: {guarantee}; actual: {actual}")
    {
        Method = method;
        Guarantee = guarantee;
        Actual = actual;
    }

    public string Method { get; }
    public string Guarantee { get; }
    public string Actual { get; }
}
