using SwtBlackbox.Tests;

namespace SwtBlackbox.Atm.Tests.Black;

/// <summary>
///     Executes the analytically expected ATM Test×Mutant matrix (report.tex, Case Study I, Test–Mutant Analysis):
///     every mutant is run against the 37-test suite and its kill set must equal the K cells of its column.
/// </summary>
public class AtmMutationMatrixTests
{
    // Analytically derived Test×Mutant matrix, transcribed verbatim from report.tex (columns M1…M14).
    private const string PaperMatrix = """
                                       TC-ATM-01: -- -- -- -- K -- -- -- K -- -- -- -- S
                                       TC-ATM-02: -- -- S S K -- -- S K S -- -- -- S
                                       TC-ATM-03: -- -- S S K -- -- S K S -- -- -- S
                                       TC-ATM-04: -- -- S S K -- -- S K S -- -- -- S
                                       TC-ATM-05: -- S S S S -- -- S K S S K -- S
                                       TC-ATM-06: S S S S S S S S K S S S S S
                                       TC-ATM-07: S S S S S S S S K S S S S S
                                       TC-ATM-08: S S S S S S S S K S S S S S
                                       TC-ATM-09: S S S S S S S S K S S S S S
                                       TC-ATM-10: -- -- S S K -- -- S K K -- -- -- S
                                       TC-ATM-11: -- -- S S K -- -- S K S -- -- -- S
                                       TC-ATM-12: S S S S S S S S K S S S S S
                                       TC-ATM-13: -- -- S S K -- -- S K S -- -- -- S
                                       TC-ATM-14: S S S S S S S S K S S S S S
                                       TC-ATM-15: S S S S S S S S K S S S S S
                                       TC-ATM-16: -- -- S S K -- -- S K S -- -- -- S
                                       TC-ATM-17: S K K S S S S S K S S S S S
                                       TC-ATM-18: -- -- S S K -- -- S K S -- -- -- S
                                       TC-ATM-19: -- -- S S K -- -- S K S -- -- -- S
                                       TC-ATM-20: S S S S S S S S K S S S S S
                                       TC-ATM-21: S K S S S S S S K S S S S S
                                       TC-ATM-22: -- S S S S -- -- S K S S K -- S
                                       TC-ATM-23: S S S S S S S S K S S S S S
                                       TC-ATM-24: K S S S S S S S K S S S S S
                                       TC-ATM-25: S S S S S S S S K S S S S S
                                       TC-ATM-26: S S S S S K K S K S S S S S
                                       TC-ATM-27: K S S S S S S S K S S S K S
                                       TC-ATM-28: S S S S S S S S K S S S K S
                                       TC-ATM-29: K S S S S S S S K S S S S S
                                       TC-ATM-30: K K K S S S S S K S S S S S
                                       TC-ATM-31: K K S S S S S S K S S S S S
                                       TC-ATM-32: S S S S S K S S K S S S S S
                                       TC-ATM-33: -- -- -- -- S -- -- -- K -- -- -- -- K
                                       TC-ATM-34: -- -- S S K -- -- S K S -- -- -- S
                                       TC-ATM-35: -- -- S S K -- -- S K S -- -- -- S
                                       TC-ATM-36: -- S S S S -- -- S K S K K -- S
                                       TC-ATM-37: -- -- S S K -- -- K K S -- -- -- S
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
    // so sweeping every amount around both bounds (status Active, ample balance, dailyUsed 0; R2 is evaluated before R3) proves equivalence.
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

    // Granularity slips beyond M8: replacing the modulus 50,000 by a divisor d of it changes R2 only for in-range
    // amounts that are multiples of d but not of 50,000. Every divisor of 10,000 is exposed by TC-ATM-37 (60,000);
    // the divisors 25,000 and 12,500 (of 25,000 = 50,000 / 2) are exposed by no designed test.
    [Theory]
    [InlineData(10_000, new[] { "TC-ATM-37" })]
    [InlineData(5_000, new[] { "TC-ATM-37" })]
    [InlineData(25_000, new string[0])]
    [InlineData(12_500, new string[0])]
    public void GranularityDivisorSlip_IsExposedOnlyByTheListedTests(long modulus, string[] expectedKillers)
    {
        var killers = AtmPaperSuiteTests.Cases
            .Where(c => c.AccountStatus == AccountStatus.Active
                        && c.Amount is >= 50_000 and <= 20_000_000
                        && c.Amount % modulus == 0
                        && c.Amount % 50_000 != 0)
            .Select(c => c.Id);

        Assert.Equal(expectedKillers, killers);
    }

    // Precedence transpositions of the four checks: two of the six transpositions are the designed mutants M14
    // (R1/R2) and M11 (R3/R5); the other four are not designed mutants, and the theory records which designed
    // tests expose each.
    [Theory]
    [InlineData("R2,R1,R3,R5", new[] { "TC-ATM-33" })]
    [InlineData("R3,R2,R1,R5",
        new[] { "TC-ATM-04", "TC-ATM-18", "TC-ATM-19", "TC-ATM-33", "TC-ATM-34", "TC-ATM-35" })]
    [InlineData("R5,R2,R3,R1", new[] { "TC-ATM-04", "TC-ATM-33", "TC-ATM-36" })]
    [InlineData("R1,R3,R2,R5", new[] { "TC-ATM-04", "TC-ATM-18", "TC-ATM-19", "TC-ATM-34", "TC-ATM-35" })]
    [InlineData("R1,R5,R3,R2",
        new[] { "TC-ATM-04", "TC-ATM-18", "TC-ATM-19", "TC-ATM-34", "TC-ATM-35", "TC-ATM-36" })]
    [InlineData("R1,R2,R5,R3", new[] { "TC-ATM-36" })]
    public void PrecedenceTransposition_IsExposedOnlyByTheListedTests(string order, string[] expectedKillers)
    {
        Assert.Equal(expectedKillers, Killers(order));
    }

    [Fact]
    public void PrecedenceTransposition_ReproducesDesignedMutants()
    {
        Assert.Equal(KillSet(AtmMutant.M14), Killers("R2,R1,R3,R5").Order());
        Assert.Equal(KillSet(AtmMutant.M11), Killers("R1,R2,R5,R3").Order());
    }

    private static string[] Killers(string order)
    {
        return
        [
            .. AtmPaperSuiteTests.Cases
                .Where(c => WithdrawInOrder(order, c) != AtmMutantModel.Withdraw(AtmMutant.None, c.Balance,
                                c.DailyUsed, c.Amount, c.AccountStatus, c.IsInternal))
                .Select(c => c.Id)
        ];
    }

    private static WithdrawalResult WithdrawInOrder(string order, AtmPaperCase c)
    {
        var checks = new Dictionary<string, (bool Satisfied, WithdrawalResult Rejection)>
        {
            ["R1"] = (c.AccountStatus == AccountStatus.Active, WithdrawalResult.RejectedInvalidAccount),
            ["R2"] = (c.Amount is >= 50_000 and <= 20_000_000 && c.Amount % 50_000 == 0,
                WithdrawalResult.RejectedInvalidAmount),
            ["R3"] = (c.DailyUsed + c.Amount <= 20_000_000, WithdrawalResult.RejectedDailyLimit),
            ["R5"] = (c.Balance >= c.Amount + (c.IsInternal ? 0 : 50_000),
                WithdrawalResult.RejectedInsufficientBalance)
        };

        foreach (var name in order.Split(','))
        {
            var (satisfied, rejection) = checks[name];
            if (!satisfied)
                return rejection;
        }

        return WithdrawalResult.Approved;
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
