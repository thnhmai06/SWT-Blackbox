namespace SwtBlackbox.Atm.Tests;

/// <summary>
///     The complete 22-test ATM suite of report.tex §4.10 (Table "ATM complete test suite", Appendix C).
///     Traits carry the paper's traceability columns so a run can be filtered by partition, boundary or combination.
/// </summary>
public class AtmPaperSuiteTests
{
    private const AccountStatus Active = AccountStatus.Active;
    private const AccountStatus Frozen = AccountStatus.Frozen;

    internal static readonly AtmPaperCase[] Cases =
    [
        Case("TC-ATM-01", "nominal baseline", 1_000_000, 0, 500_000, Active, true, WithdrawalResult.Approved,
            "P_a,2 P_bal,2 P_dl,1 P_s,1 P_i,1"),
        Case("TC-ATM-02", "one unit below R2 range floor", 1_000_000, 0, 49_999, Active, true,
            WithdrawalResult.RejectedInvalidAmount,
            "P_a,1", "amount lower boundary, range side (-1)"),
        Case("TC-ATM-03", "first reachable amount and balance = T, both tight", 50_000, 0, 50_000, Active, true,
            WithdrawalResult.Approved,
            "P_a,2", "first reachable amount = balance = T", "C1"),
        Case("TC-ATM-04", "balance one unit past T", 50_001, 0, 50_000, Active, true, WithdrawalResult.Approved,
            "P_a,2 P_bal,2", "balance = T+1", "C2"),
        Case("TC-ATM-05", "last reachable amount, balance = T, daily sum = limit", 20_000_000, 0, 20_000_000, Active,
            true, WithdrawalResult.Approved,
            "P_a,2", "last reachable amount, balance = T"),
        Case("TC-ATM-06", "over range but still a multiple", 1_000_000, 0, 20_050_000, Active, true,
            WithdrawalResult.RejectedInvalidAmount,
            "P_a,4", "upper boundary, range side (+50,000)"),
        Case("TC-ATM-07", "in range but not a multiple of 50,000", 1_000_000, 0, 50_001, Active, true,
            WithdrawalResult.RejectedInvalidAmount,
            "P_a,3", "interior granularity boundary"),
        Case("TC-ATM-08", "balance one unit below T", 499_999, 0, 500_000, Active, true,
            WithdrawalResult.RejectedInsufficientBalance,
            "P_bal,1", "balance = T-1"),
        Case("TC-ATM-09", "balance exactly T is on the valid side", 500_000, 0, 500_000, Active, true,
            WithdrawalResult.Approved,
            "P_bal,2", "balance = T"),
        Case("TC-ATM-10", "daily sum one below limit", 1_000_000, 19_499_999, 500_000, Active, true,
            WithdrawalResult.Approved,
            "P_dl,1", "daily sum = 20,000,000-1"),
        Case("TC-ATM-11", "daily sum exactly at limit is valid", 1_000_000, 19_500_000, 500_000, Active, true,
            WithdrawalResult.Approved,
            "P_dl,1", "daily sum = 20,000,000"),
        Case("TC-ATM-12", "daily sum one over limit", 1_000_000, 19_500_001, 500_000, Active, true,
            WithdrawalResult.RejectedDailyLimit,
            "P_dl,2", "daily sum = 20,000,000+1"),
        Case("TC-ATM-13", "frozen account with otherwise perfect inputs", 1_000_000, 0, 500_000, Frozen, true,
            WithdrawalResult.RejectedInvalidAccount,
            "P_s,2", "categorical status"),
        Case("TC-ATM-14", "external fee moves T to 550,000 above balance", 500_000, 0, 500_000, Active, false,
            WithdrawalResult.RejectedInsufficientBalance,
            "P_i,2", "fee threshold shift", "C5"),
        Case("TC-ATM-15", "balance = T and daily sum = limit simultaneously", 500_000, 19_500_000, 500_000, Active,
            true, WithdrawalResult.Approved,
            "R3,R5 both tight", "double derived boundary", "C3"),
        Case("TC-ATM-16", "R3 and R5 both fail, R3 reported first", 499_999, 19_500_001, 500_000, Active, true,
            WithdrawalResult.RejectedDailyLimit,
            "R3,R5 both fail", "daily sum = 20,000,001 and balance < T", "C4"),
        Case("TC-ATM-17", "negative (overdrawn) balance", -100, 0, 500_000, Active, true,
            WithdrawalResult.RejectedInsufficientBalance,
            "balance < 0 (deep invalid)"),
        Case("TC-ATM-18", "divisible by 10,000 but not by 50,000", 1_000_000, 0, 60_000, Active, true,
            WithdrawalResult.RejectedInvalidAmount,
            "P_a,3", "interior granularity boundary"),
        Case("TC-ATM-19", "balance between correct (550,000) and wrong-fee (505,000) thresholds", 520_000, 0, 500_000,
            Active, false, WithdrawalResult.RejectedInsufficientBalance,
            "P_bal,1", "balance between correct and incorrect fee threshold", "C6"),
        Case("TC-ATM-20", "zero amount is a multiple but below the floor", 1_000_000, 0, 0, Active, true,
            WithdrawalResult.RejectedInvalidAmount,
            "P_a,1", "amount = 0", "C7"),
        Case("TC-ATM-21", "only approved external test, balance between 550,000 and 600,000", 580_000, 0, 500_000,
            Active, false, WithdrawalResult.Approved,
            "P_a,2 P_bal,2 P_i,2", "fee threshold, External-Approved", "C8"),
        Case("TC-ATM-22", "R1 and R2 both fail, R1 reported first", 1_000_000, 0, 49_999, Frozen, true,
            WithdrawalResult.RejectedInvalidAccount,
            "P_a,1 P_s,2", "R1 and R2 simultaneously invalid", "C9")
    ];

    public static IEnumerable<TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>> PaperSuite =>
        Cases.Select(c => c.ToTheoryDataRow());

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
        string partition, string? boundary = null, string? combo = null)
    {
        return new AtmPaperCase(id, rationale, balance, dailyUsed, amount, accountStatus, isInternal, expected,
            partition, boundary,
            combo);
    }
}

/// <summary>One row of the paper's ATM suite with its traceability columns.</summary>
internal sealed record AtmPaperCase(
    string Id,
    string Rationale,
    long Balance,
    long DailyUsed,
    long Amount,
    AccountStatus AccountStatus,
    bool IsInternal,
    WithdrawalResult Expected,
    string Partition,
    string? Boundary,
    string? Combo)
{
    public TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult> ToTheoryDataRow()
    {
        var row = new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                Balance, DailyUsed, Amount, AccountStatus, IsInternal, Expected)
            .WithTestDisplayName($"{Id}: {Rationale} -> {Expected}")
            .WithTrait("PaperId", Id)
            .WithTrait("Partition", Partition);
        if (Boundary is not null)
            row.WithTrait("Boundary", Boundary);
        if (Combo is not null)
            row.WithTrait("Combo", Combo);
        return row;
    }
}