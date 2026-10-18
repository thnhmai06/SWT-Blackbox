using SwtBlackbox.Tests;

namespace SwtBlackbox.Quadratic.Tests.Black;

/// <summary>
///     Executes the analytically expected quadratic Test×Mutant matrix (report.tex, Case Study II, Test–Mutant
///     Analysis): every mutant is run against the 30-test suite and its kill set must equal the K cells of its column.
///     A test kills a mutant when any of its steps (TC-QE-28 has three) is classified differently.
/// </summary>
public class QuadraticMutationMatrixTests
{
    // Analytically derived Test×Mutant matrix, transcribed verbatim from report.tex (columns N1…N10).
    private const string PaperMatrix = """
                                       TC-QE-01: -- -- -- K -- -- -- -- K --
                                       TC-QE-02: -- -- -- K -- -- -- K -- --
                                       TC-QE-03: -- -- -- K -- -- K -- -- --
                                       TC-QE-04: S S S K K S -- -- -- S
                                       TC-QE-05: K K K K S S -- -- -- K
                                       TC-QE-06: S S S K K K -- -- -- K
                                       TC-QE-07: S S S K K S -- -- -- S
                                       TC-QE-08: S S S K K S -- -- -- K
                                       TC-QE-09: S S S K K S -- -- -- K
                                       TC-QE-10: S K K K S S -- -- -- S
                                       TC-QE-11: S S S K K S -- -- -- K
                                       TC-QE-12: S S S K K S -- -- -- S
                                       TC-QE-13: S S S K K S -- -- -- K
                                       TC-QE-14: S S S K K S -- -- -- K
                                       TC-QE-15: S K K K S S -- -- -- S
                                       TC-QE-16: -- -- -- K -- -- -- -- K --
                                       TC-QE-17: -- -- -- K -- -- -- -- K --
                                       TC-QE-18: -- -- -- K -- -- -- -- K --
                                       TC-QE-19: -- -- -- K -- -- -- -- K --
                                       TC-QE-20: -- -- -- K -- -- -- K -- --
                                       TC-QE-21: -- -- -- K -- -- -- K -- --
                                       TC-QE-22: S S S K K K -- -- -- S
                                       TC-QE-23: S S S K K S -- -- -- S
                                       TC-QE-24: S S S K K S -- -- -- K
                                       TC-QE-25: S S S K K S -- -- -- K
                                       TC-QE-26: S S S K K S -- -- -- S
                                       TC-QE-27: S S S K K K -- -- -- K
                                       TC-QE-28: K K K K K S -- -- -- K
                                       TC-QE-29: K K K K S K -- -- -- K
                                       TC-QE-30: K K K K S S -- -- -- K
                                       """;

    private static readonly QuadraticMutant[] Mutants =
        [.. Enum.GetValues<QuadraticMutant>().Where(m => m != QuadraticMutant.None)];

    public static IEnumerable<TheoryDataRow<string>> SuiteIds =>
        QuadraticPaperSuiteTests.Cases.Select(c =>
            new TheoryDataRow<string>(c.Id).WithTestDisplayName($"{c.Id}: model(None) = spec = production"));

    public static IEnumerable<TheoryDataRow<QuadraticMutant>> AllMutants =>
    [
        Row(QuadraticMutant.N1, "Δ = b² − 2ac"),
        Row(QuadraticMutant.N2, "Δ > 0 becomes Δ ≥ 0 for two roots"),
        Row(QuadraticMutant.N3, "Δ < 0 becomes Δ ≤ 0 for no roots"),
        Row(QuadraticMutant.N4, "a = 0 case split reversed"),
        Row(QuadraticMutant.N5, "strict sign labels swapped"),
        Row(QuadraticMutant.N6, "b² becomes 2b"),
        Row(QuadraticMutant.N7, "(0,0,0) reported as NoSolution"),
        Row(QuadraticMutant.N8, "(0,0,c≠0) reported as InfinitelyMany"),
        Row(QuadraticMutant.N9, "(0,b≠0,c) reported as NoSolution"),
        Row(QuadraticMutant.N10, "4ac becomes 4bc")
    ];

    [Fact]
    public void PaperMatrix_EveryRow_HasOneCellPerMutantAndMatchesSuiteIds()
    {
        var rows = PaperMutationMatrix.Parse(PaperMatrix);

        Assert.All(rows, row => Assert.Equal(Mutants.Length, row.Cells.Length));
        Assert.Equal(QuadraticPaperSuiteTests.Cases.Select(c => c.Id), rows.Select(r => r.TestId));
    }

