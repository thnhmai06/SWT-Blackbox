using BVA_Boundry.Atm;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace BVA_Boundry.Tests.Properties;

/// <summary>
/// Property-based tests for <see cref="AtmWithdrawal"/> (report.tex §4.2, rules R1–R5).
/// </summary>
public class AtmProperties
{
    private sealed record AtmInput(long Balance, long DailyUsed, long Amount, AccountStatus Status, bool IsInternal);

    // Amounts biased to the interesting region: valid multiples of 50_000, or any
    // int in [-100_000, 20_100_000] to hit R2 boundaries and invalid values.
    private static Gen<long> AmountGen() =>
        Gen.OneOf(
            Gen.Choose(1, 400).Select(k => k * 50_000L),
            Gen.Choose(-100_000, 20_100_000).Select(i => (long)i));

    private static Arbitrary<AtmInput> InputArb() =>
        Arb.From(
            from balance in Gen.Choose(-1_000_000, 25_000_000).Select(i => (long)i)
            from dailyUsed in Gen.Choose(0, 20_100_000).Select(i => (long)i)
            from amount in AmountGen()
            from status in Gen.Elements(AccountStatus.Active, AccountStatus.Frozen)
            from isInternal in Gen.Elements(true, false)
            select new AtmInput(balance, dailyUsed, amount, status, isInternal));

    // R1 has top precedence: a frozen account is rejected before any other rule runs.
    [Property(MaxTest = 1000)]
    public Property FrozenAccount_IsAlwaysRejectedInvalidAccount() =>
        Prop.ForAll(InputArb(), input =>
            AtmWithdrawal.Withdraw(input.Balance, input.DailyUsed, input.Amount, AccountStatus.Frozen, input.IsInternal)
            == WithdrawalResult.RejectedInvalidAccount);

    // R2 is checked before R3/R5: an Active account with a bad amount is RejectedInvalidAmount.
    [Property(MaxTest = 1000)]
    public Property Active_InvalidAmount_IsRejectedInvalidAmount() =>
        Prop.ForAll(InputArb(), input =>
        {
            var invalid = input.Amount < AtmWithdrawal.MinAmount
                || input.Amount > AtmWithdrawal.MaxAmount
                || input.Amount % AtmWithdrawal.AmountStep != 0;
            return !invalid
                || AtmWithdrawal.Withdraw(input.Balance, input.DailyUsed, input.Amount, AccountStatus.Active, input.IsInternal)
                    == WithdrawalResult.RejectedInvalidAmount;
        });

    // Inputs that satisfy R1–R5 by construction (random inputs are approved only ~7% of the time,
    // which would make approval-conditioned properties pass vacuously). Slack is how far the input
    // sits inside the valid region, so boundary-tight cases (slack 0) are generated too.
    private static Arbitrary<AtmInput> ApprovableInputArb() =>
        Arb.From(
            from k in Gen.Choose(1, 400)
            let amount = k * AtmWithdrawal.AmountStep
            from isInternal in Gen.Elements(true, false)
            from dailyUsed in Gen.Choose(0, (int)(AtmWithdrawal.DailyLimit - amount)).Select(i => (long)i)
            from balanceSlack in Gen.OneOf(Gen.Constant(0), Gen.Choose(0, 5_000_000))
            let fee = isInternal ? 0 : AtmWithdrawal.ExternalFee
            select new AtmInput(amount + fee + balanceSlack, dailyUsed, amount, AccountStatus.Active, isInternal));

    private static Arbitrary<long> DeltaArb() => Arb.From(Gen.Choose(0, 1_000_000).Select(i => (long)i));

    private static WithdrawalResult Withdraw(AtmInput i) =>
        AtmWithdrawal.Withdraw(i.Balance, i.DailyUsed, i.Amount, i.Status, i.IsInternal);

    // Converse of Approved_ImpliesAllRulesHold: when every rule R1–R5 holds the result is Approved.
    [Property(MaxTest = 1000)]
    public Property AllRulesHold_ImpliesApproved() =>
        Prop.ForAll(ApprovableInputArb(), input => Withdraw(input) == WithdrawalResult.Approved);

    // R5 is monotone in balance: more balance cannot turn an approval into a rejection.
    [Property(MaxTest = 1000)]
    public Property BalanceMonotonicity_MoreBalanceKeepsApproval() =>
        Prop.ForAll(ApprovableInputArb(), DeltaArb(), (input, delta) =>
            Withdraw(input with { Balance = input.Balance + delta }) == WithdrawalResult.Approved);

    // R4 fee: internal withdrawals pay no fee, so they are approved whenever the external variant is.
    [Property(MaxTest = 1000)]
    public Property FeeMonotonicity_ExternalApprovalImpliesInternalApproval() =>
        Prop.ForAll(ApprovableInputArb(), input =>
            Withdraw(input with { IsInternal = true }) == WithdrawalResult.Approved);

    // R3 is monotone in dailyUsed: less used today cannot turn an approval into a rejection.
    [Property(MaxTest = 1000)]
    public Property DailyUsedMonotonicity_LessUsedKeepsApproval() =>
        Prop.ForAll(ApprovableInputArb(), DeltaArb(), (input, delta) =>
            Withdraw(input with { DailyUsed = Math.Max(0, input.DailyUsed - delta) }) == WithdrawalResult.Approved);

    // Total function: Withdraw never throws for DailyUsed >= 0 and always returns a defined enum value.
    [Property(MaxTest = 1000)]
    public Property ResultIsAlwaysDefinedAndNeverThrows() =>
        Prop.ForAll(InputArb(), input =>
        {
            var result = AtmWithdrawal.Withdraw(input.Balance, input.DailyUsed, input.Amount, input.Status, input.IsInternal);
            return Enum.IsDefined(result);
        });

    // Oracle for Approved: all of R1–R5 must hold (written directly from the spec).
    [Property(MaxTest = 1000)]
    public Property Approved_ImpliesAllRulesHold() =>
        Prop.ForAll(InputArb(), input =>
        {
            var result = AtmWithdrawal.Withdraw(input.Balance, input.DailyUsed, input.Amount, input.Status, input.IsInternal);
            if (result != WithdrawalResult.Approved)
                return true;
            var fee = input.IsInternal ? 0 : AtmWithdrawal.ExternalFee;
            return input.Status == AccountStatus.Active
                && input.Amount >= AtmWithdrawal.MinAmount
                && input.Amount <= AtmWithdrawal.MaxAmount
                && input.Amount % AtmWithdrawal.AmountStep == 0
                && input.DailyUsed + input.Amount <= AtmWithdrawal.DailyLimit
                && input.Balance >= input.Amount + fee;
        });
}
