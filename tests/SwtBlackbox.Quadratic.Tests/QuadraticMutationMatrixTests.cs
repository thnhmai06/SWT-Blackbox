using SwtBlackbox.Tests;

namespace SwtBlackbox.Quadratic.Tests;

/// <summary>
///     Executes the paper's analytically expected quadratic Test×Mutant matrix (report.tex §5.12, Appendix H).
///     A test kills a mutant when any of its steps (TC-QE-08 has three) is classified differently.
/// </summary>
public class QuadraticMutationMatrixTests
{
    // Verbatim from report.tex lines 1607-1622, columns N1..N10.
    private const string PaperMatrix = """
                                       TC-QE-01: S S S K K K -- -- -- K
                                       TC-QE-02: K K K K S S -- -- -- K
                                       TC-QE-03: S S S K K S -- -- -- S
                                       TC-QE-04: -- -- -- K -- -- -- -- K --
                                       TC-QE-05: -- -- -- K -- -- -- K -- --
                                       TC-QE-06: -- -- -- K -- -- K -- -- --
                                       TC-QE-07: S S S K K K -- -- -- K
                                       TC-QE-08: K K K K K S -- -- -- K
                                       TC-QE-09: K K K K S K -- -- -- K
                                       TC-QE-10: S S S K K S -- -- -- K
                                       TC-QE-11: S S S K K S -- -- -- S
                                       TC-QE-12: S S S K K S -- -- -- K
                                       TC-QE-13: S S S K K S -- -- -- S
                                       TC-QE-14: -- -- -- K -- -- -- -- K --
                                       TC-QE-15: S S S K K S -- -- -- K
                                       TC-QE-16: S S S K K S -- -- -- S
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

    // report.tex Appendix H: no quadratic mutant is equivalent, so the expected score is 10/10.
    [Fact]
    public void MutationScore_TenOfTenMutantsKilled()
    {
        var killed = Mutants.Where(m => KillSet(m).Length > 0);

        Assert.Equal(Mutants, killed);
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