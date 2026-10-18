namespace SoftwareTesting.Atm.Tests.White;

/// <summary>
///     Control-flow tests of <see cref="AtmService.Withdraw"/>: one test group per feasible entry-to-exit path of the
///     method's control flow graph (docs/control-flow-tests.md, CFG-ATM). P3 is split into the three disjuncts of
///     the R2 range/granularity condition so that each short-circuit operand is covered separately; P5-P8 split the
///     isInternal fee ternary and the balance decision into their True and False arms.
/// </summary>
public class AtmControlFlowPathTests
{
    private const AccountStatus Active = AccountStatus.Active;
    private const AccountStatus Frozen = AccountStatus.Frozen;

    /// <summary>P1: the dailyUsed guard throws before any rule is evaluated.</summary>
    public static IEnumerable<TheoryDataRow<long, long, long>> DailyUsedNegativeThrows =>
    [
        new TheoryDataRow<long, long, long>(1_000_000, -1, 500_000)
            .WithTestDisplayName("CFT-ATM-P1: dailyUsed -1 -> ArgumentOutOfRangeException")
            .WithTrait("Path", "P1"),
        new TheoryDataRow<long, long, long>(1_000_000, long.MinValue, 500_000)
            .WithTestDisplayName("CFT-ATM-P1: dailyUsed long.MinValue -> ArgumentOutOfRangeException")
            .WithTrait("Path", "P1"),
        // The guard precedes R1, so it throws even for a frozen account.
        new TheoryDataRow<long, long, long>(1_000_000, -1, 500_000)
            .WithTestDisplayName("CFT-ATM-P1: dailyUsed -1 with Frozen status -> ArgumentOutOfRangeException")
            .WithTrait("Path", "P1")
    ];

    /// <summary>P2-P8: the normal return paths, one row per path (P3 split into its three disjuncts).</summary>
    public static IEnumerable<TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>> PathCases =>
    [
        // P2: dailyUsed >= 0 (guard passes), R1 fails: accountStatus != Active.
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                1_000_000, 0, 500_000, Frozen, true, WithdrawalResult.RejectedInvalidAccount)
            .WithTestDisplayName("CFT-ATM-P2: Frozen -> RejectedInvalidAccount")
            .WithTrait("Path", "P2"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                1_000_000, 0, 500_000, (AccountStatus)99, true, WithdrawalResult.RejectedInvalidAccount)
            .WithTestDisplayName("CFT-ATM-P2: undefined status byte 99 -> RejectedInvalidAccount")
            .WithTrait("Path", "P2"),

        // P3a: R2 first disjunct: amount < MinAmount (50,000).
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                25_000_000, 0, 25_000, Active, true, WithdrawalResult.RejectedInvalidAmount)
            .WithTestDisplayName("CFT-ATM-P3a: amount 25,000 < MinAmount -> RejectedInvalidAmount")
            .WithTrait("Path", "P3a"),
        // P3b: R2 second disjunct: amount > MaxAmount (20,000,000).
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                25_000_000, 0, 30_000_000, Active, true, WithdrawalResult.RejectedInvalidAmount)
            .WithTestDisplayName("CFT-ATM-P3b: amount 30,000,000 > MaxAmount -> RejectedInvalidAmount")
            .WithTrait("Path", "P3b"),
        // P3c: R2 third disjunct: amount in range but amount % AmountStep != 0.
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                25_000_000, 0, 512_345, Active, true, WithdrawalResult.RejectedInvalidAmount)
            .WithTestDisplayName("CFT-ATM-P3c: amount 512,345 off-lattice -> RejectedInvalidAmount")
            .WithTrait("Path", "P3c"),

        // P4: R2 passes, R3 fails: dailyUsed > DailyLimit - amount (19,750,000 > 19,500,000).
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                25_000_000, 19_750_000, 500_000, Active, true, WithdrawalResult.RejectedDailyLimit)
            .WithTestDisplayName("CFT-ATM-P4: daily sum 20,250,000 > DailyLimit -> RejectedDailyLimit")
            .WithTrait("Path", "P4"),

        // P5: internal (fee = 0), balance < amount.
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                100_000, 0, 500_000, Active, true, WithdrawalResult.RejectedInsufficientBalance)
            .WithTestDisplayName("CFT-ATM-P5: internal, balance 100,000 < amount 500,000 -> RejectedInsufficientBalance")
            .WithTrait("Path", "P5"),
        // P6: internal (fee = 0), balance >= amount.
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                25_000_000, 0, 500_000, Active, true, WithdrawalResult.Approved)
            .WithTestDisplayName("CFT-ATM-P6: internal, balance 25,000,000 >= amount -> Approved")
            .WithTrait("Path", "P6"),
        // P7: external (fee = 50,000), balance < amount + fee.
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                100_000, 0, 500_000, Active, false, WithdrawalResult.RejectedInsufficientBalance)
            .WithTestDisplayName("CFT-ATM-P7: external, balance 100,000 < amount + fee 550,000 -> RejectedInsufficientBalance")
            .WithTrait("Path", "P7"),
        // P8: external (fee = 50,000), balance >= amount + fee.
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                25_000_000, 0, 500_000, Active, false, WithdrawalResult.Approved)
            .WithTestDisplayName("CFT-ATM-P8: external, balance 25,000,000 >= amount + fee -> Approved")
            .WithTrait("Path", "P8")
    ];

    [Theory]
    [MemberData(nameof(DailyUsedNegativeThrows))]
    public void P1_NegativeDailyUsed_ThrowsArgumentOutOfRange(long balance, long dailyUsed, long amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>("dailyUsed",
            () => AtmService.Withdraw(balance, dailyUsed, amount, AccountStatus.Active, true));
    }

    [Theory]
    [MemberData(nameof(PathCases))]
    public void P2_P8_PathCase_ReturnsExpectedResult(long balance, long dailyUsed, long amount,
        AccountStatus accountStatus, bool isInternal, WithdrawalResult expected)
    {
        var actual = AtmService.Withdraw(balance, dailyUsed, amount, accountStatus, isInternal);
        Assert.Equal(expected, actual);
    }
}

