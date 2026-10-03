using SwtBlackbox.Tests;

namespace SwtBlackbox.Atm.Tests;

/// <summary>
///     The complete 37-test ATM suite of report.tex, Case Study I, Test Suite Construction.
///     Traits carry the traceability columns (decision rule, layer, boundary, combination) so a run can be
///     filtered by any of them.
/// </summary>
public class AtmPaperSuiteTests
{
    private const AccountStatus Active = AccountStatus.Active;
    private const AccountStatus Frozen = AccountStatus.Frozen;

    internal static readonly AtmPaperCase[] Cases =
    [
        // L1: decision-table core, one test per feasible collapsed rule.
        Case("TC-ATM-01", "frozen account, otherwise nominal", 25_000_000, 0, 500_000, Frozen, true,
            WithdrawalResult.RejectedInvalidAccount, "L1", "DR1"),
        Case("TC-ATM-02", "A1 nominal: midpoint of [0, 50,000)", 25_000_000, 0, 25_000, Active, true,
            WithdrawalResult.RejectedInvalidAmount, "L1", "DR2"),
        Case("TC-ATM-03", "A3 nominal: in range, not a multiple of 50,000", 25_000_000, 0, 512_345, Active, true,
            WithdrawalResult.RejectedInvalidAmount, "L1", "DR3"),
        Case("TC-ATM-04", "A4 nominal: above the 20,000,000 maximum", 25_000_000, 0, 30_000_000, Active, true,
            WithdrawalResult.RejectedInvalidAmount, "L1", "DR4"),
        Case("TC-ATM-05", "daily sum 20,250,000 exceeds the limit", 25_000_000, 19_750_000, 500_000, Active, true,
            WithdrawalResult.RejectedDailyLimit, "L1", "DR5"),
        Case("TC-ATM-06", "internal, balance 100,000 below the amount", 100_000, 0, 500_000, Active, true,
            WithdrawalResult.RejectedInsufficientBalance, "L1", "DR6"),
        Case("TC-ATM-07", "nominal baseline", 25_000_000, 0, 500_000, Active, true,
            WithdrawalResult.Approved, "L1", "DR7"),
        Case("TC-ATM-08", "external, balance 100,000 below amount + fee", 100_000, 0, 500_000, Active, false,
            WithdrawalResult.RejectedInsufficientBalance, "L1", "DR8"),
        Case("TC-ATM-09", "external with ample balance", 25_000_000, 0, 500_000, Active, false,
            WithdrawalResult.Approved, "L1", "DR9"),

        // L2: 3-value BVA on the amount condition, integer line and reachable lattice (multiples of 50,000).
        Case("TC-ATM-10", "lattice Min-: nearest multiple of 50,000 below the range", 25_000_000, 0, 0, Active, true,
            WithdrawalResult.RejectedInvalidAmount, "L2", "DR2", "lattice Min-"),
        Case("TC-ATM-11", "integer Min-: one unit below the range floor", 25_000_000, 0, 49_999, Active, true,
            WithdrawalResult.RejectedInvalidAmount, "L2", "DR2", "integer Min-"),
        Case("TC-ATM-12", "Min: the range floor is the first valid amount", 25_000_000, 0, 50_000, Active, true,
            WithdrawalResult.Approved, "L2", "DR7", "Min"),
        Case("TC-ATM-13", "integer Min+: first non-multiple above the floor (granularity)", 25_000_000, 0, 50_001,
            Active, true,
            WithdrawalResult.RejectedInvalidAmount, "L2", "DR3", "integer Min+"),
        Case("TC-ATM-14", "lattice Min+: second multiple of 50,000", 25_000_000, 0, 100_000, Active, true,
            WithdrawalResult.Approved, "L2", "DR7", "lattice Min+"),
        Case("TC-ATM-15", "lattice Max-: last multiple below the maximum", 25_000_000, 0, 19_950_000, Active, true,
            WithdrawalResult.Approved, "L2", "DR7", "lattice Max-"),
        Case("TC-ATM-16", "integer Max-: one unit below the maximum (granularity)", 25_000_000, 0, 19_999_999,
            Active, true,
            WithdrawalResult.RejectedInvalidAmount, "L2", "DR3", "integer Max-"),
        Case("TC-ATM-17", "Max: amount = daily limit, so daily sum = 20,000,000 is unavoidable", 25_000_000, 0,
            20_000_000, Active, true,
            WithdrawalResult.Approved, "L2", "DR7", "Max"),
        Case("TC-ATM-18", "integer Max+: one unit above the maximum", 25_000_000, 0, 20_000_001, Active, true,
            WithdrawalResult.RejectedInvalidAmount, "L2", "DR4", "integer Max+"),
        Case("TC-ATM-19", "lattice Max+: next multiple above the maximum", 25_000_000, 0, 20_050_000, Active, true,
            WithdrawalResult.RejectedInvalidAmount, "L2", "DR4", "lattice Max+"),

        // L2: BVA on the daily sum (dailyUsed + 500,000 against the 20,000,000 limit).
        Case("TC-ATM-20", "daily sum one below the limit", 25_000_000, 19_499_999, 500_000, Active, true,
            WithdrawalResult.Approved, "L2", "DR7", "sum T-1"),
        Case("TC-ATM-21", "daily sum exactly at the limit", 25_000_000, 19_500_000, 500_000, Active, true,
            WithdrawalResult.Approved, "L2", "DR7", "sum T"),
        Case("TC-ATM-22", "daily sum one over the limit", 25_000_000, 19_500_001, 500_000, Active, true,
            WithdrawalResult.RejectedDailyLimit, "L2", "DR5", "sum T+1"),

        // L2: BVA on balance against amount + fee, in both columns of the network condition (isInternal).
        Case("TC-ATM-23", "internal, balance one below the amount", 499_999, 0, 500_000, Active, true,
            WithdrawalResult.RejectedInsufficientBalance, "L2", "DR6", "Internal T-1"),
        Case("TC-ATM-24", "internal, balance exactly the amount", 500_000, 0, 500_000, Active, true,
            WithdrawalResult.Approved, "L2", "DR7", "Internal T"),
        Case("TC-ATM-25", "internal, balance one above the amount", 500_001, 0, 500_000, Active, true,
            WithdrawalResult.Approved, "L2", "DR7", "Internal T+1"),
        Case("TC-ATM-26", "external, balance one below amount + fee", 549_999, 0, 500_000, Active, false,
            WithdrawalResult.RejectedInsufficientBalance, "L2", "DR8", "External T-1"),
        Case("TC-ATM-27", "external, balance exactly amount + fee", 550_000, 0, 500_000, Active, false,
            WithdrawalResult.Approved, "L2", "DR9", "External T"),
        Case("TC-ATM-28", "external, balance one above amount + fee", 550_001, 0, 500_000, Active, false,
            WithdrawalResult.Approved, "L2", "DR9", "External T+1"),

        // L3a: boundary combinations (extension).
        Case("TC-ATM-29", "amount Min and balance = T", 50_000, 0, 50_000, Active, true,
            WithdrawalResult.Approved, "L3", "DR7", combo: "AC1"),
        Case("TC-ATM-30", "amount Max, daily sum = limit and balance = T", 20_000_000, 0, 20_000_000, Active, true,
            WithdrawalResult.Approved, "L3", "DR7", combo: "AC2"),
        Case("TC-ATM-31", "daily sum = limit and balance = T", 500_000, 19_500_000, 500_000, Active, true,
            WithdrawalResult.Approved, "L3", "DR7", combo: "AC3"),
        Case("TC-ATM-32", "network toggled against TC-ATM-24: same numbers, external", 500_000, 0, 500_000,
            Active, false,
            WithdrawalResult.RejectedInsufficientBalance, "L3", "DR8", combo: "AC4"),

        // L3b: don't-care expansion (extension): the rule evaluated next in the precedence order is made to fail too.
        Case("TC-ATM-33", "R1 and R2 both fail, R1 reported first", 25_000_000, 0, 49_999, Frozen, true,
            WithdrawalResult.RejectedInvalidAccount, "L3", "DR1", combo: "AX1"),
        Case("TC-ATM-34", "R2 (A1) and R3 both fail, R2 reported first", 25_000_000, 20_000_000, 49_999, Active, true,
            WithdrawalResult.RejectedInvalidAmount, "L3", "DR2", combo: "AX2"),
        Case("TC-ATM-35", "R2 (A3) and R3 both fail, R2 reported first", 25_000_000, 20_000_000, 50_001, Active, true,
            WithdrawalResult.RejectedInvalidAmount, "L3", "DR3", combo: "AX3"),
        Case("TC-ATM-36", "R3 and R5 both fail, R3 reported first", 499_999, 19_500_001, 500_000, Active, true,
            WithdrawalResult.RejectedDailyLimit, "L3", "DR5", combo: "AX4"),

        // L4: mutation feedback, the test needed to kill the designed mutant M8 that L1-L3 leave alive.
        Case("TC-ATM-37", "amount divisible by 10,000 but not by 50,000", 25_000_000, 0, 60_000, Active, true,
            WithdrawalResult.RejectedInvalidAmount, "L4", "DR3", "L4: divisible by 10,000 but not by 50,000 (M8)")
    ];

