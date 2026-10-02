using SwtBlackbox.Tests;

namespace SwtBlackbox.Atm.Tests;

/// <summary>
///     Executes the paper's analytically expected ATM Test×Mutant matrix (report.tex §4.12, Appendix D):
///     every mutant is run against the 22-test suite and its kill set must equal the paper's K cells.
/// </summary>
public class AtmMutationMatrixTests
{
    // Verbatim from report.tex lines 970-991, columns M1…M14.
    private const string PaperMatrix = """
                                       TC-ATM-01: S S S S S S S S K S S S S S
                                       TC-ATM-02: -- -- S S K -- -- S K S -- -- -- S
                                       TC-ATM-03: K S S S S S S S K S S S S S
                                       TC-ATM-04: S S S S S S S S K S S S S S
                                       TC-ATM-05: K K K S S S S S K S S S S S
                                       TC-ATM-06: -- -- S S K -- -- S K S -- -- -- S
                                       TC-ATM-07: -- -- S S K -- -- S K S -- -- -- S
                                       TC-ATM-08: S S S S S S S S K S S S S S
                                       TC-ATM-09: K S S S S S S S K S S S S S
                                       TC-ATM-10: S S S S S S S S K S S S S S
                                       TC-ATM-11: S K S S S S S S K S S S S S
                                       TC-ATM-12: -- S S S S -- -- S K S S K -- S
                                       TC-ATM-13: -- -- -- -- K -- -- -- K -- -- -- -- S
                                       TC-ATM-14: S S S S S K S S K S S S S S
                                       TC-ATM-15: K K S S S S S S K S S S S S
                                       TC-ATM-16: -- S S S S -- -- S K S K K -- S
                                       TC-ATM-17: S S S S S S S S K S S S S S
                                       TC-ATM-18: -- -- S S K -- -- K K S -- -- -- S
                                       TC-ATM-19: S S S S S K K S K S S S S S
                                       TC-ATM-20: -- -- S S K -- -- S K K -- -- -- S
                                       TC-ATM-21: S S S S S S S S K S S S K S
                                       TC-ATM-22: -- -- -- -- S -- -- -- K -- -- -- -- K
                                       """;

    private static readonly AtmMutant[] Mutants = [.. Enum.GetValues<AtmMutant>().Where(m => m != AtmMutant.None)];

    public static IEnumerable<TheoryDataRow<string>> SuiteIds =>
        AtmPaperSuiteTests.Cases.Select(c =>
            new TheoryDataRow<string>(c.Id).WithTestDisplayName($"{c.Id}: model(None) = spec = production"));

    public static IEnumerable<TheoryDataRow<AtmMutant>> AllMutants =>
    [
        Row(AtmMutant.M1, "R5 >= becomes >"),
        Row(AtmMutant.M2, "R3 <= becomes <"),
        Row(AtmMutant.M3, "upper amount bound 19,999,999"),
        Row(AtmMutant.M4, "upper amount bound 20,000,001 (equivalent)"),
        Row(AtmMutant.M5, "R1 AND R2 becomes OR"),
        Row(AtmMutant.M6, "fee removed"),
        Row(AtmMutant.M7, "external fee 5,000"),
        Row(AtmMutant.M8, "modulus 10,000"),
        Row(AtmMutant.M9, "status condition reversed"),
        Row(AtmMutant.M10, "lower amount bound 0"),
        Row(AtmMutant.M11, "R3/R5 precedence swapped"),
        Row(AtmMutant.M12, "amount dropped from daily sum"),
        Row(AtmMutant.M13, "external fee 100,000"),
        Row(AtmMutant.M14, "R1/R2 precedence swapped")
    ];

    [Fact]
    public void PaperMatrix_EveryRow_HasOneCellPerMutantAndMatchesSuiteIds()
    {
        var rows = PaperMutationMatrix.Parse(PaperMatrix);

        Assert.All(rows, row => Assert.Equal(Mutants.Length, row.Cells.Length));
        Assert.Equal(AtmPaperSuiteTests.Cases.Select(c => c.Id), rows.Select(r => r.TestId));
    }

    [Theory]
    [MemberData(nameof(SuiteIds))]
    public void Model_WithoutMutant_AgreesWithSpecAndProduction(string testId)
    {
        var c = AtmPaperSuiteTests.Cases.Single(x => x.Id == testId);

        var model = AtmMutantModel.Withdraw(AtmMutant.None, c.Balance, c.DailyUsed, c.Amount, c.AccountStatus,
            c.IsInternal);
        var production = AtmService.Withdraw(c.Balance, c.DailyUsed, c.Amount, c.AccountStatus, c.IsInternal);

        Assert.Equal(c.Expected, model);
        Assert.Equal(c.Expected, production);
    }

    [Theory]
    [MemberData(nameof(AllMutants))]
    public void Mutant_KillSetOverSuite_EqualsPaperMatrixColumn(AtmMutant mutant)
    {
        var expectedKills = PaperMutationMatrix.KilledBy(PaperMatrix, (int)mutant - 1);

        var actualKills = KillSet(mutant);

        Assert.Equal(expectedKills, actualKills);
    }

    [Fact]
    public void MutationScore_ThirteenOfThirteenNonEquivalentMutantsKilled()
    {
        var killed = Mutants.Where(m => KillSet(m).Length > 0).ToArray();

        Assert.Equal(Mutants.Where(m => m != AtmMutant.M4), killed);
    }

    // M4 can only differ from the original if some amount in (20,000,000, 20,000,001] passed R2,
    // so sweeping every amount around both bounds with every other rule passing proves equivalence.
    [Fact]
    public void M4_RaisedUpperBound_IsEquivalentForEveryAmountAroundTheRange()
    {
        long? firstDifference = null;

        for (long amount = -100_000; amount <= 20_200_000 && firstDifference is null; amount++)
            if (AtmMutantModel.Withdraw(AtmMutant.M4, long.MaxValue / 2, 0, amount, AccountStatus.Active, true)
                != AtmMutantModel.Withdraw(AtmMutant.None, long.MaxValue / 2, 0, amount, AccountStatus.Active, true))
                firstDifference = amount;

        Assert.Null(firstDifference);
    }

    private static string[] KillSet(AtmMutant mutant)
    {
        return
        [
            .. AtmPaperSuiteTests.Cases
                .Where(c => AtmMutantModel.Withdraw(mutant, c.Balance, c.DailyUsed, c.Amount, c.AccountStatus,
                                c.IsInternal)
                            != AtmMutantModel.Withdraw(AtmMutant.None, c.Balance, c.DailyUsed, c.Amount,
                                c.AccountStatus,
                                c.IsInternal))
                .Select(c => c.Id)
                .Order()
        ];
    }

    private static TheoryDataRow<AtmMutant> Row(AtmMutant mutant, string description)
    {
        return new TheoryDataRow<AtmMutant>(mutant).WithTestDisplayName($"{mutant}: {description}");
    }
}