namespace BVA_Boundry.Quadratic;

/// <summary>Output domain D_out of the solver (report.tex §5.2).</summary>
public enum QuadraticResult
{
    NoRealRoots,
    OneRepeatedRealRoot,
    TwoDistinctRealRoots,
    OneLinearRoot,
    NoSolution,
    InfinitelyManySolutions
}

/// <summary>Classifies the real solutions of ax² + bx + c = 0 (report.tex §5.2).</summary>
public static class QuadraticSolver
{
    public const int MinCoefficient = -100;
    public const int MaxCoefficient = 100;

    /// <summary>Δ = b² − 4ac, computed in 64-bit so it never overflows for any <see cref="int"/> inputs.</summary>
    public static long Discriminant(int a, int b, int c) => (long)b * b - 4L * a * c;

    /// <summary>
    /// Case A (a ≠ 0): sign of Δ selects NoRealRoots / OneRepeatedRealRoot / TwoDistinctRealRoots.
    /// Case B (a = 0): nested split on b, then c, selects OneLinearRoot / NoSolution / InfinitelyManySolutions.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A coefficient lies outside [<see cref="MinCoefficient"/>, <see cref="MaxCoefficient"/>].</exception>
    public static QuadraticResult Solve(int a, int b, int c)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(a, MinCoefficient);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(a, MaxCoefficient);
        ArgumentOutOfRangeException.ThrowIfLessThan(b, MinCoefficient);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(b, MaxCoefficient);
        ArgumentOutOfRangeException.ThrowIfLessThan(c, MinCoefficient);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(c, MaxCoefficient);

        if (a == 0)
        {
            if (b != 0)
                return QuadraticResult.OneLinearRoot;
            return c != 0 ? QuadraticResult.NoSolution : QuadraticResult.InfinitelyManySolutions;
        }

        long delta = Discriminant(a, b, c);
        if (delta < 0)
            return QuadraticResult.NoRealRoots;
        if (delta > 0)
            return QuadraticResult.TwoDistinctRealRoots;
        return QuadraticResult.OneRepeatedRealRoot;
    }
}
