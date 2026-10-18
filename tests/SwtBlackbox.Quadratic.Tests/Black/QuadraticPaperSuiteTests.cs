using SwtBlackbox.Tests;

namespace SwtBlackbox.Quadratic.Tests.Black;

/// <summary>
///     The complete 30-test quadratic suite of report.tex, Case Study II, Test Suite Construction.
///     TC-QE-28 is a three-step crossing sequence and therefore has its own facts.
/// </summary>
public class QuadraticPaperSuiteTests
{
    private const QuadraticResult NoReal = QuadraticResult.NoRealRoots;
    private const QuadraticResult Repeated = QuadraticResult.OneRepeatedRealRoot;
    private const QuadraticResult Two = QuadraticResult.TwoDistinctRealRoots;

    internal static readonly QuadraticPaperCase[] Cases =
    [
        // L1: decision-table core, one test per feasible collapsed rule.
        Case("TC-QE-01", "linear nominal: one root", (0, 2, -4), QuadraticResult.OneLinearRoot, "L1", "QR1"),
        Case("TC-QE-02", "a=b=0, c≠0: no solution", (0, 0, 5), QuadraticResult.NoSolution, "L1", "QR2"),
        Case("TC-QE-03", "a=b=c=0: singleton rule", (0, 0, 0), QuadraticResult.InfinitelyManySolutions, "L1", "QR3"),
        Case("TC-QE-04", "no-real-roots nominal, Δ=-39", (1, 1, 10), NoReal, "L1", "QR4"),
        Case("TC-QE-05", "Δ=0 exactly: every member of the rule is on the boundary", (1, 2, 1), Repeated, "L1",
            "QR5"),
        Case("TC-QE-06", "two-root nominal, Δ=25", (1, 7, 6), Two, "L1", "QR6"),
        Case("TC-QE-07", "c=0 with a≠0, b≠0, Δ=25", (1, 5, 0), Two, "L1", "QR7"),
        Case("TC-QE-08", "b=0 with ac>0, Δ=-24", (2, 0, 3), NoReal, "L1", "QR8"),
        Case("TC-QE-09", "b=0 with ac<0, Δ=24", (-2, 0, 3), Two, "L1", "QR9"),
        Case("TC-QE-10", "family (k,0,0): Δ=0 for every member", (1, 0, 0), Repeated, "L1", "QR10"),

        // L2: each equality condition x=0 moved to its neighbours -1 and +1, taken from every rule on the 0 side
        // in which the move changes the output (a neighbour already in the suite is not repeated).
        Case("TC-QE-11", "q1 from QR1: a=-1, Δ=-12", (-1, 2, -4), NoReal, "L2", "QR4", "q1: a=-1"),
        Case("TC-QE-12", "q1 from QR1: a=+1, Δ=20", (1, 2, -4), Two, "L2", "QR6", "q1: a=+1"),
        Case("TC-QE-13", "q1 from QR2: a=-1, Δ=20", (-1, 0, 5), Two, "L2", "QR9", "q1: a=-1"),
        Case("TC-QE-14", "q1 from QR2: a=+1, Δ=-20", (1, 0, 5), NoReal, "L2", "QR8", "q1: a=+1"),
        Case("TC-QE-15", "q1 from QR3: a=-1, Δ=0 (a=+1 is TC-QE-10)", (-1, 0, 0), Repeated, "L2", "QR10",
            "q1: a=-1"),
        Case("TC-QE-16", "q2 from QR2: b=-1", (0, -1, 5), QuadraticResult.OneLinearRoot, "L2", "QR1", "q2: b=-1"),
        Case("TC-QE-17", "q2 from QR2: b=+1", (0, 1, 5), QuadraticResult.OneLinearRoot, "L2", "QR1", "q2: b=+1"),
        Case("TC-QE-18", "q2 from QR3: b=-1", (0, -1, 0), QuadraticResult.OneLinearRoot, "L2", "QR1", "q2: b=-1"),
        Case("TC-QE-19", "q2 from QR3: b=+1", (0, 1, 0), QuadraticResult.OneLinearRoot, "L2", "QR1", "q2: b=+1"),
        Case("TC-QE-20", "q3 from QR3: c=-1", (0, 0, -1), QuadraticResult.NoSolution, "L2", "QR2", "q3: c=-1"),
        Case("TC-QE-21", "q3 from QR3: c=+1", (0, 0, 1), QuadraticResult.NoSolution, "L2", "QR2", "q3: c=+1"),
        Case("TC-QE-22", "q2 from QR10: b=-1, Δ=1", (1, -1, 0), Two, "L2", "QR7", "q2: b=-1"),
        Case("TC-QE-23", "q2 from QR10: b=+1, Δ=1", (1, 1, 0), Two, "L2", "QR7", "q2: b=+1"),
        Case("TC-QE-24", "q3 from QR10: c=-1, nearest reachable positive Δ=4 with b=0", (1, 0, -1), Two, "L2", "QR9",
            "q3: c=-1"),
        Case("TC-QE-25", "q3 from QR10: c=+1, nearest reachable negative Δ=-4 with b=0", (1, 0, 1), NoReal, "L2",
            "QR8", "q3: c=+1"),
        Case("TC-QE-26", "q4: nearest reachable negative Δ=-3", (1, 1, 1), NoReal, "L2", "QR4", "q4: Δ=-3"),
        Case("TC-QE-27", "q4: nearest reachable positive Δ=1", (1, 3, 2), Two, "L2", "QR6", "q4: Δ=+1"),

        // L3: extension. TC-QE-28 is the b-only crossing NoReal -> Repeated -> Two (Δ -3 -> 0 -> 5).
        new("TC-QE-28", "crossing by b only, Δ=-3→0→5", [(1, 1, 1), (1, 2, 1), (1, 3, 1)], [NoReal, Repeated, Two],
            "L3", "QR4->QR5->QR6", null, "QC1"),
        Case("TC-QE-29", "Δ=0 with all three coefficients doubled", (2, 4, 2), Repeated, "L3", "QR5", combo: "QC2"),
        Case("TC-QE-30", "Δ=0 with a=-1, a neighbour of a=0", (-1, 2, -1), Repeated, "L3", "QR5", combo: "QC3")
    ];

