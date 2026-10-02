namespace SwtBlackbox.Atm;

/// <summary>Administrative state of the account (input of rule R1).</summary>
public enum AccountStatus : byte
{
    Active,
    Frozen
}