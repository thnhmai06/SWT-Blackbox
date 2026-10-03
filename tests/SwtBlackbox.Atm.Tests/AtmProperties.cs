using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace SwtBlackbox.Atm.Tests;

/// <summary>
///     Property-based tests for <see cref="AtmService" /> (report.tex §4.2, rules R1–R5).
/// </summary>
public class AtmProperties
{
    // Amounts biased to the interesting region: valid multiples of 50_000, or any
    // int in [-100_000, 20_100_000] to hit R2 boundaries and invalid values.
    private static Gen<long> AmountGen()
    {
        return Gen.OneOf(
            Gen.Choose(1, 400).Select(k => k * 50_000L),
            Gen.Choose(-100_000, 20_100_000).Select(i => (long)i));
    }

    private static Arbitrary<AtmInput> InputArb()
    {
        return Arb.From(
            from balance in Gen.Choose(-1_000_000, 25_000_000).Select(i => (long)i)
            from dailyUsed in Gen.Choose(0, 20_100_000).Select(i => (long)i)
            from amount in AmountGen()
            from status in Gen.Elements(AccountStatus.Active, AccountStatus.Frozen)
            from isInternal in Gen.Elements(true, false)
            select new AtmInput(balance, dailyUsed, amount, status, isInternal));
    }

    // R1 has top precedence: a frozen account is rejected before any other rule runs.
    [Property(MaxTest = 1000)]
    public Property FrozenAccount_IsAlwaysRejectedInvalidAccount()
    {
        return Prop.ForAll(InputArb(), input =>
            AtmService.Withdraw(input.Balance, input.DailyUsed, input.Amount, AccountStatus.Frozen, input.IsInternal)
            == WithdrawalResult.RejectedInvalidAccount);
    }

    // R2 is checked before R3/R5: an Active account with a bad amount is RejectedInvalidAmount.
    [Property(MaxTest = 1000)]
    public Property Active_InvalidAmount_IsRejectedInvalidAmount()
    {
        return Prop.ForAll(InputArb(), input =>
        {
            var invalid = input.Amount < AtmService.MinAmount
                          || input.Amount > AtmService.MaxAmount
                          || input.Amount % AtmService.AmountStep != 0;
            return !invalid
                   || AtmService.Withdraw(input.Balance, input.DailyUsed, input.Amount, AccountStatus.Active,
                       input.IsInternal)
                   == WithdrawalResult.RejectedInvalidAmount;
        });
    }

    // Inputs that satisfy R1–R5 by construction (random inputs are approved only about 9% of the time,
    // which would make approval-conditioned properties pass vacuously). Slack is how far the input
    // sits inside the valid region, so boundary-tight cases (slack 0) are generated too.
    private static Arbitrary<AtmInput> ApprovableInputArb()
    {
        return Arb.From(
            from k in Gen.Choose(1, 400)
            let amount = k * AtmService.AmountStep
            from isInternal in Gen.Elements(true, false)
            from dailyUsed in Gen.Choose(0, (int)(AtmService.DailyLimit - amount)).Select(i => (long)i)
            from balanceSlack in Gen.OneOf(Gen.Constant(0), Gen.Choose(0, 5_000_000))
            let fee = isInternal ? 0 : AtmService.ExternalFee
            select new AtmInput(amount + fee + balanceSlack, dailyUsed, amount, AccountStatus.Active, isInternal));
    }

    private static Arbitrary<long> DeltaArb()
    {
        return Arb.From(Gen.Choose(0, 1_000_000).Select(i => (long)i));
    }

    private static WithdrawalResult Withdraw(AtmInput i)
    {
        return AtmService.Withdraw(i.Balance, i.DailyUsed, i.Amount, i.Status, i.IsInternal);
    }

    // Converse of Approved_ImpliesAllRulesHold: when every rule R1–R5 holds the result is Approved.
    [Property(MaxTest = 1000)]
    public Property AllRulesHold_ImpliesApproved()
    {
        return Prop.ForAll(ApprovableInputArb(), input => Withdraw(input) == WithdrawalResult.Approved);
    }

    // R5 is monotone in balance: more balance cannot turn an approval into a rejection.
    [Property(MaxTest = 1000)]
    public Property BalanceMonotonicity_MoreBalanceKeepsApproval()
    {
        return Prop.ForAll(ApprovableInputArb(), DeltaArb(), (input, delta) =>
            Withdraw(input with { Balance = input.Balance + delta }) == WithdrawalResult.Approved);
    }

    // R4 fee: internal withdrawals pay no fee, so they are approved whenever the external variant is.
    [Property(MaxTest = 1000)]
    public Property FeeMonotonicity_ExternalApprovalImpliesInternalApproval()
    {
        return Prop.ForAll(ApprovableInputArb(), input =>
            Withdraw(input with { IsInternal = true }) == WithdrawalResult.Approved);
    }

    // R3 is monotone in dailyUsed: less used today cannot turn an approval into a rejection.
    [Property(MaxTest = 1000)]
    public Property DailyUsedMonotonicity_LessUsedKeepsApproval()
    {
        return Prop.ForAll(ApprovableInputArb(), DeltaArb(), (input, delta) =>
            Withdraw(input with { DailyUsed = Math.Max(0, input.DailyUsed - delta) }) == WithdrawalResult.Approved);
    }

    // Total function: Withdraw never throws for DailyUsed >= 0 and always returns a defined enum value.
    [Property(MaxTest = 1000)]
    public Property ResultIsAlwaysDefinedAndNeverThrows()
    {
        return Prop.ForAll(InputArb(), input =>
        {
            var result = AtmService.Withdraw(input.Balance, input.DailyUsed, input.Amount, input.Status,
                input.IsInternal);
            return Enum.IsDefined(result);
        });
    }

    // Oracle for Approved: all of R1–R5 must hold (written directly from the spec).
    [Property(MaxTest = 1000)]
    public Property Approved_ImpliesAllRulesHold()
    {
        return Prop.ForAll(InputArb(), input =>
        {
            var result = AtmService.Withdraw(input.Balance, input.DailyUsed, input.Amount, input.Status,
                input.IsInternal);
            if (result != WithdrawalResult.Approved)
                return true;
            var fee = input.IsInternal ? 0 : AtmService.ExternalFee;
            return input is
                   {
                       Status: AccountStatus.Active, Amount: >= AtmService.MinAmount and <= AtmService.MaxAmount
                   }
                   && input.Amount % AtmService.AmountStep == 0
                   && input.DailyUsed + input.Amount <= AtmService.DailyLimit
                   && input.Balance >= input.Amount + fee;
        });
    }

    private sealed record AtmInput(long Balance, long DailyUsed, long Amount, AccountStatus Status, bool IsInternal);
}