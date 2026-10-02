namespace SwtBlackbox.Quadratic.Tests;

/// <summary>
///     The complete 16-test quadratic suite of report.tex §5.10 (Table "Quadratic complete test suite", Appendix G).
///     TC-QE-08 is a three-step crossing sequence and therefore has its own facts.
/// </summary>
public class QuadraticPaperSuiteTests
{
    private const QuadraticResult NoReal = QuadraticResult.NoRealRoots;
    private const QuadraticResult Repeated = QuadraticResult.OneRepeatedRealRoot;
    private const QuadraticResult Two = QuadraticResult.TwoDistinctRealRoots;

    internal static readonly QuadraticPaperCase[] Cases =
    [
        Case("TC-QE-01", "two-root nominal, Δ=25", (1, 7, 6), Two, "P_Δ+"),
        Case("TC-QE-02", "Δ=0 exactly, canonical repeated-root boundary", (1, 2, 1), Repeated, "P_Δ0", "Δ=0", "Q5"),
        Case("TC-QE-03", "nearest reachable negative Δ=-3", (1, 1, 1), NoReal, "P_Δ-", "nearest reachable Δ=-3", "Q4"),
        Case("TC-QE-04", "linear nominal", (0, 2, -4), QuadraticResult.OneLinearRoot, "P_a0 ∩ P_b≠0", combo: "Q3"),
        Case("TC-QE-05", "a=0, b=0, c≠0", (0, 0, 5), QuadraticResult.NoSolution, "P_b0,c≠0", "b=0, c≠0", "Q2"),
        Case("TC-QE-06", "all coefficients zero", (0, 0, 0), QuadraticResult.InfinitelyManySolutions, "P_b0,c0",
            "a=b=c=0", "Q1"),
        Case("TC-QE-07", "nearest reachable positive Δ=1", (1, 3, 2), Two, "P_Δ+", "nearest reachable Δ=1", "Q6"),
        new("TC-QE-08", "crossing by b only, Δ=-3→0→5", [(1, 1, 1), (1, 2, 1), (1, 3, 1)], [NoReal, Repeated, Two],
            "P_Δ- → P_Δ0 → P_Δ+", "full crossing", "Q7"),
        Case("TC-QE-09", "Δ=0 via a different triple (16-16)", (2, 4, 2), Repeated, "P_Δ0", "Δ=0 via different triple",
            "Q8"),
        Case("TC-QE-10", "negative a, Δ=4", (-1, 0, 1), Two, "P_a- ∩ P_Δ+", "a sign boundary", "Q9"),
        Case("TC-QE-11", "extreme leading coefficient, Δ=-399", (100, 1, 1), NoReal, "P_a+ domain edge", "a=100"),
        Case("TC-QE-12", "extreme negative leading coefficient, Δ=401", (-100, 1, 1), Two, "P_a- domain edge",
            "a=-100"),
        Case("TC-QE-13", "b at domain edge, Δ=9996", (1, 100, 1), Two, "P_Δ+, b domain edge", "b=100"),
        Case("TC-QE-14", "c=0 has no special status once b≠0", (0, 100, 0), QuadraticResult.OneLinearRoot,
            "P_b≠0, b domain edge, c=0", "a=0, c=0, b≠0"),
        Case("TC-QE-15", "b=0 with a≠0, reachable Δ=-4", (1, 0, 1), NoReal, "P_Δ-, b=0 with a≠0", "reachable Δ=-4"),
        Case("TC-QE-16", "no-real-roots nominal, Δ=-39", (1, 1, 10), NoReal, "P_Δ-")
    ];

    public static IEnumerable<TheoryDataRow<int, int, int, QuadraticResult>> PaperSuite =>
        Cases.Where(c => c.Steps.Length == 1).Select(c => c.ToTheoryDataRow());

    [Theory]
    [MemberData(nameof(PaperSuite))]
    public void Solve_PaperSuiteCase_ReturnsSpecifiedResult(int a, int b, int c, QuadraticResult expected)
    {
        var actual = QuadraticSolver.Solve(a, b, c);

        Assert.Equal(expected, actual);
    }

    // TC-QE-08 (Q7): a=1 and c=1 fixed, only b increments, so a purely quantitative change crosses Δ=0.
    [Fact]
    [Trait("PaperId", "TC-QE-08")]
    [Trait("Combo", "Q7")]
    public void Solve_TC_QE_08_CrossingByBOnly_GoesNoRealToRepeatedToTwo()
    {
        QuadraticResult[] actual =
            [QuadraticSolver.Solve(1, 1, 1), QuadraticSolver.Solve(1, 2, 1), QuadraticSolver.Solve(1, 3, 1)];

        Assert.Equal([NoReal, Repeated, Two], actual);
    }

    [Fact]
    [Trait("PaperId", "TC-QE-08")]
    [Trait("Combo", "Q7")]
    public void Discriminant_TC_QE_08_CrossingByBOnly_GoesMinus3To0To5()
    {
        long[] actual =
        [
            QuadraticSolver.Discriminant(1, 1, 1), QuadraticSolver.Discriminant(1, 2, 1),
            QuadraticSolver.Discriminant(1, 3, 1)
        ];

        Assert.Equal([-3L, 0L, 5L], actual);
    }

    private static QuadraticPaperCase Case(
        string id, string rationale, (int A, int B, int C) coefficients, QuadraticResult expected,
        string partition, string? boundary = null, string? combo = null)
    {
        return new QuadraticPaperCase(id, rationale, [coefficients], [expected], partition, boundary, combo);
    }
}

/// <summary>One row of the paper's quadratic suite; TC-QE-08 is the only row with several steps.</summary>
internal sealed record QuadraticPaperCase(
    string Id,
    string Rationale,
    (int A, int B, int C)[] Steps,
    QuadraticResult[] Expected,
    string Partition,
    string? Boundary,
    string? Combo)
{
    public TheoryDataRow<int, int, int, QuadraticResult> ToTheoryDataRow()
    {
        var (a, b, c) = Steps.Single();
        var row = new TheoryDataRow<int, int, int, QuadraticResult>(a, b, c, Expected.Single())
            .WithTestDisplayName($"{Id}: ({a},{b},{c}) {Rationale} -> {Expected.Single()}")
            .WithTrait("PaperId", Id)
            .WithTrait("Partition", Partition);
        if (Boundary is not null)
            row.WithTrait("Boundary", Boundary);
        if (Combo is not null)
            row.WithTrait("Combo", Combo);
        return row;
    }
}