    public static IEnumerable<TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>> PaperSuite =>
        Cases.Where(c => PaperLayers.Includes(c.Layer)).Select(c => c.ToTheoryDataRow());

    [Theory]
    [MemberData(nameof(PaperSuite))]
    public void Withdraw_PaperSuiteCase_ReturnsSpecifiedResult(
        long balance, long dailyUsed, long amount, AccountStatus accountStatus, bool isInternal,
        WithdrawalResult expected)
    {
        var actual = AtmService.Withdraw(balance, dailyUsed, amount, accountStatus, isInternal);

        Assert.Equal(expected, actual);
    }

    private static AtmPaperCase Case(
        string id, string rationale,
        long balance, long dailyUsed, long amount, AccountStatus accountStatus, bool isInternal,
        WithdrawalResult expected,
        string layer, string rule, string? boundary = null, string? combo = null)
    {
        return new AtmPaperCase(id, rationale, balance, dailyUsed, amount, accountStatus, isInternal, expected,
            layer, rule, boundary, combo);
    }
}

/// <summary>One row of the ATM suite with its traceability columns.</summary>
internal sealed record AtmPaperCase(
    string Id,
    string Rationale,
    long Balance,
    long DailyUsed,
    long Amount,
    AccountStatus AccountStatus,
    bool IsInternal,
    WithdrawalResult Expected,
    string Layer,
    string Rule,
    string? Boundary,
    string? Combo)
{
    public TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult> ToTheoryDataRow()
    {
        var row = new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                Balance, DailyUsed, Amount, AccountStatus, IsInternal, Expected)
            .WithTestDisplayName($"{Id}: {Rationale} -> {Expected}")
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
