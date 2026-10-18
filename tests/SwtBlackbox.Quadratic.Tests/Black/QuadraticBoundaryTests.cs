namespace SwtBlackbox.Quadratic.Tests.Black;

/// <summary>
///     Supplementary quadratic cases outside the designed suite: coefficient-domain validation, in-domain edge
///     values, discriminant reachability and a few sign patterns. None repeats a coefficient triple of the designed
///     suite.
/// </summary>
public class QuadraticBoundaryTests
{
    public static IEnumerable<TheoryDataRow<int, int, int, string>> CoefficientOutOfRangeThrows =>
    [
        new TheoryDataRow<int, int, int, string>(-101, 1, 1, "a")
            .WithTestDisplayName("SUP-QE-01: a = -101 throws ArgumentOutOfRangeException"),
        new TheoryDataRow<int, int, int, string>(101, 1, 1, "a")
            .WithTestDisplayName("SUP-QE-02: a = 101 throws ArgumentOutOfRangeException"),
        new TheoryDataRow<int, int, int, string>(1, -101, 1, "b")
            .WithTestDisplayName("SUP-QE-03: b = -101 throws ArgumentOutOfRangeException"),
        new TheoryDataRow<int, int, int, string>(1, 101, 1, "b")
            .WithTestDisplayName("SUP-QE-04: b = 101 throws ArgumentOutOfRangeException"),
        new TheoryDataRow<int, int, int, string>(1, 1, -101, "c")
            .WithTestDisplayName("SUP-QE-05: c = -101 throws ArgumentOutOfRangeException"),
        new TheoryDataRow<int, int, int, string>(1, 1, 101, "c")
            .WithTestDisplayName("SUP-QE-06: c = 101 throws ArgumentOutOfRangeException")
    ];

    public static IEnumerable<TheoryDataRow<int, int, int, QuadraticResult>> CoefficientBoundaryDoesNotThrow =>
    [
        new TheoryDataRow<int, int, int, QuadraticResult>(-100, 1, 1, QuadraticResult.TwoDistinctRealRoots)
            .WithTestDisplayName("SUP-QE-07: a=-100 allowed (TwoDistinctRealRoots)"),
        new TheoryDataRow<int, int, int, QuadraticResult>(100, 1, 1, QuadraticResult.NoRealRoots)
            .WithTestDisplayName("SUP-QE-08: a=100 allowed (NoRealRoots)"),
        new TheoryDataRow<int, int, int, QuadraticResult>(1, -100, 1, QuadraticResult.TwoDistinctRealRoots)
            .WithTestDisplayName("SUP-QE-09: b=-100 allowed (TwoDistinctRealRoots)"),
        new TheoryDataRow<int, int, int, QuadraticResult>(1, 100, 1, QuadraticResult.TwoDistinctRealRoots)
            .WithTestDisplayName("SUP-QE-10: b=100 allowed (TwoDistinctRealRoots)"),
        new TheoryDataRow<int, int, int, QuadraticResult>(1, 1, -100, QuadraticResult.TwoDistinctRealRoots)
            .WithTestDisplayName("SUP-QE-11: c=-100 allowed (TwoDistinctRealRoots)"),
        new TheoryDataRow<int, int, int, QuadraticResult>(1, 1, 100, QuadraticResult.NoRealRoots)
            .WithTestDisplayName("SUP-QE-12: c=100 allowed (NoRealRoots)"),
        new TheoryDataRow<int, int, int, QuadraticResult>(0, -100, 0, QuadraticResult.OneLinearRoot)
            .WithTestDisplayName("SUP-QE-13: a=0,b=-100,c=0 allowed (OneLinearRoot)"),
        new TheoryDataRow<int, int, int, QuadraticResult>(0, 0, -100, QuadraticResult.NoSolution)
            .WithTestDisplayName("SUP-QE-14: a=0,b=0,c=-100 allowed (NoSolution)"),
        new TheoryDataRow<int, int, int, QuadraticResult>(0, 0, 100, QuadraticResult.NoSolution)
            .WithTestDisplayName("SUP-QE-15: a=0,b=0,c=100 allowed (NoSolution)")
    ];

    public static IEnumerable<TheoryDataRow<int, int, int, long>> DiscriminantReachability =>
    [
        new TheoryDataRow<int, int, int, long>(1, 4, 3, 4)
            .WithTestDisplayName("SUP-QE-16: discriminant (1,4,3) -> 4"),
        new TheoryDataRow<int, int, int, long>(100, 0, 100, -40000)
            .WithTestDisplayName("SUP-QE-17: discriminant (100,0,100) -> -40000"),
        new TheoryDataRow<int, int, int, long>(-100, 100, 100, 50000)
            .WithTestDisplayName("SUP-QE-18: discriminant (-100,100,100) -> 50000")
    ];

    public static IEnumerable<TheoryDataRow<int, int, int, QuadraticResult>> EdgeCases =>
    [
        new TheoryDataRow<int, int, int, QuadraticResult>(-1, 0, -1, QuadraticResult.NoRealRoots)
            .WithTestDisplayName("SUP-QE-19: a<0,b=0,c<0 -> NoRealRoots")
    ];

    [Theory]
    [MemberData(nameof(CoefficientOutOfRangeThrows))]
    public void Solve_CoefficientOutOfRange_Throws(int a, int b, int c, string paramName)
    {
        Assert.Throws<ArgumentOutOfRangeException>(paramName, () => QuadraticSolver.Solve(a, b, c));
    }

    [Theory]
    [MemberData(nameof(CoefficientBoundaryDoesNotThrow))]
    public void Solve_CoefficientBoundary_Allowed(int a, int b, int c, QuadraticResult expected)
    {
        var actual = QuadraticSolver.Solve(a, b, c);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(DiscriminantReachability))]
    public void Discriminant_ReachabilityCases_Match(int a, int b, int c, long expected)
    {
        var actual = QuadraticSolver.Discriminant(a, b, c);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(EdgeCases))]
    public void Solve_EdgeCases_Expected(int a, int b, int c, QuadraticResult expected)
    {
        var actual = QuadraticSolver.Solve(a, b, c);
        Assert.Equal(expected, actual);
    }
}
