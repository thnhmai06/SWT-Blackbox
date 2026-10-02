namespace BVA_Boundry.Atm;

/// <summary>Administrative state of the account (input of rule R1).</summary>
public enum AccountStatus { Active, Frozen }

/// <summary>Output domain D_out of the ATM validator (report.tex §4.2).</summary>
public enum WithdrawalResult
{
    Approved,
    RejectedInvalidAccount,
    RejectedInvalidAmount,
    RejectedDailyLimit,
    RejectedInsufficientBalance
}

/// <summary>ATM withdrawal validation (report.tex §4.2).</summary>
public static class AtmWithdrawal
{
    public const long MinAmount = 50_000;
    public const long MaxAmount = 20_000_000;
    public const long AmountStep = 50_000;
    public const long DailyLimit = 20_000_000;
    public const long ExternalFee = 50_000;

    /// <summary>
    /// Validates a single withdrawal. Rules are evaluated in the fixed precedence order
    /// R1 → R2 → R3 → R5 and the first failing rule determines the rejection reason.
    /// </summary>
    /// <param name="balance">Current account balance; may be negative (overdrawn account).</param>
    /// <param name="dailyUsed">Amount already withdrawn today; must be non-negative.</param>
    /// <param name="amount">Requested withdrawal amount.</param>
    /// <param name="accountStatus">R1: only <see cref="AccountStatus.Active"/> passes.</param>
    /// <param name="isInternal">R4: internal network charges no fee, external charges <see cref="ExternalFee"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dailyUsed"/> is negative (checked before R1).</exception>
    public static WithdrawalResult Withdraw(long balance, long dailyUsed, long amount, AccountStatus accountStatus, bool isInternal)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dailyUsed);

        // R1: status
        if (accountStatus != AccountStatus.Active)
            return WithdrawalResult.RejectedInvalidAccount;

        // R2: range and granularity
        if (amount < MinAmount || amount > MaxAmount || amount % AmountStep != 0)
            return WithdrawalResult.RejectedInvalidAmount;

        // R3: dailyUsed + amount <= DailyLimit, rearranged so it cannot overflow
        // (R2 bounds amount and the guard bounds dailyUsed below).
        if (dailyUsed > DailyLimit - amount)
            return WithdrawalResult.RejectedDailyLimit;

        // R4 + R5: fee-adjusted balance
        long fee = isInternal ? 0 : ExternalFee;
        if (balance < amount + fee)
            return WithdrawalResult.RejectedInsufficientBalance;

        return WithdrawalResult.Approved;
    }
}
