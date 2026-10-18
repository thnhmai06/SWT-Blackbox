namespace SoftwareTesting.Quadratic.Tests.White;

/// <summary>
///     Control-flow tests of <see cref="QuadraticSolver.Solve"/>: one test group per feasible entry-to-exit path of
///     the method's control flow graph (docs/control-flow-tests.md, CFG-QE). QP1-QP6 are the six coefficient
///     validations in their actual short-circuit execution order; each case violates exactly one bound so the
///     first encountered failing validation is the one asserted. QP7-QP12 cover the nested a == 0, b != 0, c != 0
///     and delta sign decisions on both arms.
/// </summary>
public class QuadraticControlFlowPathTests
{
    /// <summary>QP1-QP6: coefficient range validation, one row per ThrowIf check (execution order a&lt;min, a&gt;max, b&lt;min, b&gt;max, c&lt;min, c&gt;max).</summary>
    public static IEnumerable<TheoryDataRow<int, int, int, string>> CoefficientOutOfRangeThrows =>
    [
        new TheoryDataRow<int, int, int, string>(-101, 0, 0, "a")
            .WithTestDisplayName("CFT-QE-QP1: a = -101 < MinCoefficient -> throws (a)")
            .WithTrait("Path", "QP1"),
        new TheoryDataRow<int, int, int, string>(101, 0, 0, "a")
            .WithTestDisplayName("CFT-QE-QP2: a = 101 > MaxCoefficient -> throws (a)")
            .WithTrait("Path", "QP2"),
        new TheoryDataRow<int, int, int, string>(0, -101, 0, "b")
            .WithTestDisplayName("CFT-QE-QP3: b = -101 < MinCoefficient -> throws (b)")
            .WithTrait("Path", "QP3"),
        new TheoryDataRow<int, int, int, string>(0, 101, 0, "b")
            .WithTestDisplayName("CFT-QE-QP4: b = 101 > MaxCoefficient -> throws (b)")
            .WithTrait("Path", "QP4"),
        new TheoryDataRow<int, int, int, string>(0, 0, -101, "c")
            .WithTestDisplayName("CFT-QE-QP5: c = -101 < MinCoefficient -> throws (c)")
            .WithTrait("Path", "QP5"),
        new TheoryDataRow<int, int, int, string>(0, 0, 101, "c")
            .WithTestDisplayName("CFT-QE-QP6: c = 101 > MaxCoefficient -> throws (c)")
            .WithTrait("Path", "QP6")
    ];

    /// <summary>QP7-QP12: the six classification paths over the nested a == 0, b != 0, c != 0 and delta sign decisions.</summary>
    public static IEnumerable<TheoryDataRow<int, int, int, QuadraticResult>> PathCases =>
    [
        // QP7: a == 0, b != 0 -> OneLinearRoot (degenerate linear equation).
        new TheoryDataRow<int, int, int, QuadraticResult>(0, 2, -4, QuadraticResult.OneLinearRoot)
            .WithTestDisplayName("CFT-QE-QP7: a=0, b!=0 -> OneLinearRoot")
            .WithTrait("Path", "QP7"),
        // QP8: a == 0, b == 0, c != 0 -> NoSolution (contradiction).
        new TheoryDataRow<int, int, int, QuadraticResult>(0, 0, 5, QuadraticResult.NoSolution)
            .WithTestDisplayName("CFT-QE-QP8: a=0, b=0, c!=0 -> NoSolution")
            .WithTrait("Path", "QP8"),
        // QP9: a == 0, b == 0, c == 0 -> InfinitelyManySolutions (identity).
        new TheoryDataRow<int, int, int, QuadraticResult>(0, 0, 0, QuadraticResult.InfinitelyManySolutions)
            .WithTestDisplayName("CFT-QE-QP9: a=0, b=0, c=0 -> InfinitelyManySolutions")
            .WithTrait("Path", "QP9"),
        // QP10: a != 0, delta = b^2 - 4ac = 1 - 40 = -39 < 0 -> NoRealRoots.
        new TheoryDataRow<int, int, int, QuadraticResult>(1, 1, 10, QuadraticResult.NoRealRoots)
            .WithTestDisplayName("CFT-QE-QP10: a!=0, delta=-39 < 0 -> NoRealRoots")
            .WithTrait("Path", "QP10"),
        // QP11: a != 0, delta = 49 - 24 = 25 > 0 -> TwoDistinctRealRoots.
        new TheoryDataRow<int, int, int, QuadraticResult>(1, 7, 6, QuadraticResult.TwoDistinctRealRoots)
            .WithTestDisplayName("CFT-QE-QP11: a!=0, delta=25 > 0 -> TwoDistinctRealRoots")
            .WithTrait("Path", "QP11"),
        // QP12: a != 0, delta = 4 - 4 = 0 -> OneRepeatedRealRoot.
        new TheoryDataRow<int, int, int, QuadraticResult>(1, 2, 1, QuadraticResult.OneRepeatedRealRoot)
            .WithTestDisplayName("CFT-QE-QP12: a!=0, delta=0 -> OneRepeatedRealRoot")
            .WithTrait("Path", "QP12")
    ];

    [Theory]
    [MemberData(nameof(CoefficientOutOfRangeThrows))]
    public void QP1_QP6_CoefficientOutOfRange_ThrowsArgumentOutOfRange(int a, int b, int c, string paramName)
    {
        Assert.Throws<ArgumentOutOfRangeException>(paramName, () => QuadraticSolver.Solve(a, b, c));
    }