/// <summary>
///     Boundary-value companions to the control-flow paths of <see cref="AtmControlFlowPathTests"/>: the equality
///     edges of every decision predicate (amount Min/Max, amount-step lattice, daily-sum limit, balance versus
///     amount + fee, dailyUsed guard). These are boundary tests, not additional path tests.
/// </summary>
public class AtmControlFlowBoundaryTests
{
    public static IEnumerable<TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>> BoundaryCases =>
    [
        // B1-B5: amount condition boundaries (R2).
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                25_000_000, 0, 50_000, AccountStatus.Active, true, WithdrawalResult.Approved)
            .WithTestDisplayName("CFT-ATM-B1: amount = MinAmount 50,000 is valid -> Approved")
            .WithTrait("Boundary", "amount Min"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                25_000_000, 0, 49_999, AccountStatus.Active, true, WithdrawalResult.RejectedInvalidAmount)
            .WithTestDisplayName("CFT-ATM-B2: amount = MinAmount - 1 -> RejectedInvalidAmount")
            .WithTrait("Boundary", "amount Min-"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                25_000_000, 0, 50_001, AccountStatus.Active, true, WithdrawalResult.RejectedInvalidAmount)
            .WithTestDisplayName("CFT-ATM-B3: amount = MinAmount + 1 (off-lattice) -> RejectedInvalidAmount")
            .WithTrait("Boundary", "amount Min+"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                25_000_000, 0, 20_000_000, AccountStatus.Active, true, WithdrawalResult.Approved)
            .WithTestDisplayName("CFT-ATM-B4: amount = MaxAmount = DailyLimit, daily sum = limit -> Approved")
            .WithTrait("Boundary", "amount Max"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                25_000_000, 0, 20_000_001, AccountStatus.Active, true, WithdrawalResult.RejectedInvalidAmount)
            .WithTestDisplayName("CFT-ATM-B5: amount = MaxAmount + 1 -> RejectedInvalidAmount")
            .WithTrait("Boundary", "amount Max+"),

        // B6-B7: daily-sum boundary (R3: dailyUsed > DailyLimit - amount).
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                25_000_000, 19_500_000, 500_000, AccountStatus.Active, true, WithdrawalResult.Approved)
            .WithTestDisplayName("CFT-ATM-B6: daily sum exactly DailyLimit -> Approved")
            .WithTrait("Boundary", "daily sum T"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                25_000_000, 19_500_001, 500_000, AccountStatus.Active, true, WithdrawalResult.RejectedDailyLimit)
            .WithTestDisplayName("CFT-ATM-B7: daily sum DailyLimit + 1 -> RejectedDailyLimit")
            .WithTrait("Boundary", "daily sum T+1"),

        // B8-B9: internal balance boundary (R5: balance < amount + 0).
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                500_000, 0, 500_000, AccountStatus.Active, true, WithdrawalResult.Approved)
            .WithTestDisplayName("CFT-ATM-B8: internal, balance exactly amount -> Approved")
            .WithTrait("Boundary", "internal balance T"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                499_999, 0, 500_000, AccountStatus.Active, true, WithdrawalResult.RejectedInsufficientBalance)
            .WithTestDisplayName("CFT-ATM-B9: internal, balance amount - 1 -> RejectedInsufficientBalance")
            .WithTrait("Boundary", "internal balance T-1"),

        // B10-B11: external balance boundary (R5: balance < amount + ExternalFee).
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                550_000, 0, 500_000, AccountStatus.Active, false, WithdrawalResult.Approved)
            .WithTestDisplayName("CFT-ATM-B10: external, balance exactly amount + fee -> Approved")
            .WithTrait("Boundary", "external balance T"),
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                549_999, 0, 500_000, AccountStatus.Active, false, WithdrawalResult.RejectedInsufficientBalance)
            .WithTestDisplayName("CFT-ATM-B11: external, balance amount + fee - 1 -> RejectedInsufficientBalance")
            .WithTrait("Boundary", "external balance T-1"),

        // B12: dailyUsed guard boundary (dailyUsed < 0).
        new TheoryDataRow<long, long, long, AccountStatus, bool, WithdrawalResult>(
                1_000_000, 0, 500_000, AccountStatus.Active, true, WithdrawalResult.Approved)
            .WithTestDisplayName("CFT-ATM-B12: dailyUsed = 0 (guard False edge) -> Approved")
            .WithTrait("Boundary", "dailyUsed 0")
    ];

    [Theory]
    [MemberData(nameof(BoundaryCases))]
    public void BoundaryCase_ReturnsExpectedResult(long balance, long dailyUsed, long amount,
        AccountStatus accountStatus, bool isInternal, WithdrawalResult expected)
    {
        var actual = AtmService.Withdraw(balance, dailyUsed, amount, accountStatus, isInternal);
        Assert.Equal(expected, actual);
    }
}
