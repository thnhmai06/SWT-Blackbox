namespace SwtBlackbox.Atm.Tests.Black;

/// <summary>
///     Supplementary ATM cases outside the designed suite: out-of-domain and extreme inputs (negative amount,
///     negative balance, long.MinValue/MaxValue, overflow-prone sums) and the dailyUsed guard. None repeats an input
///     of the designed suite.
/// </summary>
public class AtmBoundaryTests
{
    public static IEnumerable<TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>> BoundaryCases =>
    [
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                1_000_000, 0, -50_000, AccountStatus.Active, true, WithdrawalResult.RejectedInvalidAmount)
            .WithTestDisplayName("SUP-ATM-01: amount -50,000 (negative multiple)")
            .WithTrait("Type", "BoundarySupplementary"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                1_000_000, 0, long.MinValue, AccountStatus.Active, true, WithdrawalResult.RejectedInvalidAmount)
            .WithTestDisplayName("SUP-ATM-02: amount long.MinValue")
            .WithTrait("Type", "BoundarySupplementary"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                1_000_000, 0, long.MaxValue, AccountStatus.Active, true, WithdrawalResult.RejectedInvalidAmount)
            .WithTestDisplayName("SUP-ATM-03: amount long.MaxValue")
            .WithTrait("Type", "BoundarySupplementary"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                -100, 0, 500_000, AccountStatus.Active, true, WithdrawalResult.RejectedInsufficientBalance)
            .WithTestDisplayName("SUP-ATM-04: negative (overdrawn) balance -100")
            .WithTrait("Type", "BoundarySupplementary"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                1_000_000, 19_950_000, 50_000, AccountStatus.Active, true, WithdrawalResult.Approved)
            .WithTestDisplayName("SUP-ATM-05: dailyUsed 19,950,000 + amount 50,000 -> Approved")
            .WithTrait("Type", "BoundarySupplementary"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                1_000_000, 20_000_000, 50_000, AccountStatus.Active, true, WithdrawalResult.RejectedDailyLimit)
            .WithTestDisplayName("SUP-ATM-06: dailyUsed 20,000,000 + amount 50,000 -> RejectedDailyLimit")
            .WithTrait("Type", "BoundarySupplementary"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                1_000_000, long.MaxValue, 50_000, AccountStatus.Active, true, WithdrawalResult.RejectedDailyLimit)
            .WithTestDisplayName("SUP-ATM-07: dailyUsed long.MaxValue, amount 50,000 -> RejectedDailyLimit")
            .WithTrait("Type", "BoundarySupplementary"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                long.MaxValue, 0, 20_000_000, AccountStatus.Active, false, WithdrawalResult.Approved)
            .WithTestDisplayName("SUP-ATM-08: balance long.MaxValue, amount 20,000,000, External -> Approved")
            .WithTrait("Type", "BoundarySupplementary")
    ];

    [Theory]
    [MemberData(nameof(BoundaryCases))]
    public void Withdraw_SupplementaryBoundaryCases_Expected(long balance, long dailyUsed, long amount,
        AccountStatus status, bool isInternal, WithdrawalResult expected)
    {
        var actual = AtmService.Withdraw(balance, dailyUsed, amount, status, isInternal);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Withdraw_NegativeDailyUsed_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>("dailyUsed",
            () => AtmService.Withdraw(1000000, -1, 500000, AccountStatus.Active, true));
        Assert.Throws<ArgumentOutOfRangeException>("dailyUsed",
            () => AtmService.Withdraw(1000000, long.MinValue, 500000, AccountStatus.Active, true));
    }

    [Fact]
    public void Withdraw_NegativeDailyUsed_Throws_EvenIfFrozen()
    {
        Assert.Throws<ArgumentOutOfRangeException>("dailyUsed",
            () => AtmService.Withdraw(1000000, -1, 500000, AccountStatus.Frozen, true));
    }

    [Fact]
    public void Withdraw_ZeroDailyUsed_IsAccepted()
    {
        var actual = AtmService.Withdraw(1000000, 0, 500000, AccountStatus.Active, true);
        Assert.Equal(WithdrawalResult.Approved, actual);
    }

    [Fact]
    public void Withdraw_UndefinedAccountStatus_ReturnsRejectedInvalidAccount()
    {
        var actual = AtmService.Withdraw(1000000, 0, 500000, (AccountStatus)99, true);
        Assert.Equal(WithdrawalResult.RejectedInvalidAccount, actual);
    }
}
