namespace SoftwareTesting.Atm;

/// <summary>Output domain D_out of the ATM validator (report.tex §4.2).</summary>
public enum WithdrawalResult : byte
{
    Approved,
    RejectedInvalidAccount,
    RejectedInvalidAmount,
    RejectedDailyLimit,
    RejectedInsufficientBalance
}