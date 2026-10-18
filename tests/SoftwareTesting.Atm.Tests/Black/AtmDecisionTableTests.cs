namespace SoftwareTesting.Atm.Tests.Black;

/// <summary>
///     Verifies the collapsed ATM decision table (DR1..DR9) of report.tex, Case Study I, against the service:
///     the 64 raw columns 2 x 4 x 2 x 2 x 2 are each matched by exactly one rule, the 8 columns with
///     amount class A4 and an intact daily sum are infeasible, and every suite case maps to its labelled rule.
/// </summary>
public class AtmDecisionTableTests
{
    private const AccountStatus Active = AccountStatus.Active;
    private const AccountStatus Frozen = AccountStatus.Frozen;

    private static readonly Rule[] Rules =
    [
        new("DR1", Frozen, null, null, null, null, WithdrawalResult.RejectedInvalidAccount),
        new("DR2", Active, AmountClass.A1, null, null, null, WithdrawalResult.RejectedInvalidAmount),
        new("DR3", Active, AmountClass.A3, null, null, null, WithdrawalResult.RejectedInvalidAmount),
        new("DR4", Active, AmountClass.A4, null, null, null, WithdrawalResult.RejectedInvalidAmount),
        new("DR5", Active, AmountClass.A2, false, null, null, WithdrawalResult.RejectedDailyLimit),
        new("DR6", Active, AmountClass.A2, true, true, false, WithdrawalResult.RejectedInsufficientBalance),
        new("DR7", Active, AmountClass.A2, true, true, true, WithdrawalResult.Approved),
        new("DR8", Active, AmountClass.A2, true, false, false, WithdrawalResult.RejectedInsufficientBalance),
        new("DR9", Active, AmountClass.A2, true, false, true, WithdrawalResult.Approved)
    ];

    private static readonly Column[] AllColumns =
    [
        .. from status in new[] { Active, Frozen }
        from amountClass in Enum.GetValues<AmountClass>()
        from dailyOk in new[] { false, true }
        from isInternal in new[] { false, true }
        from balanceOk in new[] { false, true }
        select new Column(status, amountClass, dailyOk, isInternal, balanceOk)
    ];

    /// <summary>Each of the 56 feasible columns is matched by exactly one rule, and the service agrees with it.</summary>
    [Fact]
    public void FeasibleColumns_MatchExactlyOneRuleAndServiceOutcome()
    {
        foreach (var column in AllColumns.Where(c => c.IsFeasible))
        {
            var matches = Rules.Where(r => r.Matches(column)).ToArray();
            var rule = Assert.Single(matches);

            var actual = AtmService.Withdraw(column.Balance, column.DailyUsed, column.Amount, column.Status,
                column.IsInternal);

            Assert.Equal(rule.Outcome, actual);
        }
    }

    /// <summary>The raw table has 64 columns; exactly the 8 with class A4 and an intact daily sum are infeasible.</summary>
    [Fact]
    public void RawColumns_SixtyFourOfWhichEightAreInfeasible()
    {
        var infeasible = AllColumns.Where(c => !c.IsFeasible).ToArray();

        Assert.Equal(64, AllColumns.Length);
        Assert.Equal(8, infeasible.Length);
        Assert.All(infeasible, c => Assert.Equal(AmountClass.A4, c.Class));
    }

    /// <summary>The collapsed rules together stand for all 64 raw columns, without overlap.</summary>
    [Fact]
    public void CollapsedRules_CoverAllSixtyFourColumns()
    {
        var covered = Rules.Sum(r => AllColumns.Count(r.Matches));

        Assert.Equal(64, covered);
    }

    /// <summary>
    ///     The collapsed rules stand for exactly the 56 feasible columns once the infeasible ones are removed;
    ///     DR1 stands for 28 of its 32 raw columns and DR4 for 4 of its 8.
    /// </summary>
    [Fact]
    public void CollapsedRules_CoverExactlyTheFiftySixFeasibleColumns()
    {
        var feasibleByRule = Rules.ToDictionary(r => r.Id, r => AllColumns.Count(c => c.IsFeasible && r.Matches(c)));

        Assert.Equal(56, feasibleByRule.Values.Sum());
        Assert.Equal(28, feasibleByRule["DR1"]);
        Assert.Equal(4, feasibleByRule["DR4"]);
    }

    /// <summary>Every suite case, classified on its raw inputs, lands in the rule its Rule column names.</summary>
    [Fact]
    public void SuiteCases_MapToTheirRule()
    {
        foreach (var testCase in AtmPaperSuiteTests.Cases)
        {
            var column = new Column(
                testCase.AccountStatus,
                ClassOf(testCase.Amount),
                testCase.DailyUsed + testCase.Amount <= 20_000_000,
                testCase.IsInternal,
                testCase.Balance >= testCase.Amount + (testCase.IsInternal ? 0 : AtmService.ExternalFee));

            var rule = Rules.Single(r => r.Matches(column));

            Assert.Equal(testCase.Rule, rule.Id);
        }
    }

    private static AmountClass ClassOf(long amount)
    {
        if (amount < 50_000)
            return AmountClass.A1;
        if (amount > 20_000_000)
            return AmountClass.A4;
        return amount % 50_000 == 0 ? AmountClass.A2 : AmountClass.A3;
    }

    /// <summary>Amount classes of condition c2: A1 below range, A2 valid multiple, A3 in range but off-lattice, A4 above.</summary>
    private enum AmountClass
    {
        A1 = 1,
        A2,
        A3,
        A4
    }

    /// <summary>One raw column of the decision table with a concrete witness input for it.</summary>
    private sealed record Column(
        AccountStatus Status,
        AmountClass Class,
        bool DailyOk,
        bool IsInternal,
        bool BalanceOk)
    {
        // An amount above the maximum cannot keep the daily sum within the limit, so (A4, DailyOk) is infeasible.
        public bool IsFeasible => !(Class == AmountClass.A4 && DailyOk);

        public long Amount => Class switch
        {
            AmountClass.A1 => 25_000,
            AmountClass.A2 => 500_000,
            AmountClass.A3 => 512_345,
            _ => 30_000_000
        };

        public long DailyUsed => DailyOk ? 0 : Math.Max(0, 20_000_001 - Amount);

        public long Balance => Amount + (IsInternal ? 0 : AtmService.ExternalFee) + (BalanceOk ? 0 : -1);
    }

    /// <summary>A collapsed rule; a null condition is a don't-care.</summary>
    private sealed record Rule(
        string Id,
        AccountStatus? Status,
        AmountClass? Class,
        bool? DailyOk,
        bool? IsInternal,
        bool? BalanceOk,
        WithdrawalResult Outcome)
    {
        public bool Matches(Column column)
        {
            return (Status is null || Status == column.Status)
                   && (Class is null || Class == column.Class)
                   && (DailyOk is null || DailyOk == column.DailyOk)
                   && (IsInternal is null || IsInternal == column.IsInternal)
                   && (BalanceOk is null || BalanceOk == column.BalanceOk);
        }
    }
}
