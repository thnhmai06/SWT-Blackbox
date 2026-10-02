namespace SwtBlackbox.Atm.Tests;

public class AtmBoundaryTests
{
    public static IEnumerable<TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>> BoundaryCases =>
    [
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(1000000, 0, 19999999,
                AccountStatus.Active, true, WithdrawalResult.RejectedInvalidAmount)
            .WithTestDisplayName("SUP-ATM-01: amount 19999999 (in range, not multiple)")
            .WithTrait("Type", "BoundarySupplementary"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(1000000, 0, 20000001,
                AccountStatus.Active, true, WithdrawalResult.RejectedInvalidAmount)
            .WithTestDisplayName("SUP-ATM-02: amount 20000001 (over range and non-multiple)")
            .WithTrait("Type", "BoundarySupplementary"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(1000000, 0, 50000,
                AccountStatus.Active, true, WithdrawalResult.Approved)
            .WithTestDisplayName("SUP-ATM-03: amount 50000 is first reachable valid amount")
            .WithTrait("Type", "BoundarySupplementary"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(1000000, 0, -50000,
                AccountStatus.Active, true, WithdrawalResult.RejectedInvalidAmount)
            .WithTestDisplayName("SUP-ATM-04: amount -50000 (negative multiple)")
            .WithTrait("Type", "BoundarySupplementary"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(1000000, 0, long.MinValue,
                AccountStatus.Active, true, WithdrawalResult.RejectedInvalidAmount)
            .WithTestDisplayName("SUP-ATM-05: amount long.MinValue")
            .WithTrait("Type", "BoundarySupplementary"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(1000000, 0, long.MaxValue,
                AccountStatus.Active, true, WithdrawalResult.RejectedInvalidAmount)
            .WithTestDisplayName("SUP-ATM-06: amount long.MaxValue")
            .WithTrait("Type", "BoundarySupplementary"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(550000, 0, 500000,
                AccountStatus.Active, false, WithdrawalResult.Approved)
            .WithTestDisplayName("SUP-ATM-07: External exact boundary - balance 550000 approved")
            .WithTrait("Type", "BoundarySupplementary"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(549999, 0, 500000,
                AccountStatus.Active, false, WithdrawalResult.RejectedInsufficientBalance)
            .WithTestDisplayName("SUP-ATM-08: External exact boundary - balance 549999 rejected")
            .WithTrait("Type", "BoundarySupplementary"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(1000000, 20000000, 50000,
                AccountStatus.Active, true, WithdrawalResult.RejectedDailyLimit)
            .WithTestDisplayName("SUP-ATM-09: dailyUsed 20000000 + amount 50000 -> RejectedDailyLimit")
            .WithTrait("Type", "BoundarySupplementary"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(1000000, 19950000, 50000,
                AccountStatus.Active, true, WithdrawalResult.Approved)
            .WithTestDisplayName("SUP-ATM-10: dailyUsed 19950000 + amount 50000 -> Approved")
            .WithTrait("Type", "BoundarySupplementary"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(1000000, long.MaxValue, 50000,
                AccountStatus.Active, true, WithdrawalResult.RejectedDailyLimit)
            .WithTestDisplayName("SUP-ATM-11: dailyUsed long.MaxValue, amount 50000 -> RejectedDailyLimit")
            .WithTrait("Type", "BoundarySupplementary"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(long.MaxValue, 0, 20000000,
                AccountStatus.Active, false, WithdrawalResult.Approved)
            .WithTestDisplayName("SUP-ATM-12: balance long.MaxValue, amount 20000000, External -> Approved")
            .WithTrait("Type", "BoundarySupplementary")
    ];

    public static IEnumerable<TheoryDataRow<bool, WithdrawalResult>> FeeToggle =>
    [
        new TheoryDataRow<bool, WithdrawalResult>(true, WithdrawalResult.Approved)
            .WithTestDisplayName("SUP-ATM-13: C5 toggle - internal approved"),
        new TheoryDataRow<bool, WithdrawalResult>(false, WithdrawalResult.RejectedInsufficientBalance)
            .WithTestDisplayName("SUP-ATM-14: C5 toggle - external rejected")
    ];

    [Theory]
    [MemberData(nameof(BoundaryCases))]
    public void Withdraw_SupplementaryBoundaryCases_Expected(long balance, long dailyUsed, long amount,
        AccountStatus status, bool isInternal, WithdrawalResult expected)
    {
        var actual = AtmService.Withdraw(balance, dailyUsed, amount, status, isInternal);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(FeeToggle))]
    public void Withdraw_C5TogglePair_Expected(bool isInternal, WithdrawalResult expected)
    {
        var actual = AtmService.Withdraw(500000, 0, 500000, AccountStatus.Active, isInternal);
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