    [Theory]
    [MemberData(nameof(SuiteIds))]
    public void Model_WithoutMutant_AgreesWithSpecAndProduction(string testId)
    {
        var c = QuadraticPaperSuiteTests.Cases.Single(x => x.Id == testId);

        var model = c.Steps.Select(s => QuadraticMutantModel.Solve(QuadraticMutant.None, s.A, s.B, s.C));
        var production = c.Steps.Select(s => QuadraticSolver.Solve(s.A, s.B, s.C));

        Assert.Equal(c.Expected, model);
        Assert.Equal(c.Expected, production);
    }

    [Theory]
    [MemberData(nameof(AllMutants))]
    public void Mutant_KillSetOverSuite_EqualsPaperMatrixColumn(QuadraticMutant mutant)
    {
        var expectedKills = PaperMutationMatrix.KilledBy(PaperMatrix, (int)mutant - 1);

        var actualKills = KillSet(mutant);

        Assert.Equal(expectedKills, actualKills);
    }

    // No quadratic mutant is equivalent, so the expected score is 10/10.
    [Fact]
    public void MutationScore_TenOfTenMutantsKilled()
    {
        var killed = Mutants.Where(m => KillSet(m).Length > 0);

        Assert.Equal(Mutants, killed);
    }

    // Relational slips at the zero tests of the case split (a == 0, then b == 0, then c == 0): one test is
    // replaced by <= 0 or >= 0. Neither the designed mutants N1–N10 nor Stryker's equality mutation (== to !=)
    // models such a slip; the theory records which designed tests expose each.
    [Theory]
    [InlineData('a', "<=", new[] { "TC-QE-09", "TC-QE-11", "TC-QE-13", "TC-QE-15", "TC-QE-30" })]
    [InlineData('a', ">=",
        new[]
        {
            "TC-QE-04", "TC-QE-05", "TC-QE-06", "TC-QE-07", "TC-QE-08", "TC-QE-10", "TC-QE-12", "TC-QE-14",
            "TC-QE-22", "TC-QE-23", "TC-QE-24", "TC-QE-25", "TC-QE-26", "TC-QE-27", "TC-QE-28", "TC-QE-29"
        })]
    [InlineData('b', "<=", new[] { "TC-QE-16", "TC-QE-18" })]
    [InlineData('b', ">=", new[] { "TC-QE-01", "TC-QE-17", "TC-QE-19" })]
    [InlineData('c', "<=", new[] { "TC-QE-20" })]
    [InlineData('c', ">=", new[] { "TC-QE-02", "TC-QE-21" })]
    public void ZeroTestRelationalSlip_IsExposedOnlyByTheListedTests(char coefficient, string relation,
        string[] expectedKillers)
    {
        Assert.Equal(expectedKillers, Killers(coefficient, relation));
    }

    private static string[] Killers(char coefficient, string relation)
    {
        return
        [
            .. QuadraticPaperSuiteTests.Cases
                .Where(c => c.Steps.Any(s => SolveWithZeroSlip(coefficient, relation, s.A, s.B, s.C)
                                             != QuadraticMutantModel.Solve(QuadraticMutant.None, s.A, s.B, s.C)))
                .Select(c => c.Id)
        ];
    }

    private static QuadraticResult SolveWithZeroSlip(char coefficient, string relation, int a, int b, int c)
    {
        bool IsZero(char name, int value) => name != coefficient
            ? value == 0
            : relation switch
            {
                "<=" => value <= 0,
                ">=" => value >= 0,
                _ => throw new ArgumentOutOfRangeException(nameof(relation), relation, "expected <= or >=")
            };

        if (!IsZero('a', a))
            return QuadraticMutantModel.Solve(QuadraticMutant.None, a, b, c);
        if (!IsZero('b', b))
            return QuadraticResult.OneLinearRoot;
        return IsZero('c', c) ? QuadraticResult.InfinitelyManySolutions : QuadraticResult.NoSolution;
    }

    private static string[] KillSet(QuadraticMutant mutant)
    {
        return
        [
            .. QuadraticPaperSuiteTests.Cases
                .Where(c => c.Steps.Any(s => QuadraticMutantModel.Solve(mutant, s.A, s.B, s.C)
                                             != QuadraticMutantModel.Solve(QuadraticMutant.None, s.A, s.B, s.C)))
                .Select(c => c.Id)
                .Order()
        ];
    }

    private static TheoryDataRow<QuadraticMutant> Row(QuadraticMutant mutant, string description)
    {
        return new TheoryDataRow<QuadraticMutant>(mutant).WithTestDisplayName($"{mutant}: {description}");
    }
}
