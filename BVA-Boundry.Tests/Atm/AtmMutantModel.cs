using BVA_Boundry.Atm;

namespace BVA_Boundry.Tests.Atm;

/// <summary>The fourteen ATM mutants of report.tex §4.11 (Table "ATM mutants").</summary>
public enum AtmMutant { None, M1, M2, M3, M4, M5, M6, M7, M8, M9, M10, M11, M12, M13, M14 }

/// <summary>
/// Independent re-implementation of the ATM rules (report.tex §4.2) that can inject exactly one mutant.
/// It deliberately does not call production code, so it can act as a second oracle.
/// Only valid inputs (dailyUsed ≥ 0) are fed to it.
/// </summary>
internal static class AtmMutantModel
{
    public static WithdrawalResult Withdraw(AtmMutant m, long balance, long dailyUsed, long amount, AccountStatus status, bool isInternal)
    {
        bool r1 = m == AtmMutant.M9 ? status == AccountStatus.Frozen : status == AccountStatus.Active;

        long lower = m == AtmMutant.M10 ? 0 : 50_000;
        long upper = m switch { AtmMutant.M3 => 19_999_999, AtmMutant.M4 => 20_000_001, _ => 20_000_000 };
        long step = m == AtmMutant.M8 ? 10_000 : 50_000;
        bool r2 = amount >= lower && amount <= upper && amount % step == 0;

        bool r3 = m switch
        {
            AtmMutant.M2 => dailyUsed + amount < 20_000_000,
            AtmMutant.M12 => dailyUsed <= 20_000_000,
            _ => dailyUsed + amount <= 20_000_000,
        };

        long externalFee = m switch { AtmMutant.M6 => 0, AtmMutant.M7 => 5_000, AtmMutant.M13 => 100_000, _ => 50_000 };
        long fee = isInternal ? 0 : externalFee;
        bool r5 = m == AtmMutant.M1 ? balance > amount + fee : balance >= amount + fee;

        if (m == AtmMutant.M5)
        {
            // R1 ∧ R2 weakened to R1 ∨ R2: only a double failure stops here, reported as R1.
            if (!r1 && !r2)
                return WithdrawalResult.RejectedInvalidAccount;
        }
        else if (m == AtmMutant.M14)
        {
            if (!r2) return WithdrawalResult.RejectedInvalidAmount;
            if (!r1) return WithdrawalResult.RejectedInvalidAccount;
        }
        else
        {
            if (!r1) return WithdrawalResult.RejectedInvalidAccount;
            if (!r2) return WithdrawalResult.RejectedInvalidAmount;
        }

        if (m == AtmMutant.M11)
        {
            if (!r5) return WithdrawalResult.RejectedInsufficientBalance;
            if (!r3) return WithdrawalResult.RejectedDailyLimit;
        }
        else
        {
            if (!r3) return WithdrawalResult.RejectedDailyLimit;
            if (!r5) return WithdrawalResult.RejectedInsufficientBalance;
        }

        return WithdrawalResult.Approved;
    }
}