    public static IEnumerable<TheoryDataRow<int, int, int, QuadraticResult>> PaperSuite =>
        Cases.Where(c => c.Steps.Length == 1 && PaperLayers.Includes(c.Layer)).Select(c => c.ToTheoryDataRow());

    [Theory]
    [MemberData(nameof(PaperSuite))]
    public void Solve_PaperSuiteCase_ReturnsSpecifiedResult(int a, int b, int c, QuadraticResult expected)
    {
        var actual = QuadraticSolver.Solve(a, b, c);

        Assert.Equal(expected, actual);
    }

    // TC-QE-28 (QC1): a=1 and c=1 fixed, only b increments, so a purely quantitative change crosses Δ=0.
    [Fact]
    [Trait("PaperId", "TC-QE-28")]
    [Trait("Layer", "L3")]
    [Trait("Rule", "QR4->QR5->QR6")]
    [Trait("Combo", "QC1")]
    public void Solve_TC_QE_28_CrossingByBOnly_GoesNoRealToRepeatedToTwo()
    {
        Assert.SkipUnless(PaperLayers.Includes("L3"), "Layer L3 not selected by SWT_LAYERS");

        QuadraticResult[] actual =
            [QuadraticSolver.Solve(1, 1, 1), QuadraticSolver.Solve(1, 2, 1), QuadraticSolver.Solve(1, 3, 1)];

        Assert.Equal([NoReal, Repeated, Two], actual);
    }

    [Fact]
    [Trait("PaperId", "TC-QE-28")]
    [Trait("Layer", "L3")]
    [Trait("Rule", "QR4->QR5->QR6")]
    [Trait("Combo", "QC1")]
    public void Discriminant_TC_QE_28_CrossingByBOnly_GoesMinus3To0To5()
    {
        Assert.SkipUnless(PaperLayers.Includes("L3"), "Layer L3 not selected by SWT_LAYERS");

        long[] actual =
        [
            QuadraticSolver.Discriminant(1, 1, 1), QuadraticSolver.Discriminant(1, 2, 1),
            QuadraticSolver.Discriminant(1, 3, 1)
        ];

        Assert.Equal([-3L, 0L, 5L], actual);
    }

    private static QuadraticPaperCase Case(
        string id, string rationale, (int A, int B, int C) coefficients, QuadraticResult expected,
        string layer, string rule, string? boundary = null, string? combo = null)
    {
        return new QuadraticPaperCase(id, rationale, [coefficients], [expected], layer, rule, boundary, combo);
    }
}

/// <summary>One row of the quadratic suite; TC-QE-28 is the only row with several steps.</summary>
internal sealed record QuadraticPaperCase(
    string Id,
    string Rationale,
    (int A, int B, int C)[] Steps,
    QuadraticResult[] Expected,
    string Layer,
    string Rule,
    string? Boundary,
    string? Combo)
{
    public TheoryDataRow<int, int, int, QuadraticResult> ToTheoryDataRow()
    {
        var (a, b, c) = Steps.Single();
        var row = new TheoryDataRow<int, int, int, QuadraticResult>(a, b, c, Expected.Single())
            .WithTestDisplayName($"{Id}: ({a},{b},{c}) {Rationale} -> {Expected.Single()}")
            .WithTrait("PaperId", Id)
            .WithTrait("Layer", Layer)
            .WithTrait("Rule", Rule);
        if (Boundary is not null)
            row.WithTrait("Boundary", Boundary);
        if (Combo is not null)
            row.WithTrait("Combo", Combo);
        return row;
    }
}