    [Theory]
    [MemberData(nameof(PathCases))]
    public void QP7_QP12_PathCase_ReturnsExpectedResult(int a, int b, int c, QuadraticResult expected)
    {
        var actual = QuadraticSolver.Solve(a, b, c);
        Assert.Equal(expected, actual);
    }
}

/// <summary>
///     Boundary-value companions to the control-flow paths of <see cref="QuadraticControlFlowPathTests"/>: the
///     Min/MaxCoefficient edges of all three coefficients and the reachable discriminant edges. For integer
///     coefficients delta = b^2 - 4ac is always congruent to 0 or 1 (mod 4), so the reachable negative edges are
///     -4 and -3 and the reachable positive edges are 1 and 4 (delta = -1, -2, 2, 3 are unreachable).
///     These are boundary tests, not additional path tests.
/// </summary>
public class QuadraticControlFlowBoundaryTests
{
    public static IEnumerable<TheoryDataRow<int, int, int, QuadraticResult>> CoefficientBoundaryDoesNotThrow =>
    [
        new TheoryDataRow<int, int, int, QuadraticResult>(-100, 0, 0, QuadraticResult.OneRepeatedRealRoot)
            .WithTestDisplayName("CFT-QE-B1: a = MinCoefficient -100 allowed, delta=0 -> OneRepeatedRealRoot")
            .WithTrait("Boundary", "a Min"),
        new TheoryDataRow<int, int, int, QuadraticResult>(100, 0, 0, QuadraticResult.OneRepeatedRealRoot)
            .WithTestDisplayName("CFT-QE-B2: a = MaxCoefficient 100 allowed, delta=0 -> OneRepeatedRealRoot")
            .WithTrait("Boundary", "a Max"),
        new TheoryDataRow<int, int, int, QuadraticResult>(0, -100, 0, QuadraticResult.OneLinearRoot)
            .WithTestDisplayName("CFT-QE-B3: b = MinCoefficient -100 allowed -> OneLinearRoot")
            .WithTrait("Boundary", "b Min"),
        new TheoryDataRow<int, int, int, QuadraticResult>(0, 100, 0, QuadraticResult.OneLinearRoot)
            .WithTestDisplayName("CFT-QE-B4: b = MaxCoefficient 100 allowed -> OneLinearRoot")
            .WithTrait("Boundary", "b Max"),
        new TheoryDataRow<int, int, int, QuadraticResult>(0, 0, -100, QuadraticResult.NoSolution)
            .WithTestDisplayName("CFT-QE-B5: c = MinCoefficient -100 allowed -> NoSolution")
            .WithTrait("Boundary", "c Min"),
        new TheoryDataRow<int, int, int, QuadraticResult>(0, 0, 100, QuadraticResult.NoSolution)
            .WithTestDisplayName("CFT-QE-B6: c = MaxCoefficient 100 allowed -> NoSolution")
            .WithTrait("Boundary", "c Max")
    ];

    public static IEnumerable<TheoryDataRow<int, int, int, long, QuadraticResult>> DiscriminantBoundary =>
    [
        new TheoryDataRow<int, int, int, long, QuadraticResult>(1, 0, 1, -4, QuadraticResult.NoRealRoots)
            .WithTestDisplayName("CFT-QE-B7: delta = -4 (reachable negative edge) -> NoRealRoots")
            .WithTrait("Boundary", "delta -4"),
        new TheoryDataRow<int, int, int, long, QuadraticResult>(1, 1, 1, -3, QuadraticResult.NoRealRoots)
            .WithTestDisplayName("CFT-QE-B8: delta = -3 (reachable negative edge) -> NoRealRoots")
            .WithTrait("Boundary", "delta -3"),
        new TheoryDataRow<int, int, int, long, QuadraticResult>(1, 2, 1, 0, QuadraticResult.OneRepeatedRealRoot)
            .WithTestDisplayName("CFT-QE-B9: delta = 0 (double root edge) -> OneRepeatedRealRoot")
            .WithTrait("Boundary", "delta 0"),
        new TheoryDataRow<int, int, int, long, QuadraticResult>(1, 3, 2, 1, QuadraticResult.TwoDistinctRealRoots)
            .WithTestDisplayName("CFT-QE-B10: delta = 1 (reachable positive edge) -> TwoDistinctRealRoots")
            .WithTrait("Boundary", "delta 1"),
        new TheoryDataRow<int, int, int, long, QuadraticResult>(1, 0, -1, 4, QuadraticResult.TwoDistinctRealRoots)
            .WithTestDisplayName("CFT-QE-B11: delta = 4 (reachable positive edge) -> TwoDistinctRealRoots")
            .WithTrait("Boundary", "delta 4")
    ];

    [Theory]
    [MemberData(nameof(CoefficientBoundaryDoesNotThrow))]
    public void CoefficientBoundary_ReturnsExpectedResult(int a, int b, int c, QuadraticResult expected)
    {
        var actual = QuadraticSolver.Solve(a, b, c);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(DiscriminantBoundary))]
    public void DiscriminantBoundary_ReturnsExpectedResult(int a, int b, int c, long expectedDelta,
        QuadraticResult expected)
    {
        Assert.Equal(expectedDelta, QuadraticSolver.Discriminant(a, b, c));
        Assert.Equal(expected, QuadraticSolver.Solve(a, b, c));
    }
